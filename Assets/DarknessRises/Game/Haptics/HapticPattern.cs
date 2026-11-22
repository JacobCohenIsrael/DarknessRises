using System;
using UnityEngine;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// How one <see cref="HapticEffect"/> feels: its pulses, how it ranks against the others, and how often it may
    /// come. Authored on the <see cref="HapticsDefinition"/>; never changed at runtime.
    /// </summary>
    [Serializable]
    public sealed class HapticPattern
    {
        [SerializeField, Tooltip("The pulses, in order. Leave empty and the effect never plays.")]
        private HapticPulse[] pulses = Array.Empty<HapticPulse>();

        [SerializeField, Range(0, 100), Tooltip("How it ranks. A stronger effect cuts a weaker one that's still playing and skips the gap and the cap; a weaker one is dropped while a stronger one plays.")]
        private int priority;

        [SerializeField, Min(0f), Tooltip("Seconds before this effect may play again. Keeps fast repeats (the Machine gun's hits) from running into one long buzz.")]
        private float cooldownSeconds;

        [SerializeField, Min(0f), Tooltip("Plays only if nothing has vibrated for this many seconds: for ambient effects that must never land in a fight's haptics. 0: no such wait.")]
        private float quietBeforeSeconds;

        [SerializeField, Tooltip("The phone's own tuned click, used instead of the pulses where the phone has one (Android 10+). None: the pulses everywhere.")]
        private HapticPredefined predefined = HapticPredefined.None;

        public HapticPattern()
        {
        }

        public HapticPattern(int priority, float cooldownSeconds, float quietBeforeSeconds, HapticPredefined predefined, params HapticPulse[] pulses)
        {
            this.priority = priority;
            this.cooldownSeconds = cooldownSeconds;
            this.quietBeforeSeconds = quietBeforeSeconds;
            this.predefined = predefined;
            this.pulses = pulses ?? Array.Empty<HapticPulse>();
        }

        public int Priority => priority;
        public float CooldownSeconds => Math.Max(0f, cooldownSeconds);
        public float QuietBeforeSeconds => Math.Max(0f, quietBeforeSeconds);
        public HapticPredefined Predefined => predefined;
        public int PulseCount => pulses?.Length ?? 0;
        public bool IsEmpty => PulseCount == 0;

        public HapticPulse PulseAt(int index) => pulses[index];

        /// <summary>Milliseconds from the start to the end of the last pulse.</summary>
        public int TotalMs
        {
            get
            {
                var total = 0;
                for (var i = 0; i < PulseCount; i++)
                {
                    total += pulses[i].DelayMs + pulses[i].DurationMs;
                }

                return total;
            }
        }

        /// <summary>A pattern of one pulse straight away.</summary>
        public static HapticPattern Pulse(int priority, float cooldownSeconds, int durationMs, float amplitude, HapticPredefined predefined = HapticPredefined.None) =>
            new(priority, cooldownSeconds, 0f, predefined, new HapticPulse(0, durationMs, amplitude));
    }
}
