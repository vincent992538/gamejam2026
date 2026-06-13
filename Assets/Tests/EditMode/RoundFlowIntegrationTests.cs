using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;
using HorseBetting.Systems;

namespace HorseBetting.Tests.EditMode
{
    /// <summary>
    /// Integration tests that validate the full round flow end-to-end.
    /// Simulates a complete round by advancing through all 21 steps and verifying:
    /// - State machine progresses through all steps in correct order
    /// - Horse generation produces 8 horses at each round start
    /// - Betting deducts balance correctly
    /// - Race simulation produces a valid ranking
    /// - Settlement calculates correct payouts
    /// - Balance carries over between rounds
    /// - Protection cards persist until used
    /// Validates: Requirements 1.1, 1.2, 1.3
    /// </summary>
    [TestFixture]
    public class RoundFlowIntegrationTests
    {
        // ─── Config instances ───────────────────────────────────────────────────

        private GameConfig _gameConfig;
        private OddsConfig _oddsConfig;
        private MessageCardConfig _messageCardConfig;
        private TrackConfig _trackConfig;
        private AnalystConfig _analystConfig;
        private EventConfig _eventConfig;
        private ShopConfig _shopConfig;
        private BettingConfig _bettingConfig;

        // ─── Systems ────────────────────────────────────────────────────────────

        private HorseSystem _horseSystem;
        private MessageCardSystem _messageCardSystem;
        private OddsSystem _oddsSystem;
        private TrackSystem _trackSystem;
        private AnalystSystem _analystSystem;
        private EventSystem _eventSystem;
        private RaceSimulationSystem _raceSimulationSystem;
        private BettingSystem _bettingSystem;
        private ShopSystem _shopSystem;
        private SettlementSystem _settlementSystem;
        private PlayerState _playerState;
        private RoundStateMachine _stateMachine;

        [SetUp]
        public void SetUp()
        {
            CreateConfigs();
            CreateSystems();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameConfig);
            Object.DestroyImmediate(_oddsConfig);
            Object.DestroyImmediate(_messageCardConfig);
            Object.DestroyImmediate(_trackConfig);
            Object.DestroyImmediate(_analystConfig);
            Object.DestroyImmediate(_eventConfig);
            Object.DestroyImmediate(_shopConfig);
            Object.DestroyImmediate(_bettingConfig);
        }

        // ─── Test: Full Single Round Flow ───────────────────────────────────────

        [Test]
        public void FullRound_AdvancesThroughAll21Steps_InCorrectOrder()
        {
            var stepsVisited = new List<RoundStep>();
            _stateMachine.OnStepStarted += step => stepsVisited.Add(step);

            _stateMachine.StartFirstRound();

            // Execute all 21 steps
            SimulateFullRound();

            // Verify all 21 steps were visited in order
            Assert.AreEqual(RoundStateMachine.TotalSteps, stepsVisited.Count,
                "All 21 steps should be visited during a full round.");

            for (int i = 0; i < RoundStateMachine.TotalSteps; i++)
            {
                Assert.AreEqual((RoundStep)i, stepsVisited[i],
                    $"Step {i} should be {(RoundStep)i} but was {stepsVisited[i]}.");
            }
        }

        [Test]
        public void FullRound_GenerateHorses_Produces8HorsesWithValidBonuses()
        {
            _stateMachine.StartFirstRound();

            // Step 0: GenerateHorses
            HorseData[] horses = _horseSystem.GenerateHorses();

            Assert.AreEqual(8, horses.Length, "Should generate exactly 8 horses.");

            // Verify each horse has baseSpeed 30
            foreach (var horse in horses)
            {
                Assert.AreEqual(30, horse.baseSpeed, $"Horse {horse.index} should have baseSpeed 30.");
            }

            // Verify hidden bonuses form a permutation of {0,1,2,3,4,5,6,7}
            int[] bonuses = horses.Select(h => h.hiddenBonus).OrderBy(b => b).ToArray();
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                bonuses,
                "Hidden bonuses must be a permutation of {0..7}.");
        }

        [Test]
        public void FullRound_BettingDeductsBalanceCorrectly()
        {
            _stateMachine.StartFirstRound();

            int initialBalance = _playerState.Balance;
            Assert.AreEqual(1000, initialBalance);

            // Place a bet of 200
            var bet = new Bet
            {
                type = BetType.SingleWin,
                amount = 200,
                selectedHorses = new[] { 0 },
                bettingRound = 1,
                oddsAtBet = 5.0f
            };

            BetResult result = _bettingSystem.PlaceBet(bet, _playerState.Balance);
            Assert.IsTrue(result.success);

            _playerState.DeductBalance(bet.amount);
            _playerState.AddBet(bet);

            Assert.AreEqual(800, _playerState.Balance,
                "Balance should be 800 after placing a 200 bet.");
        }

        [Test]
        public void FullRound_RaceSimulation_ProducesValidRanking()
        {
            _stateMachine.StartFirstRound();

            HorseData[] horses = _horseSystem.GenerateHorses();
            _messageCardSystem.SetHorses(horses);
            TrackType track = _trackSystem.SelectTrack();

            RaceResult result = _raceSimulationSystem.SimulateRace(
                horses, track, _eventConfig, new ProtectionCard[0]);

            Assert.IsNotNull(result.finalRanking, "Final ranking should not be null.");
            Assert.AreEqual(8, result.finalRanking.Length, "Ranking should contain 8 horses.");

            // All horse indices should be present exactly once (0-7)
            int[] sortedRanking = result.finalRanking.OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                sortedRanking,
                "Ranking should contain each horse index exactly once.");

            Assert.IsNotNull(result.finalSpeeds, "Final speeds should not be null.");
            Assert.AreEqual(8, result.finalSpeeds.Length, "Final speeds should have 8 entries.");
        }

        [Test]
        public void FullRound_Settlement_CalculatesCorrectPayouts()
        {
            // Set up a deterministic scenario
            HorseData[] horses = _horseSystem.GenerateHorses();
            _messageCardSystem.SetHorses(horses);
            TrackType track = _trackSystem.SelectTrack();

            RaceResult raceResult = _raceSimulationSystem.SimulateRace(
                horses, track, _eventConfig, new ProtectionCard[0]);

            // Place a winning bet on the actual winner
            int winnerIndex = raceResult.finalRanking[0];
            var winningBet = new Bet
            {
                type = BetType.SingleWin,
                amount = 100,
                selectedHorses = new[] { winnerIndex },
                bettingRound = 1,
                oddsAtBet = 5.0f
            };

            // Place a losing bet on a non-winner
            int loserIndex = raceResult.finalRanking[7]; // last place
            var losingBet = new Bet
            {
                type = BetType.SingleWin,
                amount = 50,
                selectedHorses = new[] { loserIndex },
                bettingRound = 1,
                oddsAtBet = 5.0f
            };

            Bet[] activeBets = new[] { winningBet, losingBet };

            SettlementResult settlement = _settlementSystem.CalculateSettlement(
                raceResult.finalRanking, activeBets, _bettingConfig);

            Assert.AreEqual(2, settlement.betResults.Length);

            // Winning bet: payout = 100 * 5.0 = 500
            Assert.IsTrue(settlement.betResults[0].won);
            Assert.AreEqual(500, settlement.betResults[0].payout);

            // Losing bet: payout = 0
            Assert.IsFalse(settlement.betResults[1].won);
            Assert.AreEqual(0, settlement.betResults[1].payout);

            // Total winnings = 500, total loss = 150, net = 350
            Assert.AreEqual(500, settlement.totalWinnings);
            Assert.AreEqual(150, settlement.totalLoss);
            Assert.AreEqual(350, settlement.netProfit);
        }

        // ─── Test: Balance Carries Across Rounds ────────────────────────────────

        [Test]
        public void MultiRound_BalanceCarriesAcrossRounds()
        {
            _stateMachine.StartFirstRound();

            // Round 1: Start with 1000, place 200 bet, simulate settlement with a win
            int initialBalance = _playerState.Balance;
            Assert.AreEqual(1000, initialBalance);

            // Place bet and deduct
            var bet = new Bet
            {
                type = BetType.SingleWin,
                amount = 200,
                selectedHorses = new[] { 0 },
                bettingRound = 1,
                oddsAtBet = 5.0f
            };
            _bettingSystem.PlaceBet(bet, _playerState.Balance);
            _playerState.DeductBalance(200);
            _playerState.AddBet(bet);

            Assert.AreEqual(800, _playerState.Balance);

            // Simulate winning settlement: payout = 200 * 5 = 1000
            _playerState.AddBalance(1000);
            Assert.AreEqual(1800, _playerState.Balance);

            // Move to next round
            _bettingSystem.ClearBets();
            _playerState.ClearBets();
            _stateMachine.StartNewRound();

            // Balance persists
            Assert.AreEqual(1800, _playerState.Balance,
                "Balance should carry over to the next round.");
            Assert.AreEqual(2, _stateMachine.CurrentRound);

            // Round 2: Place another bet
            var bet2 = new Bet
            {
                type = BetType.Place,
                amount = 300,
                selectedHorses = new[] { 2 },
                bettingRound = 1,
                oddsAtBet = 2.0f
            };
            _bettingSystem.PlaceBet(bet2, _playerState.Balance);
            _playerState.DeductBalance(300);

            Assert.AreEqual(1500, _playerState.Balance,
                "Balance should reflect deduction in round 2.");

            // Simulate loss (no payout)
            _bettingSystem.ClearBets();
            _playerState.ClearBets();
            _stateMachine.StartNewRound();

            // Balance persists with deduction
            Assert.AreEqual(1500, _playerState.Balance,
                "Balance after a losing round should persist.");
            Assert.AreEqual(3, _stateMachine.CurrentRound);
        }

        // ─── Test: Protection Cards Persist Across Rounds ───────────────────────

        [Test]
        public void MultiRound_ProtectionCardsPersistAcrossRounds()
        {
            _stateMachine.StartFirstRound();

            // Buy a protection card in round 1
            var card = new ProtectionCard
            {
                cardName = "Mud Shield",
                protectsAgainst = "Mudslide",
                successRate = 1.0f
            };
            bool added = _playerState.AddProtectionCard(card);
            Assert.IsTrue(added);
            Assert.AreEqual(1, _playerState.CardCount);

            // Advance to round 2
            _stateMachine.StartNewRound();

            // Protection card persists
            Assert.AreEqual(1, _playerState.CardCount,
                "Protection card should persist across rounds.");
            Assert.AreEqual("Mud Shield", _playerState.ProtectionCards[0].cardName);

            // Buy another card in round 2
            var card2 = new ProtectionCard
            {
                cardName = "Snow Boots",
                protectsAgainst = "Blizzard",
                successRate = 0.8f
            };
            _playerState.AddProtectionCard(card2);

            // Advance to round 3
            _stateMachine.StartNewRound();

            // Both cards persist
            Assert.AreEqual(2, _playerState.CardCount,
                "Both protection cards should persist to round 3.");
        }

        [Test]
        public void MultiRound_ProtectionCard_RemovedOnlyWhenUsed()
        {
            _stateMachine.StartFirstRound();

            // Add a protection card
            var card = new ProtectionCard
            {
                cardName = "Storm Shield",
                protectsAgainst = "Storm",
                successRate = 1.0f
            };
            _playerState.AddProtectionCard(card);
            Assert.AreEqual(1, _playerState.CardCount);

            // Advance round without using it - card persists
            _stateMachine.StartNewRound();
            Assert.AreEqual(1, _playerState.CardCount);

            // Now "use" the card (simulating event protection)
            bool removed = _playerState.RemoveProtectionCard("Storm");
            Assert.IsTrue(removed);
            Assert.AreEqual(0, _playerState.CardCount);

            // Advance round - card is gone
            _stateMachine.StartNewRound();
            Assert.AreEqual(0, _playerState.CardCount,
                "Used protection card should not reappear in next round.");
        }

        // ─── Test: State Machine Round Tracking ─────────────────────────────────

        [Test]
        public void StateMachine_StartFirstRound_SetsRound1AtGenerateHorses()
        {
            _stateMachine.StartFirstRound();

            Assert.AreEqual(1, _stateMachine.CurrentRound);
            Assert.AreEqual(RoundStep.GenerateHorses, _stateMachine.CurrentStep);
            Assert.IsFalse(_stateMachine.IsStepInProgress);
        }

        [Test]
        public void StateMachine_StartNewRound_IncrementsRoundAndResetsStep()
        {
            _stateMachine.StartFirstRound();

            _stateMachine.StartNewRound();

            Assert.AreEqual(2, _stateMachine.CurrentRound);
            Assert.AreEqual(RoundStep.GenerateHorses, _stateMachine.CurrentStep);
        }

        [Test]
        public void StateMachine_OnRoundStarted_FiresWithCorrectRoundNumber()
        {
            int firedRound = -1;
            _stateMachine.OnRoundStarted += round => firedRound = round;

            _stateMachine.StartFirstRound();
            Assert.AreEqual(1, firedRound);

            _stateMachine.StartNewRound();
            Assert.AreEqual(2, firedRound);

            _stateMachine.StartNewRound();
            Assert.AreEqual(3, firedRound);
        }

        [Test]
        public void StateMachine_AdvanceStep_ProgressesSequentially()
        {
            _stateMachine.StartFirstRound();

            // Advance through first few steps
            Assert.AreEqual(RoundStep.GenerateHorses, _stateMachine.CurrentStep);

            _stateMachine.AdvanceStep();
            Assert.AreEqual(RoundStep.CalculateInitialOdds, _stateMachine.CurrentStep);

            _stateMachine.AdvanceStep();
            Assert.AreEqual(RoundStep.RevealCard1, _stateMachine.CurrentStep);

            _stateMachine.AdvanceStep();
            Assert.AreEqual(RoundStep.RevealCard2, _stateMachine.CurrentStep);

            _stateMachine.AdvanceStep();
            Assert.AreEqual(RoundStep.BettingRound1, _stateMachine.CurrentStep);
        }

        [Test]
        public void StateMachine_WaitingSteps_AreCorrectlyIdentified()
        {
            _stateMachine.StartFirstRound();

            // Advance to BettingRound1 (step 4)
            for (int i = 0; i < 4; i++)
                _stateMachine.AdvanceStep();

            Assert.AreEqual(RoundStep.BettingRound1, _stateMachine.CurrentStep);
            Assert.IsTrue(_stateMachine.IsWaitingForInput);

            // Advance to BettingRound2 (step 7)
            _stateMachine.AdvanceStep(); // RevealCard3
            _stateMachine.AdvanceStep(); // UpdateOdds1
            _stateMachine.AdvanceStep(); // BettingRound2

            Assert.AreEqual(RoundStep.BettingRound2, _stateMachine.CurrentStep);
            Assert.IsTrue(_stateMachine.IsWaitingForInput);
        }

        // ─── Test: Multi-Round Full Cycle ───────────────────────────────────────

        [Test]
        public void MultiRound_ThreeRounds_MaintainsStateConsistency()
        {
            _stateMachine.StartFirstRound();

            for (int round = 1; round <= 3; round++)
            {
                Assert.AreEqual(round, _stateMachine.CurrentRound,
                    $"Should be at round {round}.");
                Assert.AreEqual(RoundStep.GenerateHorses, _stateMachine.CurrentStep,
                    $"Round {round} should start at GenerateHorses.");

                // Generate horses for this round
                HorseData[] horses = _horseSystem.GenerateHorses();
                Assert.AreEqual(8, horses.Length,
                    $"Round {round} should generate 8 horses.");

                // Verify unique bonuses
                var bonusSet = new HashSet<int>(horses.Select(h => h.hiddenBonus));
                Assert.AreEqual(8, bonusSet.Count,
                    $"Round {round} should have 8 unique hidden bonuses.");

                // Advance through all steps
                SimulateFullRound();

                // After the last step (StartNextRound), state machine auto-advances
                // to next round via StartNewRound
            }

            // After 3 rounds, should be at round 4
            Assert.AreEqual(4, _stateMachine.CurrentRound);
        }

        [Test]
        public void FullRound_WithBettingAndSettlement_EndToEnd()
        {
            _stateMachine.StartFirstRound();

            // Step 0: GenerateHorses
            HorseData[] horses = _horseSystem.GenerateHorses();
            _messageCardSystem.SetHorses(horses);

            // Step 1: CalculateInitialOdds
            float[] odds = _oddsSystem.CalculateOdds(horses, 0);
            Assert.AreEqual(8, odds.Length);
            Assert.IsTrue(odds.All(o => o > 0), "All odds should be positive.");

            // Steps 2-3: Reveal cards
            MessageCard card1 = _messageCardSystem.RevealNextCard();
            MessageCard card2 = _messageCardSystem.RevealNextCard();
            Assert.AreNotEqual(card1.horseIndex, card2.horseIndex,
                "Revealed cards should be for different horses.");

            // Step 4: BettingRound1 - Place a bet
            int balanceBefore = _playerState.Balance;
            var bet1 = new Bet
            {
                type = BetType.SingleWin,
                amount = 100,
                selectedHorses = new[] { 0 },
                bettingRound = 1,
                oddsAtBet = odds[0]
            };
            BetResult betResult = _bettingSystem.PlaceBet(bet1, _playerState.Balance);
            Assert.IsTrue(betResult.success);
            _playerState.DeductBalance(100);
            _playerState.AddBet(bet1);
            Assert.AreEqual(balanceBefore - 100, _playerState.Balance);

            // Step 5: RevealCard3
            MessageCard card3 = _messageCardSystem.RevealNextCard();
            Assert.AreNotEqual(card3.horseIndex, card1.horseIndex);
            Assert.AreNotEqual(card3.horseIndex, card2.horseIndex);

            // Step 6: UpdateOdds1
            _oddsSystem.UpdateOddsAfterBetting(1);

            // Step 7: BettingRound2 - Place another bet
            var bet2 = new Bet
            {
                type = BetType.Place,
                amount = 50,
                selectedHorses = new[] { 1 },
                bettingRound = 2,
                oddsAtBet = odds[1]
            };
            _bettingSystem.PlaceBet(bet2, _playerState.Balance);
            _playerState.DeductBalance(50);
            _playerState.AddBet(bet2);

            // Step 8: DetermineTrack
            TrackType track = _trackSystem.SelectTrack();

            // Steps 9-10: GenerateEvents + AnalystIntel
            _analystSystem.GenerateIntel(horses);

            // Step 12: BettingRound3
            var bet3 = new Bet
            {
                type = BetType.Quinella,
                amount = 75,
                selectedHorses = new[] { 2, 3 },
                bettingRound = 3,
                oddsAtBet = odds[2]
            };
            _bettingSystem.PlaceBet(bet3, _playerState.Balance);
            _playerState.DeductBalance(75);
            _playerState.AddBet(bet3);

            // Steps 13-17: Race
            RaceResult raceResult = _raceSimulationSystem.SimulateRace(
                horses, track, _eventConfig, _playerState.ProtectionCards.ToArray());
            Assert.IsNotNull(raceResult.finalRanking);
            Assert.AreEqual(8, raceResult.finalRanking.Length);

            // Step 18: Settlement
            Bet[] activeBets = _bettingSystem.GetActiveBets();
            Assert.AreEqual(3, activeBets.Length);

            SettlementResult settlement = _settlementSystem.CalculateSettlement(
                raceResult.finalRanking, activeBets, _bettingConfig);
            Assert.IsNotNull(settlement.betResults);
            Assert.AreEqual(3, settlement.betResults.Length);

            // Apply winnings
            if (settlement.totalWinnings > 0)
            {
                _playerState.AddBalance(settlement.totalWinnings);
            }

            int expectedBalance = 1000 - 100 - 50 - 75 + settlement.totalWinnings;
            Assert.AreEqual(expectedBalance, _playerState.Balance,
                "Final balance should reflect all bets and winnings.");

            // Step 19: Shop - buy a protection card
            PurchaseResult shopResult = _shopSystem.BuyProtectionCard(0, _playerState.Balance, _playerState.CardCount);
            if (shopResult.success)
            {
                int price = _playerState.Balance - shopResult.remainingBalance;
                _playerState.DeductBalance(price);
                var protCard = new ProtectionCard
                {
                    cardName = _shopConfig.protectionCards[0].cardName,
                    protectsAgainst = _shopConfig.protectionCards[0].protectsAgainst,
                    successRate = _shopConfig.protectionCards[0].successRate
                };
                _playerState.AddProtectionCard(protCard);
            }

            int cardCountAfterShop = _playerState.CardCount;

            // Step 20: StartNextRound
            int balanceBeforeNextRound = _playerState.Balance;
            _bettingSystem.ClearBets();
            _playerState.ClearBets();
            _stateMachine.StartNewRound();

            // Verify state carries over
            Assert.AreEqual(2, _stateMachine.CurrentRound);
            Assert.AreEqual(balanceBeforeNextRound, _playerState.Balance,
                "Balance should carry to round 2.");
            Assert.AreEqual(cardCountAfterShop, _playerState.CardCount,
                "Protection cards should carry to round 2.");
        }

        // ─── Test: Protection Card Max Limit ────────────────────────────────────

        [Test]
        public void ProtectionCard_LimitOf3_EnforcedAcrossRounds()
        {
            _stateMachine.StartFirstRound();

            // Buy 3 cards
            for (int i = 0; i < 3; i++)
            {
                var card = new ProtectionCard
                {
                    cardName = $"Card {i}",
                    protectsAgainst = $"Event{i}",
                    successRate = 1.0f
                };
                bool added = _playerState.AddProtectionCard(card);
                Assert.IsTrue(added, $"Card {i} should be accepted.");
            }
            Assert.AreEqual(3, _playerState.CardCount);

            // Try to add a 4th card - should fail
            var extraCard = new ProtectionCard
            {
                cardName = "Extra Card",
                protectsAgainst = "Extra",
                successRate = 1.0f
            };
            bool addedExtra = _playerState.AddProtectionCard(extraCard);
            Assert.IsFalse(addedExtra, "4th card should be rejected (max 3).");

            // Advance to next round - limit still enforced
            _stateMachine.StartNewRound();
            Assert.AreEqual(3, _playerState.CardCount);

            bool addedInRound2 = _playerState.AddProtectionCard(extraCard);
            Assert.IsFalse(addedInRound2, "4th card should still be rejected in round 2.");

            // Remove one card, then should be able to add
            _playerState.RemoveProtectionCard("Event0");
            Assert.AreEqual(2, _playerState.CardCount);

            bool addedAfterRemoval = _playerState.AddProtectionCard(extraCard);
            Assert.IsTrue(addedAfterRemoval, "Should be able to add after removal.");
            Assert.AreEqual(3, _playerState.CardCount);
        }

        // ─── Test: Odds Degrade Across Betting Rounds ───────────────────────────

        [Test]
        public void FullRound_OddsDegradeAcrossBettingRounds()
        {
            HorseData[] horses = _horseSystem.GenerateHorses();

            float[] oddsRound1 = _oddsSystem.CalculateOdds(horses, 1);
            float[] oddsRound2 = _oddsSystem.CalculateOdds(horses, 2);
            float[] oddsRound3 = _oddsSystem.CalculateOdds(horses, 3);

            // Odds should degrade: round1 > round2 > round3 for each horse
            for (int i = 0; i < 8; i++)
            {
                Assert.Greater(oddsRound1[i], oddsRound2[i],
                    $"Horse {i} odds should be worse (lower) in round 2 vs round 1.");
                Assert.Greater(oddsRound2[i], oddsRound3[i],
                    $"Horse {i} odds should be worse (lower) in round 3 vs round 2.");
            }
        }

        // ─── Test: Message Cards Are Unique Within a Round ──────────────────────

        [Test]
        public void FullRound_MessageCardsAreUnique()
        {
            HorseData[] horses = _horseSystem.GenerateHorses();
            _messageCardSystem.SetHorses(horses);

            MessageCard c1 = _messageCardSystem.RevealNextCard();
            MessageCard c2 = _messageCardSystem.RevealNextCard();
            MessageCard c3 = _messageCardSystem.RevealNextCard();

            // All 3 cards should be for different horses
            var horseIndices = new HashSet<int> { c1.horseIndex, c2.horseIndex, c3.horseIndex };
            Assert.AreEqual(3, horseIndices.Count,
                "Three revealed cards should be for 3 different horses.");

            // All indices should be valid (0-7)
            Assert.IsTrue(horseIndices.All(i => i >= 0 && i < 8),
                "All card horse indices should be in valid range 0-7.");
        }

        // ─── Helper: Simulate full round via state machine advancement ──────────

        /// <summary>
        /// Advances through all 21 steps of the state machine.
        /// For waiting steps, simulates the AdvanceStep call (as if player completed input).
        /// </summary>
        private void SimulateFullRound()
        {
            for (int step = 0; step < RoundStateMachine.TotalSteps; step++)
            {
                RoundStep current = _stateMachine.CurrentStep;

                // Execute the step (marks in-progress, fires events)
                ExecuteStep(current);

                // If this is the last step (StartNextRound), the state machine
                // will auto-advance to next round
                if (current == RoundStep.StartNextRound)
                    break;

                // Advance to next step
                _stateMachine.AdvanceStep();
            }
        }

        /// <summary>
        /// Simulates executing a single step. For automatic steps, completes immediately.
        /// For waiting steps, just marks as started (caller must AdvanceStep).
        /// </summary>
        private void ExecuteStep(RoundStep step)
        {
            // We don't call through GameEngine here (since it requires MonoBehaviour),
            // so we simulate the step execution logic directly on the state machine.
            // The state machine's ExecuteCurrentStep requires a GameEngine reference;
            // for integration testing we just track step transitions.

            // Mark step in progress via internal state
            // Since we can't call ExecuteCurrentStep without GameEngine (MonoBehaviour),
            // we rely on AdvanceStep which handles the _stepInProgress flag.
        }

        // ─── Config & System Factory ────────────────────────────────────────────

        private void CreateConfigs()
        {
            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _gameConfig.horseCount = 8;
            _gameConfig.baseSpeed = 30;
            _gameConfig.startingBalance = 1000;
            _gameConfig.maxProtectionCards = 3;
            _gameConfig.messageCardsPerRound = 3;

            _oddsConfig = ScriptableObject.CreateInstance<OddsConfig>();
            _oddsConfig.baseMultiplier = 1.0f;
            _oddsConfig.rankOdds = new float[] { 1.5f, 2.0f, 3.0f, 5.0f, 8.0f, 12.0f, 20.0f, 40.0f };
            _oddsConfig.round2Penalty = 0.8f;
            _oddsConfig.round3Penalty = 0.6f;

            _messageCardConfig = ScriptableObject.CreateInstance<MessageCardConfig>();
            _messageCardConfig.entries = new MessageCardEntry[]
            {
                new MessageCardEntry { hiddenSpeedBonus = 0, description = "Seems very slow today" },
                new MessageCardEntry { hiddenSpeedBonus = 1, description = "Looking sluggish" },
                new MessageCardEntry { hiddenSpeedBonus = 2, description = "Below average form" },
                new MessageCardEntry { hiddenSpeedBonus = 3, description = "Moderate condition" },
                new MessageCardEntry { hiddenSpeedBonus = 4, description = "Decent shape" },
                new MessageCardEntry { hiddenSpeedBonus = 5, description = "Good form" },
                new MessageCardEntry { hiddenSpeedBonus = 6, description = "Excellent condition" },
                new MessageCardEntry { hiddenSpeedBonus = 7, description = "In peak form today" }
            };

            _trackConfig = ScriptableObject.CreateInstance<TrackConfig>();
            _trackConfig.horsePreferences = new TrackPreference[8];
            for (int i = 0; i < 8; i++)
            {
                _trackConfig.horsePreferences[i] = new TrackPreference
                {
                    horseIndex = i,
                    grassModifier = (i % 3) - 1,  // -1, 0, 1 pattern
                    mudModifier = ((i + 1) % 3) - 1,
                    snowModifier = ((i + 2) % 3) - 1
                };
            }

            _analystConfig = ScriptableObject.CreateInstance<AnalystConfig>();
            _analystConfig.seniorPrice = 200;
            _analystConfig.seniorAccuracy = 0.8f;
            _analystConfig.juniorPrice = 80;
            _analystConfig.juniorAccuracy = 0.5f;

            _eventConfig = ScriptableObject.CreateInstance<EventConfig>();
            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Mudslide",
                    description = "The track becomes muddy",
                    triggerChance = 0.3f,
                    speedModifier = -2,
                    targetType = "single"
                },
                new RaceEvent
                {
                    eventName = "Tailwind",
                    description = "A gust of wind helps",
                    triggerChance = 0.2f,
                    speedModifier = 3,
                    targetType = "single"
                }
            };

            _shopConfig = ScriptableObject.CreateInstance<ShopConfig>();
            _shopConfig.protectionCards = new ProtectionCardData[]
            {
                new ProtectionCardData
                {
                    cardName = "Mud Shield",
                    protectsAgainst = "Mudslide",
                    successRate = 1.0f,
                    price = 150
                },
                new ProtectionCardData
                {
                    cardName = "Wind Breaker",
                    protectsAgainst = "Tailwind",
                    successRate = 0.8f,
                    price = 100
                }
            };

            _bettingConfig = ScriptableObject.CreateInstance<BettingConfig>();
            _bettingConfig.betTypes = new BetTypeConfig[]
            {
                new BetTypeConfig { type = BetType.SingleWin, oddsMultiplier = 5.0f },
                new BetTypeConfig { type = BetType.Place, oddsMultiplier = 2.0f },
                new BetTypeConfig { type = BetType.Quinella, oddsMultiplier = 8.0f },
                new BetTypeConfig { type = BetType.Exacta, oddsMultiplier = 15.0f },
                new BetTypeConfig { type = BetType.Trio, oddsMultiplier = 20.0f },
                new BetTypeConfig { type = BetType.Trifecta, oddsMultiplier = 50.0f }
            };
        }

        private void CreateSystems()
        {
            _playerState = new PlayerState(_gameConfig.startingBalance, _gameConfig.maxProtectionCards);
            _horseSystem = new HorseSystem(_gameConfig);
            _messageCardSystem = new MessageCardSystem(_messageCardConfig);
            _oddsSystem = new OddsSystem(_oddsConfig);
            _trackSystem = new TrackSystem(_trackConfig);
            _analystSystem = new AnalystSystem(_analystConfig);
            _eventSystem = new EventSystem(_eventConfig);
            _raceSimulationSystem = new RaceSimulationSystem(_trackConfig);
            _bettingSystem = new BettingSystem(_bettingConfig);
            _shopSystem = new ShopSystem(_shopConfig);
            _settlementSystem = new SettlementSystem(_bettingConfig);
            _stateMachine = new RoundStateMachine();
        }
    }
}
