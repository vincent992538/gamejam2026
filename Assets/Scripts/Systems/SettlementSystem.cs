using System.Linq;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class SettlementSystem : ISettlementSystem
    {
        private readonly BettingConfig _config;

        public SettlementSystem(BettingConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            // No-op: settlement is triggered after race completes
        }

        public void Reset()
        {
            // No persistent state to reset
        }

        public SettlementResult CalculateSettlement(int[] finalRanking, Bet[] activeBets, BettingConfig config)
        {
            var configToUse = config ?? _config;
            var betResults = new BetSettlement[activeBets.Length];
            int totalWinnings = 0;
            int totalLoss = 0;

            for (int i = 0; i < activeBets.Length; i++)
            {
                var bet = activeBets[i];
                bool won = EvaluateBet(bet, finalRanking);
                int payout = 0;

                if (won)
                {
                    float multiplier = GetOddsMultiplier(bet.type, configToUse);
                    payout = (int)(bet.amount * multiplier);
                    totalWinnings += payout;
                }

                totalLoss += bet.amount;

                betResults[i] = new BetSettlement
                {
                    bet = bet,
                    won = won,
                    payout = payout
                };
            }

            return new SettlementResult
            {
                betResults = betResults,
                totalWinnings = totalWinnings,
                totalLoss = totalLoss,
                netProfit = totalWinnings - totalLoss
            };
        }

        /// <summary>
        /// Evaluates whether a bet won based on the final ranking and bet type win conditions.
        /// </summary>
        private bool EvaluateBet(Bet bet, int[] finalRanking)
        {
            switch (bet.type)
            {
                case BetType.SingleWin:
                    // Wins iff selectedHorses[0] == ranking[0]
                    return bet.selectedHorses[0] == finalRanking[0];

                case BetType.Place:
                    // Wins iff selectedHorses[0] is in top 3
                    return finalRanking.Length >= 3 &&
                           (bet.selectedHorses[0] == finalRanking[0] ||
                            bet.selectedHorses[0] == finalRanking[1] ||
                            bet.selectedHorses[0] == finalRanking[2]);

                case BetType.Quinella:
                    // Wins iff {selectedHorses[0], selectedHorses[1]} == {ranking[0], ranking[1]} (order irrelevant)
                    return finalRanking.Length >= 2 &&
                           ((bet.selectedHorses[0] == finalRanking[0] && bet.selectedHorses[1] == finalRanking[1]) ||
                            (bet.selectedHorses[0] == finalRanking[1] && bet.selectedHorses[1] == finalRanking[0]));

                case BetType.Exacta:
                    // Wins iff selectedHorses[0] == ranking[0] AND selectedHorses[1] == ranking[1]
                    return finalRanking.Length >= 2 &&
                           bet.selectedHorses[0] == finalRanking[0] &&
                           bet.selectedHorses[1] == finalRanking[1];

                case BetType.Trio:
                    // Wins iff {selectedHorses} == {ranking[0], ranking[1], ranking[2]} (order irrelevant)
                    if (finalRanking.Length < 3 || bet.selectedHorses.Length < 3)
                        return false;
                    var selectedSet = new System.Collections.Generic.HashSet<int>(bet.selectedHorses.Take(3));
                    var topThreeSet = new System.Collections.Generic.HashSet<int>(new[] { finalRanking[0], finalRanking[1], finalRanking[2] });
                    return selectedSet.SetEquals(topThreeSet);

                case BetType.Trifecta:
                    // Wins iff selectedHorses[i] == ranking[i] for i = 0,1,2
                    return finalRanking.Length >= 3 &&
                           bet.selectedHorses.Length >= 3 &&
                           bet.selectedHorses[0] == finalRanking[0] &&
                           bet.selectedHorses[1] == finalRanking[1] &&
                           bet.selectedHorses[2] == finalRanking[2];

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the odds multiplier for a given bet type from config.
        /// </summary>
        private float GetOddsMultiplier(BetType type, BettingConfig config)
        {
            if (config.betTypes == null)
                return 0f;

            for (int i = 0; i < config.betTypes.Length; i++)
            {
                if (config.betTypes[i].type == type)
                    return config.betTypes[i].oddsMultiplier;
            }

            return 0f;
        }
    }
}
