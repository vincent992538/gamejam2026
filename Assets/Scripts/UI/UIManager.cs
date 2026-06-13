using UnityEngine;
using UnityEngine.UIElements;
using HorseBetting.Core;

namespace HorseBetting.UI
{
    /// <summary>
    /// Orchestrates UI view switching based on RoundStateMachine step transitions.
    /// Subscribes to state machine events and toggles visibility of each view.
    /// Handles user input gates by forwarding view events to AdvanceStep().
    /// Validates: Requirements 1.1, 1.2, 13.1, 13.2, 13.3, 13.4
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class UIManager : MonoBehaviour
    {
        // ─── Inspector References ───────────────────────────────────────────────

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

        // ─── View Instances ─────────────────────────────────────────────────────

        private MainView _mainView;
        private BettingView _bettingView;
        private SettlementView _settlementView;
        private ShopView _shopView;
        private AnalystView _analystView;

        // ─── Root Elements ──────────────────────────────────────────────────────

        private VisualElement _mainRoot;
        private VisualElement _bettingRoot;
        private VisualElement _settlementRoot;
        private VisualElement _shopRoot;
        private VisualElement _analystRoot;

        // ─── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            InitializeViews();
            SubscribeToStateMachine();
            SubscribeToViewEvents();

            // Hide all views first, then show MainView
            // (all GameObjects must be active during InitializeViews to access rootVisualElement)
            ShowMainView();
        }

        private void OnDestroy()
        {
            UnsubscribeFromStateMachine();
            UnsubscribeFromViewEvents();
        }

        // ─── Initialization ─────────────────────────────────────────────────────

        private void InitializeViews()
        {
            // Get root VisualElements from UIDocuments
            if (_mainUIDocument != null)
                _mainRoot = _mainUIDocument.rootVisualElement;
            if (_bettingUIDocument != null)
                _bettingRoot = _bettingUIDocument.rootVisualElement;
            if (_settlementUIDocument != null)
                _settlementRoot = _settlementUIDocument.rootVisualElement;
            if (_shopUIDocument != null)
                _shopRoot = _shopUIDocument.rootVisualElement;
            if (_analystUIDocument != null)
                _analystRoot = _analystUIDocument.rootVisualElement;

            // Initialize view controllers
            _mainView = new MainView();
            if (_mainRoot != null)
                _mainView.Initialize(_mainRoot);

            _bettingView = new BettingView();
            if (_bettingRoot != null)
                _bettingView.Initialize(_bettingRoot);

            _settlementView = new SettlementView();
            if (_settlementRoot != null)
                _settlementView.Initialize(_settlementRoot);

            _shopView = new ShopView();
            if (_shopRoot != null)
                _shopView.Initialize(_shopRoot);

            _analystView = new AnalystView();
            if (_analystRoot != null)
                _analystView.Initialize(_analystRoot);
        }

        // ─── State Machine Subscriptions ────────────────────────────────────────

        private void SubscribeToStateMachine()
        {
            if (_gameEngine == null || _gameEngine.RoundStateMachine == null)
            {
                Debug.LogError("[UIManager] GameEngine or RoundStateMachine is null.");
                return;
            }

            _gameEngine.RoundStateMachine.OnStepStarted += HandleStepStarted;
            _gameEngine.RoundStateMachine.OnStepCompleted += HandleStepCompleted;
            _gameEngine.RoundStateMachine.OnRoundStarted += HandleRoundStarted;
        }

        private void UnsubscribeFromStateMachine()
        {
            if (_gameEngine == null || _gameEngine.RoundStateMachine == null)
                return;

            _gameEngine.RoundStateMachine.OnStepStarted -= HandleStepStarted;
            _gameEngine.RoundStateMachine.OnStepCompleted -= HandleStepCompleted;
            _gameEngine.RoundStateMachine.OnRoundStarted -= HandleRoundStarted;
        }

        // ─── View Event Subscriptions ───────────────────────────────────────────

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

        // ─── Step Event Handlers ────────────────────────────────────────────────

        private void HandleStepStarted(RoundStep step)
        {
            switch (step)
            {
                // Main view steps: horse generation, odds calculation, card reveals
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

                // Betting rounds → BettingView
                case RoundStep.BettingRound1:
                case RoundStep.BettingRound2:
                case RoundStep.BettingRound3:
                    ShowBettingView();
                    break;

                // Analyst purchase → AnalystView
                case RoundStep.BuyAnalyst:
                    ShowAnalystView();
                    break;

                // Race steps → RaceView
                case RoundStep.StartRace:
                case RoundStep.RevealTrack:
                case RoundStep.RaceAnimation:
                case RoundStep.StageEvents:
                    ShowRaceView();
                    break;

                // Results and settlement → SettlementView
                case RoundStep.FinalRanking:
                case RoundStep.Settlement:
                    ShowSettlementView();
                    break;

                // Shop → ShopView
                case RoundStep.Shop:
                    ShowShopView();
                    break;
            }
        }

        private void HandleStepCompleted(RoundStep step)
        {
            // Step completion can be used for transition animations or cleanup
            // Currently no additional logic needed on completion
        }

        private void HandleRoundStarted(int roundNumber)
        {
            // New round started — show MainView
            ShowMainView();
        }

        // ─── Input Gate Handlers ────────────────────────────────────────────────

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

        // ─── View Visibility Management ─────────────────────────────────────────

        public void ShowMainView()
        {
            SetViewVisibility(mainVisible: true);
        }

        public void ShowBettingView()
        {
            SetViewVisibility(bettingVisible: true);
        }

        public void ShowRaceView()
        {
            SetViewVisibility(raceVisible: true);
        }

        public void ShowSettlementView()
        {
            SetViewVisibility(settlementVisible: true);
        }

        public void ShowShopView()
        {
            SetViewVisibility(shopVisible: true);
        }

        public void ShowAnalystView()
        {
            SetViewVisibility(analystVisible: true);
        }

        /// <summary>
        /// Toggles visibility of all views. Only the specified view is shown;
        /// all others are hidden. Uses GameObject.SetActive to fully disable
        /// UIDocuments that are not needed.
        /// </summary>
        private void SetViewVisibility(
            bool mainVisible = false,
            bool bettingVisible = false,
            bool raceVisible = false,
            bool settlementVisible = false,
            bool shopVisible = false,
            bool analystVisible = false)
        {
            SetUIDocumentActive(_mainUIDocument, mainVisible);
            SetUIDocumentActive(_bettingUIDocument, bettingVisible);
            SetUIDocumentActive(_settlementUIDocument, settlementVisible);
            SetUIDocumentActive(_shopUIDocument, shopVisible);
            SetUIDocumentActive(_analystUIDocument, analystVisible);

            // RaceView is a MonoBehaviour with its own GameObject
            if (_raceView != null)
            {
                _raceView.gameObject.SetActive(raceVisible);
            }
        }

        /// <summary>
        /// Enables or disables a UIDocument's GameObject to fully show/hide it.
        /// </summary>
        private void SetUIDocumentActive(UIDocument doc, bool active)
        {
            if (doc != null)
            {
                doc.enabled = active;
            }
        }
    }
}
