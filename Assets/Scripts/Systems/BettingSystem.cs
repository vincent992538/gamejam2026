using System.Collections.Generic;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class BettingSystem : IBettingSystem
    {
        private readonly BettingConfig _config;
        private readonly List<Bet> _activeBets;

        public BettingSystem(BettingConfig config)
        {
            _config = config;
            _activeBets = new List<Bet>();
        }

        public void Initialize()
        {
            // No-op: betting is driven by player actions
        }

        public void Reset()
        {
            _activeBets.Clear();
        }

        public BetResult PlaceBet(Bet bet, int playerBalance)
        {
            // Validate amount > 0
            if (bet.amount <= 0)
            {
                return new BetResult
                {
                    success = false,
                    errorMessage = "Bet amount must be greater than zero.",
                    remainingBalance = playerBalance
                };
            }

            // Validate amount ≤ playerBalance (Req 9.3, 9.4)
            if (bet.amount > playerBalance)
            {
                return new BetResult
                {
                    success = false,
                    errorMessage = "Insufficient balance to place this bet.",
                    remainingBalance = playerBalance
                };
            }

            // Store the bet (Req 9.5 - allow multiple bets per round)
            _activeBets.Add(bet);

            // Deduct from balance and return success (Req 9.6)
            return new BetResult
            {
                success = true,
                errorMessage = null,
                remainingBalance = playerBalance - bet.amount
            };
        }

        public Bet[] GetActiveBets()
        {
            return _activeBets.ToArray();
        }

        public void ClearBets()
        {
            _activeBets.Clear();
        }

        /// <summary>
        /// Gets the odds multiplier for a given bet type from config.
        /// Returns 0 if bet type not found in config.
        /// </summary>
        public float GetOddsMultiplier(BetType type)
        {
            if (_config.betTypes == null)
                return 0f;

            for (int i = 0; i < _config.betTypes.Length; i++)
            {
                if (_config.betTypes[i].type == type)
                    return _config.betTypes[i].oddsMultiplier;
            }

            return 0f;
        }
    }
}
