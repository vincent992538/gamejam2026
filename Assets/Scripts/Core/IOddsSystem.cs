using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface IOddsSystem : IGameSystem
    {
        float[] CalculateOdds(HorseData[] horses, int bettingRound);
        void UpdateOddsAfterBetting(int bettingRound);
    }
}
