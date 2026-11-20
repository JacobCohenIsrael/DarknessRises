using System;
using System.Collections.Generic;
using DarknessRises.Game.Audio;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Audio
{
    public sealed class AudioVolumesTests
    {
        private const float Tolerance = 1e-4f;
        private static readonly VolumeCurve Curve = new(-40f);
        private static readonly VolumeDefaults Defaults = new(sound: 100, ambient: 90, music: 80);

        [Test]
        public void A_first_start_uses_the_authored_defaults()
        {
            var volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());

            Assert.That(volumes.Percent(AudioCategory.Sound), Is.EqualTo(100));
            Assert.That(volumes.Percent(AudioCategory.Ambient), Is.EqualTo(90));
            Assert.That(volumes.Percent(AudioCategory.Music), Is.EqualTo(80));
        }

        [Test]
        public void Each_category_plays_at_its_percent_on_the_curve()
        {
            var volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());
            volumes.Set(AudioCategory.Ambient, 0);
            volumes.Set(AudioCategory.Music, 50);

            Assert.That(volumes.Gain(AudioCategory.Sound), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(volumes.Gain(AudioCategory.Ambient), Is.EqualTo(0f));
            Assert.That(volumes.Gain(AudioCategory.Music), Is.EqualTo(Curve.Gain(50)).Within(Tolerance));
        }

        [Test]
        public void Settings_survive_a_restart()
        {
            var storage = new MemoryVolumeStorage();
            var before = new AudioVolumes(Curve, Defaults, storage);
            before.Set(AudioCategory.Sound, 35);
            before.Set(AudioCategory.Ambient, 0);
            before.Set(AudioCategory.Music, 64);
            before.Save();

            var after = new AudioVolumes(Curve, Defaults, storage);

            Assert.That(after.Percent(AudioCategory.Sound), Is.EqualTo(35));
            Assert.That(after.Percent(AudioCategory.Ambient), Is.EqualTo(0));
            Assert.That(after.Percent(AudioCategory.Music), Is.EqualTo(64));
        }

        [Test]
        public void Only_the_moved_slider_is_stored_the_rest_keep_following_their_defaults()
        {
            var storage = new MemoryVolumeStorage();
            new AudioVolumes(Curve, Defaults, storage).Set(AudioCategory.Sound, 20);

            Assert.That(storage.TryRead(AudioCategory.Sound, out var sound), Is.True);
            Assert.That(sound, Is.EqualTo(20));
            Assert.That(storage.TryRead(AudioCategory.Ambient, out _), Is.False);
            Assert.That(storage.TryRead(AudioCategory.Music, out _), Is.False);
        }

        [TestCase(-20, 0)]
        [TestCase(180, 100)]
        public void A_stored_percent_out_of_range_is_kept_to_the_slider(int stored, int expected)
        {
            var storage = new MemoryVolumeStorage();
            storage.Write(AudioCategory.Ambient, stored);

            var volumes = new AudioVolumes(Curve, Defaults, storage);

            Assert.That(volumes.Percent(AudioCategory.Ambient), Is.EqualTo(expected));
        }

        [TestCase(-1, 0)]
        [TestCase(101, 100)]
        public void Setting_out_of_range_is_kept_to_the_slider(int set, int expected)
        {
            var volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());
            volumes.Set(AudioCategory.Music, set);

            Assert.That(volumes.Percent(AudioCategory.Music), Is.EqualTo(expected));
        }

        [Test]
        public void Defaults_are_kept_to_the_slider_too()
        {
            var volumes = new AudioVolumes(Curve, new VolumeDefaults(sound: 140, ambient: -3, music: 50), new MemoryVolumeStorage());

            Assert.That(volumes.Percent(AudioCategory.Sound), Is.EqualTo(100));
            Assert.That(volumes.Percent(AudioCategory.Ambient), Is.EqualTo(0));
        }

        [Test]
        public void A_change_is_reported_once_with_its_category()
        {
            var volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());
            var changed = new List<AudioCategory>();
            volumes.Changed += changed.Add;

            Assert.That(volumes.Set(AudioCategory.Ambient, 40), Is.True);
            Assert.That(volumes.Set(AudioCategory.Ambient, 40), Is.False);
            Assert.That(volumes.Set(AudioCategory.Sound, 100), Is.False);
            Assert.That(volumes.Set(AudioCategory.Sound, 101), Is.False);

            Assert.That(changed, Is.EqualTo(new[] { AudioCategory.Ambient }));
        }

        [Test]
        public void Saving_writes_to_disk_only_after_a_change()
        {
            var storage = new MemoryVolumeStorage();
            var volumes = new AudioVolumes(Curve, Defaults, storage);
            volumes.Save();
            Assert.That(storage.Flushes, Is.EqualTo(0));

            volumes.Set(AudioCategory.Sound, 10);
            volumes.Set(AudioCategory.Sound, 12);
            volumes.Save();
            volumes.Save();

            Assert.That(storage.Flushes, Is.EqualTo(1));
        }

        [Test]
        public void An_unknown_category_is_refused()
        {
            var volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());

            Assert.Throws<ArgumentOutOfRangeException>(() => volumes.Set((AudioCategory)7, 50));
        }

        [Test]
        public void Moving_a_slider_and_reading_the_gains_allocates_nothing()
        {
            var volumes = new AudioVolumes(Curve, Defaults, new MemoryVolumeStorage());
            var heard = 0f;
            volumes.Changed += category => heard += volumes.Gain(category);
            var percent = 0;

            AllocationAssert.DoesNotAllocate(() =>
            {
                percent = (percent + 7) % 101;
                volumes.Set(AudioCategory.Sound, percent);
                heard += volumes.Gain(AudioCategory.Ambient) + volumes.Gain(AudioCategory.Music);
            });
        }

        [Test]
        public void Each_category_has_its_own_device_key()
        {
            Assert.That(DeviceVolumeStorage.KeyFor(AudioCategory.Sound), Is.EqualTo("volume.sound"));
            Assert.That(DeviceVolumeStorage.KeyFor(AudioCategory.Ambient), Is.EqualTo("volume.ambient"));
            Assert.That(DeviceVolumeStorage.KeyFor(AudioCategory.Music), Is.EqualTo("volume.music"));
        }
    }
}
