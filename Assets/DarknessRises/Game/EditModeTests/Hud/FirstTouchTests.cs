using System;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Hud
{
    /// <summary>What a new player's first touch shows: the crosshair, the shoot hint and the title card's setup line.</summary>
    public sealed class FirstTouchTests
    {
        private const float Tolerance = 1e-4f;

        // Kicks to 1.5 times its size for 0.2 s, lingers 0.5 s after the last Shot, fades over 0.25 s.
        private static Reticle NewReticle() => new(1.5f, 0.2f, 0.5f, 0.25f);

        [Test]
        public void On_touch_the_crosshair_is_hidden_until_the_first_Shot()
        {
            var reticle = NewReticle();

            reticle.Tick(1f, aiming: false);

            Assert.That(reticle.IsVisible, Is.False);
        }

        [Test]
        public void A_Shot_shows_the_crosshair_at_once_and_kicks_it()
        {
            var reticle = NewReticle();

            reticle.Fire();

            Assert.That(reticle.Alpha, Is.EqualTo(1f));
            Assert.That(reticle.Scale, Is.EqualTo(1.5f));
        }

        [Test]
        public void The_kick_settles_back()
        {
            var reticle = NewReticle();
            reticle.Fire();

            reticle.Tick(0.1f, aiming: true);
            Assert.That(reticle.Scale, Is.EqualTo(1.25f).Within(Tolerance));

            reticle.Tick(0.1f, aiming: true);
            Assert.That(reticle.Scale, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void With_a_mouse_the_crosshair_is_always_there()
        {
            var reticle = NewReticle();

            reticle.Tick(0.016f, aiming: true);
            Assert.That(reticle.Alpha, Is.EqualTo(1f));

            reticle.Tick(60f, aiming: true);
            Assert.That(reticle.Alpha, Is.EqualTo(1f));
        }

        [Test]
        public void After_the_last_Shot_on_touch_it_lingers_then_fades()
        {
            var reticle = NewReticle();
            reticle.Fire();

            reticle.Tick(0.5f, aiming: false);
            Assert.That(reticle.Alpha, Is.EqualTo(1f), "still lingering");

            reticle.Tick(0.125f, aiming: false);
            Assert.That(reticle.Alpha, Is.EqualTo(0.5f).Within(Tolerance));

            reticle.Tick(0.125f, aiming: false);
            Assert.That(reticle.IsVisible, Is.False);
        }

        [Test]
        public void A_long_step_fades_only_for_what_is_left_after_the_linger()
        {
            var reticle = NewReticle();
            reticle.Fire();

            reticle.Tick(0.625f, aiming: false);

            Assert.That(reticle.Alpha, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Holding_the_aim_keeps_it_and_restarts_the_linger()
        {
            var reticle = NewReticle();
            reticle.Fire();
            reticle.Tick(0.4f, aiming: false);

            reticle.Tick(2f, aiming: true);
            reticle.Tick(0.4f, aiming: false);

            Assert.That(reticle.Alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Hiding_takes_it_away_at_once()
        {
            var reticle = NewReticle();
            reticle.Fire();

            reticle.Hide();

            Assert.That(reticle.IsVisible, Is.False);
            Assert.That(reticle.Scale, Is.EqualTo(1f));
        }

        [Test]
        public void Ticking_the_crosshair_does_not_allocate()
        {
            var reticle = NewReticle();
            reticle.Fire();

            AllocationAssert.DoesNotAllocate(() => reticle.Tick(0.001f, aiming: false));
        }

        [Test]
        public void The_crosshair_rejects_settings_that_make_no_sense()
        {
            Assert.That(() => new Reticle(0.5f, 0.1f, 0.1f, 0.1f), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new Reticle(1.5f, -0.1f, 0.1f, 0.1f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void The_shoot_hint_shows_over_the_first_Enemy_of_a_Runs_first_Level()
        {
            var hint = new ShootHint(ShootHintFrequency.OncePerRun, wasShownOnThisInstall: true);

            Assert.That(hint.TryShow(1), Is.True);
            Assert.That(hint.IsShowing, Is.True);
        }

        [Test]
        public void The_shoot_hint_shows_only_once()
        {
            var hint = new ShootHint(ShootHintFrequency.OncePerRun, wasShownOnThisInstall: false);
            hint.TryShow(1);

            Assert.That(hint.TryShow(1), Is.False, "the second Enemy");
            hint.Dismiss();
            Assert.That(hint.TryShow(1), Is.False, "after it was dismissed");
            Assert.That(hint.IsShowing, Is.False);
        }

        [Test]
        public void The_shoot_hint_never_shows_after_the_first_Level()
        {
            var hint = new ShootHint(ShootHintFrequency.OncePerRun, wasShownOnThisInstall: false);

            Assert.That(hint.TryShow(2), Is.False);
            Assert.That(hint.TryShow(7), Is.False);
            Assert.That(hint.WasShown, Is.False);
        }

        [Test]
        public void A_hint_authored_once_per_install_stays_away_once_the_device_has_seen_it()
        {
            Assert.That(new ShootHint(ShootHintFrequency.OncePerInstall, wasShownOnThisInstall: true).TryShow(1), Is.False);
            Assert.That(new ShootHint(ShootHintFrequency.OncePerInstall, wasShownOnThisInstall: false).TryShow(1), Is.True);
        }

        [Test]
        public void A_hint_authored_never_never_shows()
        {
            Assert.That(new ShootHint(ShootHintFrequency.Never, wasShownOnThisInstall: false).TryShow(1), Is.False);
        }

        [Test]
        public void Firing_dismisses_the_shoot_hint_once()
        {
            var hint = new ShootHint(ShootHintFrequency.OncePerRun, wasShownOnThisInstall: false);
            Assert.That(hint.Dismiss(), Is.False, "nothing to dismiss yet");
            hint.TryShow(1);

            Assert.That(hint.Dismiss(), Is.True);
            Assert.That(hint.IsShowing, Is.False);
            Assert.That(hint.WasShown, Is.True);
            Assert.That(hint.Dismiss(), Is.False);
        }

        [Test]
        public void Levels_count_from_one_for_the_shoot_hint()
        {
            var hint = new ShootHint(ShootHintFrequency.OncePerRun, wasShownOnThisInstall: false);

            Assert.That(() => hint.TryShow(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void The_title_card_announces_a_stage_and_sets_the_game_up_on_the_first_Level_only()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                Assert.That(level.TitleFor(1), Is.EqualTo("STAGE 1"));
                Assert.That(level.TitleFor(4), Is.EqualTo("STAGE 4"));
                Assert.That(level.SetupLineFor(1), Is.Not.Empty);
                Assert.That(level.SetupLineFor(2), Is.Empty);
                Assert.That(level.ClearedTitleFor(3), Is.EqualTo("STAGE 3 CLEAR"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }
    }
}
