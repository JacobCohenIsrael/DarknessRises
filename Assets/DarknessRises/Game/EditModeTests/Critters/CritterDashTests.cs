using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Critters
{
    public sealed class CritterDashTests
    {
        private const float Tolerance = 1e-3f;

        // Up to 60 degrees off ahead, passing 4.5 to 7 m away, 5 to 8 m long, 4.5 to 6.5 m/s.
        private static readonly CritterDashShape Shape = new(60f, 4.5f, 7f, 5f, 8f, 4.5f, 6.5f);

        private static float DistanceToLine(Vector3 point, Vector3 a, Vector3 b)
        {
            var along = b - a;
            var t = Mathf.Clamp01(Vector3.Dot(point - a, along) / along.sqrMagnitude);
            return Vector3.Distance(point, a + along * t);
        }

        [Test]
        public void Every_dash_crosses_ahead_of_the_character_at_the_edge_of_its_light()
        {
            var random = new SystemRandom(11);
            var character = new Vector3(3f, 0f, -2f);
            var forward = new Vector3(1f, 0f, 1f).normalized;
            for (var i = 0; i < 500; i++)
            {
                var dash = CritterDash.Plan(character, forward, Shape, random);
                var middle = (dash.From + dash.To) * 0.5f;
                var toMiddle = middle - character;

                // The middle is ahead (within the angle) and between the pass distances.
                Assert.That(Vector3.Angle(forward, toMiddle), Is.LessThanOrEqualTo(60f + Tolerance));
                Assert.That(toMiddle.magnitude, Is.InRange(4.5f - Tolerance, 7f + Tolerance));

                // It runs across the line of sight, never at the character: it passes no nearer than its middle.
                Assert.That(Vector3.Dot((dash.To - dash.From).normalized, toMiddle.normalized), Is.EqualTo(0f).Within(Tolerance));
                Assert.That(DistanceToLine(character, dash.From, dash.To), Is.EqualTo(toMiddle.magnitude).Within(Tolerance));

                Assert.That(dash.Length, Is.InRange(5f - Tolerance, 8f + Tolerance));
                Assert.That(dash.Speed, Is.InRange(4.5f, 6.5f));
                Assert.That(dash.From.y, Is.EqualTo(character.y));
                Assert.That(dash.To.y, Is.EqualTo(character.y));
            }
        }

        [Test]
        public void A_dash_is_over_in_a_second_or_two()
        {
            var random = new SystemRandom(5);
            for (var i = 0; i < 200; i++)
            {
                var dash = CritterDash.Plan(Vector3.zero, Vector3.forward, Shape, random);

                Assert.That(dash.Seconds, Is.InRange(5f / 6.5f - Tolerance, 8f / 4.5f + Tolerance));
            }
        }

        [Test]
        public void Dashes_go_both_ways()
        {
            var random = new SystemRandom(2);
            var leftward = 0;
            for (var i = 0; i < 200; i++)
            {
                var dash = CritterDash.Plan(Vector3.zero, Vector3.forward, new CritterDashShape(0f, 5f, 5f, 4f, 4f, 5f, 5f), random);
                leftward += dash.To.x < dash.From.x ? 1 : 0;
            }

            Assert.That(leftward, Is.InRange(60, 140));
        }

        [Test]
        public void The_Critter_moves_from_one_end_to_the_other_at_its_speed()
        {
            var dash = new CritterDash(new Vector3(-2f, 0f, 5f), new Vector3(2f, 0f, 5f), 4f);

            Assert.That(dash.Seconds, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(dash.At(0f), Is.EqualTo(dash.From));
            Assert.That(Vector3.Distance(dash.At(0.5f), new Vector3(0f, 0f, 5f)), Is.LessThan(Tolerance));
            Assert.That(dash.At(3f), Is.EqualTo(dash.To));
            Assert.That(dash.IsOver(0.99f), Is.False);
            Assert.That(dash.IsOver(1f), Is.True);
        }

        [Test]
        public void A_character_facing_nowhere_looks_ahead_along_the_world()
        {
            var dash = CritterDash.Plan(Vector3.zero, Vector3.zero, new CritterDashShape(0f, 5f, 5f, 4f, 4f, 5f, 5f), new SystemRandom(1));

            Assert.That(((dash.From + dash.To) * 0.5f).z, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Bad_shapes_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterDashShape(200f, 1f, 2f, 1f, 2f, 1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterDashShape(60f, 3f, 2f, 1f, 2f, 1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterDashShape(60f, 1f, 2f, 3f, 2f, 1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterDashShape(60f, 1f, 2f, 1f, 2f, 0f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CritterDash(Vector3.zero, Vector3.one, 0f));
        }
    }
}
