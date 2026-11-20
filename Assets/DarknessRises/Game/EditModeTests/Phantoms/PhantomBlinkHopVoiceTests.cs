using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.EditModeTests.Levels;
using DarknessRises.Game.Phantoms;
using DarknessRises.Game.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Phantoms
{
    public sealed class PhantomBlinkHopVoiceTests
    {
        private const float Frame = 1f / 64f;
        private const float Clearance = 14f;

        private static readonly PhantomConditions Quiet = new(CritterMoment.Walking, float.PositiveInfinity, false, false);

        // Open over 0.25 s, blink out over 0.125 s; blinks of 0.25 s after exactly 1 s of open eyes.
        private static PhantomEyesSighting Blinking(float holdSeconds)
        {
            var sighting = new PhantomEyesSighting(0.25f, 0.125f, Clearance, 0f, new PhantomBlinking(0.25f, 1f, 1f), new ScriptedRandom());
            sighting.Show(holdSeconds);
            return sighting;
        }

        [Test]
        public void Holding_eyes_blink_shut_and_open_again_after_a_gap()
        {
            var sighting = Blinking(10f);

            // Opening, then almost a second of open eyes (the gap counts from when they're fully open).
            sighting.Tick(0.25f, Quiet);
            sighting.Tick(0.75f - Frame, Quiet);
            Assert.That(sighting.Strength, Is.EqualTo(1f));
            Assert.That(sighting.IsBlinking, Is.False);

            // The blink starts, shuts halfway through, and is open again by its end.
            sighting.Tick(Frame, Quiet);
            Assert.That(sighting.IsBlinking, Is.True);
            sighting.Tick(0.125f, Quiet);
            Assert.That(sighting.Strength, Is.EqualTo(0f).Within(1e-4f));
            sighting.Tick(0.0625f, Quiet);
            Assert.That(sighting.Strength, Is.EqualTo(0.5f).Within(1e-4f));
            sighting.Tick(0.0625f, Quiet);
            Assert.That(sighting.IsBlinking, Is.False);
            Assert.That(sighting.Strength, Is.EqualTo(1f));
            Assert.That(sighting.IsShowing, Is.True);
        }

        [Test]
        public void They_blink_again_and_again_at_random_gaps_while_they_hold()
        {
            var sighting = new PhantomEyesSighting(0.25f, 0.125f, Clearance, 0f, new PhantomBlinking(0.15f, 0.6f, 2f), new SystemRandom(5));
            sighting.Show(30f);
            var blinks = 0;
            var wasBlinking = false;
            for (var t = 0f; t < 29f; t += Frame)
            {
                sighting.Tick(Frame, Quiet);
                blinks += sighting.IsBlinking && !wasBlinking ? 1 : 0;
                wasBlinking = sighting.IsBlinking;
            }

            // A blink every 0.75-2.15 s.
            Assert.That(blinks, Is.InRange(13, 38));
        }

        [Test]
        public void Eyes_still_opening_do_not_blink()
        {
            var sighting = new PhantomEyesSighting(2f, 0.125f, Clearance, 0f, new PhantomBlinking(0.25f, 0f, 0f), new ScriptedRandom());
            sighting.Show(10f);

            for (var t = 0f; t < 1.9f; t += Frame)
            {
                sighting.Tick(Frame, Quiet);
                Assert.That(sighting.IsBlinking, Is.False);
            }
        }

        [Test]
        public void Light_mid_blink_still_ends_them_at_once()
        {
            var sighting = Blinking(10f);
            sighting.Tick(0.25f, Quiet);
            sighting.Tick(1f + Frame, Quiet);
            Assert.That(sighting.IsBlinking, Is.True);

            var lit = new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, true, false);
            Assert.That(sighting.Tick(Frame, lit), Is.EqualTo(PhantomVanish.Lit));
            Assert.That(sighting.IsShowing, Is.False);
            Assert.That(sighting.IsBlinking, Is.False);
            Assert.That(sighting.Strength, Is.Zero);
        }

        [Test]
        public void Aiming_mid_blink_still_blinks_them_out()
        {
            var sighting = Blinking(10f);
            sighting.Tick(0.25f, Quiet);
            sighting.Tick(1f + Frame, Quiet);

            var aimed = new PhantomConditions(CritterMoment.Walking, float.PositiveInfinity, false, true);
            sighting.Tick(Frame, aimed);
            Assert.That(sighting.IsBlinkingOut, Is.True);
            Assert.That(sighting.Tick(0.2f, aimed), Is.EqualTo(PhantomVanish.AimedAt));
        }

        [Test]
        public void Blinking_needs_a_random_source_and_sane_settings()
        {
            Assert.Throws<ArgumentNullException>(() => new PhantomEyesSighting(0f, 0f, 0f, 0f, new PhantomBlinking(0.1f, 1f, 2f)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomBlinking(-0.1f, 1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomBlinking(0.1f, 2f, 1f));
            Assert.DoesNotThrow(() => new PhantomEyesSighting(0f, 0f, 0f, 0f, new PhantomBlinking(0f, 0f, 0f)));
        }

        [Test]
        public void Ticking_blinking_eyes_does_not_allocate()
        {
            var sighting = new PhantomEyesSighting(0.25f, 0.125f, Clearance, 0f, new PhantomBlinking(0.15f, 0.1f, 0.3f), new SystemRandom(2));
            sighting.Show(10000f);

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 640; i++)
                {
                    sighting.Tick(Frame, Quiet);
                }
            });
        }

        [TestCase(PhantomVanish.TimeUp, true)]
        [TestCase(PhantomVanish.Lit, false)]
        [TestCase(PhantomVanish.AimedAt, false)]
        [TestCase(PhantomVanish.EnemyNear, false)]
        [TestCase(PhantomVanish.MomentEnded, false)]
        [TestCase(PhantomVanish.TooNear, false)]
        public void Only_a_pair_that_held_its_time_opens_again_nearby(PhantomVanish vanish, bool hops)
        {
            var relocation = new PhantomRelocation(1f, 2, 0.5f, 1f, 2f, 5f);

            Assert.That(relocation.Hops(vanish, 0, Low()), Is.EqualTo(hops));
        }

        [Test]
        public void A_pair_hops_a_few_times_at_most_and_by_chance()
        {
            var relocation = new PhantomRelocation(0.5f, 2, 0.5f, 1f, 2f, 5f);

            Assert.That(relocation.Hops(PhantomVanish.TimeUp, 1, Low()), Is.True);
            Assert.That(relocation.Hops(PhantomVanish.TimeUp, 2, Low()), Is.False);
            Assert.That(relocation.Hops(PhantomVanish.TimeUp, 0, new ScriptedRandom()), Is.False);
            Assert.That(new PhantomRelocation(0f, 2, 0f, 0f, 0f, 0f).Hops(PhantomVanish.TimeUp, 0, Low()), Is.False);
        }

        [Test]
        public void A_hop_lands_nearby_after_a_moment_of_dark()
        {
            var relocation = new PhantomRelocation(1f, 2, 0.5f, 1.2f, 2f, 5f);
            var random = new SystemRandom(7);
            var from = new Vector3(3f, 0.5f, -2f);
            for (var i = 0; i < 300; i++)
            {
                var to = relocation.PickNear(from, random);
                Assert.That((to - from).magnitude, Is.InRange(2f - 1e-3f, 5f + 1e-3f));
                Assert.That(to.y, Is.EqualTo(from.y));
                Assert.That(relocation.Gap(random), Is.InRange(0.5f, 1.2f));
            }
        }

        [Test]
        public void A_bad_hop_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomRelocation(1.5f, 2, 0f, 1f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomRelocation(0.5f, -1, 0f, 1f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomRelocation(0.5f, 2, 2f, 1f, 0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PhantomRelocation(0.5f, 2, 0f, 1f, 2f, 1f));
        }

        [Test]
        public void A_voice_is_never_the_same_twice_in_a_row()
        {
            var picker = new PhantomVoicePicker(new SystemRandom(11));
            var last = picker.Pick(3);
            var seen = new int[3];
            seen[last]++;
            for (var i = 0; i < 600; i++)
            {
                var next = picker.Pick(3);
                Assert.That(next, Is.Not.EqualTo(last));
                Assert.That(next, Is.InRange(0, 2));
                seen[next]++;
                last = next;
            }

            // Every voice still gets its turn.
            Assert.That(seen, Has.All.GreaterThan(150));
        }

        [Test]
        public void One_voice_repeats_and_none_picks_nothing()
        {
            var picker = new PhantomVoicePicker(new SystemRandom(1));

            Assert.That(picker.Pick(0), Is.EqualTo(-1));
            Assert.That(picker.Pick(1), Is.Zero);
            Assert.That(picker.Pick(1), Is.Zero);
        }

        [Test]
        public void Picking_the_top_of_the_range_still_steps_over_the_last()
        {
            var picker = new PhantomVoicePicker(new ScriptedRandom());

            // An empty script returns the top of each range.
            Assert.That(picker.Pick(3), Is.EqualTo(2));
            Assert.That(picker.Pick(3), Is.EqualTo(1));
            Assert.That(picker.Pick(3), Is.EqualTo(2));
        }

        [Test]
        public void Picking_does_not_allocate()
        {
            var picker = new PhantomVoicePicker(new SystemRandom(3));

            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 64; i++)
                {
                    picker.Pick(3);
                }
            });
        }

        private static IRandom Low()
        {
            var random = new ScriptedRandom();
            for (var i = 0; i < 100; i++)
            {
                random.Next(0f);
            }

            return random;
        }
    }
}
