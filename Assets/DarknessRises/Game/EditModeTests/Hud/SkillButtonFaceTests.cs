using System;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Skills;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Hud
{
    public sealed class SkillButtonFaceTests
    {
        private const float Duration = 4f;
        private const float Cooldown = 10f;
        private const float PulseSeconds = 0.4f;
        private const float PulseScale = 1.2f;
        private const float Tolerance = 1e-4f;

        private Skill skill;
        private SkillButtonFace face;

        [SetUp]
        public void SetUp()
        {
            skill = new Skill(new SkillTiming(Duration, Cooldown));
            face = new SkillButtonFace(skill, PulseSeconds, PulseScale);
        }

        private void Advance(float seconds)
        {
            skill.Tick(seconds);
            face.Tick(seconds);
        }

        [Test]
        public void ReadyShowsNoSweepNoRingAndNoPulse()
        {
            Assert.That(face.SweepFill, Is.Zero);
            Assert.That(face.ActiveFill, Is.Zero);
            Assert.That(face.SecondsLeft, Is.Zero);
            Assert.That(face.Scale, Is.EqualTo(1f));
            Assert.That(face.IsPulsing, Is.False);
        }

        [Test]
        public void TheActiveRingRunsDownWithTheEffect()
        {
            skill.TryActivate();
            Advance(1f);

            Assert.That(face.ActiveFill, Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(face.SweepFill, Is.Zero);
        }

        [Test]
        public void TheSweepCoversTheShareOfTheCooldownLeft()
        {
            skill.TryActivate();
            Advance(Duration);
            Advance(Cooldown * 0.3f);

            Assert.That(face.Phase, Is.EqualTo(SkillPhase.CoolingDown));
            Assert.That(face.SweepFill, Is.EqualTo(0.7f).Within(Tolerance));
            Assert.That(face.ActiveFill, Is.Zero);
        }

        [Test]
        public void SecondsLeftRoundUpSoTheLastSecondReadsOne()
        {
            skill.TryActivate();
            Advance(Duration);
            Advance(Cooldown - 0.2f);

            Assert.That(face.SecondsLeft, Is.EqualTo(1));

            Advance(0.1f);
            Assert.That(face.SecondsLeft, Is.EqualTo(1));
        }

        [Test]
        public void SecondsLeftCountTheWholeCooldownAtItsStart()
        {
            skill.TryActivate();
            Advance(Duration);

            Assert.That(face.SecondsLeft, Is.EqualTo((int)Cooldown));
        }

        [Test]
        public void BecomingReadyPulsesOnceAndSettles()
        {
            skill.TryActivate();
            Advance(Duration);
            Advance(Cooldown);

            Assert.That(face.Phase, Is.EqualTo(SkillPhase.Ready));
            Assert.That(face.IsPulsing, Is.True);
            Assert.That(face.Scale, Is.EqualTo(1f).Within(Tolerance), "The pulse starts from rest.");

            Advance(PulseSeconds / 2f);
            Assert.That(face.Scale, Is.EqualTo(PulseScale).Within(Tolerance), "It peaks halfway.");

            Advance(PulseSeconds / 2f);
            Assert.That(face.IsPulsing, Is.False);
            Assert.That(face.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void ASkillWithNoCooldownStillPulsesWhenItEnds()
        {
            skill = new Skill(new SkillTiming(Duration, 0f));
            face = new SkillButtonFace(skill, PulseSeconds, PulseScale);
            skill.TryActivate();
            Advance(Duration / 2f);
            Advance(Duration / 2f);

            Assert.That(face.Phase, Is.EqualTo(SkillPhase.Ready));
            Assert.That(face.IsPulsing, Is.True);
        }

        [Test]
        public void TriggeringCutsThePulseShort()
        {
            skill.TryActivate();
            Advance(Duration);
            Advance(Cooldown);
            Advance(PulseSeconds / 4f);

            skill.TryActivate();
            face.Tick(0f);

            Assert.That(face.IsPulsing, Is.False);
            Assert.That(face.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void NoPulseIsAllowed()
        {
            face = new SkillButtonFace(skill, 0f, PulseScale);
            skill.TryActivate();
            Advance(Duration);
            Advance(Cooldown);

            Assert.That(face.IsPulsing, Is.False);
            Assert.That(face.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void RejectsAShrinkingPulseAndNegativeTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkillButtonFace(skill, PulseSeconds, 0.9f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SkillButtonFace(skill, -1f, PulseScale));
            Assert.Throws<ArgumentOutOfRangeException>(() => face.Tick(-0.1f));
        }

        [Test]
        public void TickingDoesNotAllocate()
        {
            skill.TryActivate();
            AllocationAssert.DoesNotAllocate(() =>
            {
                Advance(0.016f);
                _ = face.SweepFill + face.ActiveFill + face.Scale + face.SecondsLeft;
            });
        }
    }
}
