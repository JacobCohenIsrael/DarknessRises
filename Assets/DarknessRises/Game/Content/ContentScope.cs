using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Content
{
    /// <summary>
    /// Content loaded for one owner (a Level, the boot), held until the owner ends it. Each asset loads once however
    /// often it's asked for. Ending the scope releases everything it loaded, newest first, including loads still
    /// running; their callers are cancelled. Nothing else releases what a scope loaded.
    /// </summary>
    public sealed class ContentScope : ILoadedContent, IDisposable
    {
        private readonly IContentSource source;

        // Every handle this scope holds, in the order it was taken.
        private readonly List<IContentHandle> handles = new();
        private readonly Dictionary<object, AssetLoad> assets = new();

        public ContentScope(IContentSource contentSource)
        {
            source = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
        }

        /// <summary>Whether the scope has ended and let go of its content.</summary>
        public bool IsEnded { get; private set; }

        /// <summary>How many loads (assets and scenes) the scope holds right now.</summary>
        public int HeldCount => handles.Count;

        /// <summary>
        /// The asset at <paramref name="key"/>, loading it if this scope hasn't yet. Cancelling
        /// <paramref name="cancellationToken"/> only stops this wait: the load stays with the scope until it ends.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The scope has ended.</exception>
        /// <exception cref="OperationCanceledException">Cancelled, or the scope ended before the asset arrived.</exception>
        public async UniTask<T> LoadAssetAsync<T>(object key, CancellationToken cancellationToken) where T : class
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfEnded();

            if (!assets.TryGetValue(key, out var load))
            {
                var handle = source.LoadAsset<T>(key);
                load = new AssetLoad(handle);
                assets.Add(key, load);
                handles.Add(handle);
            }

            object asset;
            try
            {
                asset = await load.Loaded.AttachExternalCancellation(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await ForgetFailedAsync(key, load);
                throw;
            }

            ThrowIfEndedWhileLoading();
            load.Arrived(asset);
            return (T)asset;
        }

        /// <summary>Loads every key in <paramref name="keys"/> as a <typeparamref name="T"/>, all at once.</summary>
        public async UniTask LoadAssetsAsync<T>(IReadOnlyList<object> keys, CancellationToken cancellationToken) where T : class
        {
            var loads = new UniTask<T>[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                loads[i] = LoadAssetAsync<T>(keys[i], cancellationToken);
            }

            await UniTask.WhenAll(loads);
        }

        /// <summary>
        /// Loads the scene at <paramref name="key"/> next to the scenes already loaded. Every call loads its own copy;
        /// the scope unloads it when it ends.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The scope has ended.</exception>
        /// <exception cref="OperationCanceledException">Cancelled, or the scope ended before the scene arrived.</exception>
        public async UniTask<Scene> LoadSceneAsync(object key, CancellationToken cancellationToken)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfEnded();

            var handle = source.LoadScene(key);
            handles.Add(handle);

            object scene;
            try
            {
                scene = await handle.Loaded.AttachExternalCancellation(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (handles.Remove(handle))
                {
                    await handle.ReleaseAsync();
                }

                throw;
            }

            ThrowIfEndedWhileLoading();
            return (Scene)scene;
        }

        /// <inheritdoc />
        public T Get<T>(object key) where T : class
        {
            if (key != null && assets.TryGetValue(key, out var load) && load.HasArrived)
            {
                return (T)load.Asset;
            }

            throw new InvalidOperationException($"'{key}' isn't loaded in this content scope.");
        }

        /// <summary>
        /// Ends the scope: releases everything it loaded, newest first, and waits for scenes to unload.
        /// Ending again does nothing.
        /// </summary>
        public async UniTask EndAsync()
        {
            if (IsEnded)
            {
                return;
            }

            IsEnded = true;
            var releasing = handles.ToArray();
            handles.Clear();
            assets.Clear();

            Exception firstFailure = null;
            for (var i = releasing.Length - 1; i >= 0; i--)
            {
                try
                {
                    await releasing[i].ReleaseAsync();
                }
                catch (Exception exception)
                {
                    firstFailure ??= exception;
                }
            }

            if (firstFailure != null)
            {
                throw firstFailure;
            }
        }

        /// <summary>Ends the scope without waiting for scenes to finish unloading.</summary>
        public void Dispose()
        {
            EndAsync().Forget();
        }

        private async UniTask ForgetFailedAsync(object key, AssetLoad load)
        {
            // Several callers may wait on the same failed load; only the first one lets it go.
            if (!IsEnded && assets.TryGetValue(key, out var current) && current == load)
            {
                assets.Remove(key);
                handles.Remove(load.Handle);
                await load.Handle.ReleaseAsync();
            }
        }

        private void ThrowIfEnded()
        {
            if (IsEnded)
            {
                throw new ObjectDisposedException(nameof(ContentScope), "The content scope has ended.");
            }
        }

        private void ThrowIfEndedWhileLoading()
        {
            if (IsEnded)
            {
                throw new OperationCanceledException("The content scope ended while loading.");
            }
        }

        private sealed class AssetLoad
        {
            public AssetLoad(IContentHandle handle)
            {
                Handle = handle;

                // Preserved, so every caller asking for the same key can wait on the one load.
                Loaded = handle.Loaded.Preserve();
            }

            public IContentHandle Handle { get; }
            public UniTask<object> Loaded { get; }
            public bool HasArrived { get; private set; }
            public object Asset { get; private set; }

            public void Arrived(object asset)
            {
                Asset = asset;
                HasArrived = true;
            }
        }
    }
}
