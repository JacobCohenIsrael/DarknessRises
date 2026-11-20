using System;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The volume of each <see cref="AudioCategory"/>, as the player set it with its slider: a whole percent, and the
    /// gain it gives every source of that category (<see cref="VolumeCurve"/>). Read from the device when made, written
    /// back as it changes. Plain C# (ADR 0003); each scene's <see cref="SceneAudioVolumes"/> holds one and applies it.
    /// </summary>
    public sealed class AudioVolumes
    {
        /// <summary>How many categories there are.</summary>
        public const int CategoryCount = 3;

        private readonly VolumeCurve curve;
        private readonly IVolumeStorage storage;
        private readonly int[] percents = new int[CategoryCount];
        private readonly float[] gains = new float[CategoryCount];
        private bool hasUnflushedChanges;

        /// <summary>Raised when a category's percent changes, with that category.</summary>
        public event Action<AudioCategory> Changed;

        /// <summary>
        /// The volumes stored in <paramref name="storage"/>, each kept within 0 to 100 %; a category never stored starts
        /// at its default.
        /// </summary>
        public AudioVolumes(VolumeCurve curve, VolumeDefaults defaults, IVolumeStorage storage)
        {
            this.curve = curve;
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            for (var i = 0; i < CategoryCount; i++)
            {
                var category = (AudioCategory)i;
                var percent = storage.TryRead(category, out var stored) ? stored : defaults.For(category);
                percents[i] = VolumeCurve.Clamp(percent);
                gains[i] = curve.Gain(percents[i]);
            }
        }

        /// <summary>The slider's percent of <paramref name="category"/>, 0 to 100.</summary>
        public int Percent(AudioCategory category) => percents[IndexOf(category)];

        /// <summary>The factor every source of <paramref name="category"/> plays its own volume at: 0 (silent) to 1.</summary>
        public float Gain(AudioCategory category) => gains[IndexOf(category)];

        /// <summary>
        /// Sets <paramref name="category"/> to <paramref name="percent"/>, kept within 0 to 100 %, and stores it.
        /// Returns whether it changed; <see cref="Changed"/> is raised only then.
        /// </summary>
        public bool Set(AudioCategory category, int percent)
        {
            var index = IndexOf(category);
            percent = VolumeCurve.Clamp(percent);
            if (percents[index] == percent)
            {
                return false;
            }

            percents[index] = percent;
            gains[index] = curve.Gain(percent);
            storage.Write(category, percent);
            hasUnflushedChanges = true;
            Changed?.Invoke(category);
            return true;
        }

        /// <summary>Puts the changes since the last save on disk. Does nothing when nothing changed.</summary>
        public void Save()
        {
            if (!hasUnflushedChanges)
            {
                return;
            }

            hasUnflushedChanges = false;
            storage.Flush();
        }

        /// <summary>The array index of <paramref name="category"/>.</summary>
        public static int IndexOf(AudioCategory category)
        {
            var index = (int)category;
            if (index < 0 || index >= CategoryCount)
            {
                throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }

            return index;
        }
    }
}
