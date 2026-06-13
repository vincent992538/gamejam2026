using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "TrackConfig", menuName = "HorseBetting/TrackConfig")]
    public class TrackConfig : ScriptableObject
    {
        public TrackPreference[] horsePreferences;
    }

    [System.Serializable]
    public class TrackPreference
    {
        public int horseIndex;
        public int grassModifier;
        public int mudModifier;
        public int snowModifier;
    }
}
