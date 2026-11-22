using System.IO;
using DarknessRises.Game.Death;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Continues;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Hud;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the Score on the HUD: the running Score and the Combo under the hit points with the Score boost beside the
    /// Score, the pooled points and hit markers that pop up in the Room, and the result card under the Clear banner with
    /// its accuracy stamp and Score boost offer. <see cref="BuildBoostAd"/> adds the boost's placeholder ad on the
    /// Screens canvas, once the death and end screens are built. Only the structure is built here;
    /// wording, colours and timing come from the configuration's <see cref="ScoreHudSettings"/> when they're used.
    /// Call it once the HUD and the Station banner are built.
    /// </summary>
    public static class ScoreHudBuilder
    {
        private const string TexturesFolder = "Assets/DarknessRises/Textures/Hud";
        private const string HitMarkerPath = TexturesFolder + "/HitMarker.png";
        private const int HitMarkerTextureSize = 96;
        private const string BarSpritePath = "UI/Skin/UISprite.psd";

        // Under the hit-point pips, at the top left.
        private static readonly Vector2 ScorePosition = new(56f, -96f);
        private static readonly Vector2 ScoreSize = new(420f, 96f);
        private const float CaptionFontSize = 24f;
        private const float ScoreFontSize = 60f;
        private static readonly Vector2 ComboPosition = new(56f, -204f);
        private static readonly Vector2 ComboSize = new(320f, 90f);
        private const float ComboFontSize = 64f;
        private static readonly Vector2 ComboBarSize = new(220f, 8f);

        // The Score boost, right of the Score: "x2" over its seconds left.
        private static readonly Vector2 BoostPosition = new(400f, -96f);
        private static readonly Vector2 BoostSize = new(130f, 96f);
        private const float BoostFontSize = 52f;
        private const float BoostTimerFontSize = 28f;

        // Enough for the kills and hits of a held trigger; the oldest is reused when all are busy.
        private const int PointsPoolSize = 8;
        private const int MarkerPoolSize = 8;
        private static readonly Vector2 PointsSize = new(420f, 80f);

        // The result card, under the Clear banner.
        private static readonly Vector2 CardSize = new(1100f, 300f);
        private const float CardBandHeight = 320f;
        private const float GradeFontSize = 250f;
        private static readonly Vector2 GradeOffset = new(-330f, 0f);
        private static readonly Vector2 GradeSize = new(300f, 300f);
        private const float StatFontSize = 46f;
        private const float StatRowHeight = 62f;
        private const float StatTopRow = 96f;
        private static readonly Vector2 StatCaptionOffset = new(20f, 0f);
        private static readonly Vector2 StatCaptionSize = new(320f, 60f);
        private static readonly Vector2 StatValueOffset = new(330f, 0f);
        private static readonly Vector2 StatValueSize = new(260f, 60f);
        private const float PointsFontSize = 58f;

        // The accuracy stamp, right of the points; it punches about its own middle.
        private static readonly Vector2 StampPosition = new(430f, StatTopRow - 3f * StatRowHeight - 12f);
        private static readonly Vector2 StampSize = new(400f, 70f);
        private const float StampFontSize = 46f;
        private const float StampTiltDegrees = 6f;

        // The Score boost offer, under the card.
        private static readonly Vector2 OfferPosition = new(0f, -215f);
        private static readonly Vector2 OfferSize = new(640f, 92f);
        private const float OfferFontSize = 40f;
        private const float OfferHintFontSize = 26f;
        private static readonly Color OfferColor = new(0.07f, 0.3f, 0.24f, 1f);
        private static readonly Color OfferTextColor = new(0.45f, 1f, 0.55f, 1f);

        private const string BoostAdName = "Score Boost Fake Ad";
        private const string BoostAdCountdownFormat = "Your x2 Score in {0}";

        public static void Build(GameRoot gameRoot, Camera worldCamera)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;

            serializedRoot.FindProperty("scorePopups").objectReferenceValue = BuildPopups(hud, worldCamera);
            serializedRoot.FindProperty("stationResult").objectReferenceValue = BuildResultCard(hud);
            serializedRoot.FindProperty("scoreHud").objectReferenceValue = BuildScoreHud(hud);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The Score boost's placeholder ad, last on the Screens canvas so it covers everything. It can be closed
        /// early, which earns nothing. Call it once the death and end screens are built.
        /// </summary>
        public static void BuildBoostAd(GameRoot gameRoot)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var deathFlow = (DeathFlow)serializedRoot.FindProperty("deathFlow").objectReferenceValue;
            serializedRoot.FindProperty("scoreBoostAd").objectReferenceValue =
                ContinueBuilder.BuildFakeAd(deathFlow.transform, BoostAdName, BoostAdCountdownFormat, canClose: true);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ScoreHudView BuildScoreHud(Transform hud)
        {
            var root = new GameObject("Score", typeof(RectTransform), typeof(ScoreHudView));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = ScorePosition;
            rect.sizeDelta = ScoreSize;

            var scoreCaption = AddText(rect, "Caption", "SCORE", CaptionFontSize, TextAlignmentOptions.TopLeft, false);
            Stretch(scoreCaption.rectTransform);
            var scoreValue = AddText(rect, "Value", "0", ScoreFontSize, TextAlignmentOptions.BottomLeft, true);
            Stretch(scoreValue.rectTransform);

            // The Combo sits under the Score and shows only while it multiplies.
            var combo = new GameObject("Combo", typeof(RectTransform)).GetComponent<RectTransform>();
            combo.SetParent(hud, false);
            combo.anchorMin = combo.anchorMax = combo.pivot = new Vector2(0f, 1f);
            combo.anchoredPosition = ComboPosition;
            combo.sizeDelta = ComboSize;

            var comboValue = AddText(combo, "Multiplier", "x2", ComboFontSize, TextAlignmentOptions.Left, true);
            comboValue.rectTransform.anchorMin = comboValue.rectTransform.anchorMax = new Vector2(0f, 0.5f);

            // Kicked about its own middle, so it grows in place.
            comboValue.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            comboValue.rectTransform.sizeDelta = new Vector2(120f, ComboSize.y);
            comboValue.rectTransform.anchoredPosition = new Vector2(60f, 8f);
            comboValue.alignment = TextAlignmentOptions.Center;

            var comboCaption = AddText(combo, "Caption", "COMBO", CaptionFontSize, TextAlignmentOptions.Left, false);
            comboCaption.rectTransform.anchorMin = comboCaption.rectTransform.anchorMax = comboCaption.rectTransform.pivot = new Vector2(0f, 0.5f);
            comboCaption.rectTransform.sizeDelta = new Vector2(180f, 40f);
            comboCaption.rectTransform.anchoredPosition = new Vector2(130f, 8f);

            var bar = new GameObject("Timeout Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bar.rectTransform.SetParent(combo, false);
            bar.rectTransform.anchorMin = bar.rectTransform.anchorMax = bar.rectTransform.pivot = new Vector2(0f, 0f);
            bar.rectTransform.sizeDelta = ComboBarSize;
            bar.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(BarSpritePath);
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillOrigin = (int)Image.OriginHorizontal.Left;
            bar.raycastTarget = false;
            combo.gameObject.SetActive(false);

            // The Score boost beside the Score, shown only while it runs. It pulses about its own middle.
            var boost = new GameObject("Score Boost", typeof(RectTransform)).GetComponent<RectTransform>();
            boost.SetParent(hud, false);
            boost.anchorMin = boost.anchorMax = new Vector2(0f, 1f);
            boost.pivot = new Vector2(0.5f, 0.5f);
            boost.sizeDelta = BoostSize;
            boost.anchoredPosition = BoostPosition + new Vector2(BoostSize.x / 2f, -BoostSize.y / 2f);
            var boostValue = AddText(boost, "Factor", "x2", BoostFontSize, TextAlignmentOptions.Top, true);
            Stretch(boostValue.rectTransform);
            var boostTimer = AddText(boost, "Timer", "60s", BoostTimerFontSize, TextAlignmentOptions.Bottom, true);
            Stretch(boostTimer.rectTransform);
            boost.gameObject.SetActive(false);

            var view = root.GetComponent<ScoreHudView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("scoreCaption").objectReferenceValue = scoreCaption;
            serialized.FindProperty("scoreValue").objectReferenceValue = scoreValue;
            serialized.FindProperty("comboRoot").objectReferenceValue = combo.gameObject;
            serialized.FindProperty("comboCaption").objectReferenceValue = comboCaption;
            serialized.FindProperty("comboValue").objectReferenceValue = comboValue;
            serialized.FindProperty("comboBar").objectReferenceValue = bar;
            serialized.FindProperty("boostRoot").objectReferenceValue = boost;
            serialized.FindProperty("boostValue").objectReferenceValue = boostValue;
            serialized.FindProperty("boostTimer").objectReferenceValue = boostTimer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static ScorePopupsView BuildPopups(Transform hud, Camera worldCamera)
        {
            var root = new GameObject("Score Popups", typeof(RectTransform), typeof(ScorePopupsView));
            var area = root.GetComponent<RectTransform>();
            area.SetParent(hud, false);
            Stretch(area);

            var markerSprite = LoadOrCreateHitMarker();
            var markers = new Image[MarkerPoolSize];
            for (var i = 0; i < markers.Length; i++)
            {
                markers[i] = new GameObject($"Hit Marker {i + 1}", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                Centre(markers[i].rectTransform, area);
                markers[i].sprite = markerSprite;
                markers[i].raycastTarget = false;
                markers[i].enabled = false;
            }

            var points = new TextMeshProUGUI[PointsPoolSize];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = AddText(area, $"Points {i + 1}", string.Empty, CaptionFontSize, TextAlignmentOptions.Center, true);
                Centre(points[i].rectTransform, area);
                points[i].rectTransform.sizeDelta = PointsSize;
                points[i].enabled = false;
            }

            var view = root.GetComponent<ScorePopupsView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("area").objectReferenceValue = area;
            serialized.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            SetArray(serialized.FindProperty("points"), points);
            SetArray(serialized.FindProperty("markers"), markers);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static StationResultView BuildResultCard(Transform hud)
        {
            var root = new GameObject("Station Result", typeof(RectTransform), typeof(CanvasGroup), typeof(StationResultView));
            var area = root.GetComponent<RectTransform>();
            area.SetParent(hud, false);
            Stretch(area);
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var band = new GameObject("Band", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            band.rectTransform.SetParent(area, false);
            band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            band.rectTransform.sizeDelta = new Vector2(0f, CardBandHeight);
            band.raycastTarget = false;
            band.enabled = false;

            var card = new GameObject("Card", typeof(RectTransform)).GetComponent<RectTransform>();
            Centre(card, area);
            card.sizeDelta = CardSize;

            var grade = AddText(card, "Grade", "S", GradeFontSize, TextAlignmentOptions.Center, true);
            Centre(grade.rectTransform, card);
            grade.rectTransform.anchoredPosition = GradeOffset;
            grade.rectTransform.sizeDelta = GradeSize;

            AddStat(card, "Kills", 0, out var killsCaption, out var killsValue);
            AddStat(card, "Accuracy", 1, out var accuracyCaption, out var accuracyValue);
            AddStat(card, "Time", 2, out var timeCaption, out var timeValue);
            var points = AddText(card, "Points", "+0", PointsFontSize, TextAlignmentOptions.Left, true);
            Centre(points.rectTransform, card);
            points.rectTransform.pivot = new Vector2(0f, 0.5f);
            points.rectTransform.anchoredPosition = new Vector2(StatCaptionOffset.x, StatTopRow - 3f * StatRowHeight - 12f);
            points.rectTransform.sizeDelta = new Vector2(StatCaptionSize.x + StatValueSize.x, 70f);

            var stamp = AddText(card, "Accuracy Stamp", "ACCURACY x1.5", StampFontSize, TextAlignmentOptions.Center, true);
            Centre(stamp.rectTransform, card);
            stamp.rectTransform.anchoredPosition = StampPosition;
            stamp.rectTransform.sizeDelta = StampSize;
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, StampTiltDegrees);
            stamp.enabled = false;

            // The offer: the card's one control. Hidden until the boost is on offer.
            var offer = HealthBuilder.AddButton(card, "Score Boost Offer", OfferPosition);
            var offerRect = (RectTransform)offer.transform;
            offerRect.sizeDelta = OfferSize;
            offer.targetGraphic.color = OfferColor;
            var offerLabel = offer.GetComponentInChildren<TextMeshProUGUI>();
            offerLabel.rectTransform.sizeDelta = OfferSize;
            offerLabel.fontSize = OfferFontSize;
            offerLabel.fontStyle = FontStyles.Bold;
            offerLabel.color = OfferTextColor;
            offerLabel.text = "WATCH AD: x2 SCORE";
            var offerHint = AddText(offerRect, "Hint", "for 60s of play", OfferHintFontSize, TextAlignmentOptions.Center, false);
            Centre(offerHint.rectTransform, offerRect);
            offerHint.rectTransform.anchoredPosition = new Vector2(0f, -(OfferSize.y / 2f + OfferHintFontSize));
            offerHint.rectTransform.sizeDelta = new Vector2(OfferSize.x, OfferHintFontSize * 1.5f);
            offerHint.color = OfferTextColor;
            offer.gameObject.SetActive(false);
            card.gameObject.SetActive(false);

            var view = root.GetComponent<StationResultView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("group").objectReferenceValue = group;
            serialized.FindProperty("card").objectReferenceValue = card;
            serialized.FindProperty("band").objectReferenceValue = band;
            serialized.FindProperty("killsCaption").objectReferenceValue = killsCaption;
            serialized.FindProperty("killsValue").objectReferenceValue = killsValue;
            serialized.FindProperty("accuracyCaption").objectReferenceValue = accuracyCaption;
            serialized.FindProperty("accuracyValue").objectReferenceValue = accuracyValue;
            serialized.FindProperty("timeCaption").objectReferenceValue = timeCaption;
            serialized.FindProperty("timeValue").objectReferenceValue = timeValue;
            serialized.FindProperty("pointsValue").objectReferenceValue = points;
            serialized.FindProperty("gradeLetter").objectReferenceValue = grade;
            serialized.FindProperty("accuracyStamp").objectReferenceValue = stamp;
            serialized.FindProperty("boostButton").objectReferenceValue = offer;
            serialized.FindProperty("boostButtonLabel").objectReferenceValue = offerLabel;
            serialized.FindProperty("boostHint").objectReferenceValue = offerHint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // One row of the card: a caption on the left and its number on the right of it.
        private static void AddStat(RectTransform card, string name, int row, out TextMeshProUGUI caption, out TextMeshProUGUI value)
        {
            var y = StatTopRow - row * StatRowHeight;
            caption = AddText(card, $"{name} Caption", name.ToUpperInvariant(), StatFontSize, TextAlignmentOptions.Left, false);
            Centre(caption.rectTransform, card);
            caption.rectTransform.pivot = new Vector2(0f, 0.5f);
            caption.rectTransform.anchoredPosition = new Vector2(StatCaptionOffset.x, y);
            caption.rectTransform.sizeDelta = StatCaptionSize;

            value = AddText(card, $"{name} Value", "0", StatFontSize, TextAlignmentOptions.Left, true);
            Centre(value.rectTransform, card);
            value.rectTransform.pivot = new Vector2(0f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(StatValueOffset.x, y);
            value.rectTransform.sizeDelta = StatValueSize;
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, bool bold)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(parent, false);
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Centre(RectTransform rect, RectTransform parent)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        /// <summary>
        /// The hit marker: four white diagonal ticks around an empty middle, drawn here so the game needs no art for it.
        /// Shared content, so every scene's bundle depends on one copy of it.
        /// </summary>
        private static Sprite LoadOrCreateHitMarker()
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(HitMarkerPath) == null)
            {
                TestRoomBuilder.EnsureFolder(TexturesFolder);
                var pixels = new Color32[HitMarkerTextureSize * HitMarkerTextureSize];
                for (var y = 0; y < HitMarkerTextureSize; y++)
                {
                    for (var x = 0; x < HitMarkerTextureSize; x++)
                    {
                        var alpha = (byte)(HitMarkerCoverage(x + 0.5f, y + 0.5f) * 255f);
                        pixels[y * HitMarkerTextureSize + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                var generated = new Texture2D(HitMarkerTextureSize, HitMarkerTextureSize, TextureFormat.RGBA32, false);
                generated.SetPixels32(pixels);
                generated.Apply();
                File.WriteAllBytes(HitMarkerPath, generated.EncodeToPNG());
                Object.DestroyImmediate(generated);

                AssetDatabase.ImportAsset(HitMarkerPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(HitMarkerPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(HitMarkerPath), AddressableContent.SharedGroup);
            return AssetDatabase.LoadAssetAtPath<Sprite>(HitMarkerPath);
        }

        // How much of the pixel centred on (x, y) the marker covers: four ticks along the diagonals, clear of the middle.
        private static float HitMarkerCoverage(float x, float y)
        {
            const float centre = HitMarkerTextureSize / 2f;
            const float inner = 14f;
            const float outer = 42f;
            const float halfWidth = 4f;
            const float diagonal = 0.70710678f;

            var dx = Mathf.Abs(x - centre);
            var dy = Mathf.Abs(y - centre);

            // Along and across the nearest diagonal.
            var along = (dx + dy) * diagonal;
            var across = Mathf.Abs(dx - dy) * diagonal;
            return Mathf.Min(
                Mathf.Clamp01(halfWidth - across + 0.5f),
                Mathf.Min(Mathf.Clamp01(along - inner + 0.5f), Mathf.Clamp01(outer - along + 0.5f)));
        }
    }
}
