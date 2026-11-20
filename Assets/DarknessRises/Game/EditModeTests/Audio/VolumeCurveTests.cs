using System;
using DarknessRises.Game.Audio;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Audio
{
    public sealed class VolumeCurveTests
    {
        private const float Tolerance = 1e-4f;
        private const float Quietest = -40f;

        [Test]
        public void Full_plays_sounds_as_authored()
        {
            var curve = new VolumeCurve(Quietest);

            Assert.That(curve.Decibels(100), Is.EqualTo(0f).Within(Tolerance));
            Assert.That(curve.Gain(100), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Zero_is_silent()
        {
            var curve = new VolumeCurve(Quietest);

            Assert.That(curve.Gain(0), Is.EqualTo(0f));
            Assert.That(float.IsNegativeInfinity(curve.Decibels(0)), Is.True);
        }

        [Test]
        public void The_lowest_step_is_the_authored_quietest_level()
        {
            var curve = new VolumeCurve(Quietest);

            Assert.That(curve.Decibels(1), Is.EqualTo(Quietest).Within(Tolerance));
            Assert.That(curve.Gain(1), Is.EqualTo(0.01f).Within(Tolerance));
        }

        [Test]
        public void Loudness_rises_evenly_in_decibels()
        {
            var curve = new VolumeCurve(Quietest);
            var step = curve.Decibels(51) - curve.Decibels(50);

            Assert.That(curve.Decibels(2) - curve.Decibels(1), Is.EqualTo(step).Within(Tolerance));
            Assert.That(curve.Decibels(100) - curve.Decibels(99), Is.EqualTo(step).Within(Tolerance));
            Assert.That(step, Is.EqualTo(-Quietest / 99f).Within(Tolerance));
        }

        [Test]
        public void Every_step_up_is_louder()
        {
            var curve = new VolumeCurve(Quietest);
            for (var percent = 1; percent <= 100; percent++)
            {
                Assert.That(curve.Gain(percent), Is.GreaterThan(curve.Gain(percent - 1)), $"at {percent} %");
            }
        }

        [Test]
        public void Halfway_is_far_quieter_than_half_the_amplitude()
        {
            var curve = new VolumeCurve(Quietest);

            // About -20 dB: a tenth of the amplitude, which sounds like half as loud.
            Assert.That(curve.Gain(50), Is.EqualTo(0.1f).Within(0.01f));
        }

        [TestCase(-5, 0)]
        [TestCase(0, 0)]
        [TestCase(37, 37)]
        [TestCase(100, 100)]
        [TestCase(250, 100)]
        public void Percents_are_kept_from_0_to_100(int percent, int clamped)
        {
            Assert.That(VolumeCurve.Clamp(percent), Is.EqualTo(clamped));
        }

        [Test]
        public void Out_of_range_percents_play_at_the_nearest_end()
        {
            var curve = new VolumeCurve(Quietest);

            Assert.That(curve.Gain(150), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(curve.Gain(-10), Is.EqualTo(0f));
        }

        [TestCase(0f)]
        [TestCase(6f)]
        public void The_quietest_step_must_be_below_full(float quietestDecibels)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new VolumeCurve(quietestDecibels));
        }
    }
}
