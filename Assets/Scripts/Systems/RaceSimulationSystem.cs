using System;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class RaceSimulationSystem : IRaceSimulationSystem
    {
        private readonly TrackConfig _trackConfig;
        private int[] _lastRanking;

        private readonly Random _random;

        public RaceSimulationSystem(TrackConfig trackConfig)
        {
            _trackConfig = trackConfig;
            _random = new Random();
        }

        // Constructor that accepts a Random instance for testability
        public RaceSimulationSystem(TrackConfig trackConfig, Random random)
        {
            _trackConfig = trackConfig;
            _random = random;
        }

        public void Initialize()
        {
            // No-op: race simulation is triggered explicitly via SimulateRace
        }

        public void Reset()
        {
            _lastRanking = null;
        }

        public RaceResult SimulateRace(HorseData[] horses, TrackType track, EventConfig eventConfig, ProtectionCard[] playerCards)
        {
            if (horses == null)
                throw new ArgumentNullException(nameof(horses));
            if (eventConfig == null)
                throw new ArgumentNullException(nameof(eventConfig));

            int horseCount = horses.Length;

            // Create an EventSystem with the provided config and our random
            var eventSystem = new EventSystem(eventConfig, _random);

            // Execute 3 stages of event processing
            var stageEvents = new StageEventResult[3][];
            for (int stage = 0; stage < 3; stage++)
            {
                stageEvents[stage] = eventSystem.ProcessStageEvents(horses, stage + 1, playerCards);
            }

            // Calculate track modifiers using TrackSystem logic via TrackConfig
            var trackSystem = new TrackSystem(_trackConfig);

            // Calculate Final_Speed for each horse
            int[] finalSpeeds = new int[horseCount];
            for (int h = 0; h < horseCount; h++)
            {
                int baseSpeed = horses[h].baseSpeed;
                int hiddenBonus = horses[h].hiddenBonus;
                int trackModifier = trackSystem.GetTrackModifier(horses[h].index, track);

                // Sum all event modifiers across all 3 stages for this horse
                int totalEventModifier = 0;
                for (int stage = 0; stage < 3; stage++)
                {
                    if (stageEvents[stage] != null)
                    {
                        for (int e = 0; e < stageEvents[stage].Length; e++)
                        {
                            if (stageEvents[stage][e].horseIndex == horses[h].index)
                            {
                                totalEventModifier += stageEvents[stage][e].speedModifier;
                            }
                        }
                    }
                }

                finalSpeeds[h] = baseSpeed + hiddenBonus + trackModifier + totalEventModifier;
            }

            // Generate ranking: sort by Final_Speed descending, tie-break by lower horse index
            int[] ranking = new int[horseCount];
            for (int i = 0; i < horseCount; i++)
            {
                ranking[i] = i;
            }

            // Sort indices by finalSpeeds descending, then by horse index ascending for ties
            Array.Sort(ranking, (a, b) =>
            {
                int speedCompare = finalSpeeds[b].CompareTo(finalSpeeds[a]); // descending
                if (speedCompare != 0) return speedCompare;
                return horses[a].index.CompareTo(horses[b].index); // ascending horse index for tie-break
            });

            // Convert ranking from array indices to horse indices
            int[] finalRanking = new int[horseCount];
            for (int i = 0; i < horseCount; i++)
            {
                finalRanking[i] = horses[ranking[i]].index;
            }

            _lastRanking = finalRanking;

            return new RaceResult
            {
                finalRanking = finalRanking,
                finalSpeeds = finalSpeeds,
                stageEvents = stageEvents
            };
        }

        public int[] GetFinalRanking()
        {
            return _lastRanking;
        }
    }
}
