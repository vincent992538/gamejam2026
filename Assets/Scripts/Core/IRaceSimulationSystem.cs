using HorseBetting.Data;
using HorseBetting.Config;

namespace HorseBetting.Core
{
    public interface IRaceSimulationSystem : IGameSystem
    {
        RaceResult SimulateRace(HorseData[] horses, TrackType track, EventConfig eventConfig, ProtectionCard[] playerCards);
        int[] GetFinalRanking();
    }
}
