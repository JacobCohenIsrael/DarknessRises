using System;
using DarknessRises.Game.Storage;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Keeps the volume settings with the device's other settings (<see cref="IDeviceSettings"/>): they belong to the
    /// device, not to the player's progress, so they stay out of the save (ADR 0009, ADR 0010).
    /// </summary>
    public sealed class DeviceVolumeStorage : IVolumeStorage
    {
        public const string SoundKey = "volume.sound";
        public const string AmbientKey = "volume.ambient";
        public const string MusicKey = "volume.music";

        private readonly IDeviceSettings settings;

        public DeviceVolumeStorage(IDeviceSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The name of the device setting that holds <paramref name="category"/>'s percent.</summary>
        public static string KeyFor(AudioCategory category) => category switch
        {
            AudioCategory.Sound => SoundKey,
            AudioCategory.Ambient => AmbientKey,
            AudioCategory.Music => MusicKey,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
        };

        public bool TryRead(AudioCategory category, out int percent) => settings.TryGetInt(KeyFor(category), out percent);

        public void Write(AudioCategory category, int percent) => settings.SetInt(KeyFor(category), percent);

        public void Flush() => settings.Flush();
    }
}
