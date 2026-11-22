using System.IO;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Weapons;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds what a new player sees their first touch do: the crosshair where they aim, and the first-run hint over
    /// the first Enemy. Only the structure is built here; sizes, colours, wording and timing come from the
    /// configuration's <see cref="CrosshairSettings"/> and <see cref="ShootHintSettings"/> when they're used.
    /// Call it once the HUD, the Station banner and the character's controls and Weapon are built.
    /// </summary>
    public static class FirstTouchBuilder
    {
        private const string TexturesFolder = "Assets/DarknessRises/Textures/Hud";
        private const string CrosshairPath = TexturesFolder + "/Crosshair.png";
        private const string ArrowPath = TexturesFolder + "/GoArrow.png";
        private const int CrosshairTextureSize = 128;

        // Room for the hint's text at any authored size; it never wraps.
        private static readonly Vector2 HintLabelSize = new(900f, 110f);
        private const float HintLabelGap = 8f;

        public static void Build(GameRoot gameRoot, Transform character, PrimaryWeaponView primaryWeapon, Camera worldCamera)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;
            var controls = character.GetComponent<PlayerControls>();

            serializedRoot.FindProperty("shootHint").objectReferenceValue = BuildShootHint(hud, controls, worldCamera);
            serializedRoot.FindProperty("crosshair").objectReferenceValue = BuildCrosshair(hud, controls, primaryWeapon);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CrosshairView BuildCrosshair(Transform hud, PlayerControls controls, PrimaryWeaponView primaryWeapon)
        {
            // Stretched over the HUD; the crosshair moves inside it, measured from its middle.
            var root = new GameObject("Crosshair", typeof(RectTransform), typeof(CrosshairView));
            var area = Stretch(root.GetComponent<RectTransform>(), hud);

            // The reload ring around the crosshair, under it: a faint full ring with the fill over it.
            var track = new GameObject("Reload Ring", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            track.rectTransform.SetParent(area, false);
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            track.sprite = HudIcons.Ring();
            track.raycastTarget = false;
            var ring = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            ring.rectTransform.SetParent(track.rectTransform, false);
            ring.rectTransform.anchorMin = Vector2.zero;
            ring.rectTransform.anchorMax = Vector2.one;
            ring.rectTransform.offsetMin = ring.rectTransform.offsetMax = Vector2.zero;
            ring.sprite = track.sprite;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            ring.raycastTarget = false;
            track.gameObject.SetActive(false);

            var crosshair = new GameObject("Reticle", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            crosshair.rectTransform.SetParent(area, false);
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = crosshair.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            crosshair.sprite = LoadOrCreateCrosshair();
            crosshair.raycastTarget = false;
            crosshair.enabled = false;

            var view = root.GetComponent<CrosshairView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("area").objectReferenceValue = area;
            serialized.FindProperty("crosshair").objectReferenceValue = crosshair;
            serialized.FindProperty("reloadTrack").objectReferenceValue = track;
            serialized.FindProperty("reloadRing").objectReferenceValue = ring;
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.FindProperty("weaponView").objectReferenceValue = primaryWeapon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static ShootHintView BuildShootHint(Transform hud, PlayerControls controls, Camera worldCamera)
        {
            var root = new GameObject("Shoot Hint", typeof(RectTransform), typeof(CanvasGroup), typeof(ShootHintView));
            var area = Stretch(root.GetComponent<RectTransform>(), hud);
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // The marker's own position is the arrow's tip: the arrow hangs above it and the text above the arrow.
            var marker = new GameObject("Marker", typeof(RectTransform)).GetComponent<RectTransform>();
            marker.SetParent(area, false);
            marker.anchorMin = marker.anchorMax = new Vector2(0.5f, 0.5f);
            marker.pivot = new Vector2(0.5f, 0f);
            marker.sizeDelta = Vector2.zero;

            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            arrow.rectTransform.SetParent(marker, false);
            arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0f);

            // The arrow sprite points right: turned to point down, with its tip on the marker.
            arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            arrow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
            arrow.raycastTarget = false;
            arrow.enabled = false;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(marker, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.sizeDelta = HintLabelSize;
            label.alignment = TextAlignmentOptions.Bottom;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            label.text = string.Empty;
            label.enabled = false;

            var view = root.GetComponent<ShootHintView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("area").objectReferenceValue = area;
            serialized.FindProperty("group").objectReferenceValue = group;
            serialized.FindProperty("marker").objectReferenceValue = marker;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("labelGap").floatValue = HintLabelGap;
            serialized.FindProperty("arrow").objectReferenceValue = arrow;
            serialized.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static RectTransform Stretch(RectTransform rect, Transform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// The crosshair: a white ring with four ticks and a centre dot, drawn here so the game needs no art for it.
        /// Shared content, so every scene's bundle depends on one copy of it.
        /// </summary>
        private static Sprite LoadOrCreateCrosshair()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(CrosshairPath) == null)
            {
                TestRoomBuilder.EnsureFolder(TexturesFolder);
                var pixels = new Color32[CrosshairTextureSize * CrosshairTextureSize];
                for (var y = 0; y < CrosshairTextureSize; y++)
                {
                    for (var x = 0; x < CrosshairTextureSize; x++)
                    {
                        var alpha = (byte)(CrosshairCoverage(x + 0.5f, y + 0.5f) * 255f);
                        pixels[y * CrosshairTextureSize + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                var generated = new Texture2D(CrosshairTextureSize, CrosshairTextureSize, TextureFormat.RGBA32, false);
                generated.SetPixels32(pixels);
                generated.Apply();
                File.WriteAllBytes(CrosshairPath, generated.EncodeToPNG());
                Object.DestroyImmediate(generated);

                AssetDatabase.ImportAsset(CrosshairPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(CrosshairPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(CrosshairPath), AddressableContent.SharedGroup);
            return AssetDatabase.LoadAssetAtPath<Sprite>(CrosshairPath);
        }

        // How much of the pixel centred on (x, y) the crosshair covers, from 0 to 1, with soft edges.
        private static float CrosshairCoverage(float x, float y)
        {
            const float centre = CrosshairTextureSize / 2f;
            const float ringRadius = 40f;
            const float ringHalfWidth = 3.5f;
            const float dotRadius = 4.5f;
            const float tickInner = 30f;
            const float tickOuter = 60f;
            const float tickHalfWidth = 3f;

            var dx = x - centre;
            var dy = y - centre;
            var radius = Mathf.Sqrt(dx * dx + dy * dy);
            var ring = Mathf.Clamp01(ringHalfWidth - Mathf.Abs(radius - ringRadius) + 0.5f);
            var dot = Mathf.Clamp01(dotRadius - radius + 0.5f);

            // Four ticks on the axes, cutting through the ring.
            var along = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            var across = Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy));
            var tick = Mathf.Min(
                Mathf.Clamp01(tickHalfWidth - across + 0.5f),
                Mathf.Min(Mathf.Clamp01(along - tickInner + 0.5f), Mathf.Clamp01(tickOuter - along + 0.5f)));
            return Mathf.Max(ring, Mathf.Max(dot, tick));
        }
    }
}
