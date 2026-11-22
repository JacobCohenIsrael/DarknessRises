#if UNITY_ANDROID || UNITY_IOS
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The crude fallback when the system vibrator can't be reached: Unity's one fixed buzz,
    /// <see cref="Handheld.Vibrate"/>, kept for the strong effects only (a buzz on every hit would be worse than none).
    /// Referencing it is also what makes Unity put the <c>VIBRATE</c> permission in the Android manifest, which
    /// <see cref="AndroidHapticDevice"/> needs too.
    /// </summary>
    public sealed class HandheldHapticDevice : IHapticDevice
    {
        // A waveform at least this long and this strong is worth the fixed buzz.
        private const long StrongMs = 150;
        private const int StrongAmplitude = 200;

        private bool[] strong = System.Array.Empty<bool>();

        public bool CanVibrate => true;

        public void Prepare(IReadOnlyList<HapticWaveform> waveforms)
        {
            strong = new bool[waveforms.Count];
            for (var i = 0; i < waveforms.Count; i++)
            {
                var waveform = waveforms[i];
                var peak = 0;
                foreach (var amplitude in waveform.Amplitudes)
                {
                    peak = Mathf.Max(peak, amplitude);
                }

                strong[i] = waveform.TotalMs >= StrongMs && peak >= StrongAmplitude;
            }
        }

        public void Play(int index)
        {
            if ((uint)index < (uint)strong.Length && strong[index])
            {
                Handheld.Vibrate();
            }
        }

        public void Cancel()
        {
        }
    }
}
#endif
