using System;
using System.Collections.Generic;
using DarknessRises.Game.Skills;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Skills
{
    public sealed class SkillTests
    {
        private const float Duration = 4f;
        private const float Cooldown = 15f;
        private const float Tolerance = 1e-4f;

        private Skill skill;
        private List<string> events;

        [SetUp]
        public void SetUp()
        {
            skill = new Skill(new SkillTiming(Duration, Cooldown));
            events = new List<string>();
            skill.Activated += _ => events.Add("activated");
            skill.Ended += _ => events.Add("ended");
            skill.Readied += _ => events.Add("readied");
        }

        [Test]
        public void StartsReady()
        {
            Assert.That(skill.Phase, NUnit.Framework.Is.EqualTo(SkillPhase.Ready));
            Assert.That(skill.CooldownFraction, NUnit.Framework.Is.Zero);
        }

        [Test]
        public void ActivatingStartsTheEffectForItsDuration()
        {
            Assert.That(skill.TryActivate(), NUnit.Framework.Is.True);

            Assert.That(skill.Phase, NUnit.Framework.Is.EqualTo(SkillPhase.Active));
            Assert.That(skill.ActiveSecondsLeft, NUnit.Framework.Is.EqualTo(Duration));
            Assert.That(skill.ActiveFraction, NUnit.Framework.Is.EqualTo(1f));
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "activated" }));
        }

        [Test]
        public void CantBeActivatedWhileActive()
        {
            skill.TryActivate();
            skill.Tick(1f);

            Assert.That(skill.TryActivate(), NUnit.Framework.Is.False);
            Assert.That(skill.ActiveSecondsLeft, NUnit.Framework.Is.EqualTo(Duration - 1f).Within(Tolerance));
        }

        [Test]
        public void GoesOnCooldownWhenTheEffectEnds()
        {
            skill.TryActivate();
            skill.Tick(Duration - 0.5f);
            Assert.That(skill.IsActive, NUnit.Framework.Is.True);

            skill.Tick(0.5f);

            Assert.That(skill.Phase, NUnit.Framework.Is.EqualTo(SkillPhase.CoolingDown));
            Assert.That(skill.CooldownSecondsLeft, NUnit.Framework.Is.EqualTo(Cooldown).Within(Tolerance));
            Assert.That(skill.CooldownFraction, NUnit.Framework.Is.EqualTo(1f).Within(Tolerance));
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "activated", "ended" }));
        }

        [Test]
        public void CantBeActivatedWhileCoolingDown()
        {
            skill.TryActivate();
            skill.Tick(Duration + 1f);

            Assert.That(skill.TryActivate(), NUnit.Framework.Is.False);
            Assert.That(skill.Phase, NUnit.Framework.Is.EqualTo(SkillPhase.CoolingDown));
        }

        [Test]
        public void TimePastTheEndOfTheEffectCountsTowardTheCooldown()
        {
            skill.TryActivate();

            skill.Tick(Duration + 2f);

            Assert.That(skill.CooldownSecondsLeft, NUnit.Framework.Is.EqualTo(Cooldown - 2f).Within(Tolerance));
            Assert.That(skill.CooldownFraction, NUnit.Framework.Is.EqualTo((Cooldown - 2f) / Cooldown).Within(Tolerance));
        }

        [Test]
        public void IsReadyAgainOnceTheCooldownRunsOut()
        {
            skill.TryActivate();
            skill.Tick(Duration);
            skill.Tick(Cooldown - 0.5f);
            Assert.That(skill.IsReady, NUnit.Framework.Is.False);

            skill.Tick(0.5f);

            Assert.That(skill.IsReady, NUnit.Framework.Is.True);
            Assert.That(skill.CooldownSecondsLeft, NUnit.Framework.Is.Zero);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "activated", "ended", "readied" }));
            Assert.That(skill.TryActivate(), NUnit.Framework.Is.True);
        }

        [Test]
        public void OneLongTickRunsTheWholeCycle()
        {
            skill.TryActivate();

            skill.Tick(Duration + Cooldown + 1f);

            Assert.That(skill.IsReady, NUnit.Framework.Is.True);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "activated", "ended", "readied" }));
        }

        [Test]
        public void TickingWhileReadyDoesNothing()
        {
            skill.Tick(100f);

            Assert.That(skill.IsReady, NUnit.Framework.Is.True);
            Assert.That(events, NUnit.Framework.Is.Empty);
        }

        [Test]
        public void ANegativeTickIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => skill.Tick(-0.1f));
        }

        [Test]
        public void TimingRejectsANonPositiveDurationAndANegativeCooldown()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new SkillTiming(0f, Cooldown));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new SkillTiming(Duration, -1f));
        }

        [Test]
        public void TickingDoesNotAllocate()
        {
            var quiet = new Skill(new SkillTiming(Duration, Cooldown));
            quiet.TryActivate();

            AllocationAssert.DoesNotAllocate(() => quiet.Tick(0.016f));
        }

        [Test]
        public void EndingCutsTheEffectShortAndStartsTheCooldown()
        {
            skill.TryActivate();
            skill.Tick(1f);

            Assert.That(skill.End(), NUnit.Framework.Is.True);

            Assert.That(skill.Phase, NUnit.Framework.Is.EqualTo(SkillPhase.CoolingDown));
            Assert.That(skill.ActiveSecondsLeft, NUnit.Framework.Is.Zero);
            Assert.That(skill.CooldownSecondsLeft, NUnit.Framework.Is.EqualTo(Cooldown));
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "activated", "ended" }));
        }

        [Test]
        public void EndingASkillThatIsntActiveDoesNothing()
        {
            Assert.That(skill.End(), NUnit.Framework.Is.False);
            skill.TryActivate();
            skill.End();

            Assert.That(skill.End(), NUnit.Framework.Is.False);
            Assert.That(events, NUnit.Framework.Is.EqualTo(new[] { "activated", "ended" }));
        }
    }
}
