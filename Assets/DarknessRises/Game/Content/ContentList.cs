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

        /// <summary>Loads everything listed into <paramref name="scope"/>, all at once.</summary>
        public UniTask LoadAsync(ContentScope scope, CancellationToken cancellationToken)
        {
            return UniTask.WhenAll(
                scope.LoadAssetsAsync<GameObject>(prefabs, cancellationToken),
                scope.LoadAssetsAsync<AudioClip>(clips, cancellationToken));
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
