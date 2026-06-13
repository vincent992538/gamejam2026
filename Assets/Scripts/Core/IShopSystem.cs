using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface IShopSystem : IGameSystem
    {
        ShopItem[] GetAvailableItems();
        PurchaseResult BuyProtectionCard(int itemIndex, int playerBalance, int currentCardCount);
    }
}
