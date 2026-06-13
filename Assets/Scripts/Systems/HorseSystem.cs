using System;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class HorseSystem : IHorseSystem
    {
        private readonly GameConfig _config;
        private HorseData[] _horses;
        private readonly Random _random;

        public HorseSystem(GameConfig config)
        {
            _config = config;
            _random = new Random();
        }

        public void Initialize()
        {
            // No-op: setup handled via GenerateHorses
        }

        public void Reset()
        {
            _horses = null;
        }

        public HorseData[] GenerateHorses()
        {
            int count = _config.horseCount;
            int[] bonuses = new int[count];

            // Fill with 0..count-1
            for (int i = 0; i < count; i++)
            {
                bonuses[i] = i;
            }

            // Fisher-Yates shuffle
            for (int i = count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int temp = bonuses[i];
                bonuses[i] = bonuses[j];
                bonuses[j] = temp;
            }

            _horses = new HorseData[count];
            for (int i = 0; i < count; i++)
            {
                _horses[i] = new HorseData
                {
                    index = i,
                    baseSpeed = _config.baseSpeed,
                    hiddenBonus = bonuses[i],
                    displayName = $"Horse {i + 1}"
                };
            }

            return _horses;
        }

        public int GetHiddenBonus(int horseIndex)
        {
            if (_horses == null)
                throw new InvalidOperationException("Horses have not been generated yet.");

            if (horseIndex < 0 || horseIndex >= _horses.Length)
                throw new ArgumentOutOfRangeException(nameof(horseIndex));

            return _horses[horseIndex].hiddenBonus;
        }

        /// <summary>
        /// Returns the most recently generated horses without regenerating.
        /// </summary>
        public HorseData[] GetHorses()
        {
            if (_horses == null)
                throw new InvalidOperationException("Horses have not been generated yet. Call GenerateHorses first.");
            return _horses;
        }
    }
}
