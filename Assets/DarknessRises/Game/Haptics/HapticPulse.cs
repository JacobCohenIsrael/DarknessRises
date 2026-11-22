using System;
using UnityEngine;

namespace DarknessRises.Game.Haptics
{
    /// <summary>One step of a haptic pattern: a pause, then the motor on for a while at a strength.</summary>
    [Serializable]
    public struct HapticPulse
    {
        [SerializeField, Min(0), Tooltip("Milliseconds of stillness before this pulse. 0 runs it straight on from the one before: pulses back to back with different strengths make a swell or a fade.")]
        private int delayMs;

        [SerializeField, Min(1), Tooltip("Milliseconds the motor runs.")]
        private int durationMs;

        [SerializeField, Range(0.01f, 1f), Tooltip("How hard the motor runs, from barely there to full. Phones without amplitude control (and the Web) shorten a weak pulse instead.")]
        private float amplitude;

        public HapticPulse(int delayMs, int durationMs, float amplitude)
        {
            this.delayMs = delayMs;
            this.durationMs = durationMs;
            this.amplitude = amplitude;
        }

        public int DelayMs => Math.Max(0, delayMs);
        public int DurationMs => Math.Max(1, durationMs);
        public float Amplitude => Math.Clamp(amplitude, 0f, 1f);
    }
}
