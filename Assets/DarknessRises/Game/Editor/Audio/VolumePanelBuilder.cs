using DarknessRises.Game.Audio;
using DarknessRises.Game.Editor.Health;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Audio
{
    /// <summary>
    /// Builds the volume panel, the same one for the pause menu and the Shop: a shade over the whole screen that takes
    /// every touch, and a dark card in the middle with a big slider each for Music, Sound and Ambient, their percents,
    /// and BACK. Built from code in the HUD's arcade look, with no imported art; the card fits a landscape phone's safe
    /// area at any aspect from 4:3 up, in HUD units (1080 tall).
    /// </summary>
    public static class VolumePanelBuilder
    {
        public const string PanelName = "Volume Panel";

        // The sliders, from the top.
        private static readonly AudioCategory[] Order = { AudioCategory.Music, AudioCategory.Sound, AudioCategory.Ambient };

        private static readonly Color ShadeColor = new(0f, 0f, 0f, 0.82f);
        private static readonly Color CardColor = new(0.075f, 0.075f, 0.09f, 1f);
        private static readonly Color FrameColor = new(0.85f, 0.85f, 0.85f, 0.18f);
        private static readonly Color TextColor = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color TrackColor = new(0.2f, 0.2f, 0.22f, 1f);
        private static readonly Color FillColor = new(0.8f, 0.12f, 0.1f, 1f);
        private static readonly Color HandleColor = new(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color ButtonColor = new(0.2f, 0.2f, 0.22f, 1f);

        private static readonly Vector2 CardSize = new(1160f, 760f);
        private const float FrameWidth = 3f;

        private const float TitleFontSize = 90f;
        private static readonly Vector2 TitlePosition = new(0f, 280f);
        private static readonly Vector2 TitleSize = new(800f, 120f);

        private const float FirstRowY = 130f;
        private const float RowSpacing = 140f;
        private const float NameFontSize = 46f;
        private const float NameX = -360f;
        private static readonly Vector2 NameSize = new(320f, 90f);
        private const float SliderX = 70f;
        private static readonly Vector2 SliderSize = new(580f, 110f);
        private const float TrackHeight = 22f;
        private static readonly Vector2 HandleSize = new(64f, 84f);
        private const float ValueFontSize = 46f;
        private const float ValueX = 470f;
        private static readonly Vector2 ValueSize = new(170f, 90f);

        private const float ButtonFontSize = 44f;
        private static readonly Vector2 CloseButtonPosition = new(0f, -290f);
        private static readonly Vector2 CloseButtonSize = new(380f, 100f);

        /// <summary>
        /// Builds the panel under <paramref name="parent"/> (a canvas, or a panel that fills one), hidden, opened by
        /// <paramref name="openButton"/> and setting <paramref name="volumes"/>.
        /// </summary>
        public static VolumePanelView Build(Transform parent, Button openButton, TextMeshProUGUI openLabel, SceneAudioVolumes volumes)
        {
            // The shade takes every touch and click, so nothing behind the panel is pressed.
            var panel = HealthBuilder.AddPanel(parent, PanelName, ShadeColor);
            panel.GetComponent<Image>().raycastTarget = true;

            var frame = AddImage(panel, "Frame", FrameColor, Vector2.zero, CardSize + Vector2.one * (FrameWidth * 2f));
            frame.raycastTarget = false;
            var card = AddImage(panel, "Card", CardColor, Vector2.zero, CardSize);
            card.raycastTarget = false;
            var cardRect = card.rectTransform;

            var title = HealthBuilder.AddText(cardRect, "Title", "SOUND", TitleFontSize, TextColor, TitlePosition, TitleSize);
            title.fontStyle = FontStyles.Bold;

            var sliders = new Slider[AudioVolumes.CategoryCount];
            var names = new TextMeshProUGUI[AudioVolumes.CategoryCount];
            var values = new TextMeshProUGUI[AudioVolumes.CategoryCount];
            for (var row = 0; row < Order.Length; row++)
            {
                var category = Order[row];
                var index = AudioVolumes.IndexOf(category);
                var y = FirstRowY - row * RowSpacing;
                var label = category.ToString().ToUpperInvariant();
                names[index] = HealthBuilder.AddText(cardRect, $"{category} Name", label, NameFontSize, TextColor, new Vector2(NameX, y), NameSize);
                names[index].alignment = TextAlignmentOptions.Left;
                names[index].fontStyle = FontStyles.Bold;
                sliders[index] = AddSlider(cardRect, $"{category} Slider", new Vector2(SliderX, y));
                values[index] = HealthBuilder.AddText(cardRect, $"{category} Value", "100%", ValueFontSize, TextColor, new Vector2(ValueX, y), ValueSize);
                values[index].alignment = TextAlignmentOptions.Right;
            }

            var releases = new SliderRelease[AudioVolumes.CategoryCount];
            for (var i = 0; i < releases.Length; i++)
            {
                releases[i] = sliders[i].gameObject.AddComponent<SliderRelease>();
            }

            var close = AddButton(cardRect, "Back Button", "BACK", CloseButtonPosition, CloseButtonSize, out var closeLabel);

            var sampleObject = new GameObject("Sample", typeof(AudioSource));
            sampleObject.transform.SetParent(panel, false);
            var sample = sampleObject.GetComponent<AudioSource>();
            sample.playOnAwake = false;
            sample.spatialBlend = 0f;
            AudioRouting.Route(sample, AudioCategory.Sound);

            var view = panel.gameObject.AddComponent<VolumePanelView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("volumes").objectReferenceValue = volumes;
            serialized.FindProperty("openButton").objectReferenceValue = openButton;
            serialized.FindProperty("openLabel").objectReferenceValue = openLabel;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("closeLabel").objectReferenceValue = closeLabel;
            SetArray(serialized.FindProperty("sliders"), sliders);
            SetArray(serialized.FindProperty("names"), names);
            SetArray(serialized.FindProperty("values"), values);
            SetArray(serialized.FindProperty("releases"), releases);
            serialized.FindProperty("sample").objectReferenceValue = sample;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Until the panel has its wording and sample (VolumePanelView.Use).
            openButton.interactable = false;
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>A button in the panel's look: <paramref name="text"/> on a dark face, bold.</summary>
        public static Button AddButton(RectTransform parent, string name, string text, Vector2 position, Vector2 size, out TextMeshProUGUI label)
        {
            var image = AddImage(parent, name, ButtonColor, position, size);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = HealthBuilder.AddText(image.rectTransform, "Label", text, ButtonFontSize, TextColor, Vector2.zero, size);
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        // A big slider from 0 to 100 in whole steps: a thin track that fills from the left and a tall handle. The whole
        // row's height takes the touch, not only the track.
        private static Slider AddSlider(RectTransform parent, string name, Vector2 position)
        {
            var hitArea = AddImage(parent, name, Color.clear, position, SliderSize);
            hitArea.raycastTarget = true;
            var root = hitArea.rectTransform;

            var track = AddImage(root, "Track", TrackColor, Vector2.zero, Vector2.zero);
            StretchAcross(track.rectTransform, TrackHeight, 0f);
            track.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(root, false);
            StretchAcross(fillArea, TrackHeight, 0f);
            var fill = AddImage(fillArea, "Fill", FillColor, Vector2.zero, Vector2.zero);
            fill.raycastTarget = false;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            // The handle's centre travels the track's length, inset by half the handle so it never pokes out. The slider
            // anchors the handle to the full height of this area, so the area is the handle's height.
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root, false);
            StretchAcross(handleArea, HandleSize.y, HandleSize.x / 2f);
            var handle = AddImage(handleArea, "Handle", HandleColor, Vector2.zero, Vector2.zero);
            handle.raycastTarget = false;
            var handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(HandleSize.x, 0f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = VolumeCurve.MinPercent;
            slider.maxValue = VolumeCurve.MaxPercent;
            slider.wholeNumbers = true;
            slider.value = VolumeCurve.MaxPercent;
            var colors = slider.colors;
            colors.fadeDuration = 0f;
            slider.colors = colors;

            // Arrow keys and a gamepad would move a selected slider; the panel is for touch and mouse.
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        // Full width of the parent (less an inset at each end), <paramref name="height"/> tall, centred vertically.
        private static void StretchAcross(RectTransform rect, float height, float inset)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, -height / 2f);
            rect.offsetMax = new Vector2(-inset, height / 2f);
        }

        private static Image AddImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetArray(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }
    }
}
