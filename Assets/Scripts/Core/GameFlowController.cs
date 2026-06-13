using System.Linq;
using UnityEngine;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.UI;

namespace HorseBetting.Core
{
    /// <summary>
    /// Wires all system outputs to UI data binding.
    /// Subscribes to RoundStateMachine events and UI view events,
    /// fetching system data on each step and pushing it to the appropriate view.
    /// Validates: Requirements 13.1, 13.2, 13.3, 13.4
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class GameFlowController : MonoBehaviour
    {
        // ─── References (Inspector-injected) ────────────────────────────────────

        [Header("Core")]
        [SerializeField] private GameEngine _gameEngine;

        [Header("UI Views")]
        [SerializeField] private RaceView _raceView;

        // ─── UI Toolkit Views (initialized via code) ────────────────────────────

        private MainView _mainView;
        private BettingView _bettingView;
        private SettlementView _settlementView;
        private ShopView _shopView;
        private AnalystView _analystView;

        // ─── Cached Data ────────────────────────────────────────────────────────

        private HorseData[] _currentHorses;
        private float[] _currentOdds;
        private RaceResult _lastRaceResult;

        // ─── Public Setters for UI Toolkit Views ────────────────────────────────

        /// <summary>
        /// Sets the MainView instance. Call after UI Toolkit document is loaded.
        /// </summary>
        public void SetMainView(MainView view) => _mainView = view;

        /// <summary>
        /// Sets the BettingView instance. Call after UI Toolkit document is loaded.
        /// </summary>
        public void SetBettingView(BettingView view)
        {
            // Unsubscribe from previous view if any
            if (_bettingView != null)
            {
                _bettingView.OnBetPlaced -= HandleBetPlaced;
                _bettingView.OnDoneBetting -= HandleDoneBetting;
            }

            _bettingView = view;

            if (_bettingView != null)
            {
                _bettingView.OnBetPlaced += HandleBetPlaced;
                _bettingView.OnDoneBetting += HandleDoneBetting;
            }
        }

        /// <summary>
        /// Sets the SettlementView instance. Call after UI Toolkit document is loaded.
        /// </summary>
        public void SetSettlementView(SettlementView view)
        {
            if (_settlementView != null)
            {
                _settlementView.OnGoToShopClicked -= HandleGoToShop;
            }

            _settlementView = view;

            if (_settlementView != null)
            {
                _settlementView.OnGoToShopClicked += HandleGoToShop;
            }
        }

        /// <summary>
        /// Sets the ShopView instance. Call after UI Toolkit document is loaded.
        /// </summary>
        public void SetShopView(ShopView view)
        {
            if (_shopView != null)
            {
                _shopView.OnBuyClicked -= HandleShopBuy;
                _shopView.OnStartNextRound -= HandleStartNextRound;
            }

            _shopView = view;

            if (_shopView != null)
            {
                _shopView.OnBuyClicked += HandleShopBuy;
                _shopView.OnStartNextRound += HandleStartNextRound;
            }
        }

        /// <summary>
        /// Sets the AnalystView instance. Call after UI Toolkit document is loaded.
        /// </summary>
        public void SetAnalystView(AnalystView view)
        {
            if (_analystView != null)
            {
                _analystView.OnBuyIntelClicked -= HandleBuyIntel;
                _analystView.OnContinueClicked -= HandleAnalystContinue;
            }

            _analystView = view;

            if (_analystView != null)
            {
                _analystView.OnBuyIntelClicked += HandleBuyIntel;
                _analystView.OnContinueClicked += HandleAnalystContinue;
            }
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (_gameEngine == null)
            {
                Debug.LogError("[GameFlowController] GameEngine reference is not set.");
                return;
            }

            SubscribeToStateMachine();
            SubscribeToRaceView();
        }

        private void OnDisable()
        {
            UnsubscribeFromStateMachine();
            UnsubscribeFromRaceView();
            UnsubscribeFromUIViews();
        }

        // ─── State Machine Event Subscriptions ──────────────────────────────────

        private void SubscribeToStateMachine()
        {
            var sm = _gameEngine.RoundStateMachine;
            if (sm == null) return;

            sm.OnStepStarted += HandleStepStarted;
            sm.OnStepCompleted += HandleStepCompleted;
            sm.OnRoundStarted += HandleRoundStarted;
        }

        private void UnsubscribeFromStateMachine()
        {
            if (_gameEngine == null || _gameEngine.RoundStateMachine == null) return;

            var sm = _gameEngine.RoundStateMachine;
            sm.OnStepStarted -= HandleStepStarted;
            sm.OnStepCompleted -= HandleStepCompleted;
            sm.OnRoundStarted -= HandleRoundStarted;
        }

        private void SubscribeToRaceView()
        {
            if (_raceView != null)
            {
                _raceView.OnRaceComplete += HandleRaceAnimationComplete;
            }
        }

        private void UnsubscribeFromRaceView()
        {
            if (_raceView != null)
            {
                _raceView.OnRaceComplete -= HandleRaceAnimationComplete;
            }
        }

        private void UnsubscribeFromUIViews()
        {
            if (_bettingView != null)
            {
                _bettingView.OnBetPlaced -= HandleBetPlaced;
                _bettingView.OnDoneBetting -= HandleDoneBetting;
            }

            if (_settlementView != null)
            {
                _settlementView.OnGoToShopClicked -= HandleGoToShop;
            }

            if (_shopView != null)
            {
                _shopView.OnBuyClicked -= HandleShopBuy;
                _shopView.OnStartNextRound -= HandleStartNextRound;
            }

            if (_analystView != null)
            {
                _analystView.OnBuyIntelClicked -= HandleBuyIntel;
                _analystView.OnContinueClicked -= HandleAnalystContinue;
            }
        }

        // ─── State Machine Handlers ─────────────────────────────────────────────

        private void HandleRoundStarted(int roundNumber)
        {
            // Reset cached data for new round
            _currentHorses = null;
            _currentOdds = null;
            _lastRaceResult = default;

            // Clear bets from previous round
            _gameEngine.BettingSystem.ClearBets();
            _gameEngine.PlayerState.ClearBets();

            // Update round display
            _mainView?.UpdateRound(roundNumber);
            RefreshBalance();
        }

        private void HandleStepStarted(RoundStep step)
        {
            // Pre-step UI preparation can go here if needed
        }

        private void HandleStepCompleted(RoundStep step)
        {
            switch (step)
            {
                case RoundStep.GenerateHorses:
                    OnGenerateHorsesCompleted();
                    break;

                case RoundStep.CalculateInitialOdds:
                    OnCalculateInitialOddsCompleted();
                    break;

                case RoundStep.RevealCard1:
                case RoundStep.RevealCard2:
                case RoundStep.RevealCard3:
                    OnRevealCardCompleted();
                    break;

                case RoundStep.BettingRound1:
                    // Betting round ended, refresh balance
                    RefreshBalance();
                    break;

                case RoundStep.UpdateOdds1:
                    OnUpdateOddsCompleted();
                    break;

                case RoundStep.BettingRound2:
                    RefreshBalance();
                    break;

                case RoundStep.DetermineTrack:
                    OnDetermineTrackCompleted();
                    break;

                case RoundStep.GenerateAnalystIntel:
                    OnGenerateAnalystIntelCompleted();
                    break;

                case RoundStep.BuyAnalyst:
                    RefreshBalance();
                    break;

                case RoundStep.BettingRound3:
                    RefreshBalance();
                    break;

                case RoundStep.StartRace:
                    OnStartRaceCompleted();
                    break;

                case RoundStep.RaceAnimation:
                    // Animation handled by RaceView callback
                    break;

                case RoundStep.FinalRanking:
                    // Ranking is available from the race result
                    break;

                case RoundStep.Settlement:
                    OnSettlementCompleted();
                    break;

                case RoundStep.Shop:
                    // Shop step ended, refresh
                    RefreshBalance();
                    break;
            }
        }

        // ─── Step Completion Handlers ───────────────────────────────────────────

        private void OnGenerateHorsesCompleted()
        {
            _currentHorses = _gameEngine.HorseSystem.GetHorses();

            // Push horse data to MainView (without odds yet)
            if (_mainView != null && _currentHorses != null)
            {
                float[] placeholderOdds = new float[_currentHorses.Length];
                _mainView.UpdateHorseList(_currentHorses, placeholderOdds);
            }
        }

        private void OnCalculateInitialOddsCompleted()
        {
            if (_currentHorses == null) return;

            _currentOdds = _gameEngine.OddsSystem.CalculateOdds(_currentHorses, 0);

            // Push horse list with odds to MainView
            _mainView?.UpdateHorseList(_currentHorses, _currentOdds);
        }

        private void OnRevealCardCompleted()
        {
            MessageCard[] revealedCards = _gameEngine.MessageCardSystem.GetRevealedCards();

            // Update MainView with revealed cards (balance refresh)
            RefreshBalance();

            // Pre-populate BettingView with current revealed cards
            _bettingView?.SetRevealedCards(revealedCards);
        }

        private void OnUpdateOddsCompleted()
        {
            if (_currentHorses == null) return;

            // Recalculate odds (odds system internally updated)
            _currentOdds = _gameEngine.OddsSystem.CalculateOdds(_currentHorses, 1);

            // Refresh MainView odds display
            _mainView?.UpdateHorseList(_currentHorses, _currentOdds);
        }

        private void OnDetermineTrackCompleted()
        {
            TrackType track = _gameEngine.TrackSystem.CurrentTrack;

            // Set track type on race view for background color
            _raceView?.SetTrackType(track);
        }

        private void OnGenerateAnalystIntelCompleted()
        {
            // Push analyst prices and balance to AnalystView
            if (_analystView != null)
            {
                _analystView.SetAnalystPrices(
                    _gameEngine.AnalystConfig.seniorPrice,
                    _gameEngine.AnalystConfig.juniorPrice
                );
                _analystView.UpdateBalance(_gameEngine.PlayerState.Balance);
            }
        }

        private void OnStartRaceCompleted()
        {
            if (_currentHorses == null)
            {
                _currentHorses = _gameEngine.HorseSystem.GetHorses();
            }

            // Simulate the race
            TrackType track = _gameEngine.TrackSystem.CurrentTrack;
            ProtectionCard[] playerCards = _gameEngine.PlayerState.ProtectionCards.ToArray();

            _lastRaceResult = _gameEngine.RaceSimulationSystem.SimulateRace(
                _currentHorses,
                track,
                _gameEngine.EventConfig,
                playerCards
            );

            Debug.Log($"[GameFlowController] Race simulated. Winner: Horse {_lastRaceResult.finalRanking[0] + 1}");

            // Start race animation
            _raceView?.StartRaceAnimation(_lastRaceResult);
        }

        private void OnSettlementCompleted()
        {
            // Ensure we have horses cached
            if (_currentHorses == null)
            {
                _currentHorses = _gameEngine.HorseSystem.GetHorses();
            }

            if (_lastRaceResult.finalRanking == null)
            {
                Debug.LogWarning("[GameFlowController] Settlement called but no race result available.");
                return;
            }

            // Get active bets
            Bet[] activeBets = _gameEngine.BettingSystem.GetActiveBets();

            // Calculate settlement
            SettlementResult settlement = _gameEngine.SettlementSystem.CalculateSettlement(
                _lastRaceResult.finalRanking,
                activeBets,
                _gameEngine.BettingConfig
            );

            // Apply winnings to player balance
            if (settlement.totalWinnings > 0)
            {
                _gameEngine.PlayerState.AddBalance(settlement.totalWinnings);
            }

            Debug.Log($"[GameFlowController] Settlement: Winnings={settlement.totalWinnings}, Loss={settlement.totalLoss}, Net={settlement.netProfit}");

            // Show settlement results in SettlementView
            _settlementView?.ShowResults(_lastRaceResult, settlement, _currentHorses);

            // Refresh balance display
            RefreshBalance();
        }

        // ─── Betting Round Data Push ────────────────────────────────────────────

        /// <summary>
        /// Called externally (or by state machine) when a betting round step becomes active.
        /// Pushes horse data, revealed cards, balance, and current odds to BettingView.
        /// </summary>
        public void PushBettingRoundData(int bettingRound)
        {
            if (_bettingView == null) return;

            _bettingView.SetHorseData(_currentHorses);
            _bettingView.SetRevealedCards(_gameEngine.MessageCardSystem.GetRevealedCards());
            _bettingView.UpdateBalance(_gameEngine.PlayerState.Balance);
            _bettingView.SetBettingRound(bettingRound);

            // Set current average odds for reference
            if (_currentOdds != null && _currentOdds.Length > 0)
            {
                _bettingView.SetCurrentOdds(_currentOdds[0]);
            }
        }

        /// <summary>
        /// Called externally when the shop step becomes active.
        /// Pushes shop items and balance to ShopView.
        /// </summary>
        public void PushShopData()
        {
            if (_shopView == null) return;

            ShopItem[] items = _gameEngine.ShopSystem.GetAvailableItems();
            _shopView.SetAvailableItems(items);
            _shopView.UpdateBalance(_gameEngine.PlayerState.Balance);
            _shopView.UpdateCardCount(
                _gameEngine.PlayerState.CardCount,
                _gameEngine.PlayerState.MaxProtectionCards
            );
        }

        // ─── UI Event Handlers ──────────────────────────────────────────────────

        private void HandleBetPlaced(Bet bet)
        {
            // Validate and place bet through BettingSystem
            BetResult result = _gameEngine.BettingSystem.PlaceBet(bet, _gameEngine.PlayerState.Balance);

            if (result.success)
            {
                // Deduct balance
                _gameEngine.PlayerState.DeductBalance(bet.amount);

                // Record bet in PlayerState
                _gameEngine.PlayerState.AddBet(bet);

                // Refresh balance in UI
                _bettingView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                RefreshBalance();
            }
            else
            {
                // Show error in BettingView
                _bettingView?.ShowError(result.errorMessage);
            }
        }

        private void HandleDoneBetting()
        {
            // Advance the state machine past the betting round
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleGoToShop()
        {
            // Advance state machine to shop step (settlement -> shop)
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleShopBuy(int itemIndex)
        {
            int balance = _gameEngine.PlayerState.Balance;
            int cardCount = _gameEngine.PlayerState.CardCount;

            PurchaseResult result = _gameEngine.ShopSystem.BuyProtectionCard(itemIndex, balance, cardCount);

            if (result.success)
            {
                // Deduct balance
                int price = balance - result.remainingBalance;
                _gameEngine.PlayerState.DeductBalance(price);

                // Add protection card to player state
                ShopItem[] items = _gameEngine.ShopSystem.GetAvailableItems();
                if (itemIndex >= 0 && itemIndex < items.Length)
                {
                    var cardData = items[itemIndex].data;
                    var card = new ProtectionCard
                    {
                        cardName = cardData.cardName,
                        protectsAgainst = cardData.protectsAgainst,
                        successRate = cardData.successRate
                    };
                    _gameEngine.PlayerState.AddProtectionCard(card);
                }

                // Refresh shop display
                _shopView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                _shopView?.UpdateCardCount(
                    _gameEngine.PlayerState.CardCount,
                    _gameEngine.PlayerState.MaxProtectionCards
                );

                // Refresh MainView protection cards
                _mainView?.UpdateProtectionCards(_gameEngine.PlayerState.ProtectionCards);
                RefreshBalance();
            }
            else
            {
                _shopView?.ShowError(result.errorMessage);
            }
        }

        private void HandleStartNextRound()
        {
            // Advance past Shop step, which triggers StartNextRound
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleBuyIntel(AnalystType type)
        {
            int balance = _gameEngine.PlayerState.Balance;
            PurchaseResult result = _gameEngine.AnalystSystem.BuyIntel(type, balance);

            if (result.success)
            {
                // Deduct balance
                int price = balance - result.remainingBalance;
                _gameEngine.PlayerState.DeductBalance(price);

                // Show purchased intel content in AnalystView
                var intel = _gameEngine.AnalystSystem.GetIntel(type);
                if (intel.HasValue)
                {
                    _analystView?.ShowIntel(type, intel.Value.content);
                }

                // Refresh balance
                _analystView?.UpdateBalance(_gameEngine.PlayerState.Balance);
                RefreshBalance();
            }
            else
            {
                _analystView?.ShowError(result.errorMessage);
            }
        }

        private void HandleAnalystContinue()
        {
            // Advance past BuyAnalyst step
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleRaceAnimationComplete()
        {
            // Race animation finished, advance state machine
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private void RefreshBalance()
        {
            int balance = _gameEngine.PlayerState.Balance;
            _mainView?.UpdateBalance(balance);
        }
    }
}
