using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class ShopSystem : IShopSystem
    {
        private readonly ShopConfig _config;

        public ShopSystem(ShopConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            // No-op: shop is driven by player actions
        }

        public void Reset()
        {
            // No-op: shop state is stateless, driven by config each round
        }

        /// <summary>
        /// Returns all available shop items from config.
        /// canAfford is set to true by default since the interface takes no balance parameter.
        /// The caller (UI layer) is responsible for filtering based on player balance.
        /// Validates: Requirement 10.2
        /// </summary>
        public ShopItem[] GetAvailableItems()
        {
            if (_config.protectionCards == null || _config.protectionCards.Length == 0)
            {
                return new ShopItem[0];
            }

            var items = new ShopItem[_config.protectionCards.Length];
            for (int i = 0; i < _config.protectionCards.Length; i++)
            {
                items[i] = new ShopItem
                {
                    data = _config.protectionCards[i],
                    canAfford = true
                };
            }

            return items;
        }

        /// <summary>
        /// Attempts to purchase a protection card at the given item index.
        /// Validates card limit (max 3) and sufficient balance.
        /// Validates: Requirements 10.3, 10.4, 10.5, 10.6
        /// </summary>
        public PurchaseResult BuyProtectionCard(int itemIndex, int playerBalance, int currentCardCount)
        {
            // Validate item index
            if (_config.protectionCards == null || itemIndex < 0 || itemIndex >= _config.protectionCards.Length)
            {
                return new PurchaseResult
                {
                    success = false,
                    errorMessage = "Invalid item index.",
                    remainingBalance = playerBalance
                };
            }

            // Req 10.3, 10.4: Max 3 protection cards
            if (currentCardCount >= 3)
            {
                return new PurchaseResult
                {
                    success = false,
                    errorMessage = "Protection card limit reached.",
                    remainingBalance = playerBalance
                };
            }

            int price = _config.protectionCards[itemIndex].price;

            // Req 10.5: Insufficient balance
            if (playerBalance < price)
            {
                return new PurchaseResult
                {
                    success = false,
                    errorMessage = "Insufficient balance.",
                    remainingBalance = playerBalance
                };
            }

            // Req 10.6: Deduct price on successful purchase
            return new PurchaseResult
            {
                success = true,
                errorMessage = null,
                remainingBalance = playerBalance - price
            };
        }
    }
}
