using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace DarknessRises.Game.Content
{
    /// <summary>
    /// Downloads content to the device ahead of loading it, through Addressables, and says how far it has got in bytes.
    /// It asks how many bytes the keys need, then downloads them in one measured operation. The download only fills the
    /// device's cache (the browser's on the web): nothing is loaded into memory, which stays the job of the
    /// <see cref="ContentScope"/> that loads it (ADR 0007). Bundles that ship inside the app (an APK's) need no download
    /// and measure nothing.
    /// <para>
    /// On the web Addressables can't tell which bundles the browser already holds (it has no bundle cache API there), so it
    /// counts every byte as still to download. So each finished download is recorded on the device, by the names of the
    /// bundles it fetched (they carry their content hashes): asked again for the same bundles, it downloads nothing and
    /// measures nothing, and the wait stays the plain black cut. New content (new bundle names) is downloaded again.
    /// </para>
    /// </summary>
    public sealed class AddressablesContentDownloads
    {
        // PlayerPrefs keys of the download records, one per record name.
        private const string RecordKeyPrefix = "DarknessRises.Downloaded.";
        /// <summary>
        /// Downloads what <paramref name="keys"/> need. Bytes are reported to <paramref name="progress"/> every
        /// <paramref name="pollSeconds"/> (asking Addressables how far it has got costs a little garbage, so not every
        /// frame). A failure is logged and reported as an unknown size, and the loads that follow try again on their own,
        /// as they always did; only cancellation throws.
        /// </summary>
        /// <param name="recordName">Names this download's record on the device (a destination).</param>
        /// <returns>Whether everything the keys need is on the device now.</returns>
        public async UniTask<bool> DownloadAsync(
            IList<object> keys, string recordName, IDownloadProgress progress, float pollSeconds, CancellationToken cancellationToken)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var recordKey = RecordKeyPrefix + recordName;
            var bundles = await BundlesAsync(keys, cancellationToken);
            if (bundles != null && PlayerPrefs.GetString(recordKey, string.Empty) == bundles)
            {
                return true;
            }

            var bytes = await SizeAsync(keys, cancellationToken);
            if (bytes < 0)
            {
                progress.DownloadUnknown();
                return false;
            }

            if (bytes == 0)
            {
                Record(recordKey, bundles);
                return true;
            }

            progress.BeginDownload(bytes);
            var download = Addressables.DownloadDependenciesAsync((IEnumerable)keys, Addressables.MergeMode.Union, false);
            try
            {
                while (!download.IsDone)
                {
                    progress.ReportDownloaded(download.GetDownloadStatus().DownloadedBytes);
                    await UniTask.Delay(TimeSpan.FromSeconds(pollSeconds), DelayType.Realtime, PlayerLoopTiming.Update, cancellationToken);
                }

                if (download.Status == AsyncOperationStatus.Succeeded)
                {
                    progress.EndDownload();
                    Record(recordKey, bundles);
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[Content] Couldn't download content ahead of loading it: {download.OperationException?.Message}");
                    progress.DownloadUnknown();
                    return false;
                }
            }
            finally
            {
                if (download.IsDone)
                {
                    download.Release();
                }
                else
                {
                    // Cancelled while running: let it finish first, then let it go.
                    download.Completed += operation => operation.Release();
                }
            }
        }

        private static void Record(string recordKey, string bundles)
        {
            if (bundles == null)
            {
                return;
            }

            PlayerPrefs.SetString(recordKey, bundles);
            PlayerPrefs.Save();
        }

        // What the keys need, as one short text: a hash of the names of every bundle they depend on, in order. Null when
        // the keys can't be resolved.
        private static async UniTask<string> BundlesAsync(IList<object> keys, CancellationToken cancellationToken)
        {
            var locations = Addressables.LoadResourceLocationsAsync((IEnumerable)keys, Addressables.MergeMode.Union);
            try
            {
                var found = await locations.ToUniTask(cancellationToken: cancellationToken);
                if (locations.Status != AsyncOperationStatus.Succeeded || found == null || found.Count == 0)
                {
                    return null;
                }

                var names = new SortedSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < found.Count; i++)
                {
                    AddBundles(found[i], names);
                }

                return Hash128.Compute(string.Join("\n", names)).ToString();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return null;
            }
            finally
            {
                if (locations.IsValid())
                {
                    locations.Release();
                }
            }
        }

        private static void AddBundles(IResourceLocation location, SortedSet<string> names)
        {
            if (!location.HasDependencies)
            {
                return;
            }

            foreach (var dependency in location.Dependencies)
            {
                names.Add(dependency.PrimaryKey);
            }
        }

        // The bytes still to download, or -1 when that can't be known.
        private static async UniTask<long> SizeAsync(IList<object> keys, CancellationToken cancellationToken)
        {
            var size = Addressables.GetDownloadSizeAsync((IEnumerable)keys);
            try
            {
                await size.ToUniTask(cancellationToken: cancellationToken);
                if (size.Status == AsyncOperationStatus.Succeeded)
                {
                    return size.Result;
                }

                Debug.LogWarning($"[Content] Couldn't measure the content to download: {size.OperationException?.Message}");
                return -1;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Debug.LogWarning($"[Content] Couldn't measure the content to download: {exception.Message}");
                return -1;
            }
            finally
            {
                if (size.IsValid())
                {
                    size.Release();
                }
            }
        }
    }
}
