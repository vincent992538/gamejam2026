using System;
using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class RaceSimulationTests
    {
        private EventConfig _eventConfig;
        private TrackConfig _trackConfig;
        private HorseData[] _horses;

        [SetUp]
        public void SetUp()
        {
            _eventConfig = ScriptableObject.CreateInstance<EventConfig>();
            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 0f, // default: no events trigger
                    speedModifier = -2,
                    targetType = "single"
                }
            };

            _trackConfig = ScriptableObject.CreateInstance<TrackConfig>();
            _trackConfig.horsePreferences = new TrackPreference[8];
            for (int i = 0; i < 8; i++)
            {
                _trackConfig.horsePreferences[i] = new TrackPreference
                {
                    horseIndex = i,
                    grassModifier = i,        // horse 0 gets +0, horse 7 gets +7
                    mudModifier = 7 - i,      // horse 0 gets +7, horse 7 gets +0
                    snowModifier = 0          // all get +0 on snow
                };
            }

            _horses = new HorseData[8];
            for (int i = 0; i < 8; i++)
            {
                _horses[i] = new HorseData
                {
                    index = i,
                    baseSpeed = 30,
                    hiddenBonus = i,          // horse 0 gets +0, horse 7 gets +7
                    displayName = $"Horse {i + 1}"
                };
            }
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_eventConfig);
            UnityEngine.Object.DestroyImmediate(_trackConfig);
        }

        [Test]
        public void Initialize_DoesNotThrow()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            Assert.DoesNotThrow(() => system.Initialize());
        }

        [Test]
        public void Reset_ClearsRanking()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);
            Assert.IsNotNull(system.GetFinalRanking());

            system.Reset();
            Assert.IsNull(system.GetFinalRanking());
        }

        [Test]
        public void SimulateRace_WithNullHorses_Throws()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            Assert.Throws<ArgumentNullException>(() =>
                system.SimulateRace(null, TrackType.Grass, _eventConfig, new ProtectionCard[0]));
        }

        [Test]
        public void SimulateRace_WithNullEventConfig_Throws()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            Assert.Throws<ArgumentNullException>(() =>
                system.SimulateRace(_horses, TrackType.Grass, null, new ProtectionCard[0]));
        }

        [Test]
        public void SimulateRace_ReturnsRaceResultWithCorrectStructure()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            Assert.IsNotNull(result.finalRanking);
            Assert.AreEqual(8, result.finalRanking.Length);
            Assert.IsNotNull(result.finalSpeeds);
            Assert.AreEqual(8, result.finalSpeeds.Length);
            Assert.IsNotNull(result.stageEvents);
            Assert.AreEqual(3, result.stageEvents.Length);
        }

        [Test]
        public void SimulateRace_ThreeStagesExecuted()
        {
            // With triggerChance = 1.0, each stage should produce events
            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "AlwaysEvent",
                    description = "Always triggers",
                    triggerChance = 1.0f,
                    speedModifier = 1,
                    targetType = "single"
                }
            };

            var system = new RaceSimulationSystem(_trackConfig, new System.Random(42));
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            // All 3 stages should have events
            Assert.AreEqual(3, result.stageEvents.Length);
            for (int stage = 0; stage < 3; stage++)
            {
                Assert.IsNotNull(result.stageEvents[stage]);
                Assert.AreEqual(8, result.stageEvents[stage].Length, $"Stage {stage} should have events for all 8 horses");
            }
        }

        [Test]
        public void SimulateRace_FinalSpeedFormula_NoEvents_SnowTrack()
        {
            // Snow track gives +0 modifier to all horses
            // Final_Speed = baseSpeed(30) + hiddenBonus(i) + trackModifier(0) + events(0)
            var system = new RaceSimulationSystem(_trackConfig);
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            for (int i = 0; i < 8; i++)
            {
                int expectedSpeed = 30 + i + 0; // base + hidden + snow(0)
                Assert.AreEqual(expectedSpeed, result.finalSpeeds[i],
                    $"Horse {i} expected speed {expectedSpeed} but got {result.finalSpeeds[i]}");
            }
        }

        [Test]
        public void SimulateRace_FinalSpeedFormula_NoEvents_GrassTrack()
        {
            // Grass track gives +i modifier to horse i
            // Final_Speed = baseSpeed(30) + hiddenBonus(i) + trackModifier(i) + events(0)
            var system = new RaceSimulationSystem(_trackConfig);
            RaceResult result = system.SimulateRace(_horses, TrackType.Grass, _eventConfig, new ProtectionCard[0]);

            for (int i = 0; i < 8; i++)
            {
                int expectedSpeed = 30 + i + i; // base + hidden + grass(i)
                Assert.AreEqual(expectedSpeed, result.finalSpeeds[i],
                    $"Horse {i} expected speed {expectedSpeed} but got {result.finalSpeeds[i]}");
            }
        }

        [Test]
        public void SimulateRace_FinalSpeedFormula_WithEvents()
        {
            // Use guaranteed events to verify event modifiers are summed
            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Boost",
                    description = "Speed boost",
                    triggerChance = 1.0f,
                    speedModifier = 2,
                    targetType = "single"
                }
            };

            var system = new RaceSimulationSystem(_trackConfig, new System.Random(42));
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            // Each horse gets +2 per stage * 3 stages = +6 total from events
            for (int i = 0; i < 8; i++)
            {
                int expectedSpeed = 30 + i + 0 + 6; // base + hidden + snow(0) + events(2*3)
                Assert.AreEqual(expectedSpeed, result.finalSpeeds[i],
                    $"Horse {i} expected speed {expectedSpeed} but got {result.finalSpeeds[i]}");
            }
        }

        [Test]
        public void SimulateRace_RankingByFinalSpeedDescending()
        {
            // Snow track, no events: horse 7 has highest speed, horse 0 has lowest
            var system = new RaceSimulationSystem(_trackConfig);
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            // Ranking should be [7, 6, 5, 4, 3, 2, 1, 0]
            Assert.AreEqual(7, result.finalRanking[0], "Horse 7 should be first");
            Assert.AreEqual(6, result.finalRanking[1], "Horse 6 should be second");
            Assert.AreEqual(5, result.finalRanking[2], "Horse 5 should be third");
            Assert.AreEqual(0, result.finalRanking[7], "Horse 0 should be last");
        }

        [Test]
        public void SimulateRace_TieBreak_LowerIndexRanksHigher()
        {
            // Give all horses the same hiddenBonus to create ties
            for (int i = 0; i < 8; i++)
            {
                _horses[i] = new HorseData
                {
                    index = i,
                    baseSpeed = 30,
                    hiddenBonus = 5,  // all same
                    displayName = $"Horse {i + 1}"
                };
            }

            var system = new RaceSimulationSystem(_trackConfig);
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            // All horses have the same final speed (30 + 5 + 0 = 35)
            // Tie-break: lower index ranks higher
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(i, result.finalRanking[i],
                    $"Position {i} should be horse {i} due to tie-break rule");
            }
        }

        [Test]
        public void SimulateRace_RankingContainsAllHorses_NoDuplicates()
        {
            var system = new RaceSimulationSystem(_trackConfig, new System.Random(123));

            // Use events with partial trigger chance to add variability
            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Random",
                    description = "Random event",
                    triggerChance = 0.5f,
                    speedModifier = 3,
                    targetType = "single"
                }
            };

            RaceResult result = system.SimulateRace(_horses, TrackType.Grass, _eventConfig, new ProtectionCard[0]);

            // Verify all 8 horse indices appear exactly once
            bool[] seen = new bool[8];
            for (int i = 0; i < result.finalRanking.Length; i++)
            {
                int horseIdx = result.finalRanking[i];
                Assert.IsTrue(horseIdx >= 0 && horseIdx < 8, $"Invalid horse index {horseIdx}");
                Assert.IsFalse(seen[horseIdx], $"Horse {horseIdx} appears more than once in ranking");
                seen[horseIdx] = true;
            }
        }

        [Test]
        public void GetFinalRanking_BeforeSimulation_ReturnsNull()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            Assert.IsNull(system.GetFinalRanking());
        }

        [Test]
        public void GetFinalRanking_AfterSimulation_ReturnsSameAsRaceResult()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, new ProtectionCard[0]);

            int[] ranking = system.GetFinalRanking();
            Assert.IsNotNull(ranking);
            Assert.AreEqual(result.finalRanking.Length, ranking.Length);
            for (int i = 0; i < ranking.Length; i++)
            {
                Assert.AreEqual(result.finalRanking[i], ranking[i]);
            }
        }

        [Test]
        public void SimulateRace_WithProtectionCards_EventsCancelled()
        {
            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 1.0f,
                    speedModifier = -5,
                    targetType = "single"
                }
            };

            var playerCards = new ProtectionCard[]
            {
                new ProtectionCard
                {
                    cardName = "Anti-Stumble",
                    protectsAgainst = "Stumble",
                    successRate = 1.0f
                }
            };

            var system = new RaceSimulationSystem(_trackConfig, new System.Random(42));
            RaceResult result = system.SimulateRace(_horses, TrackType.Snow, _eventConfig, playerCards);

            // All events should be protected, so speeds should be base + hidden + track only
            for (int i = 0; i < 8; i++)
            {
                int expectedSpeed = 30 + i + 0; // base + hidden + snow(0), no event modifiers
                Assert.AreEqual(expectedSpeed, result.finalSpeeds[i],
                    $"Horse {i} speed should not be affected by protected events");
            }
        }

        [Test]
        public void SimulateRace_NullPlayerCards_DoesNotThrow()
        {
            var system = new RaceSimulationSystem(_trackConfig);
            Assert.DoesNotThrow(() =>
                system.SimulateRace(_horses, TrackType.Snow, _eventConfig, null));
        }

        [Test]
        public void SimulateRace_DeterministicWithSameRandom()
        {
            var system1 = new RaceSimulationSystem(_trackConfig, new System.Random(99));
            var system2 = new RaceSimulationSystem(_trackConfig, new System.Random(99));

            _eventConfig.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Random",
                    description = "Random event",
                    triggerChance = 0.5f,
                    speedModifier = 3,
                    targetType = "single"
                }
            };

            RaceResult result1 = system1.SimulateRace(_horses, TrackType.Grass, _eventConfig, new ProtectionCard[0]);
            RaceResult result2 = system2.SimulateRace(_horses, TrackType.Grass, _eventConfig, new ProtectionCard[0]);

            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(result1.finalSpeeds[i], result2.finalSpeeds[i]);
                Assert.AreEqual(result1.finalRanking[i], result2.finalRanking[i]);
            }
        }
    }
}
