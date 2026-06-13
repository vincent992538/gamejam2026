using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "EventConfig", menuName = "HorseBetting/EventConfig")]
    public class EventConfig : ScriptableObject
    {
        public RaceEvent[] events;
    }

    [System.Serializable]
    public class RaceEvent
    {
        public string eventName;
        public string description;
        public float triggerChance;
        public int speedModifier;
        public string targetType; // "single", "all"
    }
}
