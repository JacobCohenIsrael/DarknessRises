using System;
using System.IO;
using DarknessRises.Game.Editor.Content;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// The HUD's flat icons, drawn here as white shapes on clear so the game needs no art for them; the HUD tints them.
    /// Each is written once as a sprite under the HUD textures folder and shared content, so every scene's bundle
    /// depends on one copy. Delete a PNG to have it drawn again.
    /// </summary>
    public static class HudIcons
    {
        internal const string TexturesFolder = "Assets/DarknessRises/Textures/Hud";

        private const int IconSize = 128;
        private const int VignetteSize = 256;

        // The pixel width the shapes' edges are smoothed over, in the 0-1 space of the icon.
        private const float Smoothing = 1.5f / IconSize;

        /// <summary>The Flare: a bright core with eight rays.</summary>
        public static Sprite Flare() => LoadOrCreate("SkillFlare", IconSize, FlareCoverage);

        /// <summary>A heart, for Adrenaline.</summary>
        public static Sprite Heart() => LoadOrCreate("SkillHeart", IconSize, HeartCoverage);

        /// <summary>The Lure: a little rattle can with sound waves coming off it either side.</summary>
        public static Sprite Lure() => LoadOrCreate("SkillLure", IconSize, LureCoverage);

        /// <summary>A hand grenade: a ribbed body, its cap, lever and pin ring.</summary>
        public static Sprite Grenade() => LoadOrCreate("Grenade", IconSize, GrenadeCoverage);

        /// <summary>A thin ring, for the reload ring around the crosshair and the Skill buttons' rims.</summary>
        public static Sprite Ring() => LoadOrCreate("Ring", IconSize, RingCoverage);

        /// <summary>A filled disc with a smooth edge.</summary>
        public static Sprite Disc() => LoadOrCreate("Disc", IconSize, DiscCoverage);

        /// <summary>Clear in the middle, solid at the edges: stretched over the screen for the hurt edge.</summary>
        public static Sprite Vignette() => LoadOrCreate("HurtVignette", VignetteSize, VignetteCoverage);

        private static Sprite LoadOrCreate(string name, int size, Func<float, float, float> coverage)
        {
            var path = $"{TexturesFolder}/{name}.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                TestRoomBuilder.EnsureFolder(TexturesFolder);
                var pixels = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        // Row 0 is the bottom of the texture; shapes are drawn in 0-1 with y up.
                        var alpha = Mathf.Clamp01(coverage((x + 0.5f) / size, (y + 0.5f) / size));
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                    }
                }

                var generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
                generated.SetPixels32(pixels);
                generated.Apply();
                File.WriteAllBytes(path, generated.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(generated);

                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path), AddressableContent.SharedGroup);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Coverage of a signed distance: 1 well inside, 0 well outside, smooth over a pixel or so at the edge.
        private static float Fill(float distance) => Mathf.Clamp01(0.5f - distance / Smoothing);

        private static float Circle(Vector2 p, Vector2 centre, float radius) => (p - centre).magnitude - radius;

        private static float Box(Vector2 p, Vector2 centre, Vector2 halfSize)
        {
            var d = new Vector2(Mathf.Abs(p.x - centre.x), Mathf.Abs(p.y - centre.y)) - halfSize;
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float radius)
        {
            var pa = p - a;
            var ba = b - a;
            var h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - radius;
        }

        private static float FlareCoverage(float x, float y)
        {
            var p = new Vector2(x, y);
            var centre = new Vector2(0.5f, 0.5f);
            var distance = Circle(p, centre, 0.2f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

                // Long rays on the axes, short ones between, tapering outward.
                var length = (i & 1) == 0 ? 0.45f : 0.37f;
                var ray = Segment(p, centre + direction * 0.27f, centre + direction * length, 0.035f);
                distance = Mathf.Min(distance, ray);
            }

            return Fill(distance);
        }

        // Inigo Quilez's heart distance, in a space where the heart's tip is at the origin and it stands about 1.1 tall.
        private static float HeartCoverage(float x, float y)
        {
            const float scale = 1.25f;
            var p = new Vector2(Mathf.Abs(x - 0.5f) * scale, (y - 0.5f) * scale + 0.55f);
            float distance;
            if (p.y + p.x > 1f)
            {
                distance = (p - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2f) / 4f;
            }
            else
            {
                var toTop = (p - new Vector2(0f, 1f)).sqrMagnitude;
                var toSide = (p - 0.5f * Mathf.Max(p.x + p.y, 0f) * Vector2.one).sqrMagnitude;
                distance = Mathf.Sqrt(Mathf.Min(toTop, toSide)) * Mathf.Sign(p.x - p.y);
            }

            return Fill(distance / scale);
        }

        // An upright can with a band round its middle, and two arcs of sound either side of it.
        private static float LureCoverage(float x, float y)
        {
            var p = new Vector2(x, y);
            var centre = new Vector2(0.5f, 0.5f);

            var can = Box(p, centre, new Vector2(0.09f, 0.17f)) - 0.05f;
            var band = Box(p, centre, new Vector2(0.2f, 0.025f));
            can = Mathf.Max(can, -band);
            var cap = Box(p, centre + new Vector2(0f, 0.25f), new Vector2(0.06f, 0.025f));
            var distance = Mathf.Min(can, cap);

            // Arcs of sound, left and right, each within 40 degrees of the horizontal.
            var offset = p - centre;
            var side = Mathf.Abs(Mathf.Atan2(offset.y, Mathf.Abs(offset.x))) * Mathf.Rad2Deg;
            if (side <= 40f)
            {
                var near = Mathf.Abs(offset.magnitude - 0.29f) - 0.03f;
                var far = Mathf.Abs(offset.magnitude - 0.41f) - 0.03f;
                distance = Mathf.Min(distance, Mathf.Min(near, far));
            }

            return Fill(distance);
        }

        private static float GrenadeCoverage(float x, float y)
        {
            var p = new Vector2(x, y);
            var bodyCentre = new Vector2(0.47f, 0.4f);

            // An upright oval body, with a ribbed cross cut into it.
            var q = new Vector2((p.x - bodyCentre.x) / 0.27f, (p.y - bodyCentre.y) / 0.31f);
            var body = (q.magnitude - 1f) * 0.27f;
            var rib = Mathf.Min(Box(p, bodyCentre, new Vector2(0.4f, 0.018f)), Box(p, bodyCentre, new Vector2(0.018f, 0.4f)));
            body = Mathf.Max(body, -rib);

            var cap = Box(p, new Vector2(0.47f, 0.75f), new Vector2(0.1f, 0.05f));
            var lever = Segment(p, new Vector2(0.55f, 0.79f), new Vector2(0.78f, 0.58f), 0.03f);
            var pinRing = Mathf.Abs(Circle(p, new Vector2(0.31f, 0.82f), 0.07f)) - 0.022f;

            var distance = Mathf.Min(Mathf.Min(body, cap), Mathf.Min(lever, pinRing));
            return Fill(distance);
        }

        private static float RingCoverage(float x, float y)
        {
            var distance = Mathf.Abs(Circle(new Vector2(x, y), new Vector2(0.5f, 0.5f), 0.455f)) - 0.035f;
            return Fill(distance);
        }

        private static float DiscCoverage(float x, float y) => Fill(Circle(new Vector2(x, y), new Vector2(0.5f, 0.5f), 0.49f));

        // 0 inside an oval over most of the screen, rising smoothly to full at the corners.
        private static float VignetteCoverage(float x, float y)
        {
            var d = new Vector2((x - 0.5f) * 2f, (y - 0.5f) * 2f).magnitude;
            var t = Mathf.Clamp01((d - 0.62f) / (1.3f - 0.62f));
            return t * t * (3f - 2f * t);
        }
    }
}
