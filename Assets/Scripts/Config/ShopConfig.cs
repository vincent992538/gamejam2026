using UnityEngine;

namespace HorseBetting.Config
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "HorseBetting/ShopConfig")]
    public class ShopConfig : ScriptableObject
    {
        public ProtectionCardData[] protectionCards;
    }

    [System.Serializable]
    public class ProtectionCardData
    {
        public string cardName;
        public string protectsAgainst;
        public float successRate;
        public int price;
    }
}
