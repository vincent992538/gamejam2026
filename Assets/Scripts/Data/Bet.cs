namespace HorseBetting.Data
{
    public struct Bet
    {
        public BetType type;
        public int amount;
        public int[] selectedHorses;  // 所選馬匹索引
        public int bettingRound;      // 在第幾輪下注
        public float oddsAtBet;       // 下注時的賠率
    }
}
