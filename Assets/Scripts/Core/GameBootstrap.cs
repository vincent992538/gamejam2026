using UnityEngine;
using UnityEngine.UIElements;
using HorseBetting.UI;

namespace HorseBetting.Core
{
    /// <summary>
    /// Bootstrap script that wires GameFlowController to UI views
    /// and starts the game loop. Attach to a GameObject in the scene.
    /// Runs AFTER UIManager (execution order 10) to ensure UI is ready.
    /// </summary>
    [DefaultExecutionOrder(10)]
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
            // Temporarily enable all UIDocuments to access rootVisualElement
            EnableAllDocuments(true);

            // Create view controllers and initialize them with UIDocument roots
            if (_mainUIDocument != null && _mainUIDocument.rootVisualElement != null)
            {
                var mainView = new MainView();
                mainView.Initialize(_mainUIDocument.rootVisualElement);
                _flowController.SetMainView(mainView);
            }

            if (_bettingUIDocument != null && _bettingUIDocument.rootVisualElement != null)
            {
                var bettingView = new BettingView();
                bettingView.Initialize(_bettingUIDocument.rootVisualElement);
                _flowController.SetBettingView(bettingView);
            }

            if (_settlementUIDocument != null && _settlementUIDocument.rootVisualElement != null)
            {
                var settlementView = new SettlementView();
                settlementView.Initialize(_settlementUIDocument.rootVisualElement);
                _flowController.SetSettlementView(settlementView);
            }

            if (_shopUIDocument != null && _shopUIDocument.rootVisualElement != null)
            {
                var shopView = new ShopView();
                shopView.Initialize(_shopUIDocument.rootVisualElement);
                _flowController.SetShopView(shopView);
            }

            if (_analystUIDocument != null && _analystUIDocument.rootVisualElement != null)
            {
                var analystView = new AnalystView();
                analystView.Initialize(_analystUIDocument.rootVisualElement);
                _flowController.SetAnalystView(analystView);
            }

            // Restore: disable all except main (UIManager will handle switching)
            EnableAllDocuments(false);
            if (_mainUIDocument != null) _mainUIDocument.enabled = true;
        }

        private void EnableAllDocuments(bool enabled)
        {
            if (_mainUIDocument != null) _mainUIDocument.enabled = enabled;
            if (_bettingUIDocument != null) _bettingUIDocument.enabled = enabled;
            if (_settlementUIDocument != null) _settlementUIDocument.enabled = enabled;
            if (_shopUIDocument != null) _shopUIDocument.enabled = enabled;
            if (_analystUIDocument != null) _analystUIDocument.enabled = enabled;
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
                    // Force UIManager to show the correct view for this waiting step
                    ForceShowViewForCurrentStep(sm.CurrentStep);
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
        /// Explicitly tells UIManager which view to show for the current waiting step.
        /// This ensures the correct view is visible even if event ordering was off.
        /// </summary>
        private void ForceShowViewForCurrentStep(RoundStep step)
        {
            if (_uiManager == null) return;

            // Use SendMessage to trigger view switch (simple approach)
            switch (step)
            {
                case RoundStep.BettingRound1:
                case RoundStep.BettingRound2:
                case RoundStep.BettingRound3:
                    _uiManager.SendMessage("ShowBettingView", SendMessageOptions.DontRequireReceiver);
                    break;
                case RoundStep.BuyAnalyst:
                    _uiManager.SendMessage("ShowAnalystView", SendMessageOptions.DontRequireReceiver);
                    break;
                case RoundStep.Shop:
                    _uiManager.SendMessage("ShowShopView", SendMessageOptions.DontRequireReceiver);
                    break;
            }
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
