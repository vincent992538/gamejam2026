namespace HorseBetting.Data
{
    public struct StageEventResult
    {
        public int horseIndex;
        public string eventName;
        public int speedModifier;
        public bool wasProtected;  // 是否被防禦卡抵消
    }
}
