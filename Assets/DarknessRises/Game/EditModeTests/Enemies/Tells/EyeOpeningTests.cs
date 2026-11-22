using System;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Enemies.Tells;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies.Tells
{
    public sealed class EyeOpeningTests
    {
        // 1 second: shut for 0.25, crack open to 0.5 by 0.5, blink until 0.75, open fully by 1.
        private static EyeOpening Opening()
        {
            return new EyeOpening(1f, 0.25f, 0.25f, 0.5f, 0.25f);
        }

        [Test]
        public void Eyes_are_shut_when_the_Enemy_spawns()
        {
            var opening = Opening();
            opening.Restart();

            Assert.That(opening.Strength, Is.EqualTo(0f));
            Assert.That(opening.IsOpen, Is.False);
        }

        [Test]
        public void Eyes_stay_shut_for_the_shut_share()
        {
            Assert.That(Opening().At(0.2f), Is.EqualTo(0f));
        }

        [Test]
        public void Eyes_crack_open_slowly_at_first()
        {
            var opening = Opening();

            // A quarter of the way through the peek, a linear fade would be at a quarter of the peek.
            Assert.That(opening.At(0.3125f), Is.LessThan(0.5f * 0.25f));
            Assert.That(opening.At(0.5f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Eyes_blink_shut_and_open_again_before_opening_fully()
        {
            var opening = Opening();

            Assert.That(opening.At(0.625f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(opening.At(0.75f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Eyes_open_fully_by_the_end()
        {
            var opening = Opening();

            Assert.That(opening.At(0.875f), Is.GreaterThan(0.5f).And.LessThan(1f));
            Assert.That(opening.At(1f), Is.EqualTo(1f));
            Assert.That(opening.At(5f), Is.EqualTo(1f));
        }

        [Test]
        public void Strength_follows_time_ticked_in()
        {
            var opening = Opening();
            opening.Restart();

            opening.Tick(0.5f);
            Assert.That(opening.Strength, Is.EqualTo(0.5f).Within(1e-5f));

            opening.Tick(0.5f);
            Assert.That(opening.Strength, Is.EqualTo(1f));
            Assert.That(opening.IsOpen, Is.True);
        }

        [Test]
        public void Strength_never_decreases_except_in_the_blink()
        {
            var opening = Opening();
            var previous = 0f;
            for (var t = 0f; t <= 1f; t += 1f / 64f)
            {
                var strength = opening.At(t);
                Assert.That(strength, Is.InRange(0f, 1f));
                var inBlink = t > 0.5f && t < 0.75f;
                if (!inBlink)
                {
                    Assert.That(strength, Is.GreaterThanOrEqualTo(previous - 1e-5f), $"at {t}");
                }

                previous = strength;
            }
        }

        [Test]
        public void A_paused_game_leaves_the_eyes_as_they_are()
        {
            var opening = Opening();
            opening.Restart();
            opening.Tick(0.5f);

            opening.Tick(0f);

            Assert.That(opening.Strength, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Adrenaline_slows_the_opening_with_Enemy_time()
        {
            var time = new EnemyTime { Scale = 0.5f };
            var opening = Opening();
            opening.Restart();

            opening.Tick(time.Scaled(1f));

            Assert.That(opening.IsOpen, Is.False);
            Assert.That(opening.Strength, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void A_reused_Enemy_opens_its_eyes_again_from_shut()
        {
            var opening = Opening();
            opening.Restart();
            opening.Tick(2f);

            opening.Restart();

            Assert.That(opening.Strength, Is.EqualTo(0f));
            Assert.That(opening.IsOpen, Is.False);
        }

        [Test]
        public void No_opening_time_means_open_at_once()
        {
            var opening = new EyeOpening(0f, 0.25f, 0.25f, 0.5f, 0.25f);
            opening.Restart();

            Assert.That(opening.Strength, Is.EqualTo(1f));
            Assert.That(opening.IsOpen, Is.True);
        }

        [Test]
        public void Without_a_peek_or_blink_the_eyes_open_in_one_slow_sweep()
        {
            var opening = new EyeOpening(1f, 0f, 0f, 0f, 0f);

            Assert.That(opening.At(0f), Is.EqualTo(0f));
            Assert.That(opening.At(0.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(opening.At(0.25f), Is.LessThan(0.25f));
        }

        [Test]
        public void Shares_adding_up_to_more_than_the_whole_are_refused()
        {
            Assert.Throws<ArgumentException>(() => new EyeOpening(1f, 0.5f, 0.5f, 0.5f, 0.25f));
        }

        [Test]
        public void Negative_time_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EyeOpening(-1f, 0f, 0f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Opening().Tick(-0.1f));
        }

        [Test]
        public void The_opening_multiplies_the_distance_glint()
        {
            // Close by, the glint is full, so the eyes show only as far as they've opened.
            Assert.That(EyeGlint.Strength(1f, 7f, 1.5f, 0f), Is.EqualTo(0f));
            Assert.That(EyeGlint.Strength(1f, 7f, 1.5f, 0.5f), Is.EqualTo(0.5f));
            Assert.That(EyeGlint.Strength(1f, 7f, 1.5f, 1f), Is.EqualTo(1f));

            // The distance rule is unchanged: out of range, open eyes still don't glint; in the fade, they fade.
            Assert.That(EyeGlint.Strength(8f, 7f, 1.5f, 1f), Is.EqualTo(0f));
            Assert.That(EyeGlint.Strength(6.25f, 7f, 1.5f, 0.5f), Is.EqualTo(0.25f).Within(1e-5f));
        }

        [Test]
        public void An_openness_outside_0_to_1_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EyeGlint.Strength(1f, 7f, 1.5f, 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => EyeGlint.Strength(1f, 7f, 1.5f, -0.5f));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var opening = Opening();
            opening.Restart();

            AllocationAssert.DoesNotAllocate(() =>
            {
                opening.Tick(1f / 64f);
                _ = opening.Strength;
            });
        }
    }
}
