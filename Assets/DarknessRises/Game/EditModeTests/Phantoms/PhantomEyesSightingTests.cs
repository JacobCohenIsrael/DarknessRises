using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Phantoms;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Phantoms
{
    public sealed class PhantomEyesSightingTests
    {
        private const float Frame = 1f / 64f;
        private const float Clearance = 14f;

        private static readonly PhantomConditions Quiet = new(CritterMoment.Walking, float.PositiveInfinity, false, false);

        // Open over 0.5 s, blink out over 0.125 s, keep 14 m from any Enemy.
        private static PhantomEyesSighting Shown(float holdSeconds)
        {
            var sighting = new PhantomEyesSighting(0.5f, 0.125f, Clearance);
            sighting.Show(holdSeconds);
            return sighting;
        }

        private static PhantomVanish Run(PhantomEyesSighting sighting, float seconds, in PhantomConditions conditions)
        {
            for (var t = 0f; t < seconds; t += Frame)
            {
                var vanish = sighting.Tick(Frame, conditions);
                if (vanish != PhantomVanish.None)
                {
                    return vanish;
                }
            }

            return PhantomVanish.None;
        }

        [Test]
        public void The_eyes_open_then_hold()
        {
            var sighting = Shown(3f);
            Assert.That(sighting.Strength, Is.Zero);

            sighting.Tick(0.25f, Quiet);
            Assert.That(sighting.Strength, Is.EqualTo(0.5f).Within(1e-5f));

            sighting.Tick(0.5f, Quiet);
            Assert.That(sighting.Strength, Is.EqualTo(1f));
            Assert.That(sighting.IsShowing, Is.True);
        }

        [Test]
        public void They_hold_a_few_seconds_at_most_then_blink_out()
        {
            var sighting = Shown(2f);

            Assert.That(Run(sighting, 1.9f, Quiet), Is.EqualTo(PhantomVanish.None));
            Assert.That(Run(sighting, 1f, Quiet), Is.EqualTo(PhantomVanish.TimeUp));
            Assert.That(sighting.IsShowing, Is.False);
            Assert.That(sighting.Strength, Is.Zero);
        }

        [Test]
        public void Light_on_their_spot_makes_them_vanish_in_that_very_frame()
        {
            var sighting = Shown(3f);
            Run(sighting, 1f, Quiet);

            var vanish = sighting.Tick(Frame, new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, true, false));

            Assert.That(vanish, Is.EqualTo(PhantomVanish.Lit));
            Assert.That(sighting.Strength, Is.Zero);
            Assert.That(sighting.IsShowing, Is.False);
        }

        [Test]
        public void Light_cuts_a_blink_out_short_too()
        {
            var sighting = Shown(3f);
            Run(sighting, 1f, Quiet);
            sighting.Tick(Frame, new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, false, true));
            Assert.That(sighting.IsBlinkingOut, Is.True);

            Assert.That(sighting.Tick(Frame, new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, true, false)), Is.EqualTo(PhantomVanish.Lit));
            Assert.That(sighting.Strength, Is.Zero);
        }

        [Test]
        public void Aiming_at_them_makes_them_blink_out()
        {
            var sighting = Shown(3f);
            Run(sighting, 1f, Quiet);
            var aimed = new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, false, true);

            Assert.That(sighting.Tick(Frame, aimed), Is.EqualTo(PhantomVanish.None));
            Assert.That(sighting.IsBlinkingOut, Is.True);
            sighting.Tick(Frame, Quiet);
            Assert.That(sighting.Strength, Is.LessThan(1f));

            Assert.That(Run(sighting, 0.5f, Quiet), Is.EqualTo(PhantomVanish.AimedAt));
            Assert.That(sighting.Vanish, Is.EqualTo(PhantomVanish.AimedAt));
        }

        [Test]
        public void The_character_walking_nearer_than_they_appear_makes_them_blink_out()
        {
            var sighting = new PhantomEyesSighting(0.5f, 0.125f, Clearance, 8f);
            sighting.Show(3f);
            Run(sighting, 1f, new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, false, false, 9f));
            Assert.That(sighting.IsShowing, Is.True);

            sighting.Tick(Frame, new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, false, false, 7.9f));

            Assert.That(sighting.IsBlinkingOut, Is.True);
            Assert.That(Run(sighting, 0.5f, Quiet), Is.EqualTo(PhantomVanish.TooNear));
        }

        [Test]
        public void A_real_Enemy_coming_near_sends_them_away_at_once()
        {
            var sighting = Shown(3f);
            Run(sighting, 1f, Quiet);

            var vanish = sighting.Tick(Frame, new PhantomConditions(CritterMoment.Walking, 10f, false, false));

            Assert.That(vanish, Is.EqualTo(PhantomVanish.EnemyNear));
            Assert.That(sighting.Strength, Is.Zero);
        }

        [TestCase(CritterMoment.WaveFight)]
        [TestCase(CritterMoment.BossFight)]
        [TestCase(CritterMoment.None)]
        public void A_fight_starting_sends_them_away_at_once(CritterMoment moment)
        {
            var sighting = Shown(3f);
            Run(sighting, 1f, Quiet);

            Assert.That(sighting.Tick(Frame, new PhantomConditions(moment, float.PositiveInfinity, false, false)), Is.EqualTo(PhantomVanish.MomentEnded));
            Assert.That(sighting.IsShowing, Is.False);
        }

        [Test]
        public void A_paused_game_holds_them_as_they_are()
        {
            var sighting = Shown(1f);
            sighting.Tick(0.25f, Quiet);

            for (var i = 0; i < 1000; i++)
            {
                Assert.That(sighting.Tick(0f, Quiet), Is.EqualTo(PhantomVanish.None));
            }

            Assert.That(sighting.Strength, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void A_new_sighting_starts_over()
        {
            var sighting = Shown(1f);
            Run(sighting, 2f, Quiet);

            sighting.Show(1f);

            Assert.That(sighting.IsShowing, Is.True);
            Assert.That(sighting.Vanish, Is.EqualTo(PhantomVanish.None));
            Assert.That(sighting.Strength, Is.Zero);
        }

        [Test]
        public void Nothing_happens_when_none_is_showing()
        {
            var sighting = new PhantomEyesSighting(0.5f, 0.1f, Clearance);

            Assert.That(sighting.Tick(Frame, new PhantomConditions(CritterMoment.Walking, 1f, true, true)), Is.EqualTo(PhantomVanish.None));
        }

        [Test]
        public void Bad_settings_are_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSighting(-1f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSighting(0f, -1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSighting(0f, 0f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomEyesSighting(0f, 0f, 0f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Shown(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Shown(1f).Tick(-1f, Quiet));
        }

        [Test]
        public void The_Crosshair_near_the_eyes_aims_at_them()
        {
            var eyes = new Vector3(500f, 300f, 12f);

            // 8 % of a 1000-pixel-high screen: 80 pixels.
            Assert.That(PhantomAim.IsAimedAt(new Vector2(560f, 300f), true, eyes, 1000f, 0.08f), Is.True);
            Assert.That(PhantomAim.IsAimedAt(new Vector2(500f, 381f), true, eyes, 1000f, 0.08f), Is.False);
        }

        [Test]
        public void A_finger_that_is_up_aims_at_nothing()
        {
            Assert.That(PhantomAim.IsAimedAt(new Vector2(500f, 300f), false, new Vector3(500f, 300f, 12f), 1000f, 0.08f), Is.False);
        }

        [Test]
        public void Eyes_behind_the_camera_are_never_aimed_at()
        {
            Assert.That(PhantomAim.IsAimedAt(new Vector2(500f, 300f), true, new Vector3(500f, 300f, -3f), 1000f, 0.08f), Is.False);
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var sighting = Shown(1000f);

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    sighting.Tick(Frame, Quiet);
                }
            });
        }
    }
}
