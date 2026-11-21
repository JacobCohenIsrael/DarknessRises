using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// The loading screen, on its own canvas in the boot scene: a cover over everything, and a label that comes up when
    /// the wait goes on. The boot shows it while a scene loads and hides it when that scene says it's ready, so a Level
    /// or the Shop is never seen half-made. <see cref="LoadingScreen"/> decides what shows; this draws it. It's built
    /// covering the screen, so nothing shows before the boot's first frame either. It has no raycaster and takes no taps.
    /// </summary>
    public sealed class LoadingScreenView : MonoBehaviour
    {
        private const char Dot = '.';

        // Dots that aren't there yet are still laid out, unseen, so the word doesn't shift as they appear.
        private const string Unseen = "<alpha=#00>";

        [SerializeField] private Canvas canvas;

        [SerializeField, Tooltip("Covers the whole screen.")]
        private Image cover;

        [SerializeField] private TMP_Text label;

        private LoadingScreen screen;

        // The label with no dots, one dot, two dots...: made once, so the dots cost nothing as they grow.
        private string[] labels = Array.Empty<string>();
        private int shownDots = -1;

        /// <summary>Whether the screen is covered.</summary>
        public bool IsShown => screen != null ? screen.IsShown : canvas.enabled;

        /// <summary>Whether the label is up.</summary>
        public bool IsLabelShown => label.gameObject.activeSelf;

        /// <summary>Makes the screen look and behave as <paramref name="settings"/> say. It stays as it was: covering, or not.</summary>
        public void Use(LoadingScreenSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var wasShown = canvas.enabled;
            screen = new LoadingScreen(settings.LabelDelaySeconds, settings.DotSeconds, settings.MaxDots);
            labels = new string[settings.MaxDots + 1];
            for (var dots = 0; dots < labels.Length; dots++)
            {
                labels[dots] = settings.Label + new string(Dot, dots) + Unseen + new string(Dot, settings.MaxDots - dots);
            }

            canvas.sortingOrder = settings.SortingOrder;
            cover.color = settings.CoverColor;
            label.fontSize = settings.FontSize;
            label.color = settings.LabelColor;
            if (wasShown)
            {
                screen.Show();
            }

            Draw();
        }

        /// <summary>Covers the screen. Asked again while it's up, it carries on as it was.</summary>
        public void Show()
        {
            screen.Show();
            Draw();
        }

        /// <summary>Uncovers the screen.</summary>
        public void Hide()
        {
            screen.Hide();
            Draw();
        }

        // Runs only while the screen is covered. Unscaled: Slow-mo and a pause don't hold the dots.
        private void Update()
        {
            if (screen == null)
            {
                return;
            }

            screen.Tick(Time.unscaledDeltaTime);
            Draw();
        }

        private void Draw()
        {
            canvas.enabled = screen.IsShown;
            enabled = screen.IsShown;

            var labelObject = label.gameObject;
            if (labelObject.activeSelf != screen.IsLabelShown)
            {
                labelObject.SetActive(screen.IsLabelShown);
                shownDots = -1;
            }

            if (screen.IsLabelShown && screen.Dots != shownDots)
            {
                shownDots = screen.Dots;
                label.text = labels[shownDots];
            }
        }
    }
}
