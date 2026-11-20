using DarknessRises.Game.Audio;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Death;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Editor.Weapons;
using DarknessRises.Game.Pausing;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Pausing
{
    /// <summary>
    /// Builds the pause: the Menu/Pause action (Esc and P), the on-screen pause button at the top right, and the pause
    /// menu (Resume, Sound, Restart, Exit) on a canvas between the HUD and the death and end screens. Sound opens the
    /// volume panel over the menu.
    /// Call it once the health and death screens are built: Exit reuses the end screen.
    /// </summary>
    public static class PauseBuilder
    {
        public const string MenuMap = "Menu";
        public const string PauseAction = "Pause";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";
        private static readonly string[] PauseBindings = { "<Keyboard>/escape", "<Keyboard>/p" };

        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        private const float MatchHeight = 1f;

        // Above the HUD (and the Level's title card and fade on it), below the death and end screens.
        private const int PauseSortingOrder = 5;

        private static readonly Vector2 PauseButtonMargin = new(48f, 40f);
        private static readonly Vector2 PauseButtonSize = new(96f, 96f);
        private const float PauseButtonFontSize = 48f;
        private static readonly Color PauseButtonColor = new(0.16f, 0.16f, 0.17f, 0.6f);
        private static readonly Color PauseButtonTextColor = new(0.85f, 0.85f, 0.85f, 0.85f);

        private static readonly Color MenuShadeColor = new(0f, 0f, 0f, 0.7f);
        private static readonly Color TitleColor = new(0.85f, 0.85f, 0.85f, 1f);
        private const float TitleFontSize = 110f;

        public static void Build(GameRoot gameRoot, PlayerControls controls, SceneAudioVolumes volumes)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var deathFlow = (DeathFlow)serializedRoot.FindProperty("deathFlow").objectReferenceValue;
            var endScreen = (EndScreenView)new SerializedObject(deathFlow).FindProperty("endScreen").objectReferenceValue;

            var canvasObject = new GameObject("Pause", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PauseFlow));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = PauseSortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = MatchHeight;

            var pauseButton = BuildPauseButton(canvasObject.transform);
            var pauseMenu = BuildPauseMenu(canvasObject.transform, volumes, out var volumePanel);

            var flow = canvasObject.GetComponent<PauseFlow>();
            var serializedFlow = new SerializedObject(flow);
            serializedFlow.FindProperty("pauseAction").objectReferenceValue = LoadOrAddPauseAction();
            serializedFlow.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            serializedFlow.FindProperty("pauseMenu").objectReferenceValue = pauseMenu;
            serializedFlow.FindProperty("endScreen").objectReferenceValue = endScreen;
            serializedFlow.FindProperty("controls").objectReferenceValue = controls;
            serializedFlow.ApplyModifiedPropertiesWithoutUndo();

            serializedRoot.FindProperty("pauseFlow").objectReferenceValue = flow;
            serializedRoot.FindProperty("volumePanel").objectReferenceValue = volumePanel;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button BuildPauseButton(Transform canvas)
        {
            var buttonObject = new GameObject("Pause Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-PauseButtonMargin.x, -PauseButtonMargin.y);
            rect.sizeDelta = PauseButtonSize;
            var image = buttonObject.GetComponent<Image>();
            image.color = PauseButtonColor;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var label = HealthBuilder.AddText(rect, "Label", "II", PauseButtonFontSize, PauseButtonTextColor, Vector2.zero, PauseButtonSize);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            // Hidden until a Level plays.
            buttonObject.SetActive(false);
            return button;
        }

        private static PauseMenuView BuildPauseMenu(Transform canvas, SceneAudioVolumes volumes, out VolumePanelView volumePanel)
        {
            var panel = HealthBuilder.AddPanel(canvas, "Pause Menu", MenuShadeColor);
            HealthBuilder.AddText(panel, "Title", "PAUSED", TitleFontSize, TitleColor, new Vector2(0f, 240f), new Vector2(1200f, 160f));
            var resume = HealthBuilder.AddButton(panel, "Resume", new Vector2(0f, 80f));
            var sound = HealthBuilder.AddButton(panel, "Sound", new Vector2(0f, -40f));
            var restart = HealthBuilder.AddButton(panel, "Restart", new Vector2(0f, -160f));
            var exit = HealthBuilder.AddButton(panel, "Exit", new Vector2(0f, -280f));

            // Over the menu, hidden with it.
            volumePanel = VolumePanelBuilder.Build(panel, sound, sound.GetComponentInChildren<TextMeshProUGUI>(), volumes);

            var view = panel.gameObject.AddComponent<PauseMenuView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("resumeButton").objectReferenceValue = resume;
            serialized.FindProperty("restartButton").objectReferenceValue = restart;
            serialized.FindProperty("exitButton").objectReferenceValue = exit;
            serialized.FindProperty("volumePanel").objectReferenceValue = volumePanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The Menu/Pause action in the game's input actions, added (Esc and P) if it's missing.</summary>
        private static InputActionReference LoadOrAddPauseAction()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actionPath = $"{MenuMap}/{PauseAction}";
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            if (actions.FindAction(actionPath) == null)
            {
                var edited = InputActionAsset.FromJson(actions.ToJson());
                var menu = edited.FindActionMap(MenuMap) ?? edited.AddActionMap(MenuMap);
                var pause = menu.AddAction(PauseAction, InputActionType.Button);
                foreach (var binding in PauseBindings)
                {
                    pause.AddBinding(binding, groups: KeyboardMouseGroup);
                }

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
