using System;
using System.Collections.Generic;
using DarknessRises.Game.Skills;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Skills
{
    public sealed class LureTests
    {
        private const float LureSeconds = 5f;
        private const float Cooldown = 18f;
        private const float Radius = 6f;
        private const float Flight = 0.5f;
        private const float Frame = 1f / 60f;

        private static readonly Vector3 Point = new(10f, 0f, 4f);

        private Lure lure;
        private List<string> events;

        [SetUp]
        public void SetUp()
        {
            lure = new Lure(new SkillTiming(LureSeconds, Cooldown), Radius, Flight);
            events = new List<string>();
            lure.Landed += _ => events.Add("landed");
            lure.Ended += (_, why) => events.Add($"ended {why}");
        }

        [Test]
        public void A_thrown_Lure_flies_then_lands_where_it_was_thrown()
        {
            Assert.That(lure.TryThrow(Point), Is.True);

            Assert.That(lure.IsFlying, Is.True);
            Assert.That(lure.IsOut, Is.False);
            lure.Tick(Flight - Frame);
            Assert.That(events, Is.Empty);

            lure.Tick(Frame);

            Assert.That(lure.IsOut, Is.True);
            Assert.That(lure.Point, Is.EqualTo(Point));
            Assert.That(events, Is.EqualTo(new[] { "landed" }));
        }

        [Test]
        public void It_draws_nobody_in_the_air()
        {
            lure.TryThrow(Point);

            Assert.That(lure.Draws(Point, alreadyLured: false), Is.False);
        }

        [Test]
        public void Once_landed_it_draws_Enemies_within_its_radius_and_no_farther()
        {
            Land();

            Assert.That(lure.Draws(Point + new Vector3(Radius - 0.01f, 0f, 0f), alreadyLured: false), Is.True);
            Assert.That(lure.Draws(Point + new Vector3(0f, 0f, -Radius), alreadyLured: false), Is.True, "On the edge.");
            Assert.That(lure.Draws(Point + new Vector3(Radius + 0.01f, 0f, 0f), alreadyLured: false), Is.False);
        }

        [Test]
        public void Its_radius_is_measured_across_the_floor()
        {
            Land();

            Assert.That(lure.Draws(Point + new Vector3(0f, 50f, Radius - 0.5f), alreadyLured: false), Is.True);
        }

        [Test]
        public void An_Enemy_once_drawn_stays_drawn_while_the_Lure_lasts_even_out_of_its_radius()
        {
            Land();

            Assert.That(lure.Draws(Point + new Vector3(Radius * 3f, 0f, 0f), alreadyLured: true), Is.True);
        }

        [Test]
        public void It_draws_for_its_authored_time_on_the_floor_then_everyone_comes_back()
        {
            Land();
            lure.Tick(LureSeconds - Frame);
            Assert.That(lure.Draws(Point, alreadyLured: true), Is.True);

            lure.Tick(Frame * 2f);

            Assert.That(lure.IsOut, Is.False);
            Assert.That(lure.Draws(Point, alreadyLured: true), Is.False, "Drawn Enemies are let go.");
            Assert.That(lure.Draws(Point, alreadyLured: false), Is.False);
            Assert.That(lure.LastEnd, Is.EqualTo(LureEnd.TimeUp));
            Assert.That(events, Is.EqualTo(new[] { "landed", "ended TimeUp" }));
        }

        [Test]
        public void An_Enemy_reaching_it_breaks_it_and_its_cooldown_starts_at_once()
        {
            Land();
            lure.Tick(1f);

            lure.Break();

            Assert.That(lure.IsOut, Is.False);
            Assert.That(lure.Draws(Point, alreadyLured: true), Is.False);
            Assert.That(lure.LastEnd, Is.EqualTo(LureEnd.Broken));
            Assert.That(lure.Skill.Phase, Is.EqualTo(SkillPhase.CoolingDown));
            Assert.That(lure.Skill.CooldownSecondsLeft, Is.EqualTo(Cooldown));
            Assert.That(events, Is.EqualTo(new[] { "landed", "ended Broken" }));
        }

        [Test]
        public void Breaking_it_in_the_air_or_twice_does_nothing_more()
        {
            lure.TryThrow(Point);
            lure.Break();
            Assert.That(lure.IsFlying, Is.True, "Nothing can reach it in the air.");

            lure.Tick(Flight);
            lure.Break();
            lure.Break();

            Assert.That(events, Is.EqualTo(new[] { "landed", "ended Broken" }));
        }

        [Test]
        public void It_can_be_thrown_again_only_once_its_cooldown_has_run_out()
        {
            Land();
            lure.Break();
            Assert.That(lure.TryThrow(Point), Is.False);

            lure.Tick(Cooldown);

            Assert.That(lure.Skill.IsReady, Is.True);
            Assert.That(lure.TryThrow(Point + Vector3.right), Is.True);
            Assert.That(lure.Point, Is.EqualTo(Point + Vector3.right));
        }

        [Test]
        public void Its_Skill_lasts_the_flight_plus_its_time_on_the_floor()
        {
            Assert.That(lure.Skill.Timing.DurationSeconds, Is.EqualTo(Flight + LureSeconds).Within(1e-5f));
            Assert.That(lure.Skill.Timing.CooldownSeconds, Is.EqualTo(Cooldown));
        }

        [Test]
        public void A_Lure_with_no_flight_lands_as_it_is_thrown()
        {
            var instant = new Lure(new SkillTiming(LureSeconds, Cooldown), Radius, 0f);
            var landed = 0;
            instant.Landed += _ => landed++;

            instant.TryThrow(Point);
            instant.Tick(Frame);

            Assert.That(instant.IsOut, Is.True);
            Assert.That(landed, Is.EqualTo(1));
        }

        [Test]
        public void It_rejects_what_no_Lure_can_be()
        {
            var timing = new SkillTiming(LureSeconds, Cooldown);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Lure(timing, -1f, Flight));
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new Lure(timing, Radius, -0.1f));
        }

        [Test]
        public void Throwing_ticking_and_drawing_do_not_allocate()
        {
            var far = Point + new Vector3(20f, 0f, 0f);
            var quiet = new Lure(new SkillTiming(LureSeconds, Cooldown), Radius, Flight);
            quiet.Landed += OnLanded;
            quiet.Ended += OnEnded;
            AllocationAssert.DoesNotAllocate(() =>
            {
                var lure = quiet;
                for (var throwIndex = 0; throwIndex < 3; throwIndex++)
                {
                    lure.TryThrow(Point);
                    for (var i = 0; i < 400; i++)
                    {
                        lure.Tick(Frame);
                        _ = lure.Draws(far, alreadyLured: false);
                        _ = lure.Draws(Point, alreadyLured: false);
                    }

                    lure.Break();
                    lure.Tick(Cooldown);
                }
            });
        }

        private static void OnLanded(Lure _)
        {
        }

        private static void OnEnded(Lure _, LureEnd why)
        {
        }

        private void Land()
        {
            lure.TryThrow(Point);
            lure.Tick(Flight);
        }
    }
}
