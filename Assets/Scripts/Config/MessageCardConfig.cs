using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "MessageCardConfig", menuName = "HorseBetting/MessageCardConfig")]
    public class MessageCardConfig : ScriptableObject
    {
        public MessageCardEntry[] entries;
    }

    [System.Serializable]
    public class MessageCardEntry
    {
        public int hiddenSpeedBonus;
        public string description;
    }
}
