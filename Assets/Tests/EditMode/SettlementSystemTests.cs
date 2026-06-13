using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class SettlementSystemTests
    {
        private BettingConfig _config;
        private SettlementSystem _system;

        // Standard ranking: Horse indices [7, 5, 3, 1, 6, 4, 2, 0]
        // meaning horse 7 is 1st, horse 5 is 2nd, horse 3 is 3rd, etc.
        private int[] _standardRanking;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BettingConfig>();
            _config.betTypes = new BetTypeConfig[]
            {
                new BetTypeConfig { type = BetType.SingleWin, oddsMultiplier = 5.0f },
                new BetTypeConfig { type = BetType.Place, oddsMultiplier = 2.0f },
                new BetTypeConfig { type = BetType.Quinella, oddsMultiplier = 8.0f },
                new BetTypeConfig { type = BetType.Exacta, oddsMultiplier = 15.0f },
                new BetTypeConfig { type = BetType.Trio, oddsMultiplier = 20.0f },
                new BetTypeConfig { type = BetType.Trifecta, oddsMultiplier = 50.0f }
            };

            _system = new SettlementSystem(_config);
            _standardRanking = new int[] { 7, 5, 3, 1, 6, 4, 2, 0 };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // --- SingleWin Tests (Req 11.3) ---

        [Test]
        public void SingleWin_CorrectPick_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 7 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(1, result.betResults.Length);
            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(500, result.betResults[0].payout); // 100 × 5.0
        }

        [Test]
        public void SingleWin_WrongPick_Loses()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 5 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsFalse(result.betResults[0].won);
            Assert.AreEqual(0, result.betResults[0].payout);
        }

        // --- Place Tests (Req 11.3) ---

        [Test]
        public void Place_FirstPlace_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Place, 100, new[] { 7 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(200, result.betResults[0].payout); // 100 × 2.0
        }

        [Test]
        public void Place_SecondPlace_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Place, 100, new[] { 5 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(200, result.betResults[0].payout);
        }

        [Test]
        public void Place_ThirdPlace_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Place, 100, new[] { 3 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(200, result.betResults[0].payout);
        }

        [Test]
        public void Place_FourthPlace_Loses()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Place, 100, new[] { 1 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsFalse(result.betResults[0].won);
            Assert.AreEqual(0, result.betResults[0].payout);
        }

        // --- Quinella Tests (Req 11.3) ---

        [Test]
        public void Quinella_CorrectPairInOrder_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Quinella, 100, new[] { 7, 5 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(800, result.betResults[0].payout); // 100 × 8.0
        }

        [Test]
        public void Quinella_CorrectPairReverseOrder_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Quinella, 100, new[] { 5, 7 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(800, result.betResults[0].payout);
        }

        [Test]
        public void Quinella_WrongPair_Loses()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Quinella, 100, new[] { 7, 3 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsFalse(result.betResults[0].won);
            Assert.AreEqual(0, result.betResults[0].payout);
        }

        // --- Exacta Tests (Req 11.3) ---

        [Test]
        public void Exacta_CorrectOrder_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Exacta, 100, new[] { 7, 5 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(1500, result.betResults[0].payout); // 100 × 15.0
        }

        [Test]
        public void Exacta_WrongOrder_Loses()
        {
            // Reverse order should lose for Exacta (unlike Quinella)
            var bets = new Bet[]
            {
                CreateBet(BetType.Exacta, 100, new[] { 5, 7 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsFalse(result.betResults[0].won);
            Assert.AreEqual(0, result.betResults[0].payout);
        }

        // --- Trio Tests (Req 11.3) ---

        [Test]
        public void Trio_CorrectThreeInOrder_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Trio, 100, new[] { 7, 5, 3 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(2000, result.betResults[0].payout); // 100 × 20.0
        }

        [Test]
        public void Trio_CorrectThreeShuffled_Wins()
        {
            // Trio doesn't care about order
            var bets = new Bet[]
            {
                CreateBet(BetType.Trio, 100, new[] { 3, 7, 5 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(2000, result.betResults[0].payout);
        }

        [Test]
        public void Trio_WrongSelection_Loses()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Trio, 100, new[] { 7, 5, 1 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsFalse(result.betResults[0].won);
            Assert.AreEqual(0, result.betResults[0].payout);
        }

        // --- Trifecta Tests (Req 11.3) ---

        [Test]
        public void Trifecta_CorrectExactOrder_Wins()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Trifecta, 100, new[] { 7, 5, 3 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsTrue(result.betResults[0].won);
            Assert.AreEqual(5000, result.betResults[0].payout); // 100 × 50.0
        }

        [Test]
        public void Trifecta_WrongOrder_Loses()
        {
            // Trifecta requires exact order (unlike Trio)
            var bets = new Bet[]
            {
                CreateBet(BetType.Trifecta, 100, new[] { 5, 7, 3 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.IsFalse(result.betResults[0].won);
            Assert.AreEqual(0, result.betResults[0].payout);
        }

        // --- Payout Calculation Tests (Req 11.4) ---

        [Test]
        public void Payout_EqualsAmountTimesOddsMultiplier()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 200, new[] { 7 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(1000, result.betResults[0].payout); // 200 × 5.0
        }

        [Test]
        public void Payout_LargeAmount_CalculatesCorrectly()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.Trifecta, 500, new[] { 7, 5, 3 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(25000, result.betResults[0].payout); // 500 × 50.0
        }

        // --- Total Calculations (Req 11.6, 11.7) ---

        [Test]
        public void TotalWinnings_SumsAllPayouts()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 7 }),  // wins: 500
                CreateBet(BetType.Place, 100, new[] { 5 }),      // wins: 200
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(700, result.totalWinnings);
        }

        [Test]
        public void TotalLoss_SumsAllBetAmounts()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 7 }),
                CreateBet(BetType.Place, 150, new[] { 5 }),
                CreateBet(BetType.Exacta, 200, new[] { 0, 1 }),  // loses
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(450, result.totalLoss); // 100 + 150 + 200
        }

        [Test]
        public void NetProfit_WhenWinning_IsPositive()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 7 }),  // wins: 500
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(500, result.totalWinnings);
            Assert.AreEqual(100, result.totalLoss);
            Assert.AreEqual(400, result.netProfit); // 500 - 100
        }

        [Test]
        public void NetProfit_WhenLosing_IsNegative()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 0 }),  // loses
                CreateBet(BetType.Place, 200, new[] { 0 }),      // loses
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(0, result.totalWinnings);
            Assert.AreEqual(300, result.totalLoss);
            Assert.AreEqual(-300, result.netProfit);
        }

        [Test]
        public void NetProfit_MixedWinsAndLosses()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 7 }),  // wins: 500
                CreateBet(BetType.SingleWin, 200, new[] { 0 }),  // loses
                CreateBet(BetType.Place, 50, new[] { 3 }),       // wins: 100
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(600, result.totalWinnings);
            Assert.AreEqual(350, result.totalLoss);
            Assert.AreEqual(250, result.netProfit);
        }

        // --- Edge Cases ---

        [Test]
        public void EmptyBetsArray_ReturnsZeroResults()
        {
            var bets = new Bet[0];

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(0, result.betResults.Length);
            Assert.AreEqual(0, result.totalWinnings);
            Assert.AreEqual(0, result.totalLoss);
            Assert.AreEqual(0, result.netProfit);
        }

        [Test]
        public void AllBetsLose_TotalWinningsIsZero()
        {
            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 0 }),
                CreateBet(BetType.SingleWin, 100, new[] { 1 }),
                CreateBet(BetType.SingleWin, 100, new[] { 2 }),
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, _config);

            Assert.AreEqual(0, result.totalWinnings);
            Assert.AreEqual(300, result.totalLoss);
            Assert.AreEqual(-300, result.netProfit);
        }

        // --- IGameSystem Tests ---

        [Test]
        public void Initialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _system.Initialize());
        }

        [Test]
        public void Reset_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _system.Reset());
        }

        // --- Uses config param over injected config ---

        [Test]
        public void CalculateSettlement_UsesPassedConfig()
        {
            var altConfig = ScriptableObject.CreateInstance<BettingConfig>();
            altConfig.betTypes = new BetTypeConfig[]
            {
                new BetTypeConfig { type = BetType.SingleWin, oddsMultiplier = 10.0f }
            };

            var bets = new Bet[]
            {
                CreateBet(BetType.SingleWin, 100, new[] { 7 })
            };

            SettlementResult result = _system.CalculateSettlement(_standardRanking, bets, altConfig);

            Assert.AreEqual(1000, result.betResults[0].payout); // 100 × 10.0

            Object.DestroyImmediate(altConfig);
        }

        // Helper method
        private Bet CreateBet(BetType type, int amount, int[] selectedHorses, int round = 1, float odds = 3.0f)
        {
            return new Bet
            {
                type = type,
                amount = amount,
                selectedHorses = selectedHorses,
                bettingRound = round,
                oddsAtBet = odds
            };
        }
    }
}
