using System.Collections.Generic;
using DarknessRises.Game.Haptics;
using DarknessRises.Game.Storage;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Haptics
{
    public sealed class HapticsPlayerTests
    {
        private HapticsDefinition definition;
        private SilentHapticDevice device;
        private MemoryDeviceSettings settings;
        private HapticsPlayer haptics;
        private readonly List<(HapticEffect effect, HapticDrop reason)> drops = new();
        private readonly List<HapticEffect> played = new();

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<HapticsDefinition>();
            device = new SilentHapticDevice(true);
            settings = new MemoryDeviceSettings();
            Make();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
        }

        private void Make()
        {
            haptics = new HapticsPlayer(definition, device, settings);
            drops.Clear();
            played.Clear();
            haptics.Played += (effect, _) => played.Add(effect);
            haptics.Dropped += (effect, reason) => drops.Add((effect, reason));
        }

        private void Set(string property, float value)
        {
            var serialized = new SerializedObject(definition);
            var found = serialized.FindProperty(property);
            if (found.propertyType == SerializedPropertyType.Integer)
            {
                found.intValue = (int)value;
            }
            else
            {
                found.floatValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private int Priority(HapticEffect effect) => definition.PatternOf(effect).Priority;

        private HapticDrop LastDrop => drops[drops.Count - 1].reason;

        [Test]
        public void Every_effect_has_an_authored_pattern_that_plays_something()
        {
            foreach (var effect in HapticsDefinition.Effects)
            {
                var pattern = definition.PatternOf(effect);
                Assert.That(pattern.IsEmpty, Is.False, effect.ToString());
                Assert.That(pattern.TotalMs, Is.GreaterThan(0), effect.ToString());
            }
        }

        [Test]
        public void The_authored_effects_rank_as_the_issue_asks()
        {
            Assert.That(Priority(HapticEffect.EnemyKilled), Is.GreaterThan(Priority(HapticEffect.EnemyHit)), "a kill is stronger than a hit");
            Assert.That(Priority(HapticEffect.BossStomp), Is.GreaterThan(Priority(HapticEffect.GrenadeExplosion)));
            Assert.That(Priority(HapticEffect.WeaponUnlocked), Is.GreaterThan(Priority(HapticEffect.Bought)), "unlocking a Weapon is bigger");
            Assert.That(definition.PatternOf(HapticEffect.StationClearS).TotalMs,
                Is.GreaterThan(definition.PatternOf(HapticEffect.StationClear).TotalMs), "a Grade S is a little longer");
            foreach (var effect in HapticsDefinition.Effects)
            {
                if (effect != HapticEffect.LightningNear && effect != HapticEffect.ButtonTap)
                {
                    Assert.That(Priority(HapticEffect.LightningNear), Is.LessThan(Priority(effect)), $"Lightning is under {effect}");
                }
            }
        }

        [Test]
        public void Every_effect_is_prepared_on_the_device_once_at_every_strength()
        {
            Assert.That(device.Prepared, Is.EqualTo(HapticsDefinition.Effects.Length * definition.StrengthSteps));
            Assert.That(haptics.Waveforms.Length, Is.EqualTo(device.Prepared));
        }

        [Test]
        public void An_effect_plays_its_own_waveform_at_full_strength()
        {
            haptics.Play(HapticEffect.GrenadeExplosion);

            Assert.That(played, Is.EqualTo(new[] { HapticEffect.GrenadeExplosion }));
            var steps = definition.StrengthSteps;
            Assert.That(device.LastPlayed, Is.EqualTo((int)HapticEffect.GrenadeExplosion * steps + steps - 1));
            Assert.That(haptics.IsPlaying, Is.True);
        }

        [Test]
        public void A_weaker_strength_plays_the_nearest_step_at_or_above_it()
        {
            Assert.That(haptics.StepOf(1f), Is.EqualTo(3));
            Assert.That(haptics.StepOf(0.75f), Is.EqualTo(2));
            Assert.That(haptics.StepOf(0.6f), Is.EqualTo(2));
            Assert.That(haptics.StepOf(0.25f), Is.EqualTo(0));
            Assert.That(haptics.StepOf(0.01f), Is.EqualTo(0));
            Assert.That(haptics.StepOf(0f), Is.EqualTo(0));

            haptics.Play(HapticEffect.Hurt, 0.5f);

            Assert.That(device.LastPlayed, Is.EqualTo((int)HapticEffect.Hurt * 4 + 1));
        }

        [Test]
        public void Hurt_grows_with_the_damage()
        {
            Assert.That(definition.HurtStrength(0), Is.Zero);
            Assert.That(definition.HurtStrength(2), Is.GreaterThan(definition.HurtStrength(1)));
            Assert.That(definition.HurtStrength(99), Is.EqualTo(1f));
        }

        [Test]
        public void A_repeat_waits_for_its_cooldown()
        {
            Set("enemyHit.cooldownSeconds", 0.08f);
            Make();

            haptics.Play(HapticEffect.EnemyHit);
            haptics.Tick(0.05f);
            haptics.Play(HapticEffect.EnemyHit);
            haptics.Tick(0.05f);
            haptics.Play(HapticEffect.EnemyHit);

            Assert.That(played.Count, Is.EqualTo(2));
            Assert.That(drops, Is.EqualTo(new[] { (HapticEffect.EnemyHit, HapticDrop.Cooldown) }));
        }

        [Test]
        public void A_weaker_effect_is_dropped_while_a_stronger_one_plays()
        {
            haptics.Play(HapticEffect.BossStomp);
            haptics.Tick(0.2f);
            haptics.Play(HapticEffect.EnemyHit);

            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Weaker));
            Assert.That(device.Plays, Is.EqualTo(1));

            haptics.Tick(1f);
            haptics.Play(HapticEffect.EnemyHit);

            Assert.That(device.Plays, Is.EqualTo(2), "once the stomp is over, a hit plays");
        }

        [Test]
        public void A_stronger_effect_cuts_a_weaker_one_and_skips_the_gap()
        {
            haptics.Play(HapticEffect.EnemyKilled);
            haptics.Play(HapticEffect.GrenadeExplosion);

            Assert.That(played, Is.EqualTo(new[] { HapticEffect.EnemyKilled, HapticEffect.GrenadeExplosion }));
            Assert.That(device.LastPlayed / definition.StrengthSteps, Is.EqualTo((int)HapticEffect.GrenadeExplosion));
        }

        [Test]
        public void Any_other_effect_waits_for_the_minimum_gap()
        {
            haptics.Play(HapticEffect.EnemyKilled);
            haptics.Tick(definition.MinGapSeconds / 2f);
            haptics.Play(HapticEffect.EnemyHit);

            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Weaker).Or.EqualTo(HapticDrop.TooSoon));

            haptics.Play(HapticEffect.ComboUp);
            Assert.That(device.Plays, Is.EqualTo(1));

            // The same strength, after the kill's pulse is over but within the gap.
            Set("minGapSeconds", 1f);
            Make();
            haptics.Play(HapticEffect.ReloadDone);
            haptics.Tick(0.5f);
            haptics.Play(HapticEffect.WeaponSwitch);

            Assert.That(LastDrop, Is.EqualTo(HapticDrop.TooSoon));
        }

        [Test]
        public void No_more_than_the_cap_start_in_a_second()
        {
            Set("maxPerSecond", 3);
            Set("enemyHit.cooldownSeconds", 0f);
            Set("minGapSeconds", 0f);
            Make();

            for (var i = 0; i < 10; i++)
            {
                haptics.Play(HapticEffect.EnemyHit);
                haptics.Tick(0.1f);
            }

            Assert.That(played.Count, Is.EqualTo(3), "three in the first second, the rest over the cap");
            Assert.That(LastDrop, Is.EqualTo(HapticDrop.OverCap));

            haptics.Tick(0.15f);
            haptics.Play(HapticEffect.EnemyHit);

            Assert.That(played.Count, Is.EqualTo(4), "a second after the first, there's room again");
        }

        [Test]
        public void A_stronger_effect_skips_the_cap()
        {
            Set("maxPerSecond", 2);
            Set("enemyHit.cooldownSeconds", 0f);
            Make();

            haptics.Play(HapticEffect.EnemyHit);
            haptics.Tick(0.1f);
            haptics.Play(HapticEffect.EnemyHit);
            haptics.Tick(0.1f);
            haptics.Play(HapticEffect.GrenadeExplosion);

            Assert.That(played, Is.EqualTo(new[] { HapticEffect.EnemyHit, HapticEffect.EnemyHit, HapticEffect.GrenadeExplosion }));
        }

        [Test]
        public void An_ambient_effect_waits_for_a_quiet_moment()
        {
            haptics.Play(HapticEffect.EnemyHit);
            haptics.Tick(0.5f);
            haptics.Play(HapticEffect.LightningNear);

            Assert.That(LastDrop, Is.EqualTo(HapticDrop.NotQuiet));

            haptics.Tick(definition.PatternOf(HapticEffect.LightningNear).QuietBeforeSeconds);
            haptics.Play(HapticEffect.LightningNear);

            Assert.That(played, Has.Member(HapticEffect.LightningNear));
        }

        [Test]
        public void Nothing_plays_while_held_and_a_hold_stops_what_plays()
        {
            haptics.Play(HapticEffect.BossDefeated);
            haptics.SetHeld(HapticsHold.Paused, true);

            Assert.That(device.Cancels, Is.EqualTo(1), "the pause cancels the running pattern");
            Assert.That(haptics.IsPlaying, Is.False);

            haptics.Tick(5f);
            haptics.Play(HapticEffect.Death);
            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Held));

            // Another hold is let go on its own: still held by the pause.
            haptics.SetHeld(HapticsHold.Loading, true);
            haptics.SetHeld(HapticsHold.Loading, false);
            haptics.Play(HapticEffect.Death);
            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Held));

            haptics.SetHeld(HapticsHold.Paused, false);
            haptics.Play(HapticEffect.Death);
            Assert.That(played, Is.EqualTo(new[] { HapticEffect.BossDefeated, HapticEffect.Death }));
        }

        [Test]
        public void Holding_while_nothing_plays_cancels_nothing()
        {
            haptics.SetHeld(HapticsHold.Ad, true);

            Assert.That(device.Cancels, Is.Zero);
        }

        [Test]
        public void Off_means_nothing_plays()
        {
            haptics.Play(HapticEffect.Hurt);
            haptics.SetOn(false);

            Assert.That(device.Cancels, Is.EqualTo(1), "turning it off stops what plays");
            foreach (var effect in HapticsDefinition.Effects)
            {
                haptics.Play(effect);
                Assert.That(LastDrop, Is.EqualTo(HapticDrop.Off), effect.ToString());
            }

            Assert.That(device.Plays, Is.EqualTo(1));
        }

        [Test]
        public void The_switch_is_on_by_default_and_survives_a_restart()
        {
            Assert.That(haptics.IsOn, Is.True);

            haptics.SetOn(false);
            Assert.That(settings.Flushes, Is.EqualTo(1), "kept on disk at once");

            // An app restart: new Haptics over the same device settings.
            Make();
            Assert.That(haptics.IsOn, Is.False);
            haptics.Play(HapticEffect.GrenadeExplosion);
            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Off));

            haptics.SetOn(true);
            Make();
            Assert.That(haptics.IsOn, Is.True);
        }

        [Test]
        public void The_switch_is_kept_in_the_device_settings_under_its_name()
        {
            haptics.SetOn(false);

            Assert.That(settings.TryGetInt(HapticsPlayer.OnSetting, out var on), Is.True);
            Assert.That(on, Is.Zero);
        }

        [Test]
        public void A_device_that_cant_vibrate_plays_nothing_and_hides_the_switch()
        {
            device = new SilentHapticDevice(false);
            Make();

            haptics.Play(HapticEffect.GrenadeExplosion);

            Assert.That(haptics.IsAvailable, Is.False);
            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Unsupported));
            Assert.That(device.Prepared, Is.Zero, "nothing is prepared for a motor that isn't there");
        }

        [Test]
        public void Button_ticks_play_only_when_they_are_on()
        {
            haptics.Play(HapticEffect.ButtonTap);
            Assert.That(LastDrop, Is.EqualTo(HapticDrop.Unauthored));

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("tickOnButtons").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Make();
            haptics.Play(HapticEffect.ButtonTap);

            Assert.That(played, Is.EqualTo(new[] { HapticEffect.ButtonTap }));
        }

        [Test]
        public void A_machine_gun_fight_is_rate_limited_not_one_long_buzz()
        {
            // Thirty Shots a second hitting for three seconds, with a kill every half second.
            for (var frame = 0; frame < 180; frame++)
            {
                if (frame % 2 == 0)
                {
                    haptics.Play(HapticEffect.EnemyHit);
                }

                if (frame % 30 == 15)
                {
                    haptics.Play(HapticEffect.EnemyKilled);
                }

                haptics.Tick(1f / 60f);
            }

            Assert.That(played.Count, Is.LessThanOrEqualTo(3 * definition.MaxPerSecond));
            Assert.That(played, Has.Member(HapticEffect.EnemyKilled));
            Assert.That(drops.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Playing_an_effect_allocates_nothing()
        {
            Set("enemyHit.cooldownSeconds", 0f);
            Set("minGapSeconds", 0f);

            // Without the test's own listeners, whose lists grow.
            var quiet = new HapticsPlayer(definition, device, settings);
            AllocationAssert.DoesNotAllocate(() =>
            {
                quiet.Tick(0.5f);
                quiet.Play(HapticEffect.EnemyHit);
                quiet.Play(HapticEffect.EnemyHit);
                quiet.Play(HapticEffect.Hurt, 0.4f);
                quiet.SetHeld(HapticsHold.Paused, true);
                quiet.Play(HapticEffect.GrenadeExplosion);
                quiet.SetHeld(HapticsHold.Paused, false);
            });
        }
    }
}
