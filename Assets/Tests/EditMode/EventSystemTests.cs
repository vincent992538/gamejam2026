using System;
using NUnit.Framework;
using HorseBetting.Config;
using HorseBetting.Data;
using HorseBetting.Systems;
using UnityEngine;

namespace HorseBetting.Tests.EditMode
{
    [TestFixture]
    public class EventSystemTests
    {
        private EventConfig _config;
        private HorseData[] _horses;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<EventConfig>();
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 0.3f,
                    speedModifier = -2,
                    targetType = "single"
                },
                new RaceEvent
                {
                    eventName = "Tailwind",
                    description = "Wind boost",
                    triggerChance = 0.2f,
                    speedModifier = 3,
                    targetType = "single"
                }
            };

            _horses = new HorseData[8];
            for (int i = 0; i < 8; i++)
            {
                _horses[i] = new HorseData
                {
                    index = i,
                    baseSpeed = 30,
                    hiddenBonus = i,
                    displayName = $"Horse {i + 1}"
                };
            }
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void Initialize_DoesNotThrow()
        {
            var system = new EventSystem(_config);
            Assert.DoesNotThrow(() => system.Initialize());
        }

        [Test]
        public void Reset_DoesNotThrow()
        {
            var system = new EventSystem(_config);
            Assert.DoesNotThrow(() => system.Reset());
        }

        [Test]
        public void ProcessStageEvents_WithNullHorses_Throws()
        {
            var system = new EventSystem(_config);
            Assert.Throws<ArgumentNullException>(() =>
                system.ProcessStageEvents(null, 1, new ProtectionCard[0]));
        }

        [Test]
        public void ProcessStageEvents_WithNullEvents_ReturnsEmpty()
        {
            _config.events = null;
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Length);
        }

        [Test]
        public void ProcessStageEvents_WithEmptyEvents_ReturnsEmpty()
        {
            _config.events = new RaceEvent[0];
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Length);
        }

        [Test]
        public void ProcessStageEvents_WithZeroTriggerChance_NeverTriggers()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "NeverHappens",
                    description = "Should never trigger",
                    triggerChance = 0f,
                    speedModifier = -5,
                    targetType = "single"
                }
            };
            var system = new EventSystem(_config);

            // Run multiple times to be confident
            for (int i = 0; i < 50; i++)
            {
                StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);
                Assert.AreEqual(0, results.Length, $"Event triggered on iteration {i} despite 0 trigger chance");
            }
        }

        [Test]
        public void ProcessStageEvents_WithFullTriggerChance_AlwaysTriggers()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "AlwaysHappens",
                    description = "Should always trigger",
                    triggerChance = 1.0f,
                    speedModifier = -3,
                    targetType = "single"
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            // With triggerChance = 1.0, every horse should be affected
            Assert.AreEqual(8, results.Length);
            for (int i = 0; i < results.Length; i++)
            {
                Assert.AreEqual(i, results[i].horseIndex);
                Assert.AreEqual("AlwaysHappens", results[i].eventName);
                Assert.AreEqual(-3, results[i].speedModifier);
                Assert.IsFalse(results[i].wasProtected);
            }
        }

        [Test]
        public void ProcessStageEvents_AppliesCorrectSpeedModifier()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Boost",
                    description = "Speed boost",
                    triggerChance = 1.0f,
                    speedModifier = 5,
                    targetType = "single"
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            foreach (var result in results)
            {
                Assert.AreEqual(5, result.speedModifier);
                Assert.AreEqual("Boost", result.eventName);
            }
        }

        [Test]
        public void ProcessStageEvents_ProtectionCard_CancelsEventWithFullSuccessRate()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 1.0f,
                    speedModifier = -2,
                    targetType = "single"
                }
            };
            var playerCards = new ProtectionCard[]
            {
                new ProtectionCard
                {
                    cardName = "Anti-Stumble Shield",
                    protectsAgainst = "Stumble",
                    successRate = 1.0f
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, playerCards);

            // All events should be triggered but protected
            Assert.AreEqual(8, results.Length);
            foreach (var result in results)
            {
                Assert.IsTrue(result.wasProtected);
                Assert.AreEqual(0, result.speedModifier);
                Assert.AreEqual("Stumble", result.eventName);
            }
        }

        [Test]
        public void ProcessStageEvents_ProtectionCard_ZeroSuccessRate_NeverProtects()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 1.0f,
                    speedModifier = -2,
                    targetType = "single"
                }
            };
            var playerCards = new ProtectionCard[]
            {
                new ProtectionCard
                {
                    cardName = "Weak Shield",
                    protectsAgainst = "Stumble",
                    successRate = 0f
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, playerCards);

            // All events should be triggered and NOT protected (0% success rate)
            Assert.AreEqual(8, results.Length);
            foreach (var result in results)
            {
                Assert.IsFalse(result.wasProtected);
                Assert.AreEqual(-2, result.speedModifier);
            }
        }

        [Test]
        public void ProcessStageEvents_ProtectionCard_NonMatchingCard_DoesNotProtect()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 1.0f,
                    speedModifier = -2,
                    targetType = "single"
                }
            };
            var playerCards = new ProtectionCard[]
            {
                new ProtectionCard
                {
                    cardName = "Anti-Rain Shield",
                    protectsAgainst = "HeavyRain",
                    successRate = 1.0f
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, playerCards);

            // All events should trigger without protection
            Assert.AreEqual(8, results.Length);
            foreach (var result in results)
            {
                Assert.IsFalse(result.wasProtected);
                Assert.AreEqual(-2, result.speedModifier);
            }
        }

        [Test]
        public void ProcessStageEvents_NullPlayerCards_DoesNotThrow()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 1.0f,
                    speedModifier = -2,
                    targetType = "single"
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, null);

            Assert.AreEqual(8, results.Length);
            foreach (var result in results)
            {
                Assert.IsFalse(result.wasProtected);
                Assert.AreEqual(-2, result.speedModifier);
            }
        }

        [Test]
        public void ProcessStageEvents_MultipleEvents_AllEvaluatedPerHorse()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Event1",
                    description = "First event",
                    triggerChance = 1.0f,
                    speedModifier = -1,
                    targetType = "single"
                },
                new RaceEvent
                {
                    eventName = "Event2",
                    description = "Second event",
                    triggerChance = 1.0f,
                    speedModifier = 2,
                    targetType = "single"
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            // 8 horses × 2 events = 16 results
            Assert.AreEqual(16, results.Length);

            // Verify ordering: horse 0 event1, horse 0 event2, horse 1 event1, ...
            for (int h = 0; h < 8; h++)
            {
                var result1 = results[h * 2];
                var result2 = results[h * 2 + 1];

                Assert.AreEqual(h, result1.horseIndex);
                Assert.AreEqual("Event1", result1.eventName);
                Assert.AreEqual(-1, result1.speedModifier);

                Assert.AreEqual(h, result2.horseIndex);
                Assert.AreEqual("Event2", result2.eventName);
                Assert.AreEqual(2, result2.speedModifier);
            }
        }

        [Test]
        public void ProcessStageEvents_PositiveSpeedModifier_AppliedCorrectly()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "SecondWind",
                    description = "Horse gets a second wind",
                    triggerChance = 1.0f,
                    speedModifier = 4,
                    targetType = "single"
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            Assert.AreEqual(8, results.Length);
            foreach (var result in results)
            {
                Assert.AreEqual(4, result.speedModifier);
            }
        }

        [Test]
        public void ProcessStageEvents_ReturnsResultsWithCorrectEventName()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "RainStorm",
                    description = "Heavy rain slows everyone",
                    triggerChance = 1.0f,
                    speedModifier = -3,
                    targetType = "all"
                }
            };
            var system = new EventSystem(_config);

            StageEventResult[] results = system.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            foreach (var result in results)
            {
                Assert.AreEqual("RainStorm", result.eventName);
            }
        }

        [Test]
        public void ProcessStageEvents_WithSeedableRandom_ProducesConsistentResults()
        {
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 0.5f,
                    speedModifier = -2,
                    targetType = "single"
                }
            };

            // Same seed should produce same results
            var system1 = new EventSystem(_config, new System.Random(42));
            var system2 = new EventSystem(_config, new System.Random(42));

            StageEventResult[] results1 = system1.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);
            StageEventResult[] results2 = system2.ProcessStageEvents(_horses, 1, new ProtectionCard[0]);

            Assert.AreEqual(results1.Length, results2.Length);
            for (int i = 0; i < results1.Length; i++)
            {
                Assert.AreEqual(results1[i].horseIndex, results2[i].horseIndex);
                Assert.AreEqual(results1[i].eventName, results2[i].eventName);
                Assert.AreEqual(results1[i].speedModifier, results2[i].speedModifier);
                Assert.AreEqual(results1[i].wasProtected, results2[i].wasProtected);
            }
        }

        [Test]
        public void ProcessStageEvents_PartialProtection_SomeProtectedSomeNot()
        {
            // Use a deterministic random to verify partial protection behavior
            _config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "Horse stumbles",
                    triggerChance = 1.0f,
                    speedModifier = -2,
                    targetType = "single"
                }
            };
            var playerCards = new ProtectionCard[]
            {
                new ProtectionCard
                {
                    cardName = "Anti-Stumble Shield",
                    protectsAgainst = "Stumble",
                    successRate = 0.5f
                }
            };

            // Run with enough iterations to observe both protected and unprotected
            bool sawProtected = false;
            bool sawUnprotected = false;

            for (int seed = 0; seed < 100; seed++)
            {
                var system = new EventSystem(_config, new System.Random(seed));
                StageEventResult[] results = system.ProcessStageEvents(_horses, 1, playerCards);

                foreach (var result in results)
                {
                    if (result.wasProtected) sawProtected = true;
                    else sawUnprotected = true;
                }

                if (sawProtected && sawUnprotected) break;
            }

            Assert.IsTrue(sawProtected, "Never observed a protected event with 50% success rate");
            Assert.IsTrue(sawUnprotected, "Never observed an unprotected event with 50% success rate");
        }
    }
}
