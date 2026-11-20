using System;
using UnityEngine;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Keeps the volume settings in <see cref="PlayerPrefs"/>: they belong to the device, not to the player's progress,
    /// so they stay out of the save (ADR 0009, ADR 0010).
    /// </summary>
    public sealed class PlayerPrefsVolumeStorage : IVolumeStorage
    {
        public const string SoundKey = "volume.sound";
        public const string AmbientKey = "volume.ambient";
        public const string MusicKey = "volume.music";

        /// <summary>The <see cref="PlayerPrefs"/> key that holds <paramref name="category"/>'s percent.</summary>
        public static string KeyFor(AudioCategory category) => category switch
        {
            AudioCategory.Sound => SoundKey,
            AudioCategory.Ambient => AmbientKey,
            AudioCategory.Music => MusicKey,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

        public bool TryRead(AudioCategory category, out int percent)
        {
            var key = KeyFor(category);
            if (!PlayerPrefs.HasKey(key))
            {
                percent = 0;
                return false;
            }

            percent = PlayerPrefs.GetInt(key);
            return true;
        }

        public void Write(AudioCategory category, int percent) => PlayerPrefs.SetInt(KeyFor(category), percent);

        public void Flush() => PlayerPrefs.Save();
    }
}
