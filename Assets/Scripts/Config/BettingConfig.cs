using UnityEngine;
using HorseBetting.Data;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "BettingConfig", menuName = "HorseBetting/BettingConfig")]
    public class BettingConfig : ScriptableObject
    {
        public BetTypeConfig[] betTypes;
    }

    [System.Serializable]
    public class BetTypeConfig
    {
        public BetType type;
        public float oddsMultiplier;
    }
}
