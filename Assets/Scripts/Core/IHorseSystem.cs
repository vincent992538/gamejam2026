using HorseBetting.Data;

namespace HorseBetting.Core
{
    public interface IHorseSystem : IGameSystem
    {
        HorseData[] GenerateHorses();
        int GetHiddenBonus(int horseIndex);
    }
}
