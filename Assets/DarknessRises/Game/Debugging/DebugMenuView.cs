#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Pausing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the touch debug menu for playtesting on a phone. A "DEV" button at the top right opens a panel that
    /// pauses the game and runs the same debug commands as the keyboard (weapons, god mode, Skip to Boss, the Ramp
    /// jump, Lightning). Built from code at runtime, so no release scene references it. Compiled out of release builds.
    /// </summary>
    /// <remarks>
    /// The DEV button and the panel's shade are <see cref="Selectable"/>s on a canvas with a raycaster, so the UI filter
    /// keeps a press on them from firing, throwing a Grenade or starting a Gesture. Opening pauses through the
    /// <see cref="GamePause"/> rules (the pause menu sits under the panel); closing resumes only a pause it made.
    /// </remarks>
    public sealed class DebugMenuView : MonoBehaviour
    {
        public const string DevButtonName = "Debug Menu Button";

        private PrimaryWeaponDebugSwitch weapons;
        private Func<GodModeToggle> godMode;
        private LightningDebugToggle lightning;
        private RampLevelDebugJump rampJump;
        private BossSkipDebug bossSkip;
        private GamePause pause;

        private RectTransform safeArea;
        private Rect fittedSafeArea;
        private GameObject devButton;
        private GameObject panel;
        private Image[] weaponImages;
        private Image godModeImage;
        private TextMeshProUGUI godModeLabel;
        private Image lightningImage;
        private TextMeshProUGUI lightningLabel;
        private bool pausedByMenu;

        // What the buttons show, so they only change when the state does.
        private int shownWeapon = int.MinValue;
        private int shownGodMode = -1;
        private int shownLightning = -1;

        /// <summary>Whether the panel is open.</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>
        /// Builds the menu under <paramref name="parent"/>. <paramref name="godModeToggle"/> is read on every use, since the
        /// character's health makes a new one each time it's shown. <paramref name="jump"/> and <paramref name="skip"/>
        /// may be null (no Level).
        /// </summary>
        public static DebugMenuView Create(
            Transform parent,
            string[] weaponNames,
            PrimaryWeaponDebugSwitch weaponSwitch,
            Func<GodModeToggle> godModeToggle,
            LightningDebugToggle lightningToggle,
            RampLevelDebugJump jump,
            BossSkipDebug skip,
            GamePause gamePause)
        {
            var root = new GameObject("Debug Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<DebugMenuView>();
            view.weapons = weaponSwitch;
            view.godMode = godModeToggle;
            view.lightning = lightningToggle;
            view.rampJump = jump;
            view.bossSkip = skip;
            view.pause = gamePause;
            view.Build(weaponNames);
            return view;
        }

        /// <summary>Opens the panel and pauses the game, if it can pause.</summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            pausedByMenu = pause != null && pause.Pause();
            panel.SetActive(true);
            Refresh();
            ShowDevButton();
        }

        /// <summary>Closes the panel and resumes the game, if opening it paused it.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            panel.SetActive(false);
            if (pausedByMenu && pause.IsPaused)
            {
                pause.Resume();
            }

            pausedByMenu = false;
            ShowDevButton();
        }

        private void Build(string[] weaponNames)
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DebugMenuStyle.SortingOrder;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = DebugMenuStyle.ReferenceResolution;
            scaler.matchWidthOrHeight = DebugMenuStyle.MatchHeight;

            safeArea = NewRect("Safe Area", transform);
            Stretch(safeArea);
            FitSafeArea();

            var devRect = AddButton(safeArea, DevButtonName, "DEV", DebugMenuStyle.DevButtonFontSize, DebugMenuStyle.DevButtonColor, Open, out _, out _);
            devRect.anchorMin = devRect.anchorMax = devRect.pivot = Vector2.one;
            devRect.anchoredPosition = DebugMenuStyle.DevButtonOffset;
            devRect.sizeDelta = DebugMenuStyle.DevButtonSize;
            devButton = devRect.gameObject;

            BuildPanel(weaponNames);
            panel.SetActive(false);
            if (pause != null)
            {
                pause.StateChanged += OnPauseStateChanged;
            }

            ShowDevButton();
        }

        private void BuildPanel(string[] weaponNames)
        {
            // A full-screen shade that takes every press, so nothing under the panel is pressed while it's open.
            var shade = NewRect("Debug Menu Panel", transform);
            Stretch(shade);
            AddImage(shade, DebugMenuStyle.ShadeColor);
            var blocker = shade.gameObject.AddComponent<Selectable>();
            blocker.transition = Selectable.Transition.None;
            blocker.navigation = new Navigation { mode = Navigation.Mode.None };
            panel = shade.gameObject;

            var box = NewRect("Box", shade);
            box.sizeDelta = DebugMenuStyle.PanelSize;
            AddImage(box, DebugMenuStyle.PanelColor);
            var column = box.gameObject.AddComponent<VerticalLayoutGroup>();
            var padding = (int)DebugMenuStyle.PanelPadding;
            column.padding = new RectOffset(padding, padding, padding, padding);
            column.spacing = DebugMenuStyle.RowSpacing;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var title = AddText(box, "Title", "DEV", DebugMenuStyle.TitleFontSize);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = DebugMenuStyle.RowHeight * 0.8f;

            var weaponRow = AddRow(box, "Weapons");
            weaponImages = new Image[weaponNames.Length];
            for (var i = 0; i < weaponNames.Length; i++)
            {
                var index = i;
                AddButton(weaponRow, $"Weapon {i + 1} Button", weaponNames[i], DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor,
                    () => OnWeapon(index), out weaponImages[i], out _);
            }

            var toggleRow = AddRow(box, "Toggles");
            AddButton(toggleRow, "God Mode Button", "God mode", DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor,
                OnGodMode, out godModeImage, out godModeLabel);
            AddButton(toggleRow, "Lightning Button", "Lightning", DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor,
                OnLightning, out lightningImage, out lightningLabel);

            var jumpRow = AddRow(box, "Jumps");
            AddButton(jumpRow, "Skip To Boss Button", "Skip to Boss", DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor,
                OnSkipToBoss, out _, out _).GetComponent<Button>().interactable = bossSkip != null;
            AddButton(jumpRow, "Ramp Jump Button", "Jump to Ramp level", DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor,
                OnRampJump, out _, out _).GetComponent<Button>().interactable = rampJump != null;

            var closeRow = AddRow(box, "Close");
            AddButton(closeRow, "Close Button", "Close", DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor, Close, out _, out _);
        }

        private void OnDestroy()
        {
            if (pause != null)
            {
                pause.StateChanged -= OnPauseStateChanged;
            }
        }

        private void Update()
        {
            FitSafeArea();
            if (IsOpen)
            {
                // The keyboard debug keys may change things while the panel is open.
                Refresh();
            }
        }

        private void OnWeapon(int index)
        {
            weapons.Equip(index);
            Refresh();
        }

        private void OnGodMode()
        {
            godMode()?.Toggle();
            Refresh();
        }

        private void OnLightning()
        {
            lightning.Toggle();
            Refresh();
        }

        // Both leave the current fight behind: close (and resume) first, so the game moves on at once.
        private void OnSkipToBoss()
        {
            Close();
            bossSkip.Skip();
        }

        private void OnRampJump()
        {
            Close();
            rampJump.Jump();
        }

        // Resumed or left from elsewhere (the Pause key, the pause menu): the panel goes with the pause it made.
        private void OnPauseStateChanged(PauseState state)
        {
            if (state != PauseState.Paused && IsOpen)
            {
                pausedByMenu = false;
                Close();
            }

            ShowDevButton();
        }

        // Hidden while the game is frozen by someone else (the pause menu), so the menu can't take over their pause.
        private void ShowDevButton()
        {
            devButton.SetActive(!IsOpen && (pause == null || !pause.IsFrozen));
        }

        private void Refresh()
        {
            var weapon = weapons.EquippedIndex;
            if (weapon != shownWeapon)
            {
                shownWeapon = weapon;
                for (var i = 0; i < weaponImages.Length; i++)
                {
                    weaponImages[i].color = i == weapon ? DebugMenuStyle.OnColor : DebugMenuStyle.ButtonColor;
                }
            }

            var toggle = godMode();
            ShowToggle(toggle != null && toggle.IsOn, ref shownGodMode, godModeImage, godModeLabel, "God mode");
            ShowToggle(lightning.IsOn, ref shownLightning, lightningImage, lightningLabel, "Lightning");
        }

        private static void ShowToggle(bool isOn, ref int shown, Image image, TextMeshProUGUI label, string name)
        {
            var state = isOn ? 1 : 0;
            if (state == shown)
            {
                return;
            }

            shown = state;
            image.color = isOn ? DebugMenuStyle.OnColor : DebugMenuStyle.ButtonColor;
            label.text = isOn ? $"{name}: ON" : $"{name}: OFF";
        }

        private void FitSafeArea()
        {
            var area = Screen.safeArea;
            if (area == fittedSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            fittedSafeArea = area;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }

        private static RectTransform AddRow(RectTransform parent, string name)
        {
            var row = NewRect(name, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = DebugMenuStyle.ButtonSpacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = DebugMenuStyle.RowHeight;
            return row;
        }

        private static RectTransform AddButton(
            RectTransform parent, string name, string text, float fontSize, Color color, UnityEngine.Events.UnityAction onClick,
            out Image image, out TextMeshProUGUI label)
        {
            var rect = NewRect(name, parent);
            image = AddImage(rect, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            label = AddText(rect, "Label", text, fontSize);
            Stretch(label.rectTransform);
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string name, string text, float fontSize)
        {
            var label = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = DebugMenuStyle.TextColor;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
