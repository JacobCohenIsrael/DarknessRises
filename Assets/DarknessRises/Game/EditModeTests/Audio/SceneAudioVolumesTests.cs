using System.Collections.Generic;
using DarknessRises.Game.Audio;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Audio
{
    /// <summary>The boot's volumes follow the scene that is showing, so the sounds it carries across scenes obey its sliders (ADR 0011).</summary>
    public sealed class SceneAudioVolumesTests
    {
        private const float Tolerance = 1e-5f;
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
        public void Volumes_without_settings_or_a_scene_to_follow_play_at_full()
        {
            var boot = Make("Boot");

            Assert.That(boot.Volumes, Is.Null);
            Assert.That(boot.Gain(AudioCategory.Ambient), Is.EqualTo(1f));
        }

        [Test]
        public void Following_a_scene_plays_at_its_gains_and_moves_with_its_sliders()
        {
            var boot = Make("Boot");
            var level = Make("Level", out var levelVolumes);
            levelVolumes.Set(AudioCategory.Ambient, 30);

            boot.Follow(level);
            Assert.That(boot.Gain(AudioCategory.Ambient), Is.EqualTo(Curve.Gain(30)).Within(Tolerance));
            Assert.That(boot.Volumes, Is.SameAs(levelVolumes));

            levelVolumes.Set(AudioCategory.Ambient, 70);
            Assert.That(boot.Gain(AudioCategory.Ambient), Is.EqualTo(Curve.Gain(70)).Within(Tolerance));
        }

        [Test]
        public void Between_scenes_the_last_gains_are_kept()
        {
            var boot = Make("Boot");
            var level = Make("Level", out var levelVolumes);
            boot.Follow(level);
            levelVolumes.Set(AudioCategory.Ambient, 20);

            boot.Unfollow();
            levelVolumes.Set(AudioCategory.Ambient, 90);

            Assert.That(boot.Leader, Is.Null);
            Assert.That(boot.Gain(AudioCategory.Ambient), Is.EqualTo(Curve.Gain(20)).Within(Tolerance));
        }

        [Test]
        public void The_next_scene_takes_over_from_the_last()
        {
            var boot = Make("Boot");
            var level = Make("Level", out var levelVolumes);
            var shop = Make("Shop", out var shopVolumes);
            boot.Follow(level);
            boot.Follow(shop);
            shopVolumes.Set(AudioCategory.Ambient, 10);
            levelVolumes.Set(AudioCategory.Ambient, 60);

            Assert.That(boot.Leader, Is.SameAs(shop));
            Assert.That(boot.Gain(AudioCategory.Ambient), Is.EqualTo(Curve.Gain(10)).Within(Tolerance));
        }

        [Test]
        public void Playing_on_over_the_pause_follows_the_scene_and_stops_when_it_goes()
        {
            var boot = Make("Boot");
            var level = Make("Level", out _);
            boot.Follow(level);

            level.HearWhilePaused(AudioCategory.Ambient, true);
            Assert.That(boot.IsHeardWhilePaused(AudioCategory.Ambient), Is.True);
            Assert.That(boot.IsHeardWhilePaused(AudioCategory.Sound), Is.False);

            boot.Unfollow();
            Assert.That(boot.IsHeardWhilePaused(AudioCategory.Ambient), Is.False);

            level.HearWhilePaused(AudioCategory.Ambient, false);
            level.HearWhilePaused(AudioCategory.Ambient, true);
            Assert.That(boot.IsHeardWhilePaused(AudioCategory.Ambient), Is.False);
        }

        [Test]
        public void A_scene_already_playing_on_over_the_pause_is_followed_as_it_is()
        {
            var boot = Make("Boot");
            var level = Make("Level", out _);
            level.HearWhilePaused(AudioCategory.Ambient, true);

            boot.Follow(level);

            Assert.That(boot.IsHeardWhilePaused(AudioCategory.Ambient), Is.True);
        }

        [Test]
        public void Volumes_never_follow_themselves()
        {
            var boot = Make("Boot");

            Assert.Throws<System.ArgumentException>(() => boot.Follow(boot));
        }

        [Test]
        public void A_source_that_woke_before_its_scenes_volumes_follows_their_sliders_once_they_attach_it()
        {
            // Built into a scene that loads additively, a source wakes before it can find the scene's volumes.
            var storm = new GameObject("Storm Bed", typeof(AudioSource));
            made.Add(storm);
            var source = AudioCategorySource.Add(storm, AudioCategory.Ambient);
            source.Volume = 0.8f;
            Assert.That(source.Source.volume, Is.EqualTo(0.8f), "No volumes found yet: it plays at its own volume.");

            var scene = Make("Volumes", out var volumes);
            scene.AttachSources();
            Assert.That(source.Source.volume, Is.EqualTo(0.8f).Within(Tolerance));

            volumes.Set(AudioCategory.Ambient, 50);
            Assert.That(source.Source.volume, Is.EqualTo(0.8f * Curve.Gain(50)).Within(Tolerance), "It moves with the slider.");

            volumes.Set(AudioCategory.Ambient, 0);
            Assert.That(source.Source.volume, Is.EqualTo(0f), "Ambient at 0 silences it.");
        }

        private SceneAudioVolumes Make(string name) => Make(name, null);

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
