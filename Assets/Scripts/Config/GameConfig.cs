using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "HorseBetting/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public int horseCount = 8;
        public int baseSpeed = 30;
        public int startingBalance = 1000;
        public int maxProtectionCards = 3;
        public int messageCardsPerRound = 3;
    }
}
