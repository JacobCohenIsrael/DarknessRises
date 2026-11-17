using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Levels
{
    /// <summary>
    /// Placeholder surfaces for the Level's Rooms: tiled floors and rough concrete, generated once as textures so
    /// motion and Obstacles read in a Lightning flash. Stand-ins until the approved Room packs are imported.
    /// Existing textures and materials are kept, so tuning done in the Editor survives a rebuild.
    /// </summary>
    public static class LevelMaterials
    {
        private const string TexturesFolder = "Assets/DarknessRises/Textures/Level";
        private const string MaterialsFolder = "Assets/DarknessRises/Materials/Level";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const int TextureSize = 256;

        /// <summary>A floor of square tiles with dark grout. One texture repeat is two tiles of <paramref name="tileMetres"/>.</summary>
        public static Material TiledFloor(string name, Color tint, float widthMetres, float depthMetres, float tileMetres)
        {
            var texture = LoadOrCreateTexture("FloorTiles", seed: 11, TileShade);
            var material = LoadOrCreateMaterial(name, texture, tint, smoothness: 0.25f);
            var repeat = new Vector2(widthMetres / (tileMetres * 2f), depthMetres / (tileMetres * 2f));
            material.SetTextureScale("_BaseMap", repeat);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Rough, stained concrete.</summary>
        public static Material Concrete(string name, Color tint)
        {
            var texture = LoadOrCreateTexture("Concrete", seed: 23, ConcreteShade);
            return LoadOrCreateMaterial(name, texture, tint, smoothness: 0.1f);
        }

        /// <summary>Plain painted surface for crates and other Obstacles.</summary>
        public static Material Plain(string name, Color tint)
        {
            var texture = LoadOrCreateTexture("Concrete", seed: 23, ConcreteShade);
            return LoadOrCreateMaterial(name, texture, tint, smoothness: 0.2f);
        }

        private static Material LoadOrCreateMaterial(string name, Texture2D texture, Color tint, float smoothness)
        {
            TestRoomBuilder.EnsureFolder(MaterialsFolder);
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(LitShaderName)) { name = name };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D LoadOrCreateTexture(string name, int seed, System.Func<int, int, System.Random, float> shade)
        {
            TestRoomBuilder.EnsureFolder(TexturesFolder);
            var path = $"{TexturesFolder}/{name}.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                return texture;
            }

            var random = new System.Random(seed);
            var pixels = new Color32[TextureSize * TextureSize];
            for (var y = 0; y < TextureSize; y++)
            {
                for (var x = 0; x < TextureSize; x++)
                {
                    var value = (byte)(Mathf.Clamp01(shade(x, y, random)) * 255f);
                    pixels[y * TextureSize + x] = new Color32(value, value, value, 255);
                }
            }

            var generated = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            generated.SetPixels32(pixels);
            generated.Apply();
            File.WriteAllBytes(path, generated.EncodeToPNG());
            Object.DestroyImmediate(generated);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Two by two tiles with grout lines, each tile a slightly different shade, with fine grain.
        private static float TileShade(int x, int y, System.Random random)
        {
            const int tile = TextureSize / 2;
            const int grout = 4;
            var inGrout = x % tile < grout || y % tile < grout;
            if (inGrout)
            {
                return 0.28f + (float)random.NextDouble() * 0.05f;
            }

            var tileIndex = x / tile + 2 * (y / tile);
            var tileShade = 0.78f + 0.06f * Mathf.Sin(tileIndex * 2.3f);
            return tileShade + ((float)random.NextDouble() - 0.5f) * 0.08f + Stain(x, y) * 0.1f;
        }

        // Grain plus broad, soft stains.
        private static float ConcreteShade(int x, int y, System.Random random)
        {
            return 0.75f + ((float)random.NextDouble() - 0.5f) * 0.14f + Stain(x, y) * 0.18f;
        }

        // Smooth, tileable low-frequency variation between -1 and 1.
        private static float Stain(int x, int y)
        {
            var u = x * 2f * Mathf.PI / TextureSize;
            var v = y * 2f * Mathf.PI / TextureSize;
            return 0.5f * Mathf.Sin(u * 2f + Mathf.Cos(v * 3f)) * Mathf.Cos(v * 2f - 0.7f) + 0.3f * Mathf.Sin(u * 5f + v * 3f);
        }
    }
}
