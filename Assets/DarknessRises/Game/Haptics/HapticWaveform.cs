using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// One pattern at one strength, in the forms the platforms take, worked out once when the game starts so playing it
    /// costs nothing. Android: alternating still/on <see cref="Timings"/> with an <see cref="Amplitudes"/> entry each
    /// (0 to 255), as <c>VibrationEffect.createWaveform</c> takes them. Without amplitude control a pulse can't be gentle,
    /// so it's made shorter instead: <see cref="DurationOnlyTimings"/>, where a pulse runs for its amplitude's share of
    /// its length (never under the shortest pulse), the time it loses added to the stillness after it so the rhythm
    /// stays. The Web's <c>navigator.vibrate</c> has no amplitude and starts with the motor on: <see cref="WebPattern"/>.
    /// </summary>
    public sealed class HapticWaveform
    {
        private HapticWaveform(long[] timings, int[] amplitudes, long[] durationOnlyTimings, int[] webPattern, HapticPredefined predefined, float strength)
        {
            Timings = timings;
            Amplitudes = amplitudes;
            DurationOnlyTimings = durationOnlyTimings;
            WebPattern = webPattern;
            Predefined = predefined;
            Strength = strength;
            long total = 0;
            foreach (var timing in timings)
            {
                total += timing;
            }

            TotalMs = total;
        }

        /// <summary>Milliseconds, alternating: still, on, still, on…</summary>
        public long[] Timings { get; }

        /// <summary>The motor's strength over each of <see cref="Timings"/>, 0 to 255: 0 for every still one.</summary>
        public int[] Amplitudes { get; }

        /// <summary>Milliseconds, alternating still and on, for a motor that is either off or full.</summary>
        public long[] DurationOnlyTimings { get; }

        /// <summary>Milliseconds for <c>navigator.vibrate</c>, alternating: on, still, on…</summary>
        public int[] WebPattern { get; }

        public HapticPredefined Predefined { get; }

        /// <summary>The strength this waveform was made at, 0 to 1.</summary>
        public float Strength { get; }

        /// <summary>Milliseconds from the start to the end of the last pulse.</summary>
        public long TotalMs { get; }

        /// <summary>Whether it is one pulse straight away: Android plays it as a one-shot.</summary>
        public bool IsOneShot => Timings.Length == 2 && Timings[0] == 0;

        /// <summary>An empty pattern: nothing plays.</summary>
        public bool IsEmpty => Timings.Length == 0;

        /// <summary>
        /// <paramref name="pattern"/> at <paramref name="strength"/> (0 to 1, scaling every pulse's amplitude). A pulse
        /// made shorter for want of amplitude control lasts at least <paramref name="shortestPulseMs"/>.
        /// </summary>
        public static HapticWaveform Build(HapticPattern pattern, float strength, int shortestPulseMs)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            strength = Math.Clamp(strength, 0f, 1f);
            var count = pattern.PulseCount;
            var timings = new long[count * 2];
            var amplitudes = new int[count * 2];
            var durationOnly = new long[count * 2];
            long carried = 0;
            for (var i = 0; i < count; i++)
            {
                var pulse = pattern.PulseAt(i);
                var amplitude = pulse.Amplitude * strength;
                timings[i * 2] = pulse.DelayMs;
                timings[i * 2 + 1] = pulse.DurationMs;
                amplitudes[i * 2] = 0;
                amplitudes[i * 2 + 1] = ToByte(amplitude);

                var shortened = Math.Min(pulse.DurationMs, Math.Max(shortestPulseMs, (long)Math.Round(pulse.DurationMs * amplitude)));
                durationOnly[i * 2] = pulse.DelayMs + carried;
                durationOnly[i * 2 + 1] = shortened;
                carried = pulse.DurationMs - shortened;
            }

            return new HapticWaveform(timings, amplitudes, MergeStill(durationOnly), ToWeb(MergeStill(durationOnly)), pattern.Predefined, strength);
        }

        /// <summary>The motor's strength for <paramref name="amplitude"/> (0 to 1): 1 to 255, as Android takes it.</summary>
        public static int ToByte(float amplitude) => Math.Clamp((int)Math.Round(amplitude * 255f), 1, 255);

        // Pulses back to back (a swell) are one long pulse for a motor that is only off or on.
        private static long[] MergeStill(long[] timings)
        {
            var merged = new List<long>(timings.Length);
            for (var i = 0; i < timings.Length; i += 2)
            {
                if (i > 0 && timings[i] == 0)
                {
                    merged[merged.Count - 1] += timings[i + 1];
                    continue;
                }

                merged.Add(timings[i]);
                merged.Add(timings[i + 1]);
            }

            return merged.ToArray();
        }

        // navigator.vibrate starts with the motor on: a pattern that waits first starts with a pulse of nothing.
        private static int[] ToWeb(long[] timings)
        {
            if (timings.Length == 0)
            {
                return Array.Empty<int>();
            }

            var skipFirst = timings[0] == 0;
            var web = new List<int>(timings.Length + 1);
            if (!skipFirst)
            {
                web.Add(0);
            }

            for (var i = skipFirst ? 1 : 0; i < timings.Length; i++)
            {
                web.Add((int)timings[i]);
            }

            return web.ToArray();
        }
    }
}
