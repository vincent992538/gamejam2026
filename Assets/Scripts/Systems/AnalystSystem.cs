using System;
using System.Collections.Generic;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class AnalystSystem : IAnalystSystem
    {
        private readonly AnalystConfig _config;
        private readonly Random _random;
        private Dictionary<AnalystType, AnalystIntel> _generatedIntel;

        public AnalystSystem(AnalystConfig config)
        {
            _config = config;
            _random = new Random();
            _generatedIntel = new Dictionary<AnalystType, AnalystIntel>();
        }

        public void Initialize()
        {
            // No-op: intel generation is triggered explicitly via GenerateIntel
        }

        public void Reset()
        {
            _generatedIntel.Clear();
        }

        public AnalystIntel[] GenerateIntel(HorseData[] horses)
        {
            _generatedIntel.Clear();

            // Sort horses by hiddenBonus descending to determine true rankings
            int[] sortedIndices = new int[horses.Length];
            for (int i = 0; i < horses.Length; i++)
                sortedIndices[i] = i;

            Array.Sort(sortedIndices, (a, b) => horses[b].hiddenBonus.CompareTo(horses[a].hiddenBonus));

            // Generate intel for Senior analyst
            AnalystIntel seniorIntel = GenerateIntelForType(
                AnalystType.Senior,
                horses,
                sortedIndices,
                _config.seniorAccuracy,
                _config.seniorPrice
            );
            _generatedIntel[AnalystType.Senior] = seniorIntel;

            // Generate intel for Junior analyst
            AnalystIntel juniorIntel = GenerateIntelForType(
                AnalystType.Junior,
                horses,
                sortedIndices,
                _config.juniorAccuracy,
                _config.juniorPrice
            );
            _generatedIntel[AnalystType.Junior] = juniorIntel;

            return new AnalystIntel[] { seniorIntel, juniorIntel };
        }

        public PurchaseResult BuyIntel(AnalystType type, int playerBalance)
        {
            int price = GetPriceForType(type);

            if (playerBalance < price)
            {
                return new PurchaseResult
                {
                    success = false,
                    errorMessage = "Insufficient balance to purchase analyst intel.",
                    remainingBalance = playerBalance
                };
            }

            return new PurchaseResult
            {
                success = true,
                errorMessage = null,
                remainingBalance = playerBalance - price
            };
        }

        private AnalystIntel GenerateIntelForType(
            AnalystType type,
            HorseData[] horses,
            int[] sortedIndices,
            float accuracy,
            int price)
        {
            bool isAccurate = _random.NextDouble() < accuracy;

            string content;
            if (isAccurate)
            {
                // Provide truthful intel about a top horse
                int topHorseIndex = sortedIndices[0];
                content = $"{horses[topHorseIndex].displayName} appears to be in top form";
            }
            else
            {
                // Provide misleading intel - pick a weaker horse and present it as strong
                int weakHorseIndex = sortedIndices[sortedIndices.Length - 1 - _random.Next(3)];
                content = $"{horses[weakHorseIndex].displayName} appears to be in top form";
            }

            return new AnalystIntel
            {
                type = type,
                content = content,
                isAccurate = isAccurate,
                price = price
            };
        }

        private int GetPriceForType(AnalystType type)
        {
            return type switch
            {
                AnalystType.Senior => _config.seniorPrice,
                AnalystType.Junior => _config.juniorPrice,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        /// <summary>
        /// Gets the generated intel for a specific analyst type. 
        /// Returns null if intel has not been generated yet.
        /// </summary>
        public AnalystIntel? GetIntel(AnalystType type)
        {
            if (_generatedIntel.TryGetValue(type, out AnalystIntel intel))
                return intel;
            return null;
        }
    }
}
