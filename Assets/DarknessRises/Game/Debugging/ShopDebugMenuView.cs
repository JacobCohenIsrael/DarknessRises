#if UNITY_EDITOR || DEBUG
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the DEV menu of the Shop, for playtesting on a phone. A "DEV" button at the bottom of the screen opens
    /// a panel with the save's commands: add Credits, max out everything the Shop sells, reset the save. The Shop
    /// screen follows the save as it changes, so every state of the Shop is a press away. Built from code at runtime,
    /// so no release scene references it. Compiled out of release builds.
    /// </summary>
    public sealed class ShopDebugMenuView : MonoBehaviour
    {
        public const string DevButtonName = "Shop Debug Menu Button";
        public const string PanelName = "Shop Debug Menu Panel";

        // Bottom centre: clear of the Credits (top right), the Primary line (bottom left) and PLAY (bottom right).
        private static readonly Vector2 DevButtonOffset = new(0f, 59f);
        private static readonly Vector2 PanelSize = new(1040f, 440f);

        private SaveDebugCommands save;
        private RectTransform safeArea;
        private Rect fittedSafeArea;
        private GameObject devButton;
        private GameObject panel;

        /// <summary>Whether the panel is open.</summary>
        public bool IsOpen => panel != null && panel.activeSelf;

        /// <summary>Builds the menu under <paramref name="parent"/>, over the Shop screen.</summary>
        public static ShopDebugMenuView Create(Transform parent, SaveDebugCommands saveCommands)
        {
            var root = new GameObject("Shop Debug Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<ShopDebugMenuView>();
            view.save = saveCommands;
            view.Build();
            return view;
        }

        public void Open()
        {
            panel.SetActive(true);
            devButton.SetActive(false);
        }

        public void Close()
        {
            panel.SetActive(false);
            devButton.SetActive(true);
        }

        private void Build()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DebugMenuStyle.SortingOrder;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = DebugMenuStyle.ReferenceResolution;
            scaler.matchWidthOrHeight = DebugMenuStyle.MatchHeight;

            safeArea = DebugMenuView.NewRect("Safe Area", transform);
            DebugMenuView.Stretch(safeArea);
            FitSafeArea();

            var devRect = DebugMenuView.AddButton(
                safeArea, DevButtonName, "DEV", DebugMenuStyle.DevButtonFontSize, DebugMenuStyle.DevButtonColor, Open, out _, out _);
            devRect.anchorMin = devRect.anchorMax = devRect.pivot = new Vector2(0.5f, 0f);
            devRect.anchoredPosition = DevButtonOffset;
            devRect.sizeDelta = DebugMenuStyle.DevButtonSize;
            devButton = devRect.gameObject;

            // A full-screen shade that takes every press, so nothing in the Shop is pressed while the panel is open.
            var shade = DebugMenuView.NewRect(PanelName, transform);
            DebugMenuView.Stretch(shade);
            DebugMenuView.AddImage(shade, DebugMenuStyle.ShadeColor);
            var blocker = shade.gameObject.AddComponent<Selectable>();
            blocker.transition = Selectable.Transition.None;
            blocker.navigation = new Navigation { mode = Navigation.Mode.None };
            panel = shade.gameObject;

            var box = DebugMenuView.NewRect("Box", shade);
            box.sizeDelta = PanelSize;
            DebugMenuView.AddImage(box, DebugMenuStyle.PanelColor);
            var column = box.gameObject.AddComponent<VerticalLayoutGroup>();
            var padding = (int)DebugMenuStyle.PanelPadding;
            column.padding = new RectOffset(padding, padding, padding, padding);
            column.spacing = DebugMenuStyle.RowSpacing;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            var title = DebugMenuView.AddText(box, "Title", "DEV", DebugMenuStyle.TitleFontSize);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = DebugMenuStyle.RowHeight * 0.8f;

            var saveRow = DebugMenuView.AddRow(box, "Save");
            DebugMenuView.AddButton(
                saveRow, DebugMenuView.AddCreditsButtonName, $"+{save.CreditsPerPress} Credits", DebugMenuStyle.LabelFontSize,
                DebugMenuStyle.ButtonColor, save.AddCredits, out _, out _);
            DebugMenuView.AddButton(
                saveRow, DebugMenuView.MaxOutButtonName, "Max upgrades", DebugMenuStyle.LabelFontSize,
                DebugMenuStyle.ButtonColor, save.MaxOut, out _, out _);
            DebugMenuView.AddButton(
                saveRow, DebugMenuView.ResetSaveButtonName, "Reset save", DebugMenuStyle.LabelFontSize,
                DebugMenuStyle.ButtonColor, save.ResetSave, out _, out _);

            var closeRow = DebugMenuView.AddRow(box, "Close");
            DebugMenuView.AddButton(
                closeRow, "Close Button", "Close", DebugMenuStyle.LabelFontSize, DebugMenuStyle.ButtonColor, Close, out _, out _);

            panel.SetActive(false);
        }

        private void Update()
        {
            FitSafeArea();
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
    }
}
#endif
