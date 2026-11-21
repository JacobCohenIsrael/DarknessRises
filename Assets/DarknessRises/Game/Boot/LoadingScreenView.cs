using System;
using DarknessRises.Game.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// The loading screen, on its own canvas in the boot scene: a cover over everything, a label that comes up when the
    /// wait goes on, and under it a thin bar (with the sizes) while the wait downloads content. The boot shows it while a
    /// scene loads and hides it when that scene says it's ready, so a Level or the Shop is never seen half-made; it hears
    /// the wait's download as an <see cref="IDownloadProgress"/>. <see cref="LoadingScreen"/> decides what shows; this
    /// draws it. It's built covering the screen, so nothing shows before the boot's first frame either. It has no
    /// raycaster and takes no taps.
    /// </summary>
    public sealed class LoadingScreenView : MonoBehaviour, IDownloadProgress
    {
        private const char Dot = '.';

        // Dots that aren't there yet are still laid out, unseen, so the word doesn't shift as they appear.
        private const string Unseen = "<alpha=#00>";

        // The sizes are shown to a tenth of a megabyte, and only redrawn when that changes.
        private const float SizeSteps = 10f;

        [SerializeField] private Canvas canvas;

        [SerializeField, Tooltip("Covers the whole screen.")]
        private Image cover;

        [SerializeField] private TMP_Text label;

        [SerializeField, Tooltip("The bar under the label: the track, still to download.")]
        private Image barTrack;

        [SerializeField, Tooltip("The bar's downloaded part, a child of the track stretched from its left edge.")]
        private Image barFill;

        [SerializeField, Tooltip("The megabytes in and in all, under the bar.")]
        private TMP_Text sizes;

        private LoadingScreen screen;
        private float pollSeconds = 0.1f;
        private string sizesFormat = string.Empty;

        // The label with no dots, one dot, two dots...: made once, so the dots cost nothing as they grow.
        private string[] labels = Array.Empty<string>();
        private int shownDots = -1;
        private float shownProgress = -1f;
        private long shownDownloadedSteps = -1;
        private long shownTotalSteps = -1;

        /// <summary>Whether the screen is covered.</summary>
        public bool IsShown => screen != null ? screen.IsShown : canvas.enabled;

        /// <summary>Whether the label is up.</summary>
        public bool IsLabelShown => label.gameObject.activeSelf;

        /// <summary>Whether the download bar is up.</summary>
        public bool IsBarShown => barTrack.gameObject.activeSelf;

        /// <summary>How full the bar is, from 0 to 1.</summary>
        public float Progress => screen != null ? screen.Progress : 0f;

        /// <summary>Bytes of the wait's download that are in.</summary>
        public long DownloadedBytes => screen != null ? screen.DownloadedBytes : 0L;

        /// <summary>Bytes the wait downloads in all, as far as it knows now.</summary>
        public long TotalBytes => screen != null ? screen.TotalBytes : 0L;

        /// <summary>Seconds between two looks at how far a download has got.</summary>
        public float ProgressPollSeconds => pollSeconds;

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

            var trackRect = barTrack.rectTransform;
            trackRect.sizeDelta = new Vector2(settings.BarWidth, settings.BarHeight);
            trackRect.anchoredPosition = new Vector2(0f, -settings.BarOffset);
            barTrack.color = settings.BarTrackColor;
            barFill.color = settings.BarFillColor;

            sizesFormat = settings.SizesFormat ?? string.Empty;
            sizes.fontSize = settings.SizesFontSize;
            sizes.color = settings.SizesColor;
            sizes.rectTransform.anchoredPosition = new Vector2(0f, -settings.BarOffset - settings.SizesOffset);
            pollSeconds = settings.ProgressPollSeconds;

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

        /// <inheritdoc />
        public void BeginDownload(long bytes)
        {
            screen.BeginDownload(bytes);
            Draw();
        }

        /// <inheritdoc />
        public void ReportDownloaded(long bytes)
        {
            screen.ReportDownloaded(bytes);
            Draw();
        }

        /// <inheritdoc />
        public void EndDownload()
        {
            screen.EndDownload();
            Draw();
        }

        /// <inheritdoc />
        public void DownloadUnknown()
        {
            screen.DownloadUnknown();
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

            DrawBar();
        }

        private void DrawBar()
        {
            var barObject = barTrack.gameObject;
            if (barObject.activeSelf != screen.IsBarShown)
            {
                barObject.SetActive(screen.IsBarShown);
                sizes.gameObject.SetActive(screen.IsBarShown && sizesFormat.Length > 0);
                shownProgress = -1f;
                shownDownloadedSteps = shownTotalSteps = -1;
            }

            if (!screen.IsBarShown)
            {
                return;
            }

            if (!Mathf.Approximately(screen.Progress, shownProgress))
            {
                shownProgress = screen.Progress;
                var fillRect = barFill.rectTransform;
                fillRect.anchorMax = new Vector2(shownProgress, 1f);
            }

            if (sizesFormat.Length == 0)
            {
                return;
            }

            var downloadedSteps = (long)(screen.DownloadedBytes / LoadingScreen.BytesPerMegabyte * SizeSteps);
            var totalSteps = (long)(screen.TotalBytes / LoadingScreen.BytesPerMegabyte * SizeSteps);
            if (downloadedSteps != shownDownloadedSteps || totalSteps != shownTotalSteps)
            {
                shownDownloadedSteps = downloadedSteps;
                shownTotalSteps = totalSteps;

                // TextMeshPro's own number formatting: no string is made.
                sizes.SetText(sizesFormat, downloadedSteps / SizeSteps, totalSteps / SizeSteps);
            }
        }
    }
}
