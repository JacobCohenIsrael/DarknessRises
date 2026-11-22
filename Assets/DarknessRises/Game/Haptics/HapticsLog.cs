using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The dev log of Haptics: every effect played and every one dropped, with the time and why, as <c>[Haptics]</c>
    /// lines. The Editor and development builds only; a release build never makes one.
    /// </summary>
    public sealed class HapticsLog : IDisposable
    {
        private readonly HapticsPlayer haptics;

        private HapticsLog(HapticsPlayer haptics)
        {
            this.haptics = haptics;
            haptics.Played += OnPlayed;
            haptics.Dropped += OnDropped;
            Debug.Log($"[Haptics] Ready: {(haptics.IsAvailable ? "this device can vibrate" : "this device can't vibrate")}, switch {(haptics.IsOn ? "on" : "off")}, {haptics.Waveforms.Length} waveforms prepared.");
        }

        /// <summary>Starts logging <paramref name="haptics"/>, in the Editor and development builds.</summary>
        public static HapticsLog Follow(HapticsPlayer haptics)
        {
#if UNITY_EDITOR || DEBUG
            return new HapticsLog(haptics);
#else
            return null;
#endif
        }

        public void Dispose()
        {
            haptics.Played -= OnPlayed;
            haptics.Dropped -= OnDropped;
        }

        private void OnPlayed(HapticEffect effect, float strength)
        {
            var pattern = haptics.Definition.PatternOf(effect);
            Write($"[Haptics] {haptics.Now,8:0.000}s PLAY {effect} x{strength:0.##} (priority {pattern.Priority}, {pattern.TotalMs} ms)");
        }

        private void OnDropped(HapticEffect effect, HapticDrop reason)
        {
            Write($"[Haptics] {haptics.Now,8:0.000}s drop {effect}: {reason}");
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        private static void Write(string line) => Debug.Log(line);
    }
}
