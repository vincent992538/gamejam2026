using System;
using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class AnalystSystemTests
    {
        private AnalystConfig _config;
        private AnalystSystem _system;
        private HorseData[] _horses;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<AnalystConfig>();
            _config.seniorPrice = 200;
            _config.seniorAccuracy = 0.8f;
            _config.juniorPrice = 80;
            _config.juniorAccuracy = 0.5f;

            _system = new AnalystSystem(_config);

            // Create standard test horses with known bonuses
            _horses = new HorseData[8];
            for (int i = 0; i < 8; i++)
            {
                _horses[i] = new HorseData
                {
                    index = i,
                    baseSpeed = 30,
                    hiddenBonus = 7 - i, // Horse 0 has bonus 7, Horse 7 has bonus 0
                    displayName = $"Horse {i + 1}"
                };
            }
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        // Requirement 6.1: Support Senior and Junior analyst types
        [Test]
        public void GenerateIntel_ReturnsBothAnalystTypes()
        {
            AnalystIntel[] intel = _system.GenerateIntel(_horses);

            Assert.AreEqual(2, intel.Length);
            Assert.AreEqual(AnalystType.Senior, intel[0].type);
            Assert.AreEqual(AnalystType.Junior, intel[1].type);
        }

        // Requirement 6.2: Senior price > Junior price
        [Test]
        public void GenerateIntel_SeniorPriceHigherThanJunior()
        {
            AnalystIntel[] intel = _system.GenerateIntel(_horses);

            Assert.Greater(intel[0].price, intel[1].price);
        }

        // Requirement 6.6: Read prices and accuracy from Config
        [Test]
        public void GenerateIntel_PricesMatchConfig()
        {
            AnalystIntel[] intel = _system.GenerateIntel(_horses);

            Assert.AreEqual(_config.seniorPrice, intel[0].price);
            Assert.AreEqual(_config.juniorPrice, intel[1].price);
        }

        // Requirement 6.4: Use accuracy to determine if intel is true or misleading
        [Test]
        public void GenerateIntel_WithFullAccuracy_AlwaysAccurate()
        {
            _config.seniorAccuracy = 1.0f;

            bool allAccurate = true;
            for (int i = 0; i < 50; i++)
            {
                AnalystIntel[] intel = _system.GenerateIntel(_horses);
                if (!intel[0].isAccurate)
                {
                    allAccurate = false;
                    break;
                }
            }

            Assert.IsTrue(allAccurate, "Senior with 1.0 accuracy should always be accurate");
        }

        [Test]
        public void GenerateIntel_WithZeroAccuracy_AlwaysInaccurate()
        {
            _config.seniorAccuracy = 0.0f;

            bool allInaccurate = true;
            for (int i = 0; i < 50; i++)
            {
                AnalystIntel[] intel = _system.GenerateIntel(_horses);
                if (intel[0].isAccurate)
                {
                    allInaccurate = false;
                    break;
                }
            }

            Assert.IsTrue(allInaccurate, "Senior with 0.0 accuracy should never be accurate");
        }

        // Requirement 6.3: Senior accuracy > Junior accuracy
        [Test]
        public void GenerateIntel_SeniorMoreAccurateThanJunior_Statistically()
        {
            int seniorAccurateCount = 0;
            int juniorAccurateCount = 0;
            int iterations = 500;

            for (int i = 0; i < iterations; i++)
            {
                AnalystIntel[] intel = _system.GenerateIntel(_horses);
                if (intel[0].isAccurate) seniorAccurateCount++;
                if (intel[1].isAccurate) juniorAccurateCount++;
            }

            float seniorRate = (float)seniorAccurateCount / iterations;
            float juniorRate = (float)juniorAccurateCount / iterations;

            Assert.Greater(seniorRate, juniorRate,
                $"Senior accuracy rate ({seniorRate:F2}) should be higher than junior ({juniorRate:F2})");
        }

        [Test]
        public void GenerateIntel_ContentIsNotNullOrEmpty()
        {
            AnalystIntel[] intel = _system.GenerateIntel(_horses);

            Assert.IsFalse(string.IsNullOrEmpty(intel[0].content));
            Assert.IsFalse(string.IsNullOrEmpty(intel[1].content));
        }

        // Requirement 6.5: Deduct price on purchase, reveal intel
        [Test]
        public void BuyIntel_Senior_WithSufficientBalance_Succeeds()
        {
            PurchaseResult result = _system.BuyIntel(AnalystType.Senior, 500);

            Assert.IsTrue(result.success);
            Assert.AreEqual(300, result.remainingBalance); // 500 - 200
            Assert.IsNull(result.errorMessage);
        }

        [Test]
        public void BuyIntel_Junior_WithSufficientBalance_Succeeds()
        {
            PurchaseResult result = _system.BuyIntel(AnalystType.Junior, 100);

            Assert.IsTrue(result.success);
            Assert.AreEqual(20, result.remainingBalance); // 100 - 80
            Assert.IsNull(result.errorMessage);
        }

        [Test]
        public void BuyIntel_WithExactBalance_Succeeds()
        {
            PurchaseResult result = _system.BuyIntel(AnalystType.Senior, 200);

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, result.remainingBalance);
        }

        // Requirement 6.7: Block purchase if balance insufficient
        [Test]
        public void BuyIntel_Senior_WithInsufficientBalance_Fails()
        {
            PurchaseResult result = _system.BuyIntel(AnalystType.Senior, 199);

            Assert.IsFalse(result.success);
            Assert.AreEqual(199, result.remainingBalance);
            Assert.IsNotNull(result.errorMessage);
        }

        [Test]
        public void BuyIntel_Junior_WithInsufficientBalance_Fails()
        {
            PurchaseResult result = _system.BuyIntel(AnalystType.Junior, 79);

            Assert.IsFalse(result.success);
            Assert.AreEqual(79, result.remainingBalance);
            Assert.IsNotNull(result.errorMessage);
        }

        [Test]
        public void BuyIntel_WithZeroBalance_Fails()
        {
            PurchaseResult result = _system.BuyIntel(AnalystType.Senior, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual(0, result.remainingBalance);
        }

        [Test]
        public void Reset_ClearsGeneratedIntel()
        {
            _system.GenerateIntel(_horses);
            _system.Reset();

            Assert.IsNull(_system.GetIntel(AnalystType.Senior));
            Assert.IsNull(_system.GetIntel(AnalystType.Junior));
        }

        [Test]
        public void Initialize_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _system.Initialize());
        }

        [Test]
        public void GenerateIntel_ClearsPreviousIntel()
        {
            _system.GenerateIntel(_horses);
            AnalystIntel[] firstIntel = new AnalystIntel[] { _system.GetIntel(AnalystType.Senior).Value };

            _system.GenerateIntel(_horses);
            AnalystIntel? secondIntel = _system.GetIntel(AnalystType.Senior);

            // Intel should be regenerated (not necessarily different content, but the method should not fail)
            Assert.IsNotNull(secondIntel);
        }

        [Test]
        public void BuyIntel_UsesConfigPrices()
        {
            _config.seniorPrice = 300;
            _config.juniorPrice = 100;

            PurchaseResult seniorResult = _system.BuyIntel(AnalystType.Senior, 500);
            PurchaseResult juniorResult = _system.BuyIntel(AnalystType.Junior, 500);

            Assert.AreEqual(200, seniorResult.remainingBalance); // 500 - 300
            Assert.AreEqual(400, juniorResult.remainingBalance); // 500 - 100
        }
    }
}
