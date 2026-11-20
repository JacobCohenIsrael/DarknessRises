using System;
using System.Collections.Generic;
using DarknessRises.Game.Lightning;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class LightningLoomingTests
    {
        private const float RoomYaw = 180f;
        private const float Tolerance = 0.01f;

        // Always tries to loom; qualifies Enemies 2.5-11 m away, within 80 degrees of the facing, from any side; comes in
        // 7-16 degrees up, with the top of the shadow 4 m past the character.
        private static readonly LightningLooming Always = new(1f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f);
        private static readonly LightningAngles Ordinary = new(18f, 46f, 28f);

        // The character stands at the origin facing north (+z).
        private static readonly Vector2 Character = Vector2.zero;
        private static readonly Vector2 North = Vector2.up;

        private static LoomCandidate Unseen(float x, float z, float height = 2f) => new(new Vector2(x, z), height, false);

        private static LoomCandidate Seen(float x, float z, float height = 2f) => new(new Vector2(x, z), height, true);

        [Test]
        public void ALoomingStrikeComesFromBehindTheNearestUnseenEnemyTowardTheCharacter()
        {
            var candidates = new[] { Seen(0f, 3f), Unseen(1f, 8f), Unseen(0f, 5f) };

            var strike = Always.Pick(new SystemRandom(1), Ordinary, RoomYaw, candidates, Character, North);

            Assert.That(strike.IsLooming, Is.True);
            Assert.That(strike.LoomingTarget, Is.EqualTo(2));
            // From the Enemy 5 m north, the light travels south, toward the character.
            Assert.That(Mathf.DeltaAngle(strike.Angle.YawDegrees, 180f), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void TheShadowOfTheEnemysTopReachesPastTheCharacter()
        {
            var enemy = Unseen(3f, 4f, 2f);

            var angle = Always.AngleFrom(enemy, Character);

            // 5 m to the character and 4 m past it: the shadow of a 2 m Enemy is 9 m long.
            var shadowMetres = enemy.HeightMetres / Mathf.Tan(angle.ElevationDegrees * Mathf.Deg2Rad);
            Assert.That(shadowMetres, Is.EqualTo(9f).Within(Tolerance));
            var travel = new Vector2(Mathf.Sin(angle.YawDegrees * Mathf.Deg2Rad), Mathf.Cos(angle.YawDegrees * Mathf.Deg2Rad));
            Assert.That(Vector2.Dot(travel, (Character - enemy.Floor).normalized), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void TheElevationStaysWithinTheLowRange()
        {
            Assert.That(Always.AngleFrom(Unseen(0f, 3f, 9f), Character).ElevationDegrees, Is.EqualTo(16f));
            Assert.That(Always.AngleFrom(Unseen(0f, 10f, 0.5f), Character).ElevationDegrees, Is.EqualTo(7f));
        }

        [TestCase(0f, 2f, TestName = "TooNear")]
        [TestCase(0f, 12f, TestName = "TooFar")]
        [TestCase(0f, -5f, TestName = "Behind")]
        [TestCase(-6f, 0.5f, TestName = "BesideBeyondTheAheadAngle")]
        public void AnEnemyOutOfReachOrOutOfFrontDoesNotQualify(float x, float z)
        {
            Assert.That(Always.Qualifies(Unseen(x, z), Character, North, RoomYaw), Is.False);
        }

        [Test]
        public void ASeenEnemyAZeroHeightOrOneOnTheCharacterDoesNotQualify()
        {
            var nearAll = new LightningLooming(1f, 7f, 16f, 0f, 11f, 180f, 180f, 4f);

            Assert.That(nearAll.Qualifies(Seen(0f, 5f), Character, North, RoomYaw), Is.False);
            Assert.That(nearAll.Qualifies(Unseen(0f, 5f, 0f), Character, North, RoomYaw), Is.False);
            Assert.That(nearAll.Qualifies(Unseen(0f, 0f), Character, North, RoomYaw), Is.False);
        }

        [Test]
        public void AWideAheadAngleOrNoFacingLetsAnEnemyBehindQualify()
        {
            var allAround = new LightningLooming(1f, 7f, 16f, 2.5f, 11f, 180f, 180f, 4f);

            Assert.That(allAround.Qualifies(Unseen(0f, -5f), Character, North, RoomYaw), Is.True);
            Assert.That(Always.Qualifies(Unseen(0f, -5f), Character, Vector2.zero, RoomYaw), Is.True);
        }

        [Test]
        public void TheStrikeMayNotTurnFurtherFromTheRoomsDirectionThanAuthored()
        {
            // Light from the windows travels south (180). Within 45 degrees of it, an Enemy due north qualifies (the
            // light travels south to the character); one north-east sends it south-west (225), at the edge; one east
            // sends it west (270), too far.
            var nearTheWindows = new LightningLooming(1f, 7f, 16f, 2.5f, 11f, 180f, 45f, 4f);

            Assert.That(nearTheWindows.Qualifies(Unseen(0f, 5f), Character, North, RoomYaw), Is.True);
            Assert.That(nearTheWindows.Qualifies(Unseen(3.9f, 4f), Character, North, RoomYaw), Is.True);
            Assert.That(nearTheWindows.Qualifies(Unseen(5f, 0.2f), Character, North, RoomYaw), Is.False);
        }

        [Test]
        public void WhenNoEnemyQualifiesTheStrikeIsAnOrdinaryOne()
        {
            var candidates = new[] { Seen(0f, 5f), Unseen(0f, 20f), Unseen(0f, -5f) };
            var random = new SystemRandom(101);

            for (var i = 0; i < 200; i++)
            {
                var strike = Always.Pick(random, Ordinary, RoomYaw, candidates, Character, North);

                Assert.That(strike.IsLooming, Is.False);
                Assert.That(strike.Angle.ElevationDegrees, Is.InRange(18f, 46f));
                Assert.That(strike.Angle.YawDegrees, Is.InRange(152f, 208f));
            }
        }

        [Test]
        public void WithNoEnemiesAtAllTheStrikeIsAnOrdinaryOne()
        {
            var strike = Always.Pick(new SystemRandom(3), Ordinary, RoomYaw, ReadOnlySpan<LoomCandidate>.Empty, Character, North);

            Assert.That(strike.IsLooming, Is.False);
        }

        [Test]
        public void TheChanceDecidesWhetherAStrikeTriesToLoom()
        {
            var sometimes = new LightningLooming(0.35f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f);
            var candidates = new[] { Unseen(0f, 5f) };
            var random = new ScriptedRandom();

            random.Next(0.2f);
            Assert.That(sometimes.Pick(random, Ordinary, RoomYaw, candidates, Character, North).IsLooming, Is.True);

            random.Next(0.5f);
            Assert.That(sometimes.Pick(random, Ordinary, RoomYaw, candidates, Character, North).IsLooming, Is.False);
        }

        [Test]
        public void ManySeededStrikesLoomAboutAsOftenAsAuthored_AndNeverWithNoChance()
        {
            var sometimes = new LightningLooming(0.35f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f);
            var never = new LightningLooming(0f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f);
            var candidates = new[] { Unseen(0f, 5f) };
            var random = new SystemRandom(101);
            var looming = 0;

            for (var i = 0; i < 2000; i++)
            {
                looming += sometimes.Pick(random, Ordinary, RoomYaw, candidates, Character, North).IsLooming ? 1 : 0;
                Assert.That(never.Pick(random, Ordinary, RoomYaw, candidates, Character, North).IsLooming, Is.False);
            }

            Assert.That(looming / 2000f, Is.InRange(0.3f, 0.4f));
        }

        [Test]
        public void PickingAStrikeDoesNotAllocate()
        {
            var candidates = new[] { Seen(0f, 3f), Unseen(1f, 8f), Unseen(0f, 5f), Unseen(0f, -4f) };
            var sometimes = new LightningLooming(0.5f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f);
            var angles = new LightningAngles(18f, 46f, 28f, 0.25f, 8f, 14f);
            var random = new SystemRandom(7);

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 100; i++)
                {
                    sometimes.Pick(random, angles, RoomYaw, candidates, Character, North);
                }
            });
        }

        [TestCase(-0.1f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f)]
        [TestCase(1.1f, 7f, 16f, 2.5f, 11f, 80f, 180f, 4f)]
        [TestCase(0.3f, 0f, 16f, 2.5f, 11f, 80f, 180f, 4f)]
        [TestCase(0.3f, 16f, 7f, 2.5f, 11f, 80f, 180f, 4f)]
        [TestCase(0.3f, 7f, 90f, 2.5f, 11f, 80f, 180f, 4f)]
        [TestCase(0.3f, 7f, 16f, 11f, 2.5f, 80f, 180f, 4f)]
        [TestCase(0.3f, 7f, 16f, -1f, 11f, 80f, 180f, 4f)]
        [TestCase(0.3f, 7f, 16f, 2.5f, 11f, 181f, 180f, 4f)]
        [TestCase(0.3f, 7f, 16f, 2.5f, 11f, 80f, -1f, 4f)]
        [TestCase(0.3f, 7f, 16f, 2.5f, 11f, 80f, 180f, -1f)]
        public void RejectsImpossibleSettings(
            float chance, float minElevation, float maxElevation, float minMetres, float maxMetres, float ahead, float turn, float reach)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new LightningLooming(chance, minElevation, maxElevation, minMetres, maxMetres, ahead, turn, reach));
        }

        private sealed class ScriptedRandom : IRandom
        {
            private readonly Queue<float> values = new();

            public void Next(params float[] next)
            {
                foreach (var value in next)
                {
                    values.Enqueue(value);
                }
            }

            public float Range(float min, float max)
            {
                return values.Count > 0 ? values.Dequeue() : min;
            }
        }
    }
}
