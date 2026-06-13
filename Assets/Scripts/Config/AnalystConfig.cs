using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "AnalystConfig", menuName = "HorseBetting/AnalystConfig")]
    public class AnalystConfig : ScriptableObject
    {
        public int seniorPrice = 200;
        public float seniorAccuracy = 0.8f;
        public int juniorPrice = 80;
        public float juniorAccuracy = 0.5f;
    }
}
