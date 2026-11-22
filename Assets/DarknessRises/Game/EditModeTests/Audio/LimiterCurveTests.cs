using System;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Boot;
using NUnit.Framework;
using UnityEditor;

namespace DarknessRises.Game.EditModeTests.Audio
{
    /// <summary>
    /// The Web player's safety limiter (issue #121): sounds that pile up past full scale are held under it, and everything
    /// under the threshold plays exactly as without the limiter.
    /// </summary>
    public sealed class LimiterCurveTests
    {
        private const float Tolerance = 1e-4f;
        private const string BootDefinitionPath = "Assets/DarknessRises/Boot/BootDefinition.asset";

        [Test]
        public void Under_the_threshold_a_sound_comes_out_as_it_went_in()
        {
            var curve = new LimiterCurve(-3f, 20f);

            Assert.That(curve.Limit(-3f), Is.EqualTo(-3f));
            Assert.That(curve.Limit(-20f), Is.EqualTo(-20f));
        }

        [Test]
        public void Over_the_threshold_a_peak_rises_only_one_ratio_as_fast()
        {
            var curve = new LimiterCurve(-3f, 20f);

            Assert.That(curve.Limit(9f), Is.EqualTo(-3f + 12f / 20f).Within(Tolerance));
        }

        [Test]
        public void The_browsers_makeup_gain_is_taken_back_off()
        {
            // The Web Audio compressor raises everything by (1 / its gain at full scale) ^ 0.6.
            var curve = new LimiterCurve(-3f, 20f);
            var gainAtFullScale = Math.Pow(10.0, (-3.0 + 3.0 / 20.0) / 20.0);

            Assert.That(curve.MakeupGain, Is.EqualTo((float)Math.Pow(1.0 / gainAtFullScale, 0.6)).Within(Tolerance));
            Assert.That(curve.CompensationGain * curve.MakeupGain, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void A_ratio_of_one_changes_nothing()
        {
            var curve = new LimiterCurve(-3f, 1f);

            Assert.That(curve.Limit(6f), Is.EqualTo(6f));
            Assert.That(curve.MakeupGain, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void A_threshold_over_full_scale_or_a_ratio_under_one_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LimiterCurve(1f, 20f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LimiterCurve(-3f, 0.5f));
        }

        [Test]
        public void The_authored_limiter_keeps_the_measured_pile_up_under_full_scale()
        {
            // The loudest pile-up measured in the 0.1.18 Web build: +7.4 dBFS, from a fight's footsteps, a hurt voice and the storm.
            var settings = AssetDatabase.LoadAssetAtPath<BootDefinition>(BootDefinitionPath).WebAudioLimiter;

            Assert.That(settings.Enabled, Is.True);
            Assert.That(settings.Curve.Limit(7.4f), Is.LessThan(0f));
            Assert.That(settings.ThresholdDecibels, Is.GreaterThanOrEqualTo(-6f), "Gentle: only the loudest moments are touched.");
            Assert.That(WebAudioLimiter.Install(settings), Is.False, "Off the Web there is nothing to install.");
        }
    }
}
