using System.Linq;
using UnityEngine;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.UI;

namespace HorseBetting.Core
{
    /// <summary>
    /// Single bootstrap that handles the entire game flow:
    /// - Runs the state machine auto-steps
    /// - Pushes data to UI views on step completion
    /// - Handles race simulation and settlement
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameEngine _gameEngine;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private RaceView _raceView;

        // View controllers (from UIManager)
        private MainView _mainView;
        private BettingView _bettingView;
        private SettlementView _settlementView;
        private ShopView _shopView;
        private AnalystView _analystView;

        // Cached data
        private HorseData[] _currentHorses;
        private float[] _currentOdds;
        private RaceResult _lastRaceResult;
        private bool _isRunningAutoSteps;

        private void Start()
        {
            if (_gameEngine == null)
            {
                Debug.LogError("[GameBootstrap] GameEngine not assigned!");
                return;
            }

            // Get views from UIManager
            if (_uiManager != null)
            {
                _mainView = _uiManager.MainViewCtrl;
                _bettingView = _uiManager.BettingViewCtrl;
                _settlementView = _uiManager.SettlementViewCtrl;
                _shopView = _uiManager.ShopViewCtrl;
                _analystView = _uiManager.AnalystViewCtrl;
            }

            // Subscribe to events
            _gameEngine.RoundStateMachine.OnStepCompleted += HandleStepCompleted;
            _gameEngine.RoundStateMachine.OnRoundStarted += HandleRoundStarted;

            // Subscribe to UI events
            if (_bettingView != null) _bettingView.OnBetPlaced += HandleBetPlaced;
            if (_shopView != null) _shopView.OnBuyClicked += HandleShopBuy;
            if (_analystView != null) _analystView.OnBuyIntelClicked += HandleBuyIntel;

            Debug.Log("[GameBootstrap] Initialized and starting game loop.");
            RunAutoSteps();
        }

        private void OnDestroy()
        {
            if (_gameEngine != null && _gameEngine.RoundStateMachine != null)
            {
                _gameEngine.RoundStateMachine.OnStepCompleted -= HandleStepCompleted;
                _gameEngine.RoundStateMachine.OnRoundStarted -= HandleRoundStarted;
            }
            if (_bettingView != null) _bettingView.OnBetPlaced -= HandleBetPlaced;
            if (_shopView != null) _shopView.OnBuyClicked -= HandleShopBuy;
            if (_analystView != null) _analystView.OnBuyIntelClicked -= HandleBuyIntel;
        }

        // ─── State Machine Control ──────────────────────────────────────────────

        private void HandleStepCompleted(RoundStep step)
        {
            // Push data to UI based on which step just completed
            ProcessStepCompletion(step);

            // Only resume after WAITING steps complete (player input steps)
            // Auto-steps are already handled by RunAutoSteps loop
            if (!_isRunningAutoSteps && IsWaitingStep(step))
            {
                Invoke(nameof(ResumeAfterInput), 0.05f);
            }
        }

        private bool IsWaitingStep(RoundStep step)
        {
            return step == RoundStep.BettingRound1
                || step == RoundStep.BettingRound2
                || step == RoundStep.BettingRound3
                || step == RoundStep.BuyAnalyst
                || step == RoundStep.Shop;
        }

        private void HandleRoundStarted(int roundNumber)
        {
            _currentHorses = null;
            _currentOdds = null;
            _lastRaceResult = default;
            _gameEngine.BettingSystem.ClearBets();
            _gameEngine.PlayerState.ClearBets();
            _mainView?.UpdateRound(roundNumber);
            _mainView?.UpdateBalance(_gameEngine.PlayerState.Balance);
            Debug.Log($"[GameBootstrap] Round {roundNumber} started.");
        }

        private void ResumeAfterInput()
        {
            RunAutoSteps();
        }

        private void RunAutoSteps()
        {
            _isRunningAutoSteps = true;
            var sm = _gameEngine.RoundStateMachine;
            int safetyCounter = 0;

            while (safetyCounter < 30)
            {
                safetyCounter++;
                sm.ExecuteCurrentStep(_gameEngine);

                if (sm.IsWaitingForInput)
                {
                    Debug.Log($"[GameBootstrap] Waiting at: {sm.CurrentStep}");
                    ForceShowView(sm.CurrentStep);
                    break;
                }

                if (!sm.IsStepInProgress)
                {
                    sm.AdvanceStep();
                }
                else
                {
                    break;
                }
            }
            _isRunningAutoSteps = false;
        }

        // ─── Data Push on Step Completion ───────────────────────────────────────

        private void ProcessStepCompletion(RoundStep step)
        {
            switch (step)
            {
                case RoundStep.GenerateHorses:
                    _currentHorses = _gameEngine.HorseSystem.GetHorses();
                    _mainView?.UpdateHorseList(_currentHorses, new float[8]);
                    break;

                case RoundStep.CalculateInitialOdds:
                    if (_currentHorses == null) _currentHorses = _gameEngine.HorseSystem.GetHorses();
                    _currentOdds = _gameEngine.OddsSystem.CalculateOdds(_currentHorses, 1);
                    _mainView?.UpdateHorseList(_currentHorses, _currentOdds);
                    break;

                case RoundStep.RevealCard1:
                case RoundStep.RevealCard2:
                case RoundStep.RevealCard3:
                    _bettingView?.SetRevealedCards(_gameEngine.MessageCardSystem.GetRevealedCards());
                    break;

                case RoundStep.UpdateOdds1:
                    if (_currentHorses != null)
                    {
                        _currentOdds = _gameEngine.OddsSystem.CalculateOdds(_currentHorses, 2);
                        _mainView?.UpdateHorseList(_currentHorses, _currentOdds);
                    }
                    break;

                case RoundStep.DetermineTrack:
                    if (_raceView != null)
                        _raceView.SetTrackType(_gameEngine.TrackSystem.CurrentTrack);
                    break;

                case RoundStep.GenerateAnalystIntel:
                    if (_analystView != null)
                    {
                        _analystView.SetAnalystPrices(
                            _gameEngine.AnalystConfig.seniorPrice,
                            _gameEngine.AnalystConfig.juniorPrice);
                        _analystView.UpdateBalance(_gameEngine.PlayerState.Balance);
                    }
                    break;

                case RoundStep.StartRace:
                    SimulateRace();
                    break;

                case RoundStep.Settlement:
                    CalculateSettlement();
                    break;

                case RoundStep.BettingRound1:
                case RoundStep.BettingRound2:
                case RoundStep.BettingRound3:
                    _mainView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                    break;

                case RoundStep.Shop:
                    PushShopData();
                    _mainView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                    break;
            }
        }

        // ─── Race & Settlement ──────────────────────────────────────────────────

        private void SimulateRace()
        {
            if (_currentHorses == null)
                _currentHorses = _gameEngine.HorseSystem.GetHorses();

            TrackType track = _gameEngine.TrackSystem.CurrentTrack;
            ProtectionCard[] cards = _gameEngine.PlayerState.ProtectionCards.ToArray();

            _lastRaceResult = _gameEngine.RaceSimulationSystem.SimulateRace(
                _currentHorses, track, _gameEngine.EventConfig, cards);

            Debug.Log($"[GameBootstrap] Race done! Winner: Horse {_lastRaceResult.finalRanking[0] + 1}");

            if (_raceView != null)
                _raceView.StartRaceAnimation(_lastRaceResult);
        }

        private void CalculateSettlement()
        {
            if (_currentHorses == null)
                _currentHorses = _gameEngine.HorseSystem.GetHorses();

            if (_lastRaceResult.finalRanking == null)
            {
                Debug.LogWarning("[GameBootstrap] No race result for settlement!");
                return;
            }

            Bet[] bets = _gameEngine.BettingSystem.GetActiveBets();
            SettlementResult settlement = _gameEngine.SettlementSystem.CalculateSettlement(
                _lastRaceResult.finalRanking, bets, _gameEngine.BettingConfig);

            if (settlement.totalWinnings > 0)
                _gameEngine.PlayerState.AddBalance(settlement.totalWinnings);

            Debug.Log($"[GameBootstrap] Settlement: Won=${settlement.totalWinnings} Lost=${settlement.totalLoss} Net=${settlement.netProfit}");

            _settlementView?.ShowResults(_lastRaceResult, settlement, _currentHorses);
            _mainView?.UpdateBalance(_gameEngine.PlayerState.Balance);
        }

        // ─── UI Event Handlers ──────────────────────────────────────────────────

        private void HandleBetPlaced(Bet bet)
        {
            BetResult result = _gameEngine.BettingSystem.PlaceBet(bet, _gameEngine.PlayerState.Balance);
            if (result.success)
            {
                _gameEngine.PlayerState.DeductBalance(bet.amount);
                _gameEngine.PlayerState.AddBet(bet);
                _bettingView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                _bettingView?.HideError();
            }
            else
            {
                _bettingView?.ShowError(result.errorMessage);
            }
        }

        private void HandleShopBuy(int itemIndex)
        {
            int balance = _gameEngine.PlayerState.Balance;
            int cardCount = _gameEngine.PlayerState.CardCount;
            PurchaseResult result = _gameEngine.ShopSystem.BuyProtectionCard(itemIndex, balance, cardCount);

            if (result.success)
            {
                int price = balance - result.remainingBalance;
                _gameEngine.PlayerState.DeductBalance(price);

                ShopItem[] items = _gameEngine.ShopSystem.GetAvailableItems();
                if (itemIndex >= 0 && itemIndex < items.Length)
                {
                    var cardData = items[itemIndex].data;
                    _gameEngine.PlayerState.AddProtectionCard(new ProtectionCard
                    {
                        cardName = cardData.cardName,
                        protectsAgainst = cardData.protectsAgainst,
                        successRate = cardData.successRate
                    });
                }

                _shopView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                _shopView?.UpdateCardCount(_gameEngine.PlayerState.CardCount, _gameEngine.PlayerState.MaxProtectionCards);
            }
            else
            {
                _shopView?.ShowError(result.errorMessage);
            }
        }

        private void HandleBuyIntel(AnalystType type)
        {
            int balance = _gameEngine.PlayerState.Balance;
            PurchaseResult result = _gameEngine.AnalystSystem.BuyIntel(type, balance);

            if (result.success)
            {
                int price = balance - result.remainingBalance;
                _gameEngine.PlayerState.DeductBalance(price);
                var intel = _gameEngine.AnalystSystem.GetIntel(type);
                if (intel.HasValue)
                    _analystView?.ShowIntel(type, intel.Value.content);
                _analystView?.UpdateBalance(_gameEngine.PlayerState.Balance);
            }
            else
            {
                _analystView?.ShowError(result.errorMessage);
            }
        }

        private void PushShopData()
        {
            if (_shopView == null) return;
            _shopView.SetAvailableItems(_gameEngine.ShopSystem.GetAvailableItems());
            _shopView.UpdateBalance(_gameEngine.PlayerState.Balance);
            _shopView.UpdateCardCount(_gameEngine.PlayerState.CardCount, _gameEngine.PlayerState.MaxProtectionCards);
        }

        // ─── View Switching ─────────────────────────────────────────────────────

        private void ForceShowView(RoundStep step)
        {
            if (_uiManager == null) return;

            switch (step)
            {
                case RoundStep.BettingRound1:
                case RoundStep.BettingRound2:
                case RoundStep.BettingRound3:
                    _uiManager.ShowBettingView();
                    _bettingView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                    _bettingView?.SetHorseData(_currentHorses);
                    _bettingView?.SetRevealedCards(_gameEngine.MessageCardSystem.GetRevealedCards());
                    break;
                case RoundStep.BuyAnalyst:
                    _uiManager.ShowAnalystView();
                    break;
                case RoundStep.Shop:
                    _uiManager.ShowShopView();
                    PushShopData();
                    break;
            }
        }
    }
}
