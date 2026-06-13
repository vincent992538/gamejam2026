using HorseBetting.Data;
using HorseBetting.Config;

namespace HorseBetting.Core
{
    public interface IEventSystem : IGameSystem
    {
        StageEventResult[] ProcessStageEvents(HorseData[] horses, int stage, ProtectionCard[] playerCards);
    }
}
