#if UNITY_WEBGL && !UNITY_EDITOR
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The Web player's motor: <c>navigator.vibrate</c>, over <c>WebHaptics.jslib</c>. Chrome on Android has it; iOS
    /// Safari and desktop browsers don't (or have it but no motor), so it's only used where the browser has it and the
    /// screen is a touch screen. The browser ignores it until the player has tapped the page, and it has no amplitude,
    /// so each waveform plays its duration-only pattern. Every pattern is handed to the page once; playing one passes
    /// only its index.
    /// </summary>
    public sealed class WebHapticDevice : IHapticDevice
    {
        [DllImport("__Internal")]
        private static extern int DarknessRisesHapticsSupported();

        [DllImport("__Internal")]
        private static extern void DarknessRisesHapticsRegister(int index, int[] pattern, int length);

        [DllImport("__Internal")]
        private static extern int DarknessRisesHapticsPlay(int index);

        [DllImport("__Internal")]
        private static extern void DarknessRisesHapticsCancel();

        private WebHapticDevice()
        {
        }

        public bool CanVibrate => true;

        /// <summary>The page's <c>navigator.vibrate</c>, or null where the browser can't vibrate this device.</summary>
        public static WebHapticDevice TryCreate()
        {
            return DarknessRisesHapticsSupported() == 1 ? new WebHapticDevice() : null;
        }

        public void Prepare(IReadOnlyList<HapticWaveform> waveforms)
        {
            for (var i = 0; i < waveforms.Count; i++)
            {
                var pattern = waveforms[i].WebPattern;
                DarknessRisesHapticsRegister(i, pattern, pattern.Length);
            }

            Log($"[Haptics] Web: {waveforms.Count} patterns handed to navigator.vibrate.");
        }

        public void Play(int index) => DarknessRisesHapticsPlay(index);

        public void Cancel() => DarknessRisesHapticsCancel();

        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string line) => Debug.Log(line);
    }
}
#endif
