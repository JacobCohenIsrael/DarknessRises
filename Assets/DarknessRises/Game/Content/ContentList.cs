using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Content
{
    /// <summary>
    /// What a scope should load before its owner starts: prefabs and clips, by their Addressables references.
    /// Empty references and repeats are skipped.
    /// </summary>
    public sealed class ContentList
    {
        private readonly List<object> prefabs = new();
        private readonly List<object> clips = new();

        public IReadOnlyList<object> Prefabs => prefabs;
        public IReadOnlyList<object> Clips => clips;

        public void Add(AssetReferenceGameObject prefab)
        {
            AddKey(prefabs, prefab);
        }

        public void Add(AssetReferenceT<AudioClip> clip)
        {
            AddKey(clips, clip);
        }

        public void Add(IReadOnlyList<AssetReferenceT<AudioClip>> clipList)
        {
            for (var i = 0; i < clipList.Count; i++)
            {
                Add(clipList[i]);
            }
        }

        /// <summary>
        /// Loads everything listed into <paramref name="scope"/>, all at once. Clips come with their audio data loaded, so
        /// no sound is decoded lazily on first play or measured before it's ready (on WebGL the browser decodes audio
        /// asynchronously, after the clip asset itself has arrived).
        /// </summary>
        public UniTask LoadAsync(ContentScope scope, CancellationToken cancellationToken)
        {
            var loads = new UniTask[clips.Count + 1];
            loads[0] = scope.LoadAssetsAsync<GameObject>(prefabs, cancellationToken);
            for (var i = 0; i < clips.Count; i++)
            {
                loads[i + 1] = LoadClipAsync(scope, clips[i], cancellationToken);
            }

            return UniTask.WhenAll(loads);
        }

        private static async UniTask LoadClipAsync(ContentScope scope, object key, CancellationToken cancellationToken)
        {
            var clip = await scope.LoadAssetAsync<AudioClip>(key, cancellationToken);
            if (clip == null || clip.loadState == AudioDataLoadState.Loaded)
            {
                return;
            }

            clip.LoadAudioData();
            await UniTask.WaitWhile(clip, loading => loading.loadState == AudioDataLoadState.Loading, cancellationToken: cancellationToken);
            if (clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"[Content] Couldn't load the audio data of {clip.name}; it plays silently.", clip);
            }
        }

        private static void AddKey(List<object> keys, AssetReference reference)
        {
            if (!ContentReferences.IsSet(reference))
            {
                return;
            }

            var key = reference.RuntimeKey;
            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }
    }
}
