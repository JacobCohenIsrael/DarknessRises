using System;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class StormBedTests
    {
        private const float BaseVolume = 0.5f;
        private const float FadeIn = 1f;
        private const float FadeOut = 2f;
        private const float Peak = 1f;
        private const float Attack = 0.25f;
        private const float Hold = 0.5f;
        private const float Release = 1f;
        private const float Tolerance = 1e-4f;
        private const float Frame = 1f / 60f;

        private static StormEnvelope Envelope() => new(BaseVolume, FadeIn, FadeOut, Peak, Attack, Hold, Release, 0.9f, 1.1f);

        private static StormBed AudibleBed()
        {
            var bed = new StormBed(Envelope());
            bed.SetAudible(true);
            bed.Tick(FadeIn);
            return bed;
        }

        [Test]
        public void StartsSilent_AndFadesInToTheBaseVolumeOverTheFadeInTime()
        {
            var bed = new StormBed(Envelope());
            bed.Tick(1f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(0f));

            bed.SetAudible(true);
            bed.Tick(FadeIn / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume / 2f).Within(Tolerance));

            bed.Tick(FadeIn / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(1f));
        }

        [Test]
        public void ARumbleRisesToThePeak_Holds_ThenReturnsToTheBaseVolumeAndPitch()
        {
            var bed = AudibleBed();
            bed.Rumble(new FixedRandom(1.1f));

            bed.Tick(Attack / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo((BaseVolume + Peak) / 2f).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(1.05f).Within(Tolerance));

            bed.Tick(Attack / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(Peak).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(1.1f).Within(Tolerance));

            bed.Tick(Hold);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(Peak).Within(Tolerance));

            bed.Tick(Release / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo((BaseVolume + Peak) / 2f).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(1.05f).Within(Tolerance));

            bed.Tick(Release / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(1f).Within(Tolerance));
            Assert.That(bed.IsSwelling, NUnit.Framework.Is.False);
        }

        [Test]
        public void ALongFrameCarriesTheSwellThroughItsPhases()
        {
            var bed = AudibleBed();
            bed.Rumble(new FixedRandom(1f));

            bed.Tick(Attack + Hold + Release / 4f);

            Assert.That(bed.Swell, NUnit.Framework.Is.EqualTo(0.75f).Within(Tolerance));
        }

        [Test]
        public void ARumbleDuringAnotherCarriesOnFromTheCurrentVolumeAndPitch_WithoutAJump()
        {
            var bed = AudibleBed();
            var random = new FixedRandom(1.1f);
            bed.Rumble(random);
            bed.Tick(Attack + Hold + Release / 2f);
            var volumeBefore = bed.Volume;
            var pitchBefore = bed.Pitch;

            random.Value = 0.9f;
            bed.Rumble(random);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(volumeBefore).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(pitchBefore).Within(Tolerance));

            // Frame by frame it never steps by more than one frame of the attack's full sweep.
            var maxStep = (Peak - BaseVolume) * Frame / Attack + Tolerance;
            var last = bed.Volume;
            for (var t = 0f; t < Attack + Hold + Release + 0.5f; t += Frame)
            {
                bed.Tick(Frame);
                Assert.That(Math.Abs(bed.Volume - last), NUnit.Framework.Is.LessThanOrEqualTo(maxStep));
                last = bed.Volume;
            }

            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume).Within(Tolerance));
            Assert.That(bed.Pitch, NUnit.Framework.Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void ARoomWithoutWindowsFadesTheBedToZero_AndOneWithWindowsFadesItBack()
        {
            var bed = AudibleBed();

            bed.SetAudible(false);
            bed.Tick(FadeOut / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume / 2f).Within(Tolerance));
            bed.Tick(FadeOut / 2f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(0f));

            bed.SetAudible(true);
            bed.Tick(FadeIn / 4f);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume / 4f).Within(Tolerance));
            bed.Tick(FadeIn);
            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(BaseVolume).Within(Tolerance));
        }

        [Test]
        public void FadingBackInHalfwayThroughAFadeOutStartsFromTheCurrentVolume()
        {
            var bed = AudibleBed();
            bed.SetAudible(false);
            bed.Tick(FadeOut / 4f);
            var volume = bed.Volume;

            bed.SetAudible(true);
            bed.Tick(Frame);

            Assert.That(bed.Volume, NUnit.Framework.Is.GreaterThan(volume));
            Assert.That(bed.Volume - volume, NUnit.Framework.Is.LessThan(0.02f));
        }

        [Test]
        public void ARumbleWhileFadedOutStaysSilent()
        {
            var bed = new StormBed(Envelope());
            bed.Rumble(new FixedRandom(1f));
            bed.Tick(Attack);

            Assert.That(bed.Volume, NUnit.Framework.Is.EqualTo(0f));
        }

        [Test]
        public void TheRumblesPitchNudgeIsPickedFromTheAuthoredRange()
        {
            var bed = AudibleBed();
            var random = new FixedRandom(1f);

            bed.Rumble(random);

            Assert.That(random.LastRange, NUnit.Framework.Is.EqualTo((0.9f, 1.1f)));
        }

        [TestCase(-0.1f, 0.5f, 1f, 1.1f)]
        [TestCase(0.5f, 0.4f, 1f, 1.1f)]
        [TestCase(0.5f, 1f, 0f, 1.1f)]
        [TestCase(0.5f, 1f, 1.1f, 1f)]
        public void EnvelopeRejectsImpossibleValues(float baseVolume, float peak, float minPitch, float maxPitch)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _ = new StormEnvelope(baseVolume, FadeIn, FadeOut, peak, Attack, Hold, Release, minPitch, maxPitch));
        }

        [Test]
        public void TickingDoesNotAllocate()
        {
            var bed = AudibleBed();
            var random = new SystemRandom(59);
            bed.Rumble(random);
            bed.Tick(Frame);

            Assert.That(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    if (i % 100 == 0)
                    {
                        bed.Rumble(random);
                    }

                    bed.SetAudible(i < 500);
                    bed.Tick(Frame);
                }
            }, Is.Not.AllocatingGCMemory());
        }

        private sealed class FixedRandom : IRandom
        {
            public FixedRandom(float value)
            {
                Value = value;
            }

            public float Value { get; set; }

            public (float, float) LastRange { get; private set; }

            public float Range(float min, float max)
            {
                LastRange = (min, max);
                return Value;
            }
        }
    }
}
