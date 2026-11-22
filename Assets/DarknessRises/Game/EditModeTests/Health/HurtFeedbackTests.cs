using System;
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
        private const int CriticalHitPoints = 1;
        private const float Crossfade = 0.5f;
        private const float Decay = 0.2f;
        private const float Rest = 0.2f;
        private const float Peak = 0.6f;
        private const float SoftStrength = 0.5f;
        private const float Tolerance = 1e-4f;

        // 60 BPM: a strong beat every second, the first 0.25 s into the clip, the soft beat 0.25 s after each.
        private static readonly HeartbeatRhythm SlowRhythm = new(60f, 0.25f, 0.25f);

        // 120 BPM: a strong beat every half second from the clip's start, the soft beat 0.15 s after.
        private static readonly HeartbeatRhythm FastRhythm = new(120f, 0f, 0.15f);

        private HurtFeedback feedback;

        private static HurtFeedbackTiming Timing(float crossfade = Crossfade) =>
            new(PerHitPoint, FlashMax, FlashSeconds, LowHitPoints, CriticalHitPoints, crossfade, Decay, Rest, Peak, SoftStrength, SlowRhythm, FastRhythm);

        [SetUp]
        public void SetUp()
        {
            feedback = new HurtFeedback(Timing());
        }

        [Test]
        public void StartsClearAndSilent()
        {
            Assert.That(feedback.Strength(0f, 0f), Is.Zero);
            Assert.That(feedback.Tier, Is.EqualTo(HeartbeatTier.None));
            Assert.That(feedback.SlowWeight, Is.Zero);
            Assert.That(feedback.FastWeight, Is.Zero);
        }

        [Test]
        public void AHitFlashesInProportionToItsDamage()
        {
            feedback.Hit(1);
            Assert.That(feedback.FlashStrength, Is.EqualTo(PerHitPoint).Within(Tolerance));

            var bigger = new HurtFeedback(Timing());
            bigger.Hit(2);
            Assert.That(bigger.FlashStrength, Is.EqualTo(2 * PerHitPoint).Within(Tolerance));
        }

        [Test]
        public void TheFlashIsCappedAtItsMaximum()
        {
            feedback.Hit(5);
            Assert.That(feedback.FlashStrength, Is.EqualTo(FlashMax).Within(Tolerance));
        }

        [Test]
        public void TheFlashFadesOutOverItsSeconds()
        {
            feedback.Hit(1);
            feedback.Tick(FlashSeconds / 2f);
            Assert.That(feedback.FlashStrength, Is.EqualTo(PerHitPoint / 2f).Within(Tolerance));

            feedback.Tick(FlashSeconds / 2f);
            Assert.That(feedback.Strength(0.5f, 0.5f), Is.Zero);
        }

        [Test]
        public void NoDamageNoFlash()
        {
            feedback.Hit(0);
            Assert.That(feedback.FlashStrength, Is.Zero);
        }

        [TestCase(5, 5, HeartbeatTier.None, TestName = "Full health has no heartbeat")]
        [TestCase(3, 5, HeartbeatTier.None, TestName = "Above the low threshold has no heartbeat")]
        [TestCase(2, 5, HeartbeatTier.Slow, TestName = "At the low threshold the heartbeat is slow")]
        [TestCase(1, 5, HeartbeatTier.Fast, TestName = "At the critical threshold the heartbeat is fast")]
        [TestCase(0, 5, HeartbeatTier.None, TestName = "Dead has no heartbeat")]
        [TestCase(2, 2, HeartbeatTier.None, TestName = "Full health never beats, even at the threshold")]
        public void HeartbeatTiers(int hitPoints, int maxHitPoints, HeartbeatTier tier)
        {
            Assert.That(HurtFeedback.TierAt(hitPoints, maxHitPoints, LowHitPoints, CriticalHitPoints), Is.EqualTo(tier));
        }

        [Test]
        public void ZeroThresholdsNeverBeat()
        {
            Assert.That(HurtFeedback.TierAt(1, 5, 0, 0), Is.EqualTo(HeartbeatTier.None));
        }

        [Test]
        public void ACriticalThresholdAboveTheLowOneStillBeatsFast()
        {
            Assert.That(HurtFeedback.TierAt(3, 5, 2, 3), Is.EqualTo(HeartbeatTier.Fast));
        }

        [Test]
        public void GoingLowStartsTheSlowLoopAtFullAtOnce()
        {
            feedback.SetHitPoints(2, 5);

            Assert.That(feedback.Tier, Is.EqualTo(HeartbeatTier.Slow));
            Assert.That(feedback.SlowWeight, Is.EqualTo(1f));
            Assert.That(feedback.FastWeight, Is.Zero);
        }

        [Test]
        public void DroppingToCriticalCrossfadesToTheFastLoop()
        {
            feedback.SetHitPoints(2, 5);
            feedback.SetHitPoints(1, 5);

            Assert.That(feedback.Tier, Is.EqualTo(HeartbeatTier.Fast));
            Assert.That(feedback.SlowWeight, Is.EqualTo(1f), "The crossfade starts from the slow loop.");

            feedback.Tick(Crossfade / 2f);
            Assert.That(feedback.SlowWeight, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(feedback.FastWeight, Is.EqualTo(0.5f).Within(Tolerance));

            feedback.Tick(Crossfade);
            Assert.That(feedback.SlowWeight, Is.Zero);
            Assert.That(feedback.FastWeight, Is.EqualTo(1f));
        }

        [Test]
        public void HealingBackToLowCrossfadesToTheSlowLoop()
        {
            feedback.SetHitPoints(1, 5);
            feedback.SetHitPoints(2, 5);
            feedback.Tick(Crossfade);

            Assert.That(feedback.SlowWeight, Is.EqualTo(1f));
            Assert.That(feedback.FastWeight, Is.Zero);
        }

        [Test]
        public void ANoTimeCrossfadeSwitchesAtOnce()
        {
            feedback = new HurtFeedback(Timing(crossfade: 0f));
            feedback.SetHitPoints(2, 5);
            feedback.SetHitPoints(1, 5);

            Assert.That(feedback.SlowWeight, Is.Zero);
            Assert.That(feedback.FastWeight, Is.EqualTo(1f));
        }

        [Test]
        public void ThePulsePeaksOnTheClipsStrongBeatAndRestsBetween()
        {
            feedback.SetHitPoints(2, 5);

            Assert.That(feedback.PulseStrength(SlowRhythm.FirstBeatSeconds, 0f), Is.EqualTo(Peak).Within(Tolerance), "Full on the strong beat.");
            Assert.That(feedback.PulseStrength(0.25f + 0.2f, 0f), Is.EqualTo(Rest).Within(Tolerance), "At rest between the beats.");
            var softPeak = Rest + (Peak - Rest) * SoftStrength;
            Assert.That(feedback.PulseStrength(0.5f, 0f), Is.EqualTo(softPeak).Within(Tolerance), "The soft beat swells less.");
        }

        [Test]
        public void ThePulseFollowsTheTempoAcrossLoops()
        {
            feedback.SetHitPoints(2, 5);

            Assert.That(feedback.PulseStrength(1.25f, 0f), Is.EqualTo(Peak).Within(Tolerance), "60 BPM: the next strong beat a second later.");
            Assert.That(feedback.PulseStrength(3.25f, 0f), Is.EqualTo(Peak).Within(Tolerance));
            Assert.That(feedback.PulseStrength(0.125f, 0f), Is.EqualTo(Rest).Within(Tolerance), "Before the clip's first beat is the end of the last one.");
        }

        [Test]
        public void TheFastPulseReadsTheFastLoop()
        {
            feedback = new HurtFeedback(Timing(crossfade: 0f));
            feedback.SetHitPoints(1, 5);

            Assert.That(feedback.PulseStrength(0.37f, 0.5f), Is.EqualTo(Peak).Within(Tolerance), "120 BPM: a strong beat every half second.");
            Assert.That(feedback.PulseStrength(0.25f, 0.4f), Is.EqualTo(Rest).Within(Tolerance), "The slow loop's beat doesn't count once it's silent.");
        }

        [Test]
        public void DeathSilencesBothLoopsAtOnce()
        {
            feedback.SetHitPoints(1, 5);
            feedback.SetHitPoints(0, 5);

            Assert.That(feedback.IsLow, Is.False);
            Assert.That(feedback.SlowWeight, Is.Zero);
            Assert.That(feedback.FastWeight, Is.Zero);
            Assert.That(feedback.PulseStrength(0.1f, 0f), Is.Zero);
        }

        [Test]
        public void BackToFullSilencesTheHeartbeat()
        {
            feedback.SetHitPoints(2, 5);
            feedback.SetHitPoints(5, 5);

            Assert.That(feedback.IsLow, Is.False);
            Assert.That(feedback.SlowWeight, Is.Zero);
        }

        [Test]
        public void StopClearsTheFlashAndTheHeartbeat()
        {
            feedback.SetHitPoints(1, 5);
            feedback.Hit(1);

            feedback.Stop();
            feedback.Tick(1f);

            Assert.That(feedback.Strength(0.1f, 0f), Is.Zero);
            Assert.That(feedback.FastWeight, Is.Zero);
        }

        [Test]
        public void TheFlashShowsOverTheRestingPulse()
        {
            feedback.SetHitPoints(2, 5);
            feedback.Hit(2);

            Assert.That(feedback.Strength(0.35f, 0f), Is.EqualTo(2 * PerHitPoint).Within(Tolerance));
        }

        [Test]
        public void RhythmRejectsATempoItCantBeat()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeartbeatRhythm(0f, 0f, 0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeartbeatRhythm(60f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeartbeatRhythm(60f, -0.1f, 0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => feedback.Tick(-1f));
        }

        [Test]
        public void TickingAndReadingDoNotAllocate()
        {
            feedback.SetHitPoints(2, 5);
            feedback.SetHitPoints(1, 5);
            AllocationAssert.DoesNotAllocate(() =>
            {
                feedback.Tick(0.016f);
                _ = feedback.Strength(0.4f, 0.2f);
            });
        }
    }
}
