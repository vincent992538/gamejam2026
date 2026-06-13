using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "OddsConfig", menuName = "HorseBetting/OddsConfig")]
    public class OddsConfig : ScriptableObject
    {
        public float baseMultiplier = 1.0f;
        public float[] rankOdds = { 1.5f, 2.0f, 3.0f, 5.0f, 8.0f, 12.0f, 20.0f, 40.0f };
        public float round2Penalty = 0.8f;
        public float round3Penalty = 0.6f;
    }
}
