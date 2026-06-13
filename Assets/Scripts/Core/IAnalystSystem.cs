using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface IAnalystSystem : IGameSystem
    {
        AnalystIntel[] GenerateIntel(HorseData[] horses);
        PurchaseResult BuyIntel(AnalystType type, int playerBalance);
    }
}
