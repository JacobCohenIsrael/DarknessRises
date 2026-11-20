using UnityEngine;

namespace DarknessRises.Game.Audio
{
    /// <summary>How views set a source's volume, so its category's slider still applies (ADR 0010).</summary>
    public static class AudioSourceVolume
    {
        /// <summary>
        /// Plays <paramref name="source"/> at <paramref name="volume"/> times its category's gain. Use it wherever a view
        /// would set <c>AudioSource.volume</c>. A source without a category (a build the routing check would fail) plays
        /// at <paramref name="volume"/> as it is. Allocates nothing, so it's fine every frame.
        /// </summary>
        public static void SetVolume(this AudioSource source, float volume)
        {
            if (source.TryGetComponent<AudioCategorySource>(out var categorized))
            {
                categorized.Volume = volume;
            }
            else
            {
                source.volume = volume;
            }
        }
    }
}
