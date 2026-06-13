using UnityEngine;
using HorseBetting.Config;

namespace HorseBetting.Core
{
    /// <summary>
    /// Static utility class that validates all config ScriptableObjects on startup.
    /// Logs errors for critical failures and warnings for clamped values.
    /// Validates: Requirements 12.3, 12.4
    /// </summary>
    public static class ConfigValidator
    {
        /// <summary>
        /// Runs all config validations on the GameEngine's assigned configs.
        /// Returns true if all critical checks pass; false if any critical failure occurs.
        /// </summary>
        public static bool ValidateAll(GameEngine engine)
        {
            bool allValid = true;

            if (engine.GameConfig == null)
            {
                Debug.LogError("[ConfigValidator] GameConfig is not assigned!");
                allValid = false;
            }
            else
            {
                allValid &= ValidateGameConfig(engine.GameConfig);
            }

            if (engine.MessageCardConfig == null)
            {
                Debug.LogError("[ConfigValidator] MessageCardConfig is not assigned!");
                allValid = false;
            }
            else
            {
                allValid &= ValidateMessageCardConfig(engine.MessageCardConfig);
            }

            if (engine.EventConfig == null)
            {
                Debug.LogError("[ConfigValidator] EventConfig is not assigned!");
                allValid = false;
            }
            else
            {
                ValidateEventConfig(engine.EventConfig);
            }

            if (engine.AnalystConfig == null)
            {
                Debug.LogError("[ConfigValidator] AnalystConfig is not assigned!");
                allValid = false;
            }
            else
            {
                ValidateAnalystConfig(engine.AnalystConfig);
            }

            if (engine.OddsConfig == null)
            {
                Debug.LogError("[ConfigValidator] OddsConfig is not assigned!");
                allValid = false;
            }
            else
            {
                ValidateOddsConfig(engine.OddsConfig);
            }

            if (engine.TrackConfig == null)
            {
                Debug.LogError("[ConfigValidator] TrackConfig is not assigned!");
                allValid = false;
            }

            if (engine.ShopConfig == null)
            {
                Debug.LogError("[ConfigValidator] ShopConfig is not assigned!");
                allValid = false;
            }

            if (engine.BettingConfig == null)
            {
                Debug.LogError("[ConfigValidator] BettingConfig is not assigned!");
                allValid = false;
            }

            return allValid;
        }

        /// <summary>
        /// Validates GameConfig values are reasonable.
        /// Returns true if valid.
        /// </summary>
        public static bool ValidateGameConfig(GameConfig config)
        {
            bool valid = true;

            if (config.horseCount != 8)
            {
                Debug.LogWarning($"[ConfigValidator] GameConfig.horseCount is {config.horseCount}, expected 8.");
            }

            if (config.baseSpeed <= 0)
            {
                Debug.LogWarning($"[ConfigValidator] GameConfig.baseSpeed is {config.baseSpeed}, clamping to 1.");
                config.baseSpeed = 1;
            }

            if (config.startingBalance <= 0)
            {
                Debug.LogWarning($"[ConfigValidator] GameConfig.startingBalance is {config.startingBalance}, clamping to 1.");
                config.startingBalance = 1;
            }

            return valid;
        }

        /// <summary>
        /// Validates MessageCardConfig has exactly 8 entries.
        /// Returns false (critical failure) if entry count != 8.
        /// </summary>
        public static bool ValidateMessageCardConfig(MessageCardConfig config)
        {
            if (config.entries == null || config.entries.Length != 8)
            {
                int count = config.entries == null ? 0 : config.entries.Length;
                Debug.LogError($"[ConfigValidator] MessageCardConfig must have exactly 8 entries, but has {count}. Config rejected.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates EventConfig triggerChance values are in [0,1]. Clamps if invalid.
        /// </summary>
        public static void ValidateEventConfig(EventConfig config)
        {
            if (config.events == null || config.events.Length == 0)
            {
                Debug.LogWarning("[ConfigValidator] EventConfig has no events defined.");
                return;
            }

            for (int i = 0; i < config.events.Length; i++)
            {
                var evt = config.events[i];
                if (evt.triggerChance < 0f || evt.triggerChance > 1f)
                {
                    float original = evt.triggerChance;
                    evt.triggerChance = Mathf.Clamp01(evt.triggerChance);
                    Debug.LogWarning($"[ConfigValidator] EventConfig.events[{i}] '{evt.eventName}' triggerChance was {original}, clamped to {evt.triggerChance}.");
                }
            }
        }

        /// <summary>
        /// Validates AnalystConfig prices (no negatives) and accuracy (in [0,1]). Clamps if invalid.
        /// </summary>
        public static void ValidateAnalystConfig(AnalystConfig config)
        {
            if (config.seniorPrice < 0)
            {
                Debug.LogWarning($"[ConfigValidator] AnalystConfig.seniorPrice was {config.seniorPrice}, clamping to 0.");
                config.seniorPrice = 0;
            }

            if (config.juniorPrice < 0)
            {
                Debug.LogWarning($"[ConfigValidator] AnalystConfig.juniorPrice was {config.juniorPrice}, clamping to 0.");
                config.juniorPrice = 0;
            }

            if (config.seniorAccuracy < 0f || config.seniorAccuracy > 1f)
            {
                float original = config.seniorAccuracy;
                config.seniorAccuracy = Mathf.Clamp01(config.seniorAccuracy);
                Debug.LogWarning($"[ConfigValidator] AnalystConfig.seniorAccuracy was {original}, clamped to {config.seniorAccuracy}.");
            }

            if (config.juniorAccuracy < 0f || config.juniorAccuracy > 1f)
            {
                float original = config.juniorAccuracy;
                config.juniorAccuracy = Mathf.Clamp01(config.juniorAccuracy);
                Debug.LogWarning($"[ConfigValidator] AnalystConfig.juniorAccuracy was {original}, clamped to {config.juniorAccuracy}.");
            }
        }

        /// <summary>
        /// Validates OddsConfig rankOdds has exactly 8 entries.
        /// </summary>
        public static void ValidateOddsConfig(OddsConfig config)
        {
            if (config.rankOdds == null || config.rankOdds.Length != 8)
            {
                int count = config.rankOdds == null ? 0 : config.rankOdds.Length;
                Debug.LogWarning($"[ConfigValidator] OddsConfig.rankOdds should have 8 entries, but has {count}.");
            }
        }
    }
}
