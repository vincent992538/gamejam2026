using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class ShopSystemTests
    {
        private ShopConfig _config;
        private ShopSystem _system;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ShopConfig>();
            _config.protectionCards = new ProtectionCardData[]
            {
                new ProtectionCardData
                {
                    cardName = "Rain Shield",
                    protectsAgainst = "HeavyRain",
                    successRate = 0.8f,
                    price = 100
                },
                new ProtectionCardData
                {
                    cardName = "Wind Guard",
                    protectsAgainst = "StrongWind",
                    successRate = 0.6f,
                    price = 150
                },
                new ProtectionCardData
                {
                    cardName = "Mud Boots",
                    protectsAgainst = "MuddyTrack",
                    successRate = 0.9f,
                    price = 200
                }
            };

            _system = new ShopSystem(_config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // === GetAvailableItems Tests ===

        // Requirement 10.2: Read shop items from Config
        [Test]
        public void GetAvailableItems_ReturnsAllConfigItems()
        {
            ShopItem[] items = _system.GetAvailableItems();

            Assert.AreEqual(3, items.Length);
            Assert.AreEqual("Rain Shield", items[0].data.cardName);
            Assert.AreEqual("Wind Guard", items[1].data.cardName);
            Assert.AreEqual("Mud Boots", items[2].data.cardName);
        }

        [Test]
        public void GetAvailableItems_PreservesCardData()
        {
            ShopItem[] items = _system.GetAvailableItems();

            Assert.AreEqual("HeavyRain", items[0].data.protectsAgainst);
            Assert.AreEqual(0.8f, items[0].data.successRate);
            Assert.AreEqual(100, items[0].data.price);
        }

        [Test]
        public void GetAvailableItems_SetsCanAffordTrue()
        {
            ShopItem[] items = _system.GetAvailableItems();

            foreach (var item in items)
            {
                Assert.IsTrue(item.canAfford);
            }
        }

        [Test]
        public void GetAvailableItems_EmptyConfig_ReturnsEmptyArray()
        {
            _config.protectionCards = new ProtectionCardData[0];

            ShopItem[] items = _system.GetAvailableItems();

            Assert.AreEqual(0, items.Length);
        }

        [Test]
        public void GetAvailableItems_NullConfig_ReturnsEmptyArray()
        {
            _config.protectionCards = null;

            ShopItem[] items = _system.GetAvailableItems();

            Assert.AreEqual(0, items.Length);
        }

        // === BuyProtectionCard Tests ===

        // Requirement 10.6: Successful purchase deducts price
        [Test]
        public void BuyProtectionCard_WithSufficientBalanceAndSpace_Succeeds()
        {
            PurchaseResult result = _system.BuyProtectionCard(0, 500, 0);

            Assert.IsTrue(result.success);
            Assert.IsNull(result.errorMessage);
            Assert.AreEqual(400, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_DeductsCorrectPrice()
        {
            // Buy item at index 2 (Mud Boots, price 200)
            PurchaseResult result = _system.BuyProtectionCard(2, 500, 0);

            Assert.IsTrue(result.success);
            Assert.AreEqual(300, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_WithExactBalance_Succeeds()
        {
            // Buy item at index 0 (Rain Shield, price 100) with exactly 100 balance
            PurchaseResult result = _system.BuyProtectionCard(0, 100, 0);

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, result.remainingBalance);
        }

        // Requirement 10.3, 10.4: Max 3 cards - block purchase at limit
        [Test]
        public void BuyProtectionCard_AtCardLimit_Fails()
        {
            PurchaseResult result = _system.BuyProtectionCard(0, 500, 3);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Protection card limit reached.", result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_AboveCardLimit_Fails()
        {
            PurchaseResult result = _system.BuyProtectionCard(0, 500, 4);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Protection card limit reached.", result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_BelowCardLimit_Succeeds()
        {
            PurchaseResult result = _system.BuyProtectionCard(0, 500, 2);

            Assert.IsTrue(result.success);
            Assert.AreEqual(400, result.remainingBalance);
        }

        // Requirement 10.5: Insufficient balance blocks purchase
        [Test]
        public void BuyProtectionCard_InsufficientBalance_Fails()
        {
            // Try to buy item 0 (price 100) with only 50 balance
            PurchaseResult result = _system.BuyProtectionCard(0, 50, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Insufficient balance.", result.errorMessage);
            Assert.AreEqual(50, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_ZeroBalance_Fails()
        {
            PurchaseResult result = _system.BuyProtectionCard(0, 0, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Insufficient balance.", result.errorMessage);
            Assert.AreEqual(0, result.remainingBalance);
        }

        // Validation priority: card limit checked before balance
        [Test]
        public void BuyProtectionCard_AtLimitAndInsufficientBalance_ReportsCardLimit()
        {
            PurchaseResult result = _system.BuyProtectionCard(0, 50, 3);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Protection card limit reached.", result.errorMessage);
        }

        // Invalid item index
        [Test]
        public void BuyProtectionCard_InvalidIndex_Negative_Fails()
        {
            PurchaseResult result = _system.BuyProtectionCard(-1, 500, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Invalid item index.", result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_InvalidIndex_TooHigh_Fails()
        {
            PurchaseResult result = _system.BuyProtectionCard(5, 500, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Invalid item index.", result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        [Test]
        public void BuyProtectionCard_NullConfig_Fails()
        {
            _config.protectionCards = null;

            PurchaseResult result = _system.BuyProtectionCard(0, 500, 0);

            Assert.IsFalse(result.success);
            Assert.AreEqual("Invalid item index.", result.errorMessage);
            Assert.AreEqual(500, result.remainingBalance);
        }

        // === IGameSystem Tests ===

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
    }
}
