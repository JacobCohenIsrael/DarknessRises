using System;
using System.Collections.Generic;
using DarknessRises.Game.Health;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Health
{
    public sealed class HurtFeedbackTests
    {
        private const float PerHitPoint = 0.4f;
        private const float FlashMax = 0.9f;
        private const float FlashSeconds = 0.5f;
        private const int LowHitPoints = 2;
        private const float Period = 1f;
        private const float DubDelay = 0.3f;
        private const float DubStrength = 0.5f;
        private const float Decay = 0.25f;
        private const float Rest = 0.2f;
        private const float Peak = 0.6f;
        private const float Tolerance = 1e-4f;

        private HurtFeedback feedback;
        private List<bool> beats;

        private static HurtFeedbackTiming Timing(float dubDelay = DubDelay, float period = Period) =>
            new(PerHitPoint, FlashMax, FlashSeconds, LowHitPoints, period, dubDelay, DubStrength, Decay, Rest, Peak);

        [SetUp]
        public void SetUp()
        {
            feedback = new HurtFeedback(Timing());
            beats = new List<bool>();
            feedback.Beat += beats.Add;
        }

        [Test]
        public void StartsClear()
        {
            Assert.That(feedback.Strength, Is.Zero);
            Assert.That(feedback.IsLow, Is.False);
        }

        [Test]
        public void AHitFlashesInProportionToItsDamage()
        {
            feedback.Hit(1);
            Assert.That(feedback.Strength, Is.EqualTo(PerHitPoint).Within(Tolerance));

            var bigger = new HurtFeedback(Timing());
            bigger.Hit(2);
            Assert.That(bigger.Strength, Is.EqualTo(2 * PerHitPoint).Within(Tolerance));
        }

        [Test]
        public void TheFlashIsCappedAtItsMaximum()
        {
            feedback.Hit(5);
            Assert.That(feedback.Strength, Is.EqualTo(FlashMax).Within(Tolerance));
        }

        [Test]
        public void TheFlashFadesOutOverItsSeconds()
        {
            feedback.Hit(1);
            feedback.Tick(FlashSeconds / 2f);
            Assert.That(feedback.Strength, Is.EqualTo(PerHitPoint / 2f).Within(Tolerance));

            feedback.Tick(FlashSeconds / 2f);
            Assert.That(feedback.Strength, Is.Zero);
        }

        [Test]
        public void NoDamageNoFlash()
        {
            feedback.Hit(0);
            Assert.That(feedback.Strength, Is.Zero);
        }

        [TestCase(5, 5, false, TestName = "Full health isn't low")]
        [TestCase(3, 5, false, TestName = "Above the threshold isn't low")]
        [TestCase(2, 5, true, TestName = "At the threshold is low")]
        [TestCase(1, 5, true, TestName = "Under the threshold is low")]
        [TestCase(0, 5, false, TestName = "Dead isn't low")]
        [TestCase(2, 2, false, TestName = "Full health is never low, even at the threshold")]
        public void LowHealthThreshold(int hitPoints, int maxHitPoints, bool low)
        {
            Assert.That(HurtFeedback.IsLowAt(hitPoints, maxHitPoints, LowHitPoints), Is.EqualTo(low));
        }

        [Test]
        public void GoingLowBeatsAtOnceThenTheSoftBeatAfterItsDelay()
        {
            feedback.SetHitPoints(2, 5);
            feedback.Tick(0.01f);
            Assert.That(beats, Is.EqualTo(new[] { false }));

            feedback.Tick(DubDelay);
            Assert.That(beats, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void TheHeartbeatRepeatsEachPeriod()
        {
            feedback.SetHitPoints(1, 5);
            for (var i = 0; i < 300; i++)
            {
                feedback.Tick(0.01f);
            }

            // 3 seconds from the first beat: beats at 0, 1 and 2, and their soft beats, and the beat at 3.
            Assert.That(beats.FindAll(soft => !soft).Count, Is.InRange(3, 4));
            Assert.That(beats.FindAll(soft => soft).Count, Is.EqualTo(3));
        }

        [Test]
        public void ThePulseSwellsOnABeatAndRestsBetween()
        {
            feedback.SetHitPoints(2, 5);
            feedback.Tick(0f);
            Assert.That(feedback.PulseStrength, Is.EqualTo(Peak).Within(Tolerance), "Full at the strong beat.");

            feedback.Tick(Decay);
            Assert.That(feedback.PulseStrength, Is.EqualTo(Rest).Within(Tolerance), "Back to rest once the swell has eased.");

            feedback.Tick(DubDelay - Decay);
            var dubPeak = Rest + (Peak - Rest) * DubStrength;
            Assert.That(feedback.PulseStrength, Is.EqualTo(dubPeak).Within(Tolerance), "The soft beat swells less.");
        }

        [Test]
        public void DeathStopsTheHeartbeatAtOnce()
        {
            feedback.SetHitPoints(1, 5);
            feedback.Tick(0.01f);
            beats.Clear();

            feedback.SetHitPoints(0, 5);
            feedback.Tick(Period * 2f);

            Assert.That(feedback.IsLow, Is.False);
            Assert.That(feedback.PulseStrength, Is.Zero);
            Assert.That(beats, Is.Empty);
        }

        [Test]
        public void BackToFullStopsTheHeartbeat()
        {
            feedback.SetHitPoints(1, 5);
            feedback.SetHitPoints(5, 5);

            feedback.Tick(Period);
            Assert.That(feedback.IsLow, Is.False);
            Assert.That(beats, Is.Empty);
        }

        [Test]
        public void StopClearsTheFlashAndThePulse()
        {
            feedback.SetHitPoints(1, 5);
            feedback.Hit(1);
            feedback.Tick(0.01f);
            beats.Clear();

            feedback.Stop();
            feedback.Tick(Period * 2f);

            Assert.That(feedback.Strength, Is.Zero);
            Assert.That(beats, Is.Empty);
        }

        [Test]
        public void TheFlashShowsOverTheRestingPulse()
        {
            feedback.SetHitPoints(2, 5);
            feedback.Tick(0f);
            feedback.Tick(Decay);
            feedback.Hit(2);

            Assert.That(feedback.Strength, Is.EqualTo(2 * PerHitPoint).Within(Tolerance));
        }

        [Test]
        public void ZeroLowHitPointsIsNeverLow()
        {
            Assert.That(HurtFeedback.IsLowAt(1, 5, 0), Is.False);
        }

        [Test]
        public void RejectsTimingThatCantBeat()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Timing(dubDelay: Period));
            Assert.Throws<ArgumentOutOfRangeException>(() => Timing(period: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => feedback.Tick(-1f));
        }

        [Test]
        public void TickingDoesNotAllocate()
        {
            feedback.SetHitPoints(1, 5);
            feedback.Beat -= beats.Add;
            AllocationAssert.DoesNotAllocate(() =>
            {
                feedback.Tick(0.4f);
                _ = feedback.Strength;
            });
        }
    }
}
