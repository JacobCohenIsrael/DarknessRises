using System.Collections.Generic;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The platform's vibration motor. It is handed every waveform once, before anything plays, so it can prepare what
    /// it needs (Android's <c>VibrationEffect</c> objects, the Web's patterns) and play each by its index for nothing.
    /// </summary>
    public interface IHapticDevice
    {
        /// <summary>Whether there's a motor this game can drive.</summary>
        bool CanVibrate { get; }

        /// <summary>Prepares <paramref name="waveforms"/>, played from now on by their index.</summary>
        void Prepare(IReadOnlyList<HapticWaveform> waveforms);

        /// <summary>Plays the waveform at <paramref name="index"/>, replacing whatever plays.</summary>
        void Play(int index);

        /// <summary>Stops whatever plays.</summary>
        void Cancel();
    }
}
