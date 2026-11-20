namespace DarknessRises.Game.Audio
{
    /// <summary>Keeps the volume settings in memory only: for tests.</summary>
    public sealed class MemoryVolumeStorage : IVolumeStorage
    {
        private readonly int?[] percents = new int?[AudioVolumes.CategoryCount];

        /// <summary>How many times <see cref="Flush"/> was called.</summary>
        public int Flushes { get; private set; }

        public bool TryRead(AudioCategory category, out int percent)
        {
            var stored = percents[AudioVolumes.IndexOf(category)];
            percent = stored ?? 0;
            return stored.HasValue;
        }

        public void Write(AudioCategory category, int percent) => percents[AudioVolumes.IndexOf(category)] = percent;

        public void Flush() => Flushes++;
    }
}
