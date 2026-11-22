using DarknessRises.Game.Audio;
using DarknessRises.Game.Cameras;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Death;
using DarknessRises.Game.Debugging;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Editor.Weapons;
using DarknessRises.Game.Health;
using DarknessRises.Game.Hud;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Health
{
    /// <summary>
    /// Builds the character's health and what hangs off it: the hit-point pips on the HUD, the camera's impulse
    /// listener for the hit shake, the death and end screens, and the god-mode debug action.
    /// Call it once the character, its Weapon and model, the camera and the HUD are built.
    /// </summary>
    public static class HealthBuilder
    {
        // Approved Free Pack placeholders.
        private const string HurtClipPath = "Assets/Free Pack/Monster Bite.wav";
        private const string DeathClipPath = "Assets/Free Pack/Heavy Object Impact 4.wav";

        // The low-health heartbeat: no Free Pack clip is a heartbeat, so a deep thump played low stands in for one.
        private const string HeartbeatClipPath = "Assets/Free Pack/Heavy Object Impact 10.wav";

        private const string GodModeBinding = "<Keyboard>/f1";

        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        private const float MatchHeight = 1f;
        private const int ScreensSortingOrder = 10;

        // Hit-point pips at the top left.
        private static readonly Vector2 PipsMargin = new(56f, 48f);
        private static readonly Vector2 PipSize = new(30f, 30f);
        private const float PipSpacing = 12f;
        private static readonly Color PipFullColor = new(0.85f, 0.18f, 0.16f, 0.9f);
        private static readonly Color PipEmptyColor = new(0.35f, 0.35f, 0.35f, 0.35f);

        // Death and end screens.
        private static readonly Color DeathShadeColor = new(0f, 0f, 0f, 0.6f);
        private static readonly Color EndShadeColor = new(0f, 0f, 0f, 0.92f);
        internal static readonly Color TitleColor = new(0.8f, 0.12f, 0.1f, 1f);
        internal static readonly Color TextColor = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color ButtonColor = new(0.16f, 0.16f, 0.17f, 0.95f);
        private static readonly Color ButtonTextColor = new(0.9f, 0.9f, 0.9f, 1f);
        internal const float TitleFontSize = 110f;
        internal const float StatFontSize = 48f;
        private const float ButtonFontSize = 44f;
        private static readonly Vector2 ButtonSize = new(380f, 100f);
        private const float HintFontSize = 28f;
        private const float CountdownFontSize = 220f;
        private static readonly Color CountdownColor = new(1f, 0.85f, 0.2f, 1f);
        private const float HintGap = 6f;

        // Each end-screen stat is a right-aligned label and a left-aligned value, with a gap between them.
        private static readonly Vector2 StatLabelOffset = new(-200f, 0f);
        private static readonly Vector2 StatLabelSize = new(560f, 70f);
        private static readonly Vector2 StatValueOffset = new(280f, 0f);
        private static readonly Vector2 StatValueSize = new(320f, 70f);

        // The end screen, top to bottom: the title, the Run's record, a rule, the Credits, the ad's offer and the way on.
        private static readonly Vector2 EndTitlePosition = new(0f, 420f);
        private static readonly Vector2 EndScorePosition = new(0f, 300f);
        private static readonly Vector2 EndLevelPosition = new(0f, 235f);
        private static readonly Vector2 EndKillsPosition = new(0f, 170f);
        private static readonly Vector2 EndRulePosition = new(0f, 118f);
        private static readonly Vector2 EndRuleSize = new(900f, 3f);
        private static readonly Color EndRuleColor = new(0.85f, 0.85f, 0.85f, 0.25f);
        private static readonly Vector2 EndCreditsEarnedPosition = new(0f, 62f);
        private static readonly Vector2 EndAdBonusPosition = new(0f, -3f);
        private static readonly Vector2 EndCreditsTotalPosition = new(0f, -85f);
        private const float EndCreditsTotalFontSize = 68f;
        private static readonly Vector2 EndAdButtonPosition = new(0f, -215f);
        private static readonly Vector2 EndAdButtonSize = new(820f, 104f);
        private static readonly Color EndAdButtonColor = new(0.07f, 0.26f, 0.21f, 0.98f);
        private static readonly Vector2 EndShopPosition = new(0f, -410f);

        // Until the Credits definition colours them at runtime.
        private static readonly Color CreditsColor = new(0.35f, 1f, 0.78f, 1f);

        public static void Build(GameRoot gameRoot, Transform character, RideCameraView cameraView)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var configuration = (GameConfiguration)serializedRoot.FindProperty("configuration").objectReferenceValue;
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;

            AuthorPlaceholderSounds(configuration);
            AddImpulseListener(cameraView);
            serializedRoot.FindProperty("characterHealth").objectReferenceValue = BuildCharacterHealth(character);
            serializedRoot.FindProperty("hitPointsHud").objectReferenceValue = BuildHitPointsHud(hud);
            serializedRoot.FindProperty("hurtVignette").objectReferenceValue = BuildHurtVignette(hud);
            serializedRoot.FindProperty("deathFlow").objectReferenceValue = BuildScreens();
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AuthorPlaceholderSounds(GameConfiguration configuration)
        {
            var player = new SerializedObject(configuration.Player);
            ReferenceIfEmpty(player.FindProperty("hitReaction.hurtClip"), HurtClipPath);
            ReferenceIfEmpty(player.FindProperty("hitReaction.deathClip"), DeathClipPath);
            player.ApplyModifiedPropertiesWithoutUndo();

            var serializedConfiguration = new SerializedObject(configuration);
            ReferenceIfEmpty(serializedConfiguration.FindProperty("hurtFeedback.heartbeatClip"), HeartbeatClipPath);
            serializedConfiguration.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configuration);
        }

        // The character's sounds load through Addressables with its other content.
        private static void ReferenceIfEmpty(SerializedProperty clip, string path)
        {
            AddressableContent.ReferenceIfEmpty(clip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(path), AddressableContent.SharedGroup);
        }

        // Every camera of the Ride camera shakes with a hit, whichever is live.
        private static void AddImpulseListener(RideCameraView cameraView)
        {
            foreach (var camera in cameraView.GetComponentsInChildren<CinemachineCamera>(true))
            {
                var listener = camera.gameObject.AddComponent<CinemachineImpulseListener>();
                listener.ApplyAfter = CinemachineCore.Stage.Noise;
                listener.ChannelMask = 1;
                listener.Gain = 1f;
                listener.UseCameraSpace = true;
                listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
                {
                    AmplitudeGain = 1f,
                    FrequencyGain = 1f,
                    Duration = 1f,
                };
            }
        }

        private static CharacterHealthView BuildCharacterHealth(Transform character)
        {
            // Every renderer of the character and its Weapon blinks while invulnerable.
            var blinkRenderers = character.GetComponentsInChildren<Renderer>(true);

            var impulseSource = character.gameObject.AddComponent<CinemachineImpulseSource>();
            impulseSource.ImpulseDefinition.ImpulseChannel = 1;
            impulseSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulseSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;

            // With the listener in camera space, this jolts the view down the screen.
            impulseSource.DefaultVelocity = Vector3.down;

            var voice = new GameObject("Voice", typeof(AudioSource)).GetComponent<AudioSource>();
            voice.transform.SetParent(character, false);
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            AudioRouting.Route(voice, AudioCategory.Sound);

            var view = character.gameObject.AddComponent<CharacterHealthView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("impulseSource").objectReferenceValue = impulseSource;
            serialized.FindProperty("voice").objectReferenceValue = voice;
            serialized.FindProperty("godMode").objectReferenceValue = LoadOrAddDebugAction(GodModeToggle.ActionName, GodModeBinding);
            var list = serialized.FindProperty("blinkRenderers");
            list.arraySize = blinkRenderers.Length;
            for (var i = 0; i < blinkRenderers.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = blinkRenderers[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // The hurt edge: over the whole screen, behind the rest of the HUD, with the heartbeat's 2D source beside it.
        private static HurtVignetteView BuildHurtVignette(Transform hud)
        {
            var root = new GameObject("Hurt Vignette", typeof(RectTransform), typeof(Image), typeof(AudioSource), typeof(HurtVignetteView));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var edge = root.GetComponent<Image>();
            edge.sprite = HudIcons.Vignette();
            edge.color = new Color(1f, 0f, 0f, 0f);
            edge.raycastTarget = false;
            edge.enabled = false;

            var heartbeat = root.GetComponent<AudioSource>();
            heartbeat.playOnAwake = false;
            heartbeat.loop = false;
            heartbeat.spatialBlend = 0f;
            AudioRouting.Route(heartbeat, AudioCategory.Sound);

            var view = root.GetComponent<HurtVignetteView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("edge").objectReferenceValue = edge;
            serialized.FindProperty("heartbeat").objectReferenceValue = heartbeat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static HitPointsHudView BuildHitPointsHud(Transform hud)
        {
            var hitPoints = new GameObject("Hit Points", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(HitPointsHudView));
            var rect = hitPoints.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(PipsMargin.x, -PipsMargin.y);
            rect.sizeDelta = new Vector2(0f, PipSize.y);

            var layout = hitPoints.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = PipSpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var pip = new GameObject("Pip", typeof(RectTransform), typeof(Image));
            var pipRect = pip.GetComponent<RectTransform>();
            pipRect.SetParent(rect, false);
            pipRect.sizeDelta = PipSize;
            var pipImage = pip.GetComponent<Image>();
            pipImage.color = PipFullColor;
            pipImage.raycastTarget = false;

            var view = hitPoints.GetComponent<HitPointsHudView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("pip").objectReferenceValue = pipImage;
            serialized.FindProperty("fullColor").colorValue = PipFullColor;
            serialized.FindProperty("emptyColor").colorValue = PipEmptyColor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static DeathFlow BuildScreens()
        {
            var screens = new GameObject("Screens", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DeathFlow));
            var canvas = screens.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ScreensSortingOrder;
            var scaler = screens.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MatchHeight;

            // The screens' buttons are clicked through the Input System's UI module.
            var eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetParent(screens.transform, false);

            var deathScreen = BuildDeathScreen(screens.transform);
            var endScreen = BuildEndScreen(screens.transform);

            var flow = screens.GetComponent<DeathFlow>();
            var serialized = new SerializedObject(flow);
            serialized.FindProperty("deathScreen").objectReferenceValue = deathScreen;
            serialized.FindProperty("endScreen").objectReferenceValue = endScreen;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return flow;
        }

        private static DeathScreenView BuildDeathScreen(Transform screens)
        {
            var panel = AddPanel(screens, "Death Screen", DeathShadeColor);

            // The wording comes from the Continue definition: "CONTINUE?" over the countdown, or "GAME OVER".
            var title = AddText(panel, "Title", "CONTINUE?", TitleFontSize, TitleColor, new Vector2(0f, 360f), new Vector2(1400f, 160f));
            var countdown = AddText(
                panel, "Countdown", "10", CountdownFontSize, CountdownColor, new Vector2(0f, 190f), new Vector2(600f, 240f));
            countdown.fontStyle = FontStyles.Bold;

            // Continue is offered once per Run, so it goes away with its hint under it.
            var continueButton = AddButton(panel, "Continue", new Vector2(0f, 10f));
            var hintHeight = HintFontSize * 1.5f;
            var hint = AddText(
                (RectTransform)continueButton.transform, "Hint", "Watch an ad, once per Run", HintFontSize, TextColor,
                new Vector2(0f, -(ButtonSize.y + hintHeight) / 2f - HintGap), new Vector2(ButtonSize.x * 2f, hintHeight));
            hint.alignment = TextAlignmentOptions.Center;
            var restart = AddButton(panel, "Restart", new Vector2(0f, -180f));
            var exit = AddButton(panel, "Exit", new Vector2(0f, -310f));

            var view = panel.gameObject.AddComponent<DeathScreenView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("restartButton").objectReferenceValue = restart;
            serialized.FindProperty("exitButton").objectReferenceValue = exit;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("countdownLabel").objectReferenceValue = countdown;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return view;
        }

        private static EndScreenView BuildEndScreen(Transform screens)
        {
            var panel = AddPanel(screens, "End Screen", EndShadeColor);
            AddText(panel, "Title", "THE DARKNESS WINS", TitleFontSize, TitleColor, EndTitlePosition, new Vector2(1600f, 160f));
            AddStat(panel, "Score", "Score", EndScorePosition, out var score);
            AddStat(panel, "Level Reached", "Level reached", EndLevelPosition, out var levelReached);
            AddStat(panel, "Enemies Killed", "Enemies killed", EndKillsPosition, out var enemiesKilled);

            var rule = new GameObject("Rule", typeof(RectTransform), typeof(Image));
            var ruleRect = rule.GetComponent<RectTransform>();
            ruleRect.SetParent(panel, false);
            ruleRect.anchoredPosition = EndRulePosition;
            ruleRect.sizeDelta = EndRuleSize;
            var ruleImage = rule.GetComponent<Image>();
            ruleImage.color = EndRuleColor;
            ruleImage.raycastTarget = false;

            // Credits get their own lines and colour, apart from the Score: they're what the Run pays, not how well it went.
            var earnedLabel = AddStat(panel, "Credits Earned", "Credits earned", EndCreditsEarnedPosition, out var creditsEarned);

            // The ad's bonus has its own line, shown only once the ad has paid out.
            var adBonusRow = new GameObject("Ad Bonus", typeof(RectTransform)).GetComponent<RectTransform>();
            adBonusRow.SetParent(panel, false);
            adBonusRow.anchoredPosition = EndAdBonusPosition;
            var bonusLabel = AddStat(adBonusRow, "Ad Bonus", "Ad bonus", Vector2.zero, out var adBonus);
            adBonusRow.gameObject.SetActive(false);

            var totalLabel = AddStat(panel, "Credits Total", "CREDITS", EndCreditsTotalPosition, out var creditsTotal);
            totalLabel.fontSize = creditsTotal.fontSize = EndCreditsTotalFontSize;
            totalLabel.fontStyle = creditsTotal.fontStyle = FontStyles.Bold;

            // The end-of-Run ad's offer: its wording and the bonus under it come from the Credits definition.
            var adButton = AddButton(panel, "Watch Ad", EndAdButtonPosition);
            var adButtonRect = (RectTransform)adButton.transform;
            adButtonRect.sizeDelta = EndAdButtonSize;
            adButton.targetGraphic.color = EndAdButtonColor;
            var adButtonLabel = adButton.GetComponentInChildren<TextMeshProUGUI>();
            adButtonLabel.rectTransform.sizeDelta = EndAdButtonSize;
            adButtonLabel.fontStyle = FontStyles.Bold;
            var hintHeight = HintFontSize * 1.5f;
            var adHint = AddText(
                adButtonRect, "Hint", string.Empty, HintFontSize, CreditsColor,
                new Vector2(0f, -(EndAdButtonSize.y + hintHeight) / 2f - HintGap), new Vector2(EndAdButtonSize.x, hintHeight));

            // The way on: the Shop, where the next Run starts.
            var shop = AddButton(panel, "Shop", EndShopPosition);

            var sound = new GameObject("Sound", typeof(AudioSource)).GetComponent<AudioSource>();
            sound.transform.SetParent(panel, false);
            sound.playOnAwake = false;
            sound.spatialBlend = 0f;
            AudioRouting.Route(sound, AudioCategory.Sound);

            var view = panel.gameObject.AddComponent<EndScreenView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("levelReached").objectReferenceValue = levelReached;
            serialized.FindProperty("enemiesKilled").objectReferenceValue = enemiesKilled;
            serialized.FindProperty("score").objectReferenceValue = score;
            serialized.FindProperty("creditsEarned").objectReferenceValue = creditsEarned;
            serialized.FindProperty("adBonusRow").objectReferenceValue = adBonusRow.gameObject;
            serialized.FindProperty("adBonus").objectReferenceValue = adBonus;
            serialized.FindProperty("creditsTotal").objectReferenceValue = creditsTotal;
            serialized.FindProperty("adButton").objectReferenceValue = adButton;
            serialized.FindProperty("adButtonLabel").objectReferenceValue = adButtonLabel;
            serialized.FindProperty("adHint").objectReferenceValue = adHint;
            serialized.FindProperty("sound").objectReferenceValue = sound;
            serialized.FindProperty("shopButton").objectReferenceValue = shop;
            var creditsTexts = new[] { earnedLabel, creditsEarned, bonusLabel, adBonus, totalLabel, creditsTotal, adButtonLabel, adHint };
            var creditsTextsProperty = serialized.FindProperty("creditsTexts");
            creditsTextsProperty.arraySize = creditsTexts.Length;
            for (var i = 0; i < creditsTexts.Length; i++)
            {
                creditsTexts[i].color = CreditsColor;
                creditsTextsProperty.GetArrayElementAtIndex(i).objectReferenceValue = creditsTexts[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return view;
        }

        internal static RectTransform AddPanel(Transform parent, string name, Color shade)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = shade;
            return rect;
        }

        private static TextMeshProUGUI AddStat(RectTransform panel, string name, string label, Vector2 position, out TextMeshProUGUI value)
        {
            var labelText = AddText(panel, $"{name} Label", label, StatFontSize, TextColor, position + StatLabelOffset, StatLabelSize);
            labelText.alignment = TextAlignmentOptions.Right;
            value = AddText(panel, $"{name} Value", "0", StatFontSize, TitleColor, position + StatValueOffset, StatValueSize);
            value.alignment = TextAlignmentOptions.Left;
            return labelText;
        }

        internal static Button AddButton(RectTransform panel, string label, Vector2 position)
        {
            var buttonObject = new GameObject($"{label} Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(panel, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = ButtonSize;
            var image = buttonObject.GetComponent<Image>();
            image.color = ButtonColor;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var text = AddText(rect, "Label", label.ToUpperInvariant(), ButtonFontSize, ButtonTextColor, Vector2.zero, ButtonSize);
            text.raycastTarget = false;
            return button;
        }

        internal static TextMeshProUGUI AddText(
            RectTransform parent,
            string name,
            string text,
            float fontSize,
            Color color,
            Vector2 position,
            Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// A Debug-map action in the game's input actions, added with <paramref name="binding"/> if it's missing.
        /// Every other action is kept as it is.
        /// </summary>
        internal static InputActionReference LoadOrAddDebugAction(string actionName, string binding)
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actionPath = $"{LightningDebugToggle.MapName}/{actionName}";
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            if (actions.FindAction(actionPath) == null)
            {
                var edited = InputActionAsset.FromJson(actions.ToJson());
                var debugMap = edited.FindActionMap(LightningDebugToggle.MapName) ?? edited.AddActionMap(LightningDebugToggle.MapName);
                debugMap.AddAction(actionName, InputActionType.Button, binding);
                System.IO.File.WriteAllText(controlsPath, edited.ToJson());
                Object.DestroyImmediate(edited);
                AssetDatabase.ImportAsset(controlsPath);
                actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            }

            var action = actions.FindAction(actionPath, throwIfNotFound: true);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(controlsPath))
            {
                if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                {
                    return reference;
                }
            }

            throw new System.InvalidOperationException($"No action reference for {actionPath} in {controlsPath}.");
        }
    }
}
