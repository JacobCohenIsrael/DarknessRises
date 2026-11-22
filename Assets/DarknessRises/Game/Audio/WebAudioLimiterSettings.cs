using System;
using UnityEngine;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The Web player's safety limiter (issue #121): on the Web every AudioSource plays straight into the browser's output
    /// with no mixer between, so sounds that pile up (a fight's footsteps, a hurt voice, a banner sting over the storm)
    /// sum past full scale and the browser clips them hard. The limiter sits between the game and the output: it leaves
    /// everything below its threshold exactly as it was and holds the rare peaks above it down. Authored in the
    /// <see cref="Boot.BootDefinition"/>; never changed at runtime. Other platforms mix through Unity's own mixer and
    /// don't use it.
    /// </summary>
    [Serializable]
    public sealed class WebAudioLimiterSettings
    {
        [SerializeField, Tooltip("Whether the Web player limits its summed output. Off: sounds that pile up past full scale clip hard in the browser.")]
        private bool enabled = true;

        [SerializeField, Range(-24f, 0f), Tooltip("dBFS above which the summed output is held down. Everything quieter plays exactly as it would without the limiter.")]
        private float thresholdDecibels = -3f;

        [SerializeField, Range(1f, 20f), Tooltip("How hard peaks above the threshold are held down: 20 is a limiter (12 dB over comes out 0.6 dB over).")]
        private float ratio = 20f;

        [SerializeField, Range(0f, 1f), Tooltip("Seconds the limiter takes to catch a peak. Short, so a sudden pile-up is caught before it clips.")]
        private float attackSeconds = 0.003f;

        [SerializeField, Range(0f, 1f), Tooltip("Seconds the limiter takes to let go after a peak. Long enough not to pump on every footstep.")]
        private float releaseSeconds = 0.25f;

        public bool Enabled => enabled;
        public float ThresholdDecibels => thresholdDecibels;
        public float Ratio => ratio;
        public float AttackSeconds => attackSeconds;
        public float ReleaseSeconds => releaseSeconds;

        /// <summary>The limiter's curve for these settings.</summary>
        public LimiterCurve Curve => new(thresholdDecibels, ratio);
    }
}
