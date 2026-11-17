using System;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Skills;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Skills
{
    public sealed class SlowMoTests
    {
        private const float Duration = 4f;
        private const float Cooldown = 15f;
        private const float SlowScale = 0.25f;
        private const float Ease = 0.5f;
        private const float Tolerance = 1e-4f;

        private EnemyTime enemyTime;
        private SlowMo slowMo;

        [SetUp]
        public void SetUp()
        {
            enemyTime = new EnemyTime();
            slowMo = new SlowMo(new SkillTiming(Duration, Cooldown), SlowScale, Ease, enemyTime);
        }

        [Test]
        public void LeavesEnemyTimeAloneUntilActivated()
        {
            slowMo.Tick(1f);

            Assert.That(enemyTime.Scale, Is.EqualTo(EnemyTime.NormalScale));
        }

        [Test]
        public void EasesEnemyTimeDownToItsStrength()
        {
            slowMo.Skill.TryActivate();

            slowMo.Tick(Ease / 2f);
            Assert.That(enemyTime.Scale, Is.EqualTo((1f + SlowScale) / 2f).Within(Tolerance));

            slowMo.Tick(Ease / 2f);
            Assert.That(enemyTime.Scale, Is.EqualTo(SlowScale).Within(Tolerance));

            slowMo.Tick(1f);
            Assert.That(enemyTime.Scale, Is.EqualTo(SlowScale).Within(Tolerance));
        }

        [Test]
        public void EasesEnemyTimeBackToNormalWhenItEnds()
        {
            const float lastFrame = 0.01f;
            slowMo.Skill.TryActivate();
            slowMo.Tick(Duration - lastFrame);
            Assert.That(enemyTime.Scale, Is.EqualTo(SlowScale).Within(Tolerance));

            slowMo.Tick(lastFrame);
            Assert.That(slowMo.Skill.IsActive, Is.False);
            Assert.That(enemyTime.Scale, Is.GreaterThan(SlowScale).And.LessThan(EnemyTime.NormalScale));

            slowMo.Tick(Ease);

            Assert.That(enemyTime.Scale, Is.EqualTo(EnemyTime.NormalScale).Within(Tolerance));
        }

        [Test]
        public void WithoutEasingEnemyTimeSnaps()
        {
            var snapping = new SlowMo(new SkillTiming(Duration, Cooldown), SlowScale, 0f, enemyTime);

            snapping.Skill.TryActivate();
            snapping.Tick(0.01f);
            Assert.That(enemyTime.Scale, Is.EqualTo(SlowScale));

            snapping.Tick(Duration);
            Assert.That(enemyTime.Scale, Is.EqualTo(EnemyTime.NormalScale));
        }

        [Test]
        public void CooldownsDontSlowUnderSlowMo()
        {
            var flare = new Flare(new SkillTiming(1f, Cooldown), 0f, 0f);
            flare.Skill.TryActivate();
            flare.Tick(1f);
            slowMo.Skill.TryActivate();

            // A second of real time, a frame at a time, with Enemies slowed throughout.
            const int frames = 60;
            for (var i = 0; i < frames; i++)
            {
                flare.Tick(1f / frames);
                slowMo.Tick(1f / frames);
            }

            Assert.That(enemyTime.Scale, Is.LessThan(EnemyTime.NormalScale));
            Assert.That(flare.Skill.CooldownSecondsLeft, Is.EqualTo(Cooldown - 1f).Within(Tolerance));
            Assert.That(slowMo.Skill.ActiveSecondsLeft, Is.EqualTo(Duration - 1f).Within(Tolerance));

            // Slow-mo's own cooldown runs at full speed too, even while Enemies are still easing back.
            slowMo.Tick(Duration - 1f + 2f);
            Assert.That(slowMo.Skill.CooldownSecondsLeft, Is.EqualTo(Cooldown - 2f).Within(Tolerance));
        }

        [Test]
        public void SlowMoNeverStopsEnemyTimeBeyondItsStrength()
        {
            slowMo.Skill.TryActivate();

            slowMo.Tick(100f);

            Assert.That(enemyTime.Scale, Is.GreaterThanOrEqualTo(SlowScale));
        }

        [Test]
        public void RejectsAStrengthOutsideZeroToOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new SlowMo(new SkillTiming(Duration, Cooldown), 1.5f, Ease, enemyTime));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new SlowMo(new SkillTiming(Duration, Cooldown), -0.1f, Ease, enemyTime));
        }
    }
}
