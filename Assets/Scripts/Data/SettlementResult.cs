namespace HorseBetting.Data
{
    public struct SettlementResult
    {
        public BetSettlement[] betResults;
        public int totalWinnings;
        public int totalLoss;
        public int netProfit;
    }
}
