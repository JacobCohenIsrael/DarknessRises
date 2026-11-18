using System;
using DarknessRises.Game.Continues;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Continues
{
    public sealed class ContinuePushBackTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly Vector3 Centre = new(2f, 0f, 3f);

        private readonly ContinuePushBack pushBack = new(radius: 5f, force: 10f);

        [Test]
        public void An_Enemy_inside_the_radius_is_pushed_straight_away_out_to_the_radius()
        {
            var enemy = Centre + new Vector3(0f, 0f, 2f);

            var push = pushBack.For(Centre, enemy, Vector3.right);

            Assert.That(push.Moves, Is.True);
            Assert.That(Vector3.Distance(push.Velocity.normalized, Vector3.forward), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(enemy + push.Displacement, Centre + new Vector3(0f, 0f, 5f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void It_slides_at_the_push_back_force()
        {
            var push = pushBack.For(Centre, Centre + new Vector3(-1f, 0f, 0f), Vector3.forward);

            Assert.That(push.Velocity.magnitude, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(push.Seconds, Is.EqualTo(0.4f).Within(Tolerance), "4 metres to go at 10 metres a second.");
        }

        [Test]
        public void Closer_Enemies_are_pushed_further()
        {
            var near = pushBack.For(Centre, Centre + new Vector3(1f, 0f, 0f), Vector3.forward);
            var far = pushBack.For(Centre, Centre + new Vector3(4f, 0f, 0f), Vector3.forward);

            Assert.That(near.Displacement.magnitude, Is.GreaterThan(far.Displacement.magnitude));
        }

        [TestCase(5f)]
        [TestCase(8f)]
        public void Enemies_at_or_beyond_the_radius_arent_touched(float distance)
        {
            var push = pushBack.For(Centre, Centre + new Vector3(distance, 0f, 0f), Vector3.forward);

            Assert.That(push.Moves, Is.False);
            Assert.That(push.Displacement, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Height_is_ignored_and_the_push_stays_on_the_floor()
        {
            var push = pushBack.For(Centre, Centre + new Vector3(3f, 2.5f, 0f), Vector3.forward);

            Assert.That(push.Velocity.y, Is.EqualTo(0f));
            Assert.That(push.Displacement.magnitude, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void An_Enemy_right_on_top_of_the_character_goes_the_fallback_way()
        {
            var push = pushBack.For(Centre, Centre, new Vector3(0f, 1f, -3f));

            Assert.That(Vector3.Distance(push.Velocity.normalized, Vector3.back), Is.LessThan(Tolerance));
            Assert.That(push.Displacement.magnitude, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void Needs_a_radius_and_a_force()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ContinuePushBack(0f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new ContinuePushBack(5f, 0f));
        }
    }
}
