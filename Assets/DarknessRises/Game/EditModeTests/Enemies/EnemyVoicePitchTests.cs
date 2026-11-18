using System;
using DarknessRises.Game.Enemies;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies
{
    public sealed class EnemyVoicePitchTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void AtNormalEnemyTime_PlaysAtItsOwnPitch()
        {
            Assert.That(EnemyVoicePitch.For(1f, EnemyTime.NormalScale), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(EnemyVoicePitch.For(1.2f, EnemyTime.NormalScale), Is.EqualTo(1.2f).Within(Tolerance));
        }

        [Test]
        public void SlowedEnemyTime_SlowsThePitchByTheScale()
        {
            Assert.That(EnemyVoicePitch.For(1f, 0.25f), Is.EqualTo(0.25f).Within(Tolerance));
        }

        [Test]
        public void ARandomTellPitch_StillVariesOnTopOfTheScale()
        {
            var low = EnemyVoicePitch.For(0.8f, 0.5f);
            var high = EnemyVoicePitch.For(1.2f, 0.5f);

            Assert.That(low, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(high, Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void FrozenEnemyTime_SilencesThePitch()
        {
            Assert.That(EnemyVoicePitch.For(1f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void NegativeValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyVoicePitch.For(-1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyVoicePitch.For(1f, -0.5f));
        }
    }
}
