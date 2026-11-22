using System.Collections.Generic;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// A motor that does nothing: the desktop player and iOS for now, a phone whose vibrator can't be reached, and
    /// tests. In the Editor it claims it can vibrate, so the switch shows and the dev log (<see cref="HapticsLog"/>)
    /// shows what a phone would play.
    /// </summary>
    public sealed class SilentHapticDevice : IHapticDevice
    {
        public SilentHapticDevice(bool canVibrate)
        {
            CanVibrate = canVibrate;
        }

        public bool CanVibrate { get; }

        /// <summary>How many waveforms were prepared.</summary>
        public int Prepared { get; private set; }

        /// <summary>The index of the last waveform played; -1 before any.</summary>
        public int LastPlayed { get; private set; } = -1;

        /// <summary>How many times a waveform was played.</summary>
        public int Plays { get; private set; }

        /// <summary>How many times what plays was stopped.</summary>
        public int Cancels { get; private set; }

        public void Prepare(IReadOnlyList<HapticWaveform> waveforms) => Prepared = waveforms.Count;

        public void Play(int index)
        {
            LastPlayed = index;
            Plays++;
        }

        public void Cancel() => Cancels++;
    }
}
