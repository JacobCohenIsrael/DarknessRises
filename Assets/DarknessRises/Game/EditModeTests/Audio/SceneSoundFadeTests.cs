using System;
using System.Collections.Generic;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Boot;
using DarknessRises.Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarknessRises.Game.EditModeTests.Audio
{
    /// <summary>
    /// A scene's sounds fade to silence before it unloads, so nothing in it stops dead there and nothing is loud over the
    /// end of a Level (issue #121): the fade itself, the scene's sources following it, and the check that names any that
    /// would still be cut.
    /// </summary>
    public sealed class SceneSoundFadeTests
    {
        private const float Tolerance = 1e-5f;
        private const string BootDefinitionPath = "Assets/DarknessRises/Boot/BootDefinition.asset";
        private const string LevelDefinitionPath = "Assets/DarknessRises/Configuration/Levels/Level1.asset";
        private static readonly VolumeCurve Curve = new(-40f);
        private static readonly VolumeDefaults Defaults = new(sound: 100, ambient: 100, music: 80);

        private readonly List<GameObject> made = new();

        [TearDown]
        public void DestroyMade()
        {
            foreach (var gameObject in made)
            {
                Object.DestroyImmediate(gameObject);
            }

            made.Clear();
        }

        [Test]
        public void A_scene_plays_at_full_until_it_fades()
        {
            var fade = new SceneSoundFade();

            Assert.That(fade.Tick(10f), Is.False);
            Assert.That(fade.Level, Is.EqualTo(1f));
            Assert.That(fade.IsFading, Is.False);
            Assert.That(fade.IsSilent, Is.False);
        }

        [Test]
        public void Fading_out_falls_evenly_to_silence_over_the_time_and_stays_there()
        {
            var fade = new SceneSoundFade();
            fade.FadeOut(2f);

            Assert.That(fade.Tick(0.5f), Is.True);
            Assert.That(fade.Level, Is.EqualTo(0.75f).Within(Tolerance));
            fade.Tick(1f);
            Assert.That(fade.Level, Is.EqualTo(0.25f).Within(Tolerance));
            fade.Tick(1f);
            Assert.That(fade.Level, Is.EqualTo(0f));
            Assert.That(fade.IsSilent, Is.True);
            Assert.That(fade.Tick(1f), Is.False, "Silent stays silent: the scene is about to go.");
        }

        [Test]
        public void A_shorter_fade_takes_over_from_where_the_longer_one_is()
        {
            var fade = new SceneSoundFade();
            fade.FadeOut(4f);
            fade.Tick(1f);

            fade.FadeOut(0.5f);
            fade.Tick(0.25f);

            Assert.That(fade.Level, Is.EqualTo(0.75f * 0.5f).Within(Tolerance));
            fade.Tick(0.25f);
            Assert.That(fade.IsSilent, Is.True);
        }

        [Test]
        public void A_longer_fade_never_slows_one_that_ends_sooner()
        {
            var fade = new SceneSoundFade();
            fade.FadeOut(1f);
            fade.Tick(0.5f);

            fade.FadeOut(5f);
            fade.Tick(0.5f);

            Assert.That(fade.IsSilent, Is.True);
        }

        [Test]
        public void A_zero_fade_silences_at_once()
        {
            var fade = new SceneSoundFade();
            fade.FadeOut(0f);

            Assert.That(fade.IsSilent, Is.True);
        }

        [Test]
        public void A_fade_cant_take_negative_time()
        {
            var fade = new SceneSoundFade();

            Assert.Throws<ArgumentOutOfRangeException>(() => fade.FadeOut(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => fade.Tick(-1f));
        }

        [Test]
        public void Every_source_of_a_fading_scene_plays_at_its_volume_times_its_gain_times_the_fade()
        {
            var scene = Make("Level", out var volumes);
            volumes.Set(AudioCategory.Sound, 50);
            var defeat = AddSource("Colossus", AudioCategory.Sound, 1f);
            var storm = AddSource("Storm Bed", AudioCategory.Ambient, 0.6f);
            scene.AttachSources();

            scene.FadeOut(0f);

            Assert.That(scene.Fade, Is.EqualTo(0f));
            Assert.That(scene.IsSilent, Is.True);
            Assert.That(defeat.Source.volume, Is.EqualTo(0f), "A one-shot still playing is silent before the scene goes.");
            Assert.That(storm.Source.volume, Is.EqualTo(0f));

            volumes.Set(AudioCategory.Sound, 100);
            Assert.That(defeat.Source.volume, Is.EqualTo(0f), "A slider moved during the fade doesn't bring it back.");

            defeat.Volume = 0.9f;
            Assert.That(defeat.Source.volume, Is.EqualTo(0f), "A sound started during the fade plays faded too.");
        }

        [Test]
        public void Until_it_fades_a_scene_plays_its_sources_at_their_gains()
        {
            var scene = Make("Level", out var volumes);
            volumes.Set(AudioCategory.Sound, 50);
            var defeat = AddSource("Colossus", AudioCategory.Sound, 0.8f);
            scene.AttachSources();

            Assert.That(scene.Fade, Is.EqualTo(1f));
            Assert.That(defeat.Source.volume, Is.EqualTo(0.8f * Curve.Gain(50)).Within(Tolerance));
        }

        [Test]
        public void The_boot_following_a_fading_Level_keeps_its_own_sounds_at_the_Levels_gains()
        {
            // The storm is the boot's: it outlives the Level and fades on its own (its hush), not with the Level's sounds.
            var boot = Make("Boot", null);
            var level = Make("Level", out var levelVolumes);
            levelVolumes.Set(AudioCategory.Ambient, 70);
            boot.Follow(level);

            level.FadeOut(0f);

            Assert.That(boot.Fade, Is.EqualTo(1f));
            Assert.That(boot.Gain(AudioCategory.Ambient), Is.EqualTo(Curve.Gain(70)).Within(Tolerance));
        }

        [Test]
        public void The_unload_check_names_each_source_still_playing_above_the_threshold()
        {
            var readings = new[]
            {
                new SceneUnloadAudit.Reading("Level/Colossus (Tsunami)", true, 1f),
                new SceneUnloadAudit.Reading("Level/Footstep Voice 1", true, 0.005f),
                new SceneUnloadAudit.Reading("Level/Reload Sound", false, 0.9f),
                new SceneUnloadAudit.Reading("Level/Ambient Bed Voice", true, 0.2f),
            };
            var cuts = new List<SceneUnloadAudit.Reading>();

            var found = SceneUnloadAudit.FindCuts(readings, 0.01f, cuts);

            Assert.That(found, Is.EqualTo(2));
            Assert.That(cuts.ConvertAll(cut => cut.Name), Is.EqualTo(new[] { "Level/Colossus (Tsunami)", "Level/Ambient Bed Voice" }));
        }

        [Test]
        public void After_a_scenes_fade_the_unload_check_finds_nothing_to_cut()
        {
            var scene = Make("Level", out _);
            var defeat = AddSource("Colossus", AudioCategory.Sound, 1f);
            scene.AttachSources();
            scene.FadeOut(0f);

            var readings = new[] { new SceneUnloadAudit.Reading(defeat.name, true, defeat.Source.volume) };

            Assert.That(SceneUnloadAudit.FindCuts(readings, 0f, new List<SceneUnloadAudit.Reading>()), Is.Zero);
        }

        [Test]
        public void The_authored_fades_are_silent_by_black_and_short_before_an_unload()
        {
            var end = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelDefinitionPath).End;
            var boot = AssetDatabase.LoadAssetAtPath<BootDefinition>(BootDefinitionPath);

            Assert.That(end.SoundFadeOutSeconds, Is.GreaterThan(0f), "The Level's sounds fade, never cut.");
            Assert.That(end.SoundFadeOutSeconds, Is.LessThanOrEqualTo(end.FadeOutSeconds), "Silent by the time the screen is black.");
            Assert.That(boot.SceneSoundFadeOutSeconds, Is.GreaterThan(0f), "A scene's sounds fade before it unloads, never cut.");
            Assert.That(boot.SceneSoundFadeOutSeconds, Is.LessThanOrEqualTo(1f), "A short fade: the player is waiting on it.");
        }

        private AudioCategorySource AddSource(string name, AudioCategory category, float volume)
        {
            var gameObject = new GameObject(name, typeof(AudioSource));
            made.Add(gameObject);
            var source = AudioCategorySource.Add(gameObject, category);
            source.Volume = volume;
            return source;
        }

        private SceneAudioVolumes Make(string name, out AudioVolumes volumes)
        {
            volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());
            return Make(name, volumes);
        }

        private SceneAudioVolumes Make(string name, AudioVolumes volumes)
        {
            var gameObject = new GameObject(name, typeof(SceneAudioVolumes));
            made.Add(gameObject);
            var sceneVolumes = gameObject.GetComponent<SceneAudioVolumes>();
            if (volumes != null)
            {
                sceneVolumes.UseVolumes(volumes);
            }

            return sceneVolumes;
        }
    }
}
