using System;

namespace DarknessRises.Game.Boot
{
    /// <summary>
    /// What the loading screen shows over time. It covers the screen for as long as nothing is ready to be seen: from
    /// boot until the first Level or the Shop is ready, and through every change between them. It's plain black at
    /// first, so a quick change between Levels stays the black cut it always was. If the wait goes on, a label comes up.
    /// When the wait is downloading content, a bar under the label shows how many of its bytes are in; when there is
    /// nothing to download, or how much can't be known, the label carries dots that grow instead, so it's plainly not
    /// stuck. The bar is never faked: it moves only when bytes arrive, and never goes backwards within a wait.
    /// Plain C#: time and bytes arrive as explicit numbers (ADR 0003).
    /// </summary>
    public sealed class LoadingScreen
    {
        /// <summary>Bytes in one megabyte, as the sizes under the bar count them.</summary>
        public const float BytesPerMegabyte = 1024f * 1024f;

        private readonly float labelDelaySeconds;
        private readonly float dotSeconds;
        private readonly int maxDots;
        private float secondsShown;

        // The wait's download: every byte expected, every byte in, and where the download that's running started.
        private long totalBytes;
        private long downloadedBytes;
        private long runningStartBytes;
        private long runningTotalBytes;
        private bool isSizeUnknown;
        private bool isBarShown;
        private float progress;

        /// <param name="labelDelaySeconds">Seconds the screen stays plain black before the label (and the bar) come up.</param>
        /// <param name="dotSeconds">Seconds each dot after the label takes to appear.</param>
        /// <param name="maxDots">How many dots follow the label before they start over.</param>
        public LoadingScreen(float labelDelaySeconds, float dotSeconds, int maxDots)
        {
            if (!(labelDelaySeconds >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(labelDelaySeconds), labelDelaySeconds, "The label can't come up before the screen does.");
            }

            if (!(dotSeconds > 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(dotSeconds), dotSeconds, "A dot takes some time to appear.");
            }

            if (maxDots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDots), maxDots, "There can't be fewer than no dots.");
            }

            this.labelDelaySeconds = labelDelaySeconds;
            this.dotSeconds = dotSeconds;
            this.maxDots = maxDots;
        }

        /// <summary>Whether the screen is covered.</summary>
        public bool IsShown { get; private set; }

        /// <summary>Whether the wait has gone on long enough for the label to show.</summary>
        public bool IsLabelShown => IsShown && secondsShown >= labelDelaySeconds;

        /// <summary>
        /// Whether the download bar shows under the label. It comes up with the label, only if the wait has a download
        /// of a known size that is still running then; once up, it stays until the wait ends. A download that was over
        /// before the label came up (the content was already on the device) never flashes a bar.
        /// </summary>
        public bool IsBarShown => isBarShown;

        /// <summary>How many dots follow the label now: none as it comes up, then one more each beat, then none again. None while the bar shows.</summary>
        public int Dots => IsLabelShown && !isBarShown ? (int)((secondsShown - labelDelaySeconds) / dotSeconds) % (maxDots + 1) : 0;

        /// <summary>
        /// How full the bar is, from 0 to 1: the wait's bytes in over its bytes to download. It never goes backwards in
        /// one wait: if more bytes join the wait, it holds where it was until the bytes catch up with it.
        /// </summary>
        public float Progress => progress;

        /// <summary>Bytes of the wait's download that are in.</summary>
        public long DownloadedBytes => downloadedBytes;

        /// <summary>Bytes the wait downloads in all, as far as it knows now.</summary>
        public long TotalBytes => totalBytes;

        /// <summary>
        /// Covers the screen: something is loading and there is nothing ready to see. Asked again while it's already
        /// up, it carries on as it was, so one wait made of several loads counts as one.
        /// </summary>
        public void Show()
        {
            if (IsShown)
            {
                return;
            }

            IsShown = true;
            secondsShown = 0f;
            ClearDownload();
            UpdateBar();
        }

        /// <summary>Uncovers the screen: what was loading is ready.</summary>
        public void Hide()
        {
            IsShown = false;
            secondsShown = 0f;
            ClearDownload();
        }

        /// <summary>Advances the wait by <paramref name="deltaSeconds"/>.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            if (IsShown)
            {
                secondsShown += deltaSeconds;
                UpdateBar();
            }
        }

        /// <summary>
        /// A download of <paramref name="bytes"/> starts as part of this wait; its bytes join the wait's total. Nothing
        /// to download (no bytes) adds nothing. Ignored while the screen isn't covered.
        /// </summary>
        public void BeginDownload(long bytes)
        {
            if (bytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes), bytes, "A download can't be smaller than nothing.");
            }

            if (!IsShown || bytes == 0)
            {
                return;
            }

            // Whatever ran before is counted as far as it got.
            runningStartBytes = downloadedBytes;
            runningTotalBytes = bytes;
            totalBytes += bytes;
            UpdateBar();
        }

        /// <summary>
        /// <paramref name="bytes"/> of the download that is running are in. It never counts more than that download's
        /// size, and fewer than it counted before change nothing.
        /// </summary>
        public void ReportDownloaded(long bytes)
        {
            if (!IsShown || runningTotalBytes == 0)
            {
                return;
            }

            var counted = runningStartBytes + Math.Clamp(bytes, 0L, runningTotalBytes);
            if (counted > downloadedBytes)
            {
                downloadedBytes = counted;
                UpdateBar();
            }
        }

        /// <summary>The download that is running has finished: all its bytes are in.</summary>
        public void EndDownload()
        {
            ReportDownloaded(runningTotalBytes);
            runningStartBytes = downloadedBytes;
            runningTotalBytes = 0;
        }

        /// <summary>
        /// How much this wait downloads can't be known (asking failed, or the download did): the rest of the wait shows the
        /// label without a bar rather than a bar that isn't true.
        /// </summary>
        public void DownloadUnknown()
        {
            if (!IsShown)
            {
                return;
            }

            isSizeUnknown = true;
            runningTotalBytes = 0;
            UpdateBar();
        }

        private void ClearDownload()
        {
            totalBytes = 0;
            downloadedBytes = 0;
            runningStartBytes = 0;
            runningTotalBytes = 0;
            isSizeUnknown = false;
            isBarShown = false;
            progress = 0f;
        }

        private void UpdateBar()
        {
            if (totalBytes > 0)
            {
                var share = (float)((double)downloadedBytes / totalBytes);
                if (share > progress)
                {
                    progress = share;
                }
            }

            if (isSizeUnknown || !IsLabelShown || totalBytes == 0)
            {
                isBarShown = false;
                return;
            }

            // Up only while bytes are still coming when the label comes up; then it stays to the end of the wait.
            isBarShown = isBarShown || downloadedBytes < totalBytes;
        }
    }
}
