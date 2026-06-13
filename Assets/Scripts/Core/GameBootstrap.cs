using UnityEngine;
using UnityEngine.UIElements;
using HorseBetting.UI;

namespace HorseBetting.Core
{
    /// <summary>
    /// Bootstrap script that wires GameFlowController to UI views
    /// and starts the game loop. Attach to a GameObject in the scene.
    /// This bridges the gap between UIManager (view switching) and
    /// GameFlowController (data binding) by passing view references
    /// and triggering the first round execution.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameEngine _gameEngine;
        [SerializeField] private GameFlowController _flowController;
        [SerializeField] private UIManager _uiManager;

        [Header("UI Documents")]
        [SerializeField] private UIDocument _mainUIDocument;
        [SerializeField] private UIDocument _bettingUIDocument;
        [SerializeField] private UIDocument _settlementUIDocument;
        [SerializeField] private UIDocument _shopUIDocument;
        [SerializeField] private UIDocument _analystUIDocument;

        private void Start()
        {
            if (_gameEngine == null || _flowController == null)
            {
                Debug.LogError("[GameBootstrap] Missing references. Cannot start game.");
                return;
            }

            // Create and wire UI view controllers to GameFlowController
            WireViewsToFlowController();

            // Subscribe to state machine step completed to resume after player input
            _gameEngine.RoundStateMachine.OnStepCompleted += HandleStepCompleted;

            // Start the game loop - execute the first step
            StartGameLoop();
        }

        private void OnDestroy()
        {
            if (_gameEngine != null && _gameEngine.RoundStateMachine != null)
            {
                _gameEngine.RoundStateMachine.OnStepCompleted -= HandleStepCompleted;
            }
        }

        private bool _isRunningAutoSteps = false;

        /// <summary>
        /// When a waiting step completes (player pressed Done Betting, Continue, etc.),
        /// resume auto-executing the next steps.
        /// </summary>
        private void HandleStepCompleted(RoundStep step)
        {
            // Only resume if we're not already running auto steps
            // (prevents recursive calls from auto-completing steps)
            if (!_isRunningAutoSteps)
            {
                Invoke(nameof(ResumeAfterInput), 0.05f);
            }
        }

        private void ResumeAfterInput()
        {
            RunAutoSteps();
        }

        private void WireViewsToFlowController()
        {
            // Create view controllers and initialize them with UIDocument roots
            if (_mainUIDocument != null)
            {
                var mainView = new MainView();
                mainView.Initialize(_mainUIDocument.rootVisualElement);
                _flowController.SetMainView(mainView);
            }

            if (_bettingUIDocument != null)
            {
                var bettingView = new BettingView();
                bettingView.Initialize(_bettingUIDocument.rootVisualElement);
                _flowController.SetBettingView(bettingView);
            }

            if (_settlementUIDocument != null)
            {
                var settlementView = new SettlementView();
                settlementView.Initialize(_settlementUIDocument.rootVisualElement);
                _flowController.SetSettlementView(settlementView);
            }

            if (_shopUIDocument != null)
            {
                var shopView = new ShopView();
                shopView.Initialize(_shopUIDocument.rootVisualElement);
                _flowController.SetShopView(shopView);
            }

            if (_analystUIDocument != null)
            {
                var analystView = new AnalystView();
                analystView.Initialize(_analystUIDocument.rootVisualElement);
                _flowController.SetAnalystView(analystView);
            }
        }

        /// <summary>
        /// Starts executing the game round from the first step.
        /// Runs all automatic steps until hitting a waiting step (player input required).
        /// </summary>
        private void StartGameLoop()
        {
            Debug.Log("[GameBootstrap] Starting game loop...");
            RunAutoSteps();
        }

        /// <summary>
        /// Continuously executes and advances automatic steps until
        /// a waiting step is reached (requires player input).
        /// </summary>
        private void RunAutoSteps()
        {
            _isRunningAutoSteps = true;
            var sm = _gameEngine.RoundStateMachine;
            int safetyCounter = 0;

            while (safetyCounter < 25) // prevent infinite loop
            {
                safetyCounter++;

                // Execute the current step
                sm.ExecuteCurrentStep(_gameEngine);

                // If this step waits for player input, stop auto-advancing
                if (sm.IsWaitingForInput)
                {
                    Debug.Log($"[GameBootstrap] Waiting for player input at step: {sm.CurrentStep}");
                    break;
                }

                // If step completed automatically, advance to next
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

        /// <summary>
        /// Called by UIManager or views when the player finishes input
        /// at a waiting step. Resumes auto-execution.
        /// Subscribe this to state machine's OnStepCompleted for waiting steps.
        /// </summary>
        public void OnPlayerInputComplete()
        {
            var sm = _gameEngine.RoundStateMachine;
            sm.AdvanceStep();
            RunAutoSteps();
        }
    }
}
