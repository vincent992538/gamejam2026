using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface IBettingSystem : IGameSystem
    {
        BetResult PlaceBet(Bet bet, int playerBalance);
        Bet[] GetActiveBets();
        void ClearBets();
    }
}
