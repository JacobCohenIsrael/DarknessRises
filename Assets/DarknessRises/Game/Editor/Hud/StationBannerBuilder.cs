using System.IO;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the Station rhythm's banner on the HUD: one object that shows the Ready banner, the Clear banner and the
    /// Go cue with its arrow, over a dark band, with two 2D voices for their sounds. Authors the sounds if none are set.
    /// Only the structure is built here; texts, colours, sizes and timing come from the configuration's
    /// <see cref="StationBannerSettings"/> when the banner is used. Call it once the HUD is built.
    /// </summary>
    public static class StationBannerBuilder
    {
        // Approved Free Pack placeholders: a metal hit as the character plants, a chime for the clear, a starting shot for the go.
        private const string ReadyClipPath = "Assets/Free Pack/Metal Impact 7.wav";
        private const string ClearClipPath = "Assets/Free Pack/Magic Spell_Coins_2.wav";
        private const string GoClipPath = "Assets/Free Pack/Flare gun 5-2.wav";

        // The chime opens with over a second of near silence.
        private const float ClearClipStartSeconds = 1.3f;

        private const string TexturesFolder = "Assets/DarknessRises/Textures/Hud";
        private const string ArrowPath = TexturesFolder + "/GoArrow.png";
        private const int ArrowTextureSize = 128;

        // Room for the longest banner at the largest authored size; the text never wraps.
        private static readonly Vector2 LabelSize = new(1800f, 320f);
        private const int VoiceCount = 2;

        public static void Build(GameRoot gameRoot, Camera worldCamera)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var configuration = (GameConfiguration)serializedRoot.FindProperty("configuration").objectReferenceValue;
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;

            AuthorSounds(configuration);
            serializedRoot.FindProperty("stationBanner").objectReferenceValue = BuildBanner(hud, worldCamera);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Points each banner's sound at its placeholder clip, unless another clip was chosen in the Inspector, and gives
        /// the clips their import settings.
        /// </summary>
        public static void AuthorSounds(GameConfiguration configuration)
        {
            var serialized = new SerializedObject(configuration);
            var clear = serialized.FindProperty("stationBanners.clear");
            if (AddressableContent.Referenced<AudioClip>(clear.FindPropertyRelative("clip")) == null)
            {
                clear.FindPropertyRelative("clipStartSeconds").floatValue = ClearClipStartSeconds;
            }

            AuthorSound(serialized.FindProperty("stationBanners.ready.clip"), ReadyClipPath);
            AuthorSound(clear.FindPropertyRelative("clip"), ClearClipPath);
            AuthorSound(serialized.FindProperty("stationBanners.go.clip"), GoClipPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Written to disk with the settings' defaults, even when the clips were already set.
            EditorUtility.SetDirty(configuration);
        }

        // Loaded through Addressables with the Level's other content.
        private static void AuthorSound(SerializedProperty clip, string path)
        {
            AddressableContent.ReferenceIfEmpty(clip, () => GameplaySoundImport.Load(path), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(clip);
        }

        private static StationBannerView BuildBanner(Transform hud, Camera worldCamera)
        {
            // Stretched over the HUD; the band and the banner are placed inside it when the banner is used.
            var root = new GameObject("Station Banner", typeof(RectTransform), typeof(CanvasGroup), typeof(StationBannerView));
            var area = root.GetComponent<RectTransform>();
            area.SetParent(hud, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // A dark strip across the screen behind the text, so it reads over a Room lit by Lightning.
            var bandObject = new GameObject("Band", typeof(RectTransform), typeof(Image));
            var band = bandObject.GetComponent<Image>();
            band.rectTransform.SetParent(area, false);
            band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            band.raycastTarget = false;
            band.enabled = false;

            var bannerObject = new GameObject("Banner", typeof(RectTransform));
            var banner = bannerObject.GetComponent<RectTransform>();
            banner.SetParent(area, false);
            banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(0.5f, 0.5f);

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(banner, false);
            label.rectTransform.sizeDelta = LabelSize;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.text = string.Empty;
            label.enabled = false;

            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            arrow.rectTransform.SetParent(banner, false);
            arrow.sprite = LoadOrCreateArrow();
            arrow.raycastTarget = false;
            arrow.enabled = false;

            var voices = new AudioSource[VoiceCount];
            for (var i = 0; i < voices.Length; i++)
            {
                voices[i] = new GameObject($"Voice {i + 1}", typeof(AudioSource)).GetComponent<AudioSource>();
                voices[i].transform.SetParent(area, false);
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
                AudioRouting.Route(voices[i], AudioCategory.Sound);
            }

            var view = root.GetComponent<StationBannerView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("group").objectReferenceValue = group;
            serialized.FindProperty("band").objectReferenceValue = band;
            serialized.FindProperty("banner").objectReferenceValue = banner;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("arrow").objectReferenceValue = arrow;
            serialized.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            var voicesProperty = serialized.FindProperty("voices");
            voicesProperty.arraySize = voices.Length;
            for (var i = 0; i < voices.Length; i++)
            {
                voicesProperty.GetArrayElementAtIndex(i).objectReferenceValue = voices[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// The Go cue's arrow: a white chevron pointing right, drawn here so the game needs no art for it. Shared content,
        /// so every scene's bundle depends on one copy of it.
        /// </summary>
        private static Sprite LoadOrCreateArrow()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath) == null)
            {
                TestRoomBuilder.EnsureFolder(TexturesFolder);
                var pixels = new Color32[ArrowTextureSize * ArrowTextureSize];
                for (var y = 0; y < ArrowTextureSize; y++)
                {
                    for (var x = 0; x < ArrowTextureSize; x++)
                    {
                        var alpha = (byte)(ChevronCoverage(x + 0.5f, y + 0.5f) * 255f);
                        pixels[y * ArrowTextureSize + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                var generated = new Texture2D(ArrowTextureSize, ArrowTextureSize, TextureFormat.RGBA32, false);
                generated.SetPixels32(pixels);
                generated.Apply();
                File.WriteAllBytes(ArrowPath, generated.EncodeToPNG());
                Object.DestroyImmediate(generated);

                AssetDatabase.ImportAsset(ArrowPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(ArrowPath), AddressableContent.SharedGroup);
            return sprite;
        }

        // How much of the pixel centred on (x, y) the chevron covers, from 0 to 1, with a soft edge one pixel wide.
        // The chevron is a filled triangle pointing right with a triangular notch cut into its back.
        private static float ChevronCoverage(float x, float y)
        {
            const float margin = 10f;
            const float notchDepth = 0.38f;
            const float size = ArrowTextureSize;
            var half = size / 2f - margin;
            var across = Mathf.Abs(y - size / 2f) / half;
            if (across >= 1f)
            {
                return 0f;
            }

            var width = size - 2f * margin;
            var tip = margin + width * (1f - across);
            var back = margin + width * notchDepth * (1f - across);

            // Distances to the slanted front and back edges, measured along x and corrected for their slope.
            var slope = half / width;
            var front = (tip - x) * slope / Mathf.Sqrt(1f + slope * slope) * 2f;
            var rear = (x - back) * slope / Mathf.Sqrt(1f + slope * slope) * 2f;
            return Mathf.Clamp01(Mathf.Min(front, rear) + 0.5f);
        }
    }
}
