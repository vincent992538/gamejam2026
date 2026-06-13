using UnityEngine;
using HorseBetting.Data;

namespace HorseBetting.Core
{
    /// <summary>
    /// Loads sprite assets from Resources/Sprites/ at runtime.
    /// Provides horse sprites, track backgrounds, message card images,
    /// analyst portraits, and champion trophies.
    /// </summary>
    public static class SpriteLoader
    {
        private static readonly string[] HorsePaths = new string[]
        {
            "Sprites/Horses/Horse1_Thief",
            "Sprites/Horses/Horse2_Grandma",
            "Sprites/Horses/Horse3_Rock",
            "Sprites/Horses/Horse4_Goldfish",
            "Sprites/Horses/Horse5_PhoneBooth",
            "Sprites/Horses/Horse6_Cat",
            "Sprites/Horses/Horse7_Mystery1",
            "Sprites/Horses/Horse8_Mystery2"
        };

        private static readonly string[] TrackPaths = new string[]
        {
            "Sprites/Tracks/Track_Grass",
            "Sprites/Tracks/Track_Mud",
            "Sprites/Tracks/Track_Snow"
        };

        private static readonly string[] MessageCardPaths = new string[]
        {
            "Sprites/MessageCards/Card_1",
            "Sprites/MessageCards/Card_2",
            "Sprites/MessageCards/Card_3",
            "Sprites/MessageCards/Card_4",
            "Sprites/MessageCards/Card_5",
            "Sprites/MessageCards/Card_6",
            "Sprites/MessageCards/Card_7",
            "Sprites/MessageCards/Card_8"
        };

        /// <summary>
        /// Load the sprite for a horse by index (0-7).
        /// </summary>
        public static Sprite LoadHorseSprite(int horseIndex)
        {
            if (horseIndex < 0 || horseIndex >= HorsePaths.Length)
            {
                Debug.LogWarning($"[SpriteLoader] Invalid horse index: {horseIndex}");
                return null;
            }
            return Resources.Load<Sprite>(HorsePaths[horseIndex]);
        }

        /// <summary>
        /// Load all 8 horse sprites.
        /// </summary>
        public static Sprite[] LoadAllHorseSprites()
        {
            var sprites = new Sprite[HorsePaths.Length];
            for (int i = 0; i < HorsePaths.Length; i++)
            {
                sprites[i] = Resources.Load<Sprite>(HorsePaths[i]);
                if (sprites[i] == null)
                    Debug.LogWarning($"[SpriteLoader] Failed to load horse sprite: {HorsePaths[i]}");
            }
            return sprites;
        }

        /// <summary>
        /// Load the track background sprite for a given track type.
        /// </summary>
        public static Sprite LoadTrackSprite(TrackType trackType)
        {
            int index = (int)trackType;
            if (index < 0 || index >= TrackPaths.Length)
                return null;
            return Resources.Load<Sprite>(TrackPaths[index]);
        }

        /// <summary>
        /// Load a message card sprite by index (0-7).
        /// </summary>
        public static Sprite LoadMessageCardSprite(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= MessageCardPaths.Length)
                return null;
            return Resources.Load<Sprite>(MessageCardPaths[cardIndex]);
        }

        /// <summary>
        /// Load the senior analyst portrait.
        /// </summary>
        public static Sprite LoadAnalystSenior()
        {
            return Resources.Load<Sprite>("Sprites/Analysts/Analyst_Senior");
        }

        /// <summary>
        /// Load the junior analyst portrait.
        /// </summary>
        public static Sprite LoadAnalystJunior()
        {
            return Resources.Load<Sprite>("Sprites/Analysts/Analyst_Junior");
        }

        /// <summary>
        /// Load trophy sprites (gold=0, silver=1, bronze=2).
        /// </summary>
        public static Sprite LoadTrophy(int rank)
        {
            switch (rank)
            {
                case 0: return Resources.Load<Sprite>("Sprites/Champion/Trophy_Gold");
                case 1: return Resources.Load<Sprite>("Sprites/Champion/Trophy_Silver");
                case 2: return Resources.Load<Sprite>("Sprites/Champion/Trophy_Bronze");
                default: return null;
            }
        }
    }
}
