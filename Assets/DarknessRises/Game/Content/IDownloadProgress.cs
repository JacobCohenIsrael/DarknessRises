namespace DarknessRises.Game.Content
{
    /// <summary>Hears how a content download goes, in bytes: whoever shows the player how far it has got.</summary>
    public interface IDownloadProgress
    {
        /// <summary>A download of <paramref name="bytes"/> starts.</summary>
        void BeginDownload(long bytes);

        /// <summary><paramref name="bytes"/> of the download that is running are in.</summary>
        void ReportDownloaded(long bytes);

        /// <summary>The download that is running has finished: all its bytes are in.</summary>
        void EndDownload();

        /// <summary>How much is downloaded can't be known: asking failed, or the download did.</summary>
        void DownloadUnknown();
    }
}
