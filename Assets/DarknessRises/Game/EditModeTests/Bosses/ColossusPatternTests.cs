using System.Collections.Generic;
using DarknessRises.Game.Bosses;
using DarknessRises.Game.EditModeTests.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Bosses
{
    public sealed class ColossusPatternTests
    {
        // Three Positions in a row, west to east. The character starts in the middle.
        private const int West = 0;
        private const int Middle = 1;
        private const int East = 2;

        private static readonly Vector3[] Row = { new(-6f, 0f, 0f), Vector3.zero, new(6f, 0f, 0f) };

        // First growl after 2 s; growl 1 s; flash to impact 2 s with a 1.5 s Relocate window; planted 3 s; lift 0.5 s; rest 1 s.
        private static readonly ColossusStompTiming Timing = new(2f, 1f, 2f, 1.5f, 3f, 0.5f, 1f);

        private ScriptedRandom random;
        private Relocation relocation;
        private ColossusPattern pattern;
        private readonly List<string> heard = new();

        [SetUp]
        public void SetUp()
        {
            heard.Clear();
            random = new ScriptedRandom();
            relocation = new Relocation(Row, Middle, new RelocateSettings(dashSeconds: 0.2f, maxAngleDegrees: 60f));
            pattern = new ColossusPattern(Timing, new ColossusPhaseThresholds(0.5f, 0.15f), 0.75f, 100, relocation, random);
            pattern.FlashRequested += lead => heard.Add($"flash requested, {lead} s lead");
            pattern.ShadowShown += target => heard.Add($"shadow on {target}");
            pattern.RelocateWindowClosed += () => heard.Add("window closed");
            pattern.Landed += (target, stomped) => heard.Add(stomped ? $"stomped on {target}" : $"missed on {target}");
            pattern.Lifted += () => heard.Add("lifted");
            pattern.Begin();
        }

        // Ticks in steps small enough for the Relocate dash and the pattern to stay in step, as frames would.
        private void Run(float seconds)
        {
            const float step = 0.05f;
            for (var t = 0f; t < seconds - 1e-4f; t += step)
            {
                relocation.Tick(step);
                pattern.Tick(step);
            }
        }

        // The stomp aims at the character's Position (a roll under the 75% chance).
        private void AimAtCharacter() => random.Next(0.1f);

        [Test]
        public void It_growls_then_Lightning_flashes_and_shows_the_raised_foots_shadow()
        {
            AimAtCharacter();

            Run(2.1f);
            Assert.That(heard, Is.EqualTo(new[] { "flash requested, 1 s lead" }));
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Growling));

            Run(1f);
            Assert.That(heard[1], Is.EqualTo("shadow on 1"));
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Raised));
            Assert.That(pattern.Target, Is.EqualTo(Middle));
        }

        [Test]
        public void Relocate_is_open_only_during_the_Relocate_window()
        {
            AimAtCharacter();

            Run(2.9f);
            Assert.That(relocation.IsEnabled, Is.False, "Not before the flash.");

            Run(0.2f);
            Assert.That(relocation.IsEnabled, Is.True, "Open after the flash.");

            Run(1.5f);
            Assert.That(relocation.IsEnabled, Is.False, "Closed once the window is over, before the impact.");
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Falling));
        }

        [Test]
        public void Staying_on_the_target_gets_the_character_stomped()
        {
            AimAtCharacter();

            Run(5.1f);

            Assert.That(heard, Has.Member("stomped on 1"));
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Planted));
        }

        [Test]
        public void A_stomped_character_is_knocked_out_from_under_the_foot_to_the_nearest_other_Position()
        {
            random.Next(0.9f, 1.2f); // The east Position.
            Run(3.1f);
            relocation.TryRelocate(Vector2.right);
            Run(2f);

            Assert.That(heard, Has.Member("stomped on 2"));
            Run(0.3f);
            Assert.That(relocation.Position, Is.EqualTo(Middle));
        }

        [Test]
        public void Relocating_off_the_target_in_time_escapes_the_stomp()
        {
            AimAtCharacter();
            Run(3.1f);

            Assert.That(relocation.TryRelocate(Vector2.right), Is.True);
            Run(2f);

            Assert.That(relocation.Position, Is.EqualTo(East));
            Assert.That(heard, Has.Member("missed on 1"));
        }

        [Test]
        public void Relocating_under_the_shadow_gets_the_character_stomped()
        {
            random.Next(0.9f, 0f); // Not the character: the first of the other Positions, the west one.
            Run(3.1f);
            Assert.That(pattern.Target, Is.EqualTo(West));

            relocation.TryRelocate(Vector2.left);
            Run(2f);

            Assert.That(heard, Has.Member("stomped on 0"));
        }

        [Test]
        public void A_dash_that_hasnt_arrived_by_the_impact_is_too_late()
        {
            // A dash slower than the time between the window closing and the impact.
            relocation = new Relocation(Row, Middle, new RelocateSettings(dashSeconds: 1f, maxAngleDegrees: 60f));
            pattern = new ColossusPattern(Timing, new ColossusPhaseThresholds(0.5f, 0.15f), 0.75f, 100, relocation, random);
            var stomped = false;
            pattern.Landed += (_, hit) => stomped = hit;
            pattern.Begin();
            AimAtCharacter();

            Run(4.45f);
            relocation.TryRelocate(Vector2.right);
            Run(0.65f);

            Assert.That(stomped, Is.True);
        }

        [Test]
        public void A_stomp_that_isnt_aimed_at_the_character_picks_another_Position()
        {
            random.Next(0.9f, 1.2f); // Not the character; the second of the other two, the east one.

            Run(3.1f);

            Assert.That(pattern.Target, Is.EqualTo(East));
        }

        [Test]
        public void The_weak_spot_takes_damage_only_while_the_foot_is_planted()
        {
            AimAtCharacter();

            Run(4.9f);
            Assert.That(pattern.TakeWeakSpotHit(10), Is.False, "Not while the foot is in the air.");

            Run(0.2f);
            Assert.That(pattern.IsWeakSpotExposed, Is.True);
            Assert.That(pattern.TakeWeakSpotHit(10), Is.True);
            Assert.That(pattern.HitPoints, Is.EqualTo(90));

            Run(3f);
            Assert.That(heard[heard.Count - 1], Is.EqualTo("lifted"));
            Assert.That(pattern.TakeWeakSpotHit(10), Is.False, "Not once it lifts.");
            Assert.That(pattern.HitPoints, Is.EqualTo(90));
        }

        [Test]
        public void After_lifting_and_resting_it_growls_again()
        {
            AimAtCharacter();
            AimAtCharacter();

            Run(5f + 3f + 0.5f + 1f + 0.1f);

            Assert.That(heard[heard.Count - 1], Is.EqualTo("flash requested, 1 s lead"));
            Assert.That(pattern.Target, Is.EqualTo(-1));
        }

        [Test]
        public void It_moves_to_phase_2_below_half_health_and_phase_3_in_the_last_15_percent()
        {
            var phases = new List<int>();
            pattern.PhaseChanged += phases.Add;
            AimAtCharacter();
            Run(5.1f);

            pattern.TakeWeakSpotHit(50);
            Assert.That(pattern.Phase, Is.EqualTo(1), "Exactly half is still phase 1.");

            pattern.TakeWeakSpotHit(1);
            Assert.That(pattern.Phase, Is.EqualTo(2));

            pattern.TakeWeakSpotHit(35);
            Assert.That(pattern.Phase, Is.EqualTo(3));
            Assert.That(phases, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Losing_its_last_hit_point_defeats_it_and_stops_the_pattern()
        {
            var defeated = 0;
            pattern.Defeated += () => defeated++;
            AimAtCharacter();
            Run(5.1f);

            pattern.TakeWeakSpotHit(100);
            Run(10f);

            Assert.That(defeated, Is.EqualTo(1));
            Assert.That(pattern.IsDefeated, Is.True);
            Assert.That(pattern.TakeWeakSpotHit(10), Is.False);
            Assert.That(heard, Has.No.Member("lifted"));
            Assert.That(relocation.IsEnabled, Is.False);
        }

        [Test]
        public void One_long_tick_runs_through_the_steps_in_order()
        {
            AimAtCharacter();

            pattern.Tick(5.5f);

            Assert.That(heard, Is.EqualTo(new[]
            {
                "flash requested, 1 s lead",
                "shadow on 1",
                "window closed",
                "stomped on 1",
            }));
            Assert.That(pattern.StepSecondsLeft, Is.EqualTo(2.5f).Within(1e-4f));
        }
    }
}
