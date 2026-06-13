using HorseBetting.Data;
using HorseBetting.Config;

namespace HorseBetting.Core
{
    public interface ISettlementSystem : IGameSystem
    {
        SettlementResult CalculateSettlement(int[] finalRanking, Bet[] activeBets, BettingConfig config);
    }
}
