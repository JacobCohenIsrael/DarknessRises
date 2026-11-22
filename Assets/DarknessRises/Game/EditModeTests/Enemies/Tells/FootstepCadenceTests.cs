using System;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Enemies.Tells;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies.Tells
{
    public sealed class FootstepCadenceTests
    {
        private const float Frame = 1f / 64f;

        // Walks for <paramref name="seconds"/> at <paramref name="speed"/> and counts the steps.
        private static int Walk(FootstepCadence cadence, float seconds, float speed)
        {
            var steps = 0;
            for (var t = 0f; t < seconds; t += Frame)
            {
                if (cadence.Tick(Frame, speed))
                {
                    steps++;
                }
            }

            return steps;
        }

        [Test]
        public void Steps_come_once_per_stride_of_ground()
        {
            // 1 m/s, 0.5 m strides: 2 steps a second, after the first step half a stride in.
            var cadence = new FootstepCadence(0.5f, 0f, 0.5f, 0.1f);

            Assert.That(Walk(cadence, 4f, 1f), Is.InRange(7, 9));
        }

        [Test]
        public void A_faster_Enemy_steps_faster()
        {
            var slow = Walk(new FootstepCadence(0.75f, 0f, 0.5f, 0.1f), 4f, 1.5f);
            var fast = Walk(new FootstepCadence(0.75f, 0f, 0.5f, 0.1f), 4f, 4.5f);

            Assert.That(fast, Is.GreaterThan(slow * 2));
        }

        [Test]
        public void Cadence_follows_speed_over_stride()
        {
            var cadence = new FootstepCadence(0.75f, 0.4f, 0.3f, 0.3f);

            Assert.That(cadence.StepsPerSecond(1.5f), Is.EqualTo(2f).Within(1e-5f));
            Assert.That(cadence.StepsPerSecond(0.2f), Is.EqualTo(0f));
        }

        [Test]
        public void A_Shambler_and_a_Runner_sound_different_at_their_own_speeds()
        {
            // Authored defaults: a Shambler's 0.75 m step-drag at 1.4 m/s against a Runner's even 1 m stride at 4.2 m/s.
            var shambler = Walk(new FootstepCadence(0.75f, 0.4f, 0.3f, 0.3f), 4f, 1.4f);
            var runner = Walk(new FootstepCadence(1f, 0f, 0.3f, 0.3f), 4f, 4.2f);

            Assert.That(shambler, Is.InRange(6, 9));
            Assert.That(runner, Is.InRange(15, 18));
        }

        [Test]
        public void A_limp_makes_steps_come_in_uneven_pairs()
        {
            var cadence = new FootstepCadence(1f, 0.5f, 0f, 0.1f);
            var gaps = new float[4];
            var since = 0f;
            var found = -1;
            for (var t = 0f; t < 10f && found < gaps.Length - 1; t += Frame)
            {
                since += Frame;
                if (!cadence.Tick(Frame, 1f))
                {
                    continue;
                }

                if (found >= 0)
                {
                    gaps[found] = since;
                }

                found++;
                since = 0f;
            }

            // Short, long, short, long: 0.5 m then 1.5 m at 1 m/s.
            Assert.That(gaps[0], Is.EqualTo(0.5f).Within(Frame * 1.5f));
            Assert.That(gaps[1], Is.EqualTo(1.5f).Within(Frame * 1.5f));
            Assert.That(gaps[2], Is.EqualTo(0.5f).Within(Frame * 1.5f));
        }

        [Test]
        public void With_a_limp_every_other_step_is_the_dragged_foot()
        {
            var cadence = new FootstepCadence(1f, 0.5f, 0f, 0.1f);
            var dragged = new System.Collections.Generic.List<bool>();
            for (var t = 0f; t < 10f && dragged.Count < 4; t += Frame)
            {
                if (cadence.Tick(Frame, 1f))
                {
                    dragged.Add(cadence.LastStepDragged);
                }
            }

            // The good foot, then the dragged one that comes after the short stride, and so on.
            Assert.That(dragged, Is.EqualTo(new[] { false, true, false, true }));
        }

        [Test]
        public void An_even_gait_never_drags_a_foot()
        {
            var cadence = new FootstepCadence(0.5f, 0f, 0f, 0.1f);
            for (var t = 0f; t < 5f; t += Frame)
            {
                if (cadence.Tick(Frame, 1f))
                {
                    Assert.That(cadence.LastStepDragged, Is.False);
                }
            }
        }

        [Test]
        public void Steps_stop_when_the_Enemy_stands_still()
        {
            var cadence = new FootstepCadence(0.5f, 0f, 0f, 0.3f);
            Walk(cadence, 1f, 2f);

            Assert.That(Walk(cadence, 2f, 0f), Is.EqualTo(0));
            Assert.That(cadence.IsWalking, Is.False);
        }

        [Test]
        public void A_slow_shuffle_below_the_stop_speed_makes_no_steps()
        {
            Assert.That(Walk(new FootstepCadence(0.5f, 0f, 0f, 0.3f), 5f, 0.2f), Is.EqualTo(0));
        }

        [Test]
        public void Steps_resume_soon_after_the_Enemy_walks_again()
        {
            var cadence = new FootstepCadence(1f, 0f, 0.25f, 0.1f);
            Walk(cadence, 3f, 1f);
            Walk(cadence, 1f, 0f);

            var secondsToFirstStep = 0f;
            while (!cadence.Tick(Frame, 1f))
            {
                secondsToFirstStep += Frame;
            }

            Assert.That(secondsToFirstStep, Is.EqualTo(0.25f).Within(Frame * 1.5f));
        }

        [Test]
        public void Stop_starts_a_respawned_Enemy_over()
        {
            var cadence = new FootstepCadence(1f, 0f, 0.5f, 0.1f);
            Walk(cadence, 2.2f, 1f);

            cadence.Stop();

            Assert.That(cadence.IsWalking, Is.False);
            Assert.That(cadence.Tick(0.25f, 1f), Is.False);
            Assert.That(cadence.Tick(0.25f, 1f), Is.True);
        }

        [Test]
        public void A_paused_game_makes_no_steps()
        {
            var cadence = new FootstepCadence(0.5f, 0f, 0f, 0.1f);
            Walk(cadence, 0.4f, 1f);

            for (var i = 0; i < 100; i++)
            {
                Assert.That(cadence.Tick(0f, 1f), Is.False);
            }

            Assert.That(cadence.IsWalking, Is.True);
        }

        [Test]
        public void Adrenaline_slows_the_steps_with_Enemy_time()
        {
            var time = new EnemyTime { Scale = 0.5f };
            var normal = new FootstepCadence(0.5f, 0f, 0.5f, 0.1f);
            var slowed = new FootstepCadence(0.5f, 0f, 0.5f, 0.1f);
            var normalSteps = 0;
            var slowedSteps = 0;
            for (var t = 0f; t < 4f; t += Frame)
            {
                normalSteps += normal.Tick(Frame, 1f) ? 1 : 0;
                slowedSteps += slowed.Tick(time.Scaled(Frame), 1f) ? 1 : 0;
            }

            Assert.That(slowedSteps, Is.EqualTo(normalSteps / 2).Within(1));
        }

        [Test]
        public void A_long_frame_makes_one_step_not_a_burst()
        {
            var cadence = new FootstepCadence(0.5f, 0f, 0f, 0.1f);
            cadence.Tick(Frame, 1f);

            Assert.That(cadence.Tick(5f, 1f), Is.True);
            Assert.That(cadence.Tick(Frame, 1f), Is.False);
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepCadence(0f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepCadence(1f, 1f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepCadence(1f, -0.1f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepCadence(1f, 0f, -1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepCadence(1f, 0f, 0f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FootstepCadence(1f, 0f, 0f, 0f).Tick(-1f, 1f));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var cadence = new FootstepCadence(0.5f, 0.3f, 0.2f, 0.1f);

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    cadence.Tick(Frame, 1.4f);
                }
            });
        }
    }
}
