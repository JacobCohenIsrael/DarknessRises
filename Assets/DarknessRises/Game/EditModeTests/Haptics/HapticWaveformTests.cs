using DarknessRises.Game.Haptics;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Haptics
{
    public sealed class HapticWaveformTests
    {
        private static HapticPattern Pattern(params HapticPulse[] pulses) => new(50, 0f, 0f, HapticPredefined.None, pulses);

        [Test]
        public void Pulses_become_alternating_still_and_on_timings_with_amplitudes()
        {
            var waveform = HapticWaveform.Build(Pattern(new HapticPulse(0, 60, 1f), new HapticPulse(40, 30, 0.5f)), 1f, 8);

            Assert.That(waveform.Timings, Is.EqualTo(new long[] { 0, 60, 40, 30 }));
            Assert.That(waveform.Amplitudes, Is.EqualTo(new[] { 0, 255, 0, 128 }));
            Assert.That(waveform.TotalMs, Is.EqualTo(130));
            Assert.That(waveform.IsOneShot, Is.False);
        }

        [Test]
        public void Strength_scales_every_amplitude_and_never_reaches_zero()
        {
            var waveform = HapticWaveform.Build(Pattern(new HapticPulse(0, 60, 1f), new HapticPulse(0, 60, 0.01f)), 0.5f, 8);

            Assert.That(waveform.Amplitudes, Is.EqualTo(new[] { 0, 128, 0, 1 }));
            Assert.That(waveform.Strength, Is.EqualTo(0.5f));
        }

        [Test]
        public void One_pulse_straight_away_is_a_one_shot()
        {
            var waveform = HapticWaveform.Build(Pattern(new HapticPulse(0, 12, 0.35f)), 1f, 8);

            Assert.That(waveform.IsOneShot, Is.True);
            Assert.That(waveform.WebPattern, Is.EqualTo(new[] { 8 }), "a weak 12 ms tick, with no amplitude, is shortened but never under the shortest pulse");
        }

        [Test]
        public void Without_amplitude_control_a_weak_pulse_is_shorter_and_the_rhythm_stays()
        {
            var waveform = HapticWaveform.Build(Pattern(new HapticPulse(0, 100, 0.5f), new HapticPulse(50, 100, 1f)), 1f, 8);

            Assert.That(waveform.DurationOnlyTimings, Is.EqualTo(new long[] { 0, 50, 100, 100 }), "the 50 ms lost is added to the stillness after it");
            Assert.That(waveform.WebPattern, Is.EqualTo(new[] { 50, 100, 100 }));
        }

        [Test]
        public void Pulses_back_to_back_are_one_pulse_without_amplitude_control()
        {
            var swell = Pattern(new HapticPulse(0, 100, 1f), new HapticPulse(0, 100, 1f), new HapticPulse(0, 100, 1f));

            var waveform = HapticWaveform.Build(swell, 1f, 8);

            Assert.That(waveform.Timings, Is.EqualTo(new long[] { 0, 100, 0, 100, 0, 100 }));
            Assert.That(waveform.DurationOnlyTimings, Is.EqualTo(new long[] { 0, 300 }));
            Assert.That(waveform.WebPattern, Is.EqualTo(new[] { 300 }));
        }

        [Test]
        public void A_pattern_that_waits_first_starts_the_web_pattern_with_nothing()
        {
            var waveform = HapticWaveform.Build(Pattern(new HapticPulse(30, 40, 1f)), 1f, 8);

            Assert.That(waveform.WebPattern, Is.EqualTo(new[] { 0, 30, 40 }));
        }

        [Test]
        public void An_empty_pattern_plays_nothing()
        {
            var waveform = HapticWaveform.Build(new HapticPattern(), 1f, 8);

            Assert.That(waveform.IsEmpty, Is.True);
            Assert.That(waveform.WebPattern, Is.Empty);
            Assert.That(waveform.TotalMs, Is.Zero);
        }

        [Test]
        public void A_predefined_click_is_carried_to_the_device()
        {
            var waveform = HapticWaveform.Build(HapticPattern.Pulse(10, 0f, 12, 0.3f, HapticPredefined.Tick), 1f, 8);

            Assert.That(waveform.Predefined, Is.EqualTo(HapticPredefined.Tick));
            Assert.That((int)HapticPredefined.Tick, Is.EqualTo(2), "Android's EFFECT_TICK");
            Assert.That((int)HapticPredefined.HeavyClick, Is.EqualTo(5), "Android's EFFECT_HEAVY_CLICK");
        }
    }
}
