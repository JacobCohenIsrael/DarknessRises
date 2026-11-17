using DarknessRises.Game.Skills;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Skills
{
    public sealed class FlareTests
    {
        private const float Duration = 4f;
        private const float Cooldown = 15f;
        private const float FadeIn = 0.2f;
        private const float FadeOut = 1f;
        private const float Tolerance = 1e-4f;

        private Flare flare;

        [SetUp]
        public void SetUp()
        {
            flare = new Flare(new SkillTiming(Duration, Cooldown), FadeIn, FadeOut);
        }

        [Test]
        public void IsDarkUntilActivated()
        {
            Assert.That(flare.Brightness, Is.Zero);
        }

        [Test]
        public void FadesInToFullStrength()
        {
            flare.Skill.TryActivate();
            Assert.That(flare.Brightness, Is.Zero);

            flare.Tick(FadeIn / 2f);
            Assert.That(flare.Brightness, Is.EqualTo(0.5f).Within(Tolerance));

            flare.Tick(FadeIn / 2f);
            Assert.That(flare.Brightness, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void FadesOutOverTheEndOfItsDuration()
        {
            flare.Skill.TryActivate();

            flare.Tick(Duration - FadeOut / 2f);

            Assert.That(flare.Brightness, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void IsDarkAgainOnCooldown()
        {
            flare.Skill.TryActivate();

            flare.Tick(Duration);

            Assert.That(flare.Skill.Phase, Is.EqualTo(SkillPhase.CoolingDown));
            Assert.That(flare.Brightness, Is.Zero);
        }

        [Test]
        public void WithoutFadesIsAtFullStrengthThroughout()
        {
            var sharp = new Flare(new SkillTiming(Duration, Cooldown), 0f, 0f);
            sharp.Skill.TryActivate();

            Assert.That(sharp.Brightness, Is.EqualTo(1f));
            sharp.Tick(Duration - 0.01f);
            Assert.That(sharp.Brightness, Is.EqualTo(1f));
        }
    }
}
