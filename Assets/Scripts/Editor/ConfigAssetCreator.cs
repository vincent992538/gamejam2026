using UnityEngine;
using UnityEditor;
using HorseBetting.Config;
using HorseBetting.Data;

namespace HorseBetting.Editor
{
    public static class ConfigAssetCreator
    {
        private const string ConfigPath = "Assets/Resources/Config/";

        [MenuItem("HorseBetting/Create Default Config Assets")]
        public static void CreateAllConfigAssets()
        {
            EnsureDirectoryExists();

            CreateGameConfig();
            CreateOddsConfig();
            CreateMessageCardConfig();
            CreateTrackConfig();
            CreateAnalystConfig();
            CreateEventConfig();
            CreateShopConfig();
            CreateBettingConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HorseBetting] All default config assets created in " + ConfigPath);
        }

        private static void EnsureDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Config"))
                AssetDatabase.CreateFolder("Assets/Resources", "Config");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/GameConfig")]
        public static void CreateGameConfig()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.horseCount = 8;
            config.baseSpeed = 30;
            config.startingBalance = 1000;
            config.maxProtectionCards = 3;
            config.messageCardsPerRound = 3;
            SaveAsset(config, "GameConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/OddsConfig")]
        public static void CreateOddsConfig()
        {
            var config = ScriptableObject.CreateInstance<OddsConfig>();
            config.baseMultiplier = 1.0f;
            config.rankOdds = new float[] { 1.5f, 2.0f, 3.0f, 5.0f, 8.0f, 12.0f, 20.0f, 40.0f };
            config.round2Penalty = 0.8f;
            config.round3Penalty = 0.6f;
            SaveAsset(config, "OddsConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/MessageCardConfig")]
        public static void CreateMessageCardConfig()
        {
            var config = ScriptableObject.CreateInstance<MessageCardConfig>();
            config.entries = new MessageCardEntry[]
            {
                new MessageCardEntry { hiddenSpeedBonus = 0, description = "這匹馬看起來無精打采" },
                new MessageCardEntry { hiddenSpeedBonus = 1, description = "這匹馬狀態一般" },
                new MessageCardEntry { hiddenSpeedBonus = 2, description = "這匹馬有些許活力" },
                new MessageCardEntry { hiddenSpeedBonus = 3, description = "這匹馬看來準備好了" },
                new MessageCardEntry { hiddenSpeedBonus = 4, description = "這匹馬狀態不錯" },
                new MessageCardEntry { hiddenSpeedBonus = 5, description = "這匹馬充滿鬥志" },
                new MessageCardEntry { hiddenSpeedBonus = 6, description = "這匹馬狀態極佳" },
                new MessageCardEntry { hiddenSpeedBonus = 7, description = "這匹馬今天特別興奮" },
            };
            SaveAsset(config, "MessageCardConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/TrackConfig")]
        public static void CreateTrackConfig()
        {
            var config = ScriptableObject.CreateInstance<TrackConfig>();
            config.horsePreferences = new TrackPreference[]
            {
                new TrackPreference { horseIndex = 0, grassModifier = 2, mudModifier = -1, snowModifier = 0 },
                new TrackPreference { horseIndex = 1, grassModifier = 0, mudModifier = 2, snowModifier = -1 },
                new TrackPreference { horseIndex = 2, grassModifier = -1, mudModifier = 0, snowModifier = 2 },
                new TrackPreference { horseIndex = 3, grassModifier = 1, mudModifier = 1, snowModifier = -2 },
                new TrackPreference { horseIndex = 4, grassModifier = -2, mudModifier = 1, snowModifier = 1 },
                new TrackPreference { horseIndex = 5, grassModifier = 1, mudModifier = -2, snowModifier = 1 },
                new TrackPreference { horseIndex = 6, grassModifier = 0, mudModifier = 0, snowModifier = 0 },
                new TrackPreference { horseIndex = 7, grassModifier = -1, mudModifier = 1, snowModifier = 1 },
            };
            SaveAsset(config, "TrackConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/AnalystConfig")]
        public static void CreateAnalystConfig()
        {
            var config = ScriptableObject.CreateInstance<AnalystConfig>();
            config.seniorPrice = 200;
            config.seniorAccuracy = 0.8f;
            config.juniorPrice = 80;
            config.juniorAccuracy = 0.5f;
            SaveAsset(config, "AnalystConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/EventConfig")]
        public static void CreateEventConfig()
        {
            var config = ScriptableObject.CreateInstance<EventConfig>();
            config.events = new RaceEvent[]
            {
                new RaceEvent
                {
                    eventName = "Stumble",
                    description = "馬匹絆倒",
                    triggerChance = 0.15f,
                    speedModifier = -3,
                    targetType = "single"
                },
                new RaceEvent
                {
                    eventName = "Second Wind",
                    description = "爆發衝刺",
                    triggerChance = 0.1f,
                    speedModifier = 4,
                    targetType = "single"
                },
                new RaceEvent
                {
                    eventName = "Mud Splash",
                    description = "泥漿飛濺",
                    triggerChance = 0.2f,
                    speedModifier = -2,
                    targetType = "single"
                },
                new RaceEvent
                {
                    eventName = "Strong Headwind",
                    description = "強勁逆風",
                    triggerChance = 0.05f,
                    speedModifier = -1,
                    targetType = "all"
                },
            };
            SaveAsset(config, "EventConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/ShopConfig")]
        public static void CreateShopConfig()
        {
            var config = ScriptableObject.CreateInstance<ShopConfig>();
            config.protectionCards = new ProtectionCardData[]
            {
                new ProtectionCardData
                {
                    cardName = "Anti-Stumble Guard",
                    protectsAgainst = "Stumble",
                    successRate = 0.8f,
                    price = 150
                },
                new ProtectionCardData
                {
                    cardName = "Mud Shield",
                    protectsAgainst = "Mud Splash",
                    successRate = 0.9f,
                    price = 120
                },
                new ProtectionCardData
                {
                    cardName = "Wind Barrier",
                    protectsAgainst = "Strong Headwind",
                    successRate = 0.7f,
                    price = 100
                },
            };
            SaveAsset(config, "ShopConfig");
        }

        [MenuItem("HorseBetting/Create Default Config Assets/BettingConfig")]
        public static void CreateBettingConfig()
        {
            var config = ScriptableObject.CreateInstance<BettingConfig>();
            config.betTypes = new BetTypeConfig[]
            {
                new BetTypeConfig { type = BetType.SingleWin, oddsMultiplier = 1.0f },
                new BetTypeConfig { type = BetType.Place, oddsMultiplier = 0.4f },
                new BetTypeConfig { type = BetType.Quinella, oddsMultiplier = 2.0f },
                new BetTypeConfig { type = BetType.Exacta, oddsMultiplier = 5.0f },
                new BetTypeConfig { type = BetType.Trio, oddsMultiplier = 8.0f },
                new BetTypeConfig { type = BetType.Trifecta, oddsMultiplier = 20.0f },
            };
            SaveAsset(config, "BettingConfig");
        }

        private static void SaveAsset(ScriptableObject asset, string assetName)
        {
            string path = ConfigPath + assetName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                EditorUtility.SetDirty(existing);
                Debug.Log($"[HorseBetting] Updated existing asset: {path}");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[HorseBetting] Created new asset: {path}");
            }
        }
    }
}
