using System;
using System.Collections.Generic;
using HorseBetting.Config;
using HorseBetting.Core;
using HorseBetting.Data;

namespace HorseBetting.Systems
{
    public class EventSystem : IEventSystem
    {
        private readonly EventConfig _config;
        private readonly Random _random;

        public EventSystem(EventConfig config)
        {
            _config = config;
            _random = new Random();
        }

        // Constructor that accepts a Random instance for testability
        public EventSystem(EventConfig config, Random random)
        {
            _config = config;
            _random = random;
        }

        public void Initialize()
        {
            // No-op: event processing is triggered explicitly via ProcessStageEvents
        }

        public void Reset()
        {
            // No persistent state to reset
        }

        public StageEventResult[] ProcessStageEvents(HorseData[] horses, int stage, ProtectionCard[] playerCards)
        {
            if (horses == null)
                throw new ArgumentNullException(nameof(horses));

            if (_config.events == null || _config.events.Length == 0)
                return Array.Empty<StageEventResult>();

            var results = new List<StageEventResult>();

            for (int horseIndex = 0; horseIndex < horses.Length; horseIndex++)
            {
                for (int eventIndex = 0; eventIndex < _config.events.Length; eventIndex++)
                {
                    RaceEvent raceEvent = _config.events[eventIndex];

                    // Roll for trigger chance
                    float roll = (float)_random.NextDouble();
                    if (roll < raceEvent.triggerChance)
                    {
                        // Event triggered for this horse
                        bool wasProtected = false;
                        int appliedModifier = raceEvent.speedModifier;

                        // Check if player holds a matching protection card
                        if (playerCards != null)
                        {
                            for (int cardIndex = 0; cardIndex < playerCards.Length; cardIndex++)
                            {
                                ProtectionCard card = playerCards[cardIndex];
                                if (string.Equals(card.protectsAgainst, raceEvent.eventName, StringComparison.Ordinal))
                                {
                                    // Found matching card, roll for success rate
                                    float protectionRoll = (float)_random.NextDouble();
                                    if (protectionRoll < card.successRate)
                                    {
                                        // Protection successful - cancel the event
                                        wasProtected = true;
                                        appliedModifier = 0;
                                    }
                                    break; // Only use the first matching card
                                }
                            }
                        }

                        results.Add(new StageEventResult
                        {
                            horseIndex = horseIndex,
                            eventName = raceEvent.eventName,
                            speedModifier = appliedModifier,
                            wasProtected = wasProtected
                        });
                    }
                }
            }

            return results.ToArray();
        }
    }
}
