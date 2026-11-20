using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Shop
{
    /// <summary>
    /// Builds the Shop: its definition asset (the Primary weapons it sells, with first-pass prices) and its scene, which
    /// the boot loads through Addressables between Runs. The scene is a screen and nothing else: one overlay canvas laid
    /// out for a landscape phone inside the safe area, an event system, a listener and a sound. It has no camera, lights
    /// or world, so nothing of a Level is behind it. Everything is built from code; there is no imported art.
    /// </summary>
    public static class ShopBuilder
    {
        public const string ScenePath = "Assets/DarknessRises/Scenes/Shop.unity";

        private const string ShopDefinitionPath = "Assets/DarknessRises/Configuration/ShopDefinition.asset";

        // An approved Free Pack placeholder, shared with the Clear banner: its coins start after a quiet lead-in.
        private const string BuyClipPath = "Assets/Free Pack/Magic Spell_Coins_2.wav";
        private const float BuyClipStartSeconds = 1.3f;

        // First-pass prices. A cleared Level 1 pays about 575 Credits (1150 with the end-of-Run ad), so the Machine gun
        // takes four or five such Runs, or two or three with the ad; the Flamethrower is the longer goal after it.
        private const int MachineGunPrice = 2500;
        private const int FlamethrowerPrice = 6000;

        // Above the Level's HUD order, below the boot's version label.
        private const int SortingOrder = 10;

        private static readonly Color BackgroundColor = new(0.025f, 0.025f, 0.035f, 1f);
        private static readonly Color CardColor = new(0.075f, 0.075f, 0.09f, 1f);
        private static readonly Color FrameColor = new(0.85f, 0.85f, 0.85f, 0.18f);
        private static readonly Color TextColor = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color DimTextColor = new(0.6f, 0.6f, 0.62f, 1f);
        private static readonly Color PlayColor = new(0.8f, 0.12f, 0.1f, 1f);
        private static readonly Color ButtonColor = new(0.2f, 0.2f, 0.22f, 1f);

        // Until the Credits definition colours them at runtime.
        private static readonly Color CreditsColor = new(0.35f, 1f, 0.78f, 1f);

        // Margins from the safe area's edges, in HUD units (1080 tall).
        private const float SideMargin = 80f;
        private const float TopBarHeight = 170f;
        private const float BottomBarHeight = 190f;

        private const float TitleFontSize = 110f;
        private static readonly Vector2 TitleSize = new(700f, 130f);
        private const float CreditsCaptionFontSize = 34f;
        private const float CreditsValueFontSize = 88f;
        private static readonly Vector2 CreditsSize = new(700f, 100f);
        private const float CreditsCaptionTop = 26f;
        private const float CreditsValueTop = 58f;

        private const float CardSpacing = 36f;
        private const float CardHeight = 400f;
        private const float CardFrameWidth = 3f;
        private const float CardPadding = 30f;
        private const float CardTitleFontSize = 58f;
        private const float CardTitleHeight = 80f;
        private const float CardDescriptionFontSize = 30f;
        private const float CardDescriptionHeight = 90f;
        private const float CardButtonHeight = 92f;
        private const float CardButtonFontSize = 40f;
        private const float CardHintFontSize = 26f;
        private const float CardHintHeight = 40f;

        private const float PrimaryLineFontSize = 44f;
        private static readonly Vector2 PrimaryLineSize = new(1100f, 70f);
        private static readonly Vector2 PlayButtonSize = new(460f, 130f);
        private const float PlayFontSize = 84f;

        /// <summary>
        /// Loads the Shop definition, creating it if it's missing. A definition with no Weapons gets the three Primaries:
        /// the Pistol owned from the start, the Machine gun and the Flamethrower to unlock.
        /// </summary>
        public static ShopDefinition LoadOrCreateShop(WeaponDefinition pistol, WeaponDefinition machineGun, WeaponDefinition flamethrower)
        {
            var definition = TestRoomBuilder.LoadOrCreate<ShopDefinition>(ShopDefinitionPath);
            var serialized = new SerializedObject(definition);
            var weapons = serialized.FindProperty("weapons");
            if (weapons.arraySize == 0)
            {
                weapons.arraySize = 3;
                Author(weapons.GetArrayElementAtIndex(0), "pistol", "PISTOL", "Steady and accurate. Yours from the start.", pistol, 0);
                Author(weapons.GetArrayElementAtIndex(1), "machine-gun", "MACHINE GUN", "Fast fire and a deep magazine.", machineGun, MachineGunPrice);
                Author(weapons.GetArrayElementAtIndex(2), "flamethrower", "FLAMETHROWER", "A stream of fire that lights the Room.", flamethrower, FlamethrowerPrice);
            }

            var clip = serialized.FindProperty("screen.buyClip");
            if (AddressableContent.Referenced<AudioClip>(clip) == null)
            {
                serialized.FindProperty("screen.buyClipStartSeconds").floatValue = BuyClipStartSeconds;
            }

            AddressableContent.ReferenceIfEmpty(clip, () => GameplaySoundImport.Load(BuyClipPath), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(clip);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void Author(SerializedProperty offer, string id, string displayName, string description, WeaponDefinition weapon, int price)
        {
            offer.FindPropertyRelative("id").stringValue = id;
            offer.FindPropertyRelative("displayName").stringValue = displayName;
            offer.FindPropertyRelative("description").stringValue = description;
            offer.FindPropertyRelative("weapon").objectReferenceValue = weapon;
            offer.FindPropertyRelative("price").intValue = price;
        }

        [MenuItem("Darkness Rises/Rebuild Shop Scene")]
        public static void Build()
        {
            HudBuilder.RequireTextMeshProEssentials();

            // A new single scene unloads unused assets, so load the assets only after creating it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TestRoomBuilder.MakeDark();

            // The configuration the Level and the Test Room were built with. Authoring it again here would only move
            // content shared with the Enemies between Addressables groups; it's created only when there is none yet.
            var configuration = AssetDatabase.LoadAssetAtPath<GameConfiguration>(TestRoomBuilder.GameConfigurationPath);
            if (configuration == null || configuration.Shop == null)
            {
                configuration = TestRoomBuilder.LoadOrCreateConfiguration();
            }

            var view = BuildScreen();

            // The screen's buttons are clicked through the Input System's UI module, by touch or by mouse.
            new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // The Shop has no character to carry the ears.
            new GameObject("Ears", typeof(AudioListener));

            var root = new GameObject("Shop Root", typeof(ShopRoot)).GetComponent<ShopRoot>();
            var serializedRoot = new SerializedObject(root);
            serializedRoot.FindProperty("configuration").objectReferenceValue = configuration;
            serializedRoot.FindProperty("view").objectReferenceValue = view;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Loaded additively through Addressables by the boot scene; only the boot scene is in Build Settings.
            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), AddressableContent.ShopGroup());
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShopBuilder] Built {ScenePath}.");
        }

        private static ShopView BuildScreen()
        {
            var canvasObject = new GameObject("Shop", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ShopView));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudBuilder.ReferenceResolution;
            scaler.matchWidthOrHeight = HudBuilder.MatchHeight;

            // Behind the whole screen, the notch and the rounded corners included.
            var background = HealthBuilder.AddPanel(canvasObject.transform, "Background", BackgroundColor);
            background.GetComponent<Image>().raycastTarget = false;

            var safeArea = AddRect("Safe Area", canvasObject.transform);
            Stretch(safeArea, 0f, 0f, 0f, 0f);

            // The top bar: the title on the left, the Credits in the bank on the right.
            var title = AddText(safeArea, "Title", "SHOP", TitleFontSize, HealthBuilder.TitleColor, TextAlignmentOptions.Left);
            Corner(title.rectTransform, new Vector2(0f, 1f), new Vector2(SideMargin, -(TopBarHeight - TitleSize.y) / 2f), TitleSize);
            title.fontStyle = FontStyles.Bold;

            var creditsCaption = AddText(safeArea, "Credits Caption", "CREDITS", CreditsCaptionFontSize, CreditsColor, TextAlignmentOptions.Right);
            Corner(creditsCaption.rectTransform, new Vector2(1f, 1f), new Vector2(-SideMargin, -CreditsCaptionTop), new Vector2(CreditsSize.x, CreditsCaptionFontSize * 1.3f));
            var creditsValue = AddText(safeArea, "Credits Value", "0", CreditsValueFontSize, CreditsColor, TextAlignmentOptions.Right);
            Corner(creditsValue.rectTransform, new Vector2(1f, 1f), new Vector2(-SideMargin, -CreditsValueTop), CreditsSize);
            creditsValue.fontStyle = FontStyles.Bold;

            // The Weapons' cards, side by side between the bars, sharing the width.
            var cards = AddRect("Cards", safeArea);
            Stretch(cards, SideMargin, SideMargin, TopBarHeight, BottomBarHeight);
            var row = cards.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = CardSpacing;
            row.childAlignment = TextAnchor.UpperCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;
            var cardTemplate = BuildCardTemplate(cards);

            // The bottom bar: the Primary the next Run carries on the left, the big PLAY on the right.
            var primaryLine = AddText(safeArea, "Primary Line", "NEXT RUN: PISTOL", PrimaryLineFontSize, TextColor, TextAlignmentOptions.Left);
            Corner(primaryLine.rectTransform, new Vector2(0f, 0f), new Vector2(SideMargin, (BottomBarHeight - PrimaryLineSize.y) / 2f), PrimaryLineSize);

            var playButton = AddButton(safeArea, "Play Button", "PLAY", PlayFontSize, PlayColor, Color.white, out var playLabel);
            Corner((RectTransform)playButton.transform, new Vector2(1f, 0f), new Vector2(-SideMargin, (BottomBarHeight - PlayButtonSize.y) / 2f), PlayButtonSize);
            playLabel.fontStyle = FontStyles.Bold;

            var sound = new GameObject("Sound", typeof(AudioSource)).GetComponent<AudioSource>();
            sound.transform.SetParent(canvasObject.transform, false);
            sound.playOnAwake = false;
            sound.spatialBlend = 0f;

            var view = canvasObject.GetComponent<ShopView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("safeArea").objectReferenceValue = safeArea;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("creditsCaption").objectReferenceValue = creditsCaption;
            serialized.FindProperty("creditsValue").objectReferenceValue = creditsValue;
            serialized.FindProperty("primaryLine").objectReferenceValue = primaryLine;
            serialized.FindProperty("cards").objectReferenceValue = cards;
            serialized.FindProperty("cardTemplate").objectReferenceValue = cardTemplate;
            serialized.FindProperty("playButton").objectReferenceValue = playButton;
            serialized.FindProperty("playLabel").objectReferenceValue = playLabel;
            serialized.FindProperty("sound").objectReferenceValue = sound;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // One Weapon's card: a framed panel with its name, its pitch, one button and a hint under it. The Shop copies it
        // for each Weapon; the template itself stays hidden.
        private static WeaponCardView BuildCardTemplate(RectTransform cards)
        {
            var card = AddRect("Weapon Card", cards);
            var frame = card.gameObject.AddComponent<Image>();
            frame.color = FrameColor;
            frame.raycastTarget = false;
            var layout = card.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = CardHeight;
            layout.flexibleWidth = 1f;

            var fill = AddRect("Fill", card);
            Stretch(fill, CardFrameWidth, CardFrameWidth, CardFrameWidth, CardFrameWidth);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = CardColor;
            fillImage.raycastTarget = false;

            var title = AddText(card, "Title", "WEAPON", CardTitleFontSize, Color.white, TextAlignmentOptions.Left);
            TopStrip(title.rectTransform, CardPadding, CardPadding, CardTitleHeight);
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = CardTitleFontSize * 0.6f;
            title.fontSizeMax = CardTitleFontSize;
            title.textWrappingMode = TextWrappingModes.NoWrap;

            var description = AddText(card, "Description", "What it's like to fire.", CardDescriptionFontSize, DimTextColor, TextAlignmentOptions.TopLeft);
            TopStrip(description.rectTransform, CardPadding, CardPadding + CardTitleHeight + 10f, CardDescriptionHeight);

            var button = AddButton(card, "Button", "UNLOCK", CardButtonFontSize, ButtonColor, Color.white, out var buttonLabel);
            BottomStrip((RectTransform)button.transform, CardPadding, CardPadding + CardHintHeight, CardButtonHeight);
            buttonLabel.fontStyle = FontStyles.Bold;

            var hint = AddText(card, "Hint", string.Empty, CardHintFontSize, DimTextColor, TextAlignmentOptions.Center);
            BottomStrip(hint.rectTransform, CardPadding, CardPadding * 0.5f, CardHintHeight);

            var view = card.gameObject.AddComponent<WeaponCardView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("frame").objectReferenceValue = frame;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("description").objectReferenceValue = description;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("buttonImage").objectReferenceValue = button.targetGraphic;
            serialized.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
            serialized.FindProperty("hint").objectReferenceValue = hint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
            return view;
        }

        private static RectTransform AddRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        // Fills its parent, inset by the given margins.
        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        // Sits in a corner of its parent: anchored and pivoted there, offset inwards.
        private static void Corner(RectTransform rect, Vector2 corner, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        // Spans its parent's width, inset at the sides, a fixed height down from the top.
        private static void TopStrip(RectTransform rect, float side, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(side, -top - height);
            rect.offsetMax = new Vector2(-side, -top);
        }

        // Spans its parent's width, inset at the sides, a fixed height up from the bottom.
        private static void BottomStrip(RectTransform rect, float side, float bottom, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(side, bottom);
            rect.offsetMax = new Vector2(-side, bottom + height);
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(parent, false);
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static Button AddButton(
            RectTransform parent, string name, string text, float fontSize, Color color, Color textColor, out TextMeshProUGUI label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            // The view sets each state's colours itself, so a disabled button isn't also tinted by the button.
            var colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;

            label = AddText(rect, "Label", text, fontSize, textColor, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
            return button;
        }
    }
}
