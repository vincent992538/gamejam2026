using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class BettingSystemTests
    {
        private BettingConfig _config;
        private BettingSystem _system;

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

            _system = new BettingSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // Requirement 9.3: Validate bet amount ≤ player balance
        [Test]
        public void PlaceBet_WithSufficientBalance_Succeeds()
        {
            var bet = CreateBet(BetType.SingleWin, 100, new[] { 0 });

            BetResult result = _system.PlaceBet(bet, 500);

            Assert.IsTrue(result.success);
            Assert.IsNull(result.errorMessage);
            Assert.AreEqual(400, result.remainingBalance);
        }

        [Test]
        public void PlaceBet_WithExactBalance_Succeeds()
        {
            var bet = CreateBet(BetType.SingleWin, 500, new[] { 0 });

            BetResult result = _system.PlaceBet(bet, 500);

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, result.remainingBalance);
        }

        // Requirement 9.4: Reject if amount > balance
        [Test]
        public void PlaceBet_WithInsufficientBalance_Fails()
        {
            var bet = CreateBet(BetType.SingleWin, 600, new[] { 0 });

            BetResult result = _system.PlaceBet(bet, 500);

            Assert.IsFalse(result.success);
            Assert.IsNotNull(result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        [Test]
        public void PlaceBet_WithZeroBalance_Fails()
        {
            var bet = CreateBet(BetType.SingleWin, 100, new[] { 0 });

            BetResult result = _system.PlaceBet(bet, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual(0, result.remainingBalance);
        }

        // Validate amount > 0
        [Test]
        public void PlaceBet_WithZeroAmount_Fails()
        {
            var bet = CreateBet(BetType.SingleWin, 0, new[] { 0 });

            BetResult result = _system.PlaceBet(bet, 500);

            Assert.IsFalse(result.success);
            Assert.IsNotNull(result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        [Test]
        public void PlaceBet_WithNegativeAmount_Fails()
        {
            var bet = CreateBet(BetType.SingleWin, -50, new[] { 0 });

            BetResult result = _system.PlaceBet(bet, 500);

            Assert.IsFalse(result.success);
            Assert.AreEqual(500, result.remainingBalance);
        }

        // Requirement 9.6: Immediately deduct bet amount from balance
        [Test]
        public void PlaceBet_DeductsAmountFromBalance()
        {
            var bet = CreateBet(BetType.Place, 200, new[] { 3 });

            BetResult result = _system.PlaceBet(bet, 1000);

            Assert.AreEqual(800, result.remainingBalance);
        }

        // Requirement 9.5: Allow multiple bets per round
        [Test]
        public void PlaceBet_MultipleBets_AllStored()
        {
            var bet1 = CreateBet(BetType.SingleWin, 100, new[] { 0 });
            var bet2 = CreateBet(BetType.Place, 50, new[] { 1 });
            var bet3 = CreateBet(BetType.Quinella, 75, new[] { 2, 3 });

            _system.PlaceBet(bet1, 1000);
            _system.PlaceBet(bet2, 900);
            _system.PlaceBet(bet3, 850);

            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(3, activeBets.Length);
        }

        // Requirement 9.1: Support all 6 bet types
        [Test]
        public void PlaceBet_SingleWin_Accepted()
        {
            var bet = CreateBet(BetType.SingleWin, 100, new[] { 0 });
            BetResult result = _system.PlaceBet(bet, 500);
            Assert.IsTrue(result.success);
        }

        [Test]
        public void PlaceBet_Place_Accepted()
        {
            var bet = CreateBet(BetType.Place, 100, new[] { 1 });
            BetResult result = _system.PlaceBet(bet, 500);
            Assert.IsTrue(result.success);
        }

        [Test]
        public void PlaceBet_Quinella_Accepted()
        {
            var bet = CreateBet(BetType.Quinella, 100, new[] { 0, 1 });
            BetResult result = _system.PlaceBet(bet, 500);
            Assert.IsTrue(result.success);
        }

        [Test]
        public void PlaceBet_Exacta_Accepted()
        {
            var bet = CreateBet(BetType.Exacta, 100, new[] { 0, 1 });
            BetResult result = _system.PlaceBet(bet, 500);
            Assert.IsTrue(result.success);
        }

        [Test]
        public void PlaceBet_Trio_Accepted()
        {
            var bet = CreateBet(BetType.Trio, 100, new[] { 0, 1, 2 });
            BetResult result = _system.PlaceBet(bet, 500);
            Assert.IsTrue(result.success);
        }

        [Test]
        public void PlaceBet_Trifecta_Accepted()
        {
            var bet = CreateBet(BetType.Trifecta, 100, new[] { 0, 1, 2 });
            BetResult result = _system.PlaceBet(bet, 500);
            Assert.IsTrue(result.success);
        }

        // GetActiveBets tests
        [Test]
        public void GetActiveBets_Initially_ReturnsEmptyArray()
        {
            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(0, activeBets.Length);
        }

        [Test]
        public void GetActiveBets_AfterPlacingBets_ReturnsAllBets()
        {
            var bet1 = CreateBet(BetType.SingleWin, 100, new[] { 0 });
            var bet2 = CreateBet(BetType.Place, 50, new[] { 1 });

            _system.PlaceBet(bet1, 1000);
            _system.PlaceBet(bet2, 900);

            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(2, activeBets.Length);
            Assert.AreEqual(BetType.SingleWin, activeBets[0].type);
            Assert.AreEqual(BetType.Place, activeBets[1].type);
        }

        [Test]
        public void GetActiveBets_FailedBet_NotStored()
        {
            var bet = CreateBet(BetType.SingleWin, 600, new[] { 0 });

            _system.PlaceBet(bet, 500);

            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(0, activeBets.Length);
        }

        // ClearBets tests
        [Test]
        public void ClearBets_RemovesAllBets()
        {
            var bet1 = CreateBet(BetType.SingleWin, 100, new[] { 0 });
            var bet2 = CreateBet(BetType.Place, 50, new[] { 1 });

            _system.PlaceBet(bet1, 1000);
            _system.PlaceBet(bet2, 900);

            _system.ClearBets();

            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(0, activeBets.Length);
        }

        // Requirement 9.2: Read odds multipliers from Config
        [Test]
        public void GetOddsMultiplier_ReturnsConfigValues()
        {
            Assert.AreEqual(5.0f, _system.GetOddsMultiplier(BetType.SingleWin));
            Assert.AreEqual(2.0f, _system.GetOddsMultiplier(BetType.Place));
            Assert.AreEqual(8.0f, _system.GetOddsMultiplier(BetType.Quinella));
            Assert.AreEqual(15.0f, _system.GetOddsMultiplier(BetType.Exacta));
            Assert.AreEqual(20.0f, _system.GetOddsMultiplier(BetType.Trio));
            Assert.AreEqual(50.0f, _system.GetOddsMultiplier(BetType.Trifecta));
        }

        // IGameSystem tests
        [Test]
        public void Initialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _system.Initialize());
        }

        [Test]
        public void Reset_ClearsAllBets()
        {
            var bet = CreateBet(BetType.SingleWin, 100, new[] { 0 });
            _system.PlaceBet(bet, 1000);

            _system.Reset();

            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(0, activeBets.Length);
        }

        // Bet data preserved correctly
        [Test]
        public void PlaceBet_PreservesBetData()
        {
            var bet = new Bet
            {
                type = BetType.Exacta,
                amount = 200,
                selectedHorses = new[] { 3, 5 },
                bettingRound = 2,
                oddsAtBet = 12.5f
            };

            _system.PlaceBet(bet, 1000);

            Bet[] activeBets = _system.GetActiveBets();
            Assert.AreEqual(1, activeBets.Length);
            Assert.AreEqual(BetType.Exacta, activeBets[0].type);
            Assert.AreEqual(200, activeBets[0].amount);
            Assert.AreEqual(new[] { 3, 5 }, activeBets[0].selectedHorses);
            Assert.AreEqual(2, activeBets[0].bettingRound);
            Assert.AreEqual(12.5f, activeBets[0].oddsAtBet);
        }

        // Helper method to create test bets
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
