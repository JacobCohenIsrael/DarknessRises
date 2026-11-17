using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Content
{
    /// <summary>Looks up loaded content by the Addressables references the definitions hold.</summary>
    public static class ContentReferences
    {
        /// <summary>Whether <paramref name="reference"/> points at an asset at all.</summary>
        public static bool IsSet(AssetReference reference)
        {
            return reference != null && reference.RuntimeKeyIsValid();
        }

        /// <summary>The loaded prefab's <typeparamref name="T"/> component, ready to instantiate.</summary>
        public static T Prefab<T>(this ILoadedContent content, AssetReferenceGameObject reference) where T : Component
        {
            var prefab = content.Get<GameObject>(reference.RuntimeKey);
            return prefab.TryGetComponent<T>(out var component)
                ? component
                : throw new MissingComponentException($"The prefab '{prefab.name}' has no {typeof(T).Name}.");
        }

        /// <summary>The loaded clip, or null when the reference is empty (no sound).</summary>
        public static AudioClip Clip(this ILoadedContent content, AssetReferenceT<AudioClip> reference)
        {
            return IsSet(reference) ? content.Get<AudioClip>(reference.RuntimeKey) : null;
        }

        /// <summary>The loaded clips, in order. Empty references come back as null.</summary>
        public static AudioClip[] Clips(this ILoadedContent content, IReadOnlyList<AssetReferenceT<AudioClip>> references)
        {
            var clips = new AudioClip[references.Count];
            for (var i = 0; i < clips.Length; i++)
            {
                clips[i] = content.Clip(references[i]);
            }

            return clips;
        }
    }
}
