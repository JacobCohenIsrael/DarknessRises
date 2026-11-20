namespace DarknessRises.Game.Audio
{
    /// <summary>Where the volume sliders' settings are kept on the device, one whole percent per <see cref="AudioCategory"/>.</summary>
    public interface IVolumeStorage
    {
        /// <summary>The stored percent of <paramref name="category"/>, if one was ever stored.</summary>
        bool TryRead(AudioCategory category, out int percent);

        /// <summary>Stores <paramref name="percent"/> for <paramref name="category"/>. It may stay in memory until <see cref="Flush"/>.</summary>
        void Write(AudioCategory category, int percent);

        /// <summary>Puts what was written on disk.</summary>
        void Flush();
    }
}
