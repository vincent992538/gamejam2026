namespace HorseBetting.Data
{
    public struct HorseData
    {
        public int index;           // 0~7
        public int baseSpeed;       // 固定 30
        public int hiddenBonus;     // 0~7 隨機分配
        public string displayName;  // "Horse 1" ~ "Horse 8"
    }
}
