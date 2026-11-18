using System.Collections.Generic;
using DarknessRises.Game.Bosses;
using DarknessRises.Game.EditModeTests.Levels;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Runs;
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

        // Feints half the time (by the roll), revealed 0.9 s after the flash with a 0.3 s growl, 0.6 s more to read them;
        // two adds a drop, three at most.
        private static readonly ColossusPhase2Settings Phase2 = new(0.5f, 0.9f, 0.3f, 0.6f, 2, 3);

        // The second flash 0.3 s after the first, the second foot 0.7 s after the first, planted 2 s, rest 0.5 s.
        private static readonly ColossusPhase3Settings Phase3 = new(0.3f, 0.7f, 2f, 0.5f);

        private static ColossusPattern Pattern(Relocation relocation, IRandom random, int hitPoints = 100)
        {
            return new ColossusPattern(
                Timing, new ColossusPhaseThresholds(0.5f, 0.15f), Phase2, Phase3, 0.75f, hitPoints, relocation, random);
        }

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
            pattern = Pattern(relocation, random);
            pattern.FlashRequested += lead => heard.Add($"flash requested, {lead:0.#} s lead");
            pattern.ShadowShown += target => heard.Add($"shadow on {target}");
            pattern.ShadowHidden += target => heard.Add($"shadow off {target}");
            pattern.AddsDue += count => heard.Add($"{count} adds");
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
            Assert.That(pattern.IsRelocateWindowOpen, Is.True);

            Run(1.5f);
            Assert.That(relocation.IsEnabled, Is.False, "Closed once the window is over, before the impact.");
            Assert.That(pattern.IsRelocateWindowOpen, Is.False);
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
            pattern = Pattern(relocation, random);
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
        public void Each_weak_spot_hit_reports_the_damage_dealt_and_the_hit_points_left()
        {
            var damaged = new List<string>();
            pattern.Damaged += (dealt, left) => damaged.Add($"{dealt} dealt, {left} left");
            AimAtCharacter();

            Run(4.9f);
            pattern.TakeWeakSpotHit(10);
            Run(0.2f);
            pattern.TakeWeakSpotHit(10);
            pattern.TakeWeakSpotHit(0);
            pattern.TakeWeakSpotHit(95);

            Assert.That(damaged, Is.EqualTo(new[] { "10 dealt, 90 left", "90 dealt, 0 left" }),
                "Nothing in the air or for no damage, and never more than it has left.");
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
    

        // The first stomp aims at the west Position, so the character stays unhurt on the middle one; in its damage window
        // the weak spot takes the damage. Leaves the pattern at 5.1 s, just after the impact.
        private void FirstStompAimedWestThenHit(int damage)
        {
            random.Next(0.9f, 0f);
            Run(5.1f);
            Assert.That(pattern.TakeWeakSpotHit(damage), Is.True);
        }

        [Test]
        public void In_phase_2_a_feint_shows_two_shadows_and_a_late_flash_reveals_the_real_one()
        {
            FirstStompAimedWestThenHit(60);
            Assert.That(pattern.Phase, Is.EqualTo(2));

            // Next stomp: a feint (roll under 0.5), aimed away from the character (0.9) at the east Position (1.5).
            random.Next(0.1f, 0.9f, 1.5f);
            heard.Clear();
            Run(10.6f - 5.1f);
            Assert.That(pattern.IsFeint, Is.True);
            Assert.That(heard, Is.EqualTo(new[] { "lifted", "2 adds", "flash requested, 1 s lead", "shadow on 2", "shadow on 1" }));
            Assert.That(pattern.ShownShadow, Is.EqualTo(East));
            Assert.That(pattern.ShownSecondShadow, Is.EqualTo(Middle), "The false shadow is over the character.");

            heard.Clear();
            Run(0.9f);
            Assert.That(heard, Is.EqualTo(new[] { "flash requested, 0.3 s lead", "shadow off 1" }));
            Assert.That(pattern.ShownSecondShadow, Is.EqualTo(-1));
            Assert.That(relocation.IsEnabled, Is.True, "A feint's window is longer, so the late flash can still be acted on.");

            heard.Clear();
            Run(2.5f - 0.9f + 0.1f);
            Assert.That(heard, Is.EqualTo(new[] { "window closed", "missed on 2" }), "Only the real foot comes down.");
        }

        [Test]
        public void Dodging_a_feints_false_shadow_onto_the_real_one_gets_the_character_stomped()
        {
            FirstStompAimedWestThenHit(60);
            random.Next(0.1f, 0.9f, 1.5f);
            Run(10.6f - 5.1f);

            relocation.TryRelocate(Vector2.right);
            Run(2.6f);

            Assert.That(heard, Has.Member("stomped on 2"));
        }

        [Test]
        public void In_phase_2_Shamblers_drop_after_each_stomp_up_to_the_most_alive()
        {
            var drops = new List<int>();
            pattern.AddsDue += drops.Add;
            FirstStompAimedWestThenHit(60);
            random.Next(0.9f, 0.9f, 0f);

            Run(9f - 5.1f);
            Assert.That(drops, Is.EqualTo(new[] { 2 }), "Between stomps, once the foot has lifted.");
            pattern.AddSpawned();
            pattern.AddSpawned();

            random.Next(0.9f, 0.9f, 0f);
            Run(16.6f - 9f);
            Assert.That(drops, Is.EqualTo(new[] { 2, 1 }), "Topped up to three alive.");

            pattern.AddDied();
            pattern.AddDied();
            pattern.AddDied();
            Assert.That(pattern.AddsAlive, Is.Zero);
        }

        [Test]
        public void No_adds_drop_in_phase_1()
        {
            var drops = 0;
            pattern.AddsDue += _ => drops++;
            random.Next(0.9f, 0f, 0.9f, 0f);

            Run(15f);

            Assert.That(drops, Is.Zero);
        }

        [Test]
        public void In_phase_3_Lightning_flashes_twice_and_the_two_stomps_chain()
        {
            var phases = new List<int>();
            pattern.PhaseChanged += phases.Add;
            FirstStompAimedWestThenHit(90);
            Assert.That(phases, Is.EqualTo(new[] { 3 }));

            // Next stomp: the first foot at the character (0.1), the second at the west Position (0).
            random.Next(0.1f, 0f);
            heard.Clear();
            Run(10.6f - 5.1f);
            Assert.That(pattern.IsChain, Is.True);
            Assert.That(heard, Is.EqualTo(new[] { "lifted", "2 adds", "flash requested, 1 s lead", "shadow on 1" }));

            heard.Clear();
            Run(0.3f);
            Assert.That(heard, Is.EqualTo(new[] { "flash requested, 0 s lead", "shadow on 0" }), "The second flash, right after the first.");

            relocation.TryRelocate(Vector2.right);
            heard.Clear();
            Run(12.6f - 10.9f);
            Assert.That(heard, Is.EqualTo(new[] { "window closed", "missed on 1" }));
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Chaining));

            heard.Clear();
            Run(0.7f);
            Assert.That(heard, Is.EqualTo(new[] { "missed on 0" }), "The second foot lands right after the first.");
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Planted));
            Assert.That(pattern.Target, Is.EqualTo(West));
            Assert.That(pattern.StepSeconds, Is.EqualTo(2f), "A shorter damage window.");
        }

        [Test]
        public void In_a_chain_a_stomped_character_is_never_knocked_under_the_second_foot()
        {
            FirstStompAimedWestThenHit(90);
            random.Next(0.1f, 0f);

            Run(12.6f - 5.1f);

            Assert.That(heard, Has.Member("stomped on 1"));
            Assert.That(relocation.Destination, Is.EqualTo(East), "Away from the west Position, where the second foot lands.");
        }

        [Test]
        public void Flare_shows_the_next_shadow_early_and_opens_Relocate_before_the_flash()
        {
            AimAtCharacter();
            Run(2.5f);
            Assert.That(pattern.Step, Is.EqualTo(ColossusStep.Growling));
            Assert.That(relocation.IsEnabled, Is.False);

            Assert.That(pattern.RevealEarly(), Is.True);
            Assert.That(heard, Is.EqualTo(new[] { "flash requested, 1 s lead", "shadow on 1" }));
            Assert.That(relocation.IsEnabled, Is.True);
            Assert.That(pattern.RevealEarly(), Is.False, "Once per stomp.");

            relocation.TryRelocate(Vector2.left);
            Run(2.6f);
            Assert.That(heard, Is.EqualTo(new[] { "flash requested, 1 s lead", "shadow on 1", "window closed", "missed on 1" }),
                "The flash still comes, without showing the shadow again.");
        }

        [Test]
        public void Flare_between_stomps_shows_the_next_one_too()
        {
            AimAtCharacter();
            Run(1f);

            Assert.That(pattern.RevealEarly(), Is.True);
            Assert.That(heard, Is.EqualTo(new[] { "shadow on 1" }));
        }

        [Test]
        public void Flare_shows_only_the_real_foot_of_a_feint()
        {
            FirstStompAimedWestThenHit(60);
            random.Next(0.1f, 0.9f, 1.5f);
            Run(9f - 5.1f);
            heard.Clear();

            pattern.RevealEarly();
            Run(10.6f - 9f + 0.9f);

            Assert.That(heard, Is.EqualTo(new[] { "shadow on 2", "flash requested, 1 s lead" }), "No false shadow and no late flash.");
        }

        [Test]
        public void Slow_mo_stretches_the_Relocate_window_through_Enemy_time()
        {
            var enemyTime = new EnemyTime { Scale = 0.5f };
            AimAtCharacter();
            const float frame = 0.05f;
            var realSeconds = 0f;
            var openRealSeconds = 0f;
            while (pattern.Step != ColossusStep.Falling)
            {
                pattern.Tick(enemyTime.Scaled(frame));
                realSeconds += frame;
                if (relocation.IsEnabled)
                {
                    openRealSeconds += frame;
                }
            }

            Assert.That(openRealSeconds, Is.EqualTo(3f).Within(0.06f), "A 1.5 s window at half Enemy speed.");
            Assert.That(realSeconds, Is.EqualTo(9f).Within(0.06f));
        }

        [Test]
        public void The_Colossus_gets_tougher_with_the_Ramp()
        {
            var ramp = new Ramp<string>(new RampTuning(bossHitPointsGrowthPerLevel: 0.35f), "Runner");

            var level1 = Pattern(relocation, random, ramp.StepAt(1).BossHitPoints(50));
            var level2 = Pattern(relocation, random, ramp.StepAt(2).BossHitPoints(50));
            var level5 = Pattern(relocation, random, ramp.StepAt(5).BossHitPoints(50));

            Assert.That(level1.MaxHitPoints, Is.EqualTo(50));
            Assert.That(level2.MaxHitPoints, Is.EqualTo(68));
            Assert.That(level5.MaxHitPoints, Is.GreaterThan(level2.MaxHitPoints));
        }
    }
}
