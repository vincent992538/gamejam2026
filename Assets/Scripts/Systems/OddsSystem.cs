using System;
using System.Linq;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class OddsSystem : IOddsSystem
    {
        private readonly OddsConfig _config;
        private float[] _currentOdds;
        private HorseData[] _currentHorses;

        public OddsSystem(OddsConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            // No-op: setup handled via CalculateOdds
        }

        public void Reset()
        {
            _currentOdds = null;
            _currentHorses = null;
        }

        public float[] CalculateOdds(HorseData[] horses, int bettingRound)
        {
            if (horses == null)
                throw new ArgumentNullException(nameof(horses));

            _currentHorses = horses;

            // Sort horses by Final_Score (baseSpeed + hiddenBonus) descending
            var sorted = horses
                .Select((h, originalIndex) => new { Horse = h, OriginalIndex = originalIndex })
                .OrderByDescending(x => x.Horse.baseSpeed + x.Horse.hiddenBonus)
                .ThenBy(x => x.Horse.index) // tie-break: lower index ranks higher
                .ToArray();

            // Assign odds: rankOdds[0] to highest scored horse, etc.
            float[] odds = new float[horses.Length];
            for (int rank = 0; rank < sorted.Length; rank++)
            {
                float baseOdd = rank < _config.rankOdds.Length
                    ? _config.rankOdds[rank]
                    : _config.rankOdds[_config.rankOdds.Length - 1];

                odds[sorted[rank].OriginalIndex] = baseOdd * _config.baseMultiplier;
            }

            // Apply round penalty
            float penalty = GetRoundPenalty(bettingRound);
            for (int i = 0; i < odds.Length; i++)
            {
                odds[i] *= penalty;
            }

            _currentOdds = odds;
            return odds;
        }

        public void UpdateOddsAfterBetting(int bettingRound)
        {
            if (_currentOdds == null || _currentHorses == null)
                return; // silently skip if not initialized

            // Recalculate odds for the next round
            int nextRound = bettingRound + 1;
            CalculateOdds(_currentHorses, nextRound);
        }

        private float GetRoundPenalty(int bettingRound)
        {
            switch (bettingRound)
            {
                case 1:
                    return 1.0f; // No penalty for first round
                case 2:
                    return _config.round2Penalty;
                case 3:
                    return _config.round3Penalty;
                default:
                    return _config.round3Penalty; // Cap at worst penalty
            }
        }
    }
}
