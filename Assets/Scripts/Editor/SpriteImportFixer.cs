using UnityEngine;
using UnityEditor;
using System.IO;

namespace HorseBetting.Editor
{
    /// <summary>
    /// Automatically sets all PNG files in Assets/Resources/Sprites/ to Sprite texture type.
    /// Run from menu: HorseBetting > Fix Sprite Imports
    /// </summary>
    public static class SpriteImportFixer
    {
        [MenuItem("HorseBetting/Fix Sprite Imports")]
        public static void FixAllSpriteImports()
        {
            string[] paths = new string[]
            {
                "Assets/Resources/Sprites/Horses",
                "Assets/Resources/Sprites/Tracks",
                "Assets/Resources/Sprites/MessageCards",
                "Assets/Resources/Sprites/Analysts",
                "Assets/Resources/Sprites/Champion"
            };

            int fixedCount = 0;

            foreach (string folderPath in paths)
            {
                if (!Directory.Exists(folderPath)) continue;

                string[] files = Directory.GetFiles(folderPath, "*.png");
                foreach (string file in files)
                {
                    string assetPath = file.Replace("\\", "/");
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                    if (importer != null && importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = 100;
                        importer.mipmapEnabled = false;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.SaveAndReimport();
                        fixedCount++;
                        Debug.Log($"[SpriteImportFixer] Fixed: {assetPath}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[SpriteImportFixer] Done! Fixed {fixedCount} textures to Sprite type.");

            EditorUtility.DisplayDialog("Sprite Import Fix",
                $"Fixed {fixedCount} texture(s) to Sprite type.\n\n" +
                "All images in Resources/Sprites/ are now set as Sprite (2D and UI).",
                "OK");
        }
    }
}
