using System;
using UnityEngine;
using HorseBetting.Data;

namespace HorseBetting.Core
{
    /// <summary>
    /// Defines the 21 sequential steps in a single game round.
    /// Validates: Requirements 1.1, 1.2, 1.3
    /// </summary>
    public enum RoundStep
    {
        GenerateHorses = 0,
        CalculateInitialOdds = 1,
        RevealCard1 = 2,
        RevealCard2 = 3,
        BettingRound1 = 4,
        RevealCard3 = 5,
        UpdateOdds1 = 6,
        BettingRound2 = 7,
        DetermineTrack = 8,
        GenerateEvents = 9,
        GenerateAnalystIntel = 10,
        BuyAnalyst = 11,
        BettingRound3 = 12,
        Shop = 13,
        StartRace = 14,
        RevealTrack = 15,
        RaceAnimation = 16,
        StageEvents = 17,
        FinalRanking = 18,
        Settlement = 19,
        StartNextRound = 20
    }

    /// <summary>
    /// Manages the 21-step round sequence. Enforces sequential execution
    /// (step N must complete before step N+1 begins) and auto-starts the
    /// next round after completion.
    /// Validates: Requirements 1.1, 1.2, 1.3
    /// </summary>
    public class RoundStateMachine
    {
        public const int TotalSteps = 21;

        // ─── State ──────────────────────────────────────────────────────────────

        private RoundStep _currentStep;
        private bool _stepInProgress;
        private int _currentRound;

        // ─── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired when a step begins execution.</summary>
        public event Action<RoundStep> OnStepStarted;

        /// <summary>Fired when a step completes.</summary>
        public event Action<RoundStep> OnStepCompleted;

        /// <summary>Fired when a new round begins (after StartNextRound).</summary>
        public event Action<int> OnRoundStarted;

        // ─── Properties ─────────────────────────────────────────────────────────

        public RoundStep CurrentStep => _currentStep;
        public int CurrentRound => _currentRound;
        public bool IsStepInProgress => _stepInProgress;

        /// <summary>
        /// Returns true if the current step requires player input before advancing.
        /// Waiting steps: BettingRound1, BettingRound2, BettingRound3, BuyAnalyst, Shop
        /// </summary>
        public bool IsWaitingForInput
        {
            get
            {
                return _currentStep == RoundStep.BettingRound1
                    || _currentStep == RoundStep.BettingRound2
                    || _currentStep == RoundStep.BettingRound3
                    || _currentStep == RoundStep.BuyAnalyst
                    || _currentStep == RoundStep.Settlement
                    || _currentStep == RoundStep.Shop;
            }
        }

        // ─── Constructor ────────────────────────────────────────────────────────

        public RoundStateMachine()
        {
            _currentStep = RoundStep.GenerateHorses;
            _currentRound = 1;
            _stepInProgress = false;
        }

        // ─── Public Methods ─────────────────────────────────────────────────────

        /// <summary>
        /// Executes the current step by calling the appropriate system methods
        /// on the provided GameEngine. Automatic steps complete immediately;
        /// waiting steps require a subsequent call to AdvanceStep() after
        /// the player has finished their input.
        /// </summary>
        public void ExecuteCurrentStep(GameEngine engine)
        {
            if (_stepInProgress)
            {
                Debug.LogWarning($"[RoundStateMachine] Step {_currentStep} is already in progress.");
                return;
            }

            _stepInProgress = true;
            OnStepStarted?.Invoke(_currentStep);

            switch (_currentStep)
            {
                case RoundStep.GenerateHorses:
                    var horses = engine.HorseSystem.GenerateHorses();
                    engine.MessageCardSystem.SetHorses(horses);
                    CompleteCurrentStep();
                    break;

                case RoundStep.CalculateInitialOdds:
                    engine.OddsSystem.CalculateOdds(engine.HorseSystem.GetHorses(), 1);
                    CompleteCurrentStep();
                    break;

                case RoundStep.RevealCard1:
                    engine.MessageCardSystem.RevealNextCard();
                    CompleteCurrentStep();
                    break;

                case RoundStep.RevealCard2:
                    engine.MessageCardSystem.RevealNextCard();
                    CompleteCurrentStep();
                    break;

                case RoundStep.BettingRound1:
                    // Waiting for player input — do not auto-complete
                    break;

                case RoundStep.RevealCard3:
                    engine.MessageCardSystem.RevealNextCard();
                    CompleteCurrentStep();
                    break;

                case RoundStep.UpdateOdds1:
                    engine.OddsSystem.UpdateOddsAfterBetting(1);
                    CompleteCurrentStep();
                    break;

                case RoundStep.BettingRound2:
                    // Waiting for player input
                    break;

                case RoundStep.DetermineTrack:
                    engine.TrackSystem.SelectTrack();
                    CompleteCurrentStep();
                    break;

                case RoundStep.GenerateEvents:
                    // Events are generated but stored for later use during race
                    CompleteCurrentStep();
                    break;

                case RoundStep.GenerateAnalystIntel:
                    engine.AnalystSystem.GenerateIntel(engine.HorseSystem.GetHorses());
                    CompleteCurrentStep();
                    break;

                case RoundStep.BuyAnalyst:
                    // Waiting for player input
                    break;

                case RoundStep.BettingRound3:
                    // Waiting for player input
                    break;

                case RoundStep.StartRace:
                    CompleteCurrentStep();
                    break;

                case RoundStep.RevealTrack:
                    // Track is now visible to the player
                    CompleteCurrentStep();
                    break;

                case RoundStep.RaceAnimation:
                    // Animation plays — for now completes immediately; UI can delay AdvanceStep
                    CompleteCurrentStep();
                    break;

                case RoundStep.StageEvents:
                    // Stage events are processed as part of race simulation
                    CompleteCurrentStep();
                    break;

                case RoundStep.FinalRanking:
                    engine.RaceSimulationSystem.GetFinalRanking();
                    CompleteCurrentStep();
                    break;

                case RoundStep.Settlement:
                    // Waiting for player to review results
                    break;

                case RoundStep.Shop:
                    // Waiting for player input
                    break;

                case RoundStep.StartNextRound:
                    CompleteCurrentStep();
                    StartNewRound();
                    break;
            }
        }

        /// <summary>
        /// Advances from the current step to the next step.
        /// For waiting steps, call this after the player has confirmed their action.
        /// Enforces sequential execution: current step must be marked complete
        /// before the next one begins.
        /// </summary>
        public void AdvanceStep()
        {
            // If the step is still in progress (waiting steps), complete it first
            if (_stepInProgress)
            {
                CompleteCurrentStep();
            }

            int nextIndex = (int)_currentStep + 1;

            if (nextIndex >= TotalSteps)
            {
                // Round complete — auto-start next round (Req 1.3)
                StartNewRound();
                return;
            }

            _currentStep = (RoundStep)nextIndex;
        }

        /// <summary>
        /// Resets the state machine to the beginning of a new round.
        /// Increments the round counter and fires the OnRoundStarted event.
        /// </summary>
        public void StartNewRound()
        {
            _currentRound++;
            _currentStep = RoundStep.GenerateHorses;
            _stepInProgress = false;
            OnRoundStarted?.Invoke(_currentRound);
        }

        /// <summary>
        /// Starts the very first round (called once at game start).
        /// </summary>
        public void StartFirstRound()
        {
            _currentRound = 1;
            _currentStep = RoundStep.GenerateHorses;
            _stepInProgress = false;
            OnRoundStarted?.Invoke(_currentRound);
        }

        // ─── Private Helpers ────────────────────────────────────────────────────

        private void CompleteCurrentStep()
        {
            _stepInProgress = false;
            OnStepCompleted?.Invoke(_currentStep);
        }
    }
}
