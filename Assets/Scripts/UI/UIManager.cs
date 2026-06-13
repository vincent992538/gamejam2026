using UnityEngine;
using UnityEngine.UIElements;
using HorseBetting.Core;

namespace HorseBetting.UI
{
    /// <summary>
    /// Orchestrates UI view switching based on RoundStateMachine step transitions.
    /// All UIDocuments remain enabled; visibility is controlled via display style
    /// on each document's root visual element.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class UIManager : MonoBehaviour
    {
        [Header("Game Engine")]
        [SerializeField] private GameEngine _gameEngine;

        [Header("UI Toolkit Documents")]
        [SerializeField] private UIDocument _mainUIDocument;
        [SerializeField] private UIDocument _bettingUIDocument;
        [SerializeField] private UIDocument _settlementUIDocument;
        [SerializeField] private UIDocument _shopUIDocument;
        [SerializeField] private UIDocument _analystUIDocument;

        [Header("2D Race Scene")]
        [SerializeField] private RaceView _raceView;

        // View controllers
        private MainView _mainView;
        private BettingView _bettingView;
        private SettlementView _settlementView;
        private ShopView _shopView;
        private AnalystView _analystView;

        // Public accessors for GameBootstrap/FlowController
        public MainView MainViewCtrl => _mainView;
        public BettingView BettingViewCtrl => _bettingView;
        public SettlementView SettlementViewCtrl => _settlementView;
        public ShopView ShopViewCtrl => _shopView;
        public AnalystView AnalystViewCtrl => _analystView;

        private void Awake()
        {
            // Initialize views immediately in Awake (all UIDocuments are enabled)
            InitializeViews();
            // Hide all except main
            HideAllViews();
        }

        private void Start()
        {
            SubscribeToStateMachine();
            SubscribeToViewEvents();
            ShowMainView();
        }

        private void OnDestroy()
        {
            UnsubscribeFromStateMachine();
            UnsubscribeFromViewEvents();
        }

        private void InitializeViews()
        {
            _mainView = new MainView();
            if (_mainUIDocument != null && _mainUIDocument.rootVisualElement != null)
                _mainView.Initialize(_mainUIDocument.rootVisualElement);

            _bettingView = new BettingView();
            if (_bettingUIDocument != null && _bettingUIDocument.rootVisualElement != null)
                _bettingView.Initialize(_bettingUIDocument.rootVisualElement);

            _settlementView = new SettlementView();
            if (_settlementUIDocument != null && _settlementUIDocument.rootVisualElement != null)
                _settlementView.Initialize(_settlementUIDocument.rootVisualElement);

            _shopView = new ShopView();
            if (_shopUIDocument != null && _shopUIDocument.rootVisualElement != null)
                _shopView.Initialize(_shopUIDocument.rootVisualElement);

            _analystView = new AnalystView();
            if (_analystUIDocument != null && _analystUIDocument.rootVisualElement != null)
                _analystView.Initialize(_analystUIDocument.rootVisualElement);
        }

        private void SubscribeToStateMachine()
        {
            if (_gameEngine == null || _gameEngine.RoundStateMachine == null)
            {
                Debug.LogError("[UIManager] GameEngine or RoundStateMachine is null.");
                return;
            }
            _gameEngine.RoundStateMachine.OnStepStarted += HandleStepStarted;
            _gameEngine.RoundStateMachine.OnRoundStarted += HandleRoundStarted;
        }

        private void UnsubscribeFromStateMachine()
        {
            if (_gameEngine == null || _gameEngine.RoundStateMachine == null) return;
            _gameEngine.RoundStateMachine.OnStepStarted -= HandleStepStarted;
            _gameEngine.RoundStateMachine.OnRoundStarted -= HandleRoundStarted;
        }

        private void SubscribeToViewEvents()
        {
            if (_bettingView != null)
                _bettingView.OnDoneBetting += HandleDoneBetting;
            if (_shopView != null)
                _shopView.OnStartNextRound += HandleStartNextRound;
            if (_analystView != null)
                _analystView.OnContinueClicked += HandleAnalystContinue;
            if (_raceView != null)
                _raceView.OnRaceComplete += HandleRaceComplete;
        }

        private void UnsubscribeFromViewEvents()
        {
            if (_bettingView != null)
                _bettingView.OnDoneBetting -= HandleDoneBetting;
            if (_shopView != null)
                _shopView.OnStartNextRound -= HandleStartNextRound;
            if (_analystView != null)
                _analystView.OnContinueClicked -= HandleAnalystContinue;
            if (_raceView != null)
                _raceView.OnRaceComplete -= HandleRaceComplete;
        }

        // ─── Step Handlers ──────────────────────────────────────────────────────

        private void HandleStepStarted(RoundStep step)
        {
            switch (step)
            {
                case RoundStep.GenerateHorses:
                case RoundStep.CalculateInitialOdds:
                case RoundStep.RevealCard1:
                case RoundStep.RevealCard2:
                case RoundStep.RevealCard3:
                case RoundStep.UpdateOdds1:
                case RoundStep.DetermineTrack:
                case RoundStep.GenerateEvents:
                case RoundStep.GenerateAnalystIntel:
                case RoundStep.StartNextRound:
                    ShowMainView();
                    break;

                case RoundStep.BettingRound1:
                case RoundStep.BettingRound2:
                case RoundStep.BettingRound3:
                    ShowBettingView();
                    break;

                case RoundStep.BuyAnalyst:
                    ShowAnalystView();
                    break;

                case RoundStep.StartRace:
                case RoundStep.RevealTrack:
                case RoundStep.RaceAnimation:
                case RoundStep.StageEvents:
                    ShowRaceView();
                    break;

                case RoundStep.FinalRanking:
                case RoundStep.Settlement:
                    ShowSettlementView();
                    break;

                case RoundStep.Shop:
                    ShowShopView();
                    break;
            }
        }

        private void HandleRoundStarted(int roundNumber)
        {
            ShowMainView();
        }

        // ─── Input Gates ────────────────────────────────────────────────────────

        private void HandleDoneBetting()
        {
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleStartNextRound()
        {
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleAnalystContinue()
        {
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        private void HandleRaceComplete()
        {
            _gameEngine.RoundStateMachine.AdvanceStep();
        }

        // ─── View Switching (public for GameBootstrap) ──────────────────────────

        public void ShowMainView() => SetVisibility(main: true);
        public void ShowBettingView() => SetVisibility(betting: true);
        public void ShowRaceView() => SetVisibility(race: true);
        public void ShowSettlementView() => SetVisibility(settlement: true);
        public void ShowShopView() => SetVisibility(shop: true);
        public void ShowAnalystView() => SetVisibility(analyst: true);

        public void HideAllViews() => SetVisibility();

        private void SetVisibility(
            bool main = false, bool betting = false, bool race = false,
            bool settlement = false, bool shop = false, bool analyst = false)
        {
            SetDocDisplay(_mainUIDocument, main);
            SetDocDisplay(_bettingUIDocument, betting);
            SetDocDisplay(_settlementUIDocument, settlement);
            SetDocDisplay(_shopUIDocument, shop);
            SetDocDisplay(_analystUIDocument, analyst);

            if (_raceView != null)
                _raceView.gameObject.SetActive(race);
        }

        private void SetDocDisplay(UIDocument doc, bool visible)
        {
            if (doc == null) return;
            var root = doc.rootVisualElement;
            if (root == null) return;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
