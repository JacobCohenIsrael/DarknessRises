using System;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Storage;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Storage
{
    /// <summary>
    /// Where the device settings are kept on each platform: the device's own place off the Web, the browser's storage
    /// first on the Web, so a new upload of the game still has the volume sliders and the one-time hints.
    /// </summary>
    public sealed class DeviceSettingsTests
    {
        private const string HintShown = "DarknessRises.ShootHint.Shown";

        // The browser's storage: one for the site, shared by every upload of the game.
        private MemoryKeyValueStorage browser;

        // An upload's own settings (PlayerPrefs on the Web player): kept under that upload's address.
        private MemoryDeviceSettings own;
        private IDeviceSettings settings;

        [SetUp]
        public void SetUp()
        {
            browser = new MemoryKeyValueStorage();
            own = new MemoryDeviceSettings();
            settings = DeviceSettings.Choose(browser, own);
        }

        [Test]
        public void Off_the_Web_the_settings_are_the_devices_own_and_nothing_else()
        {
            Assert.That(DeviceSettings.Choose(null, own), Is.SameAs(own));
        }

        [Test]
        public void On_the_Web_a_setting_is_kept_in_the_browsers_storage()
        {
            Assert.That(settings.SetInt(DeviceVolumeStorage.SoundKey, 60), Is.True);

            Assert.That(browser["DarknessRises.settings.volume.sound"], Is.EqualTo("60"));
            Assert.That(own.Count, Is.Zero, "The upload's own settings aren't needed.");
        }

        [Test]
        public void A_new_upload_finds_the_settings_of_the_last_one()
        {
            settings.SetInt(DeviceVolumeStorage.MusicKey, 35);
            settings.SetInt(HintShown, 1);
            settings.SetFloat("DarknessRises.GrenadeButton.X", 0.25f);

            // The next version is served from a new address: its own settings are empty.
            var nextUpload = DeviceSettings.Choose(browser, new MemoryDeviceSettings());

            Assert.That(nextUpload.TryGetInt(DeviceVolumeStorage.MusicKey, out var music), Is.True);
            Assert.That(music, Is.EqualTo(35));
            Assert.That(nextUpload.TryGetInt(HintShown, out var shown), Is.True);
            Assert.That(shown, Is.EqualTo(1));
            Assert.That(nextUpload.TryGetFloat("DarknessRises.GrenadeButton.X", out var x), Is.True);
            Assert.That(x, Is.EqualTo(0.25f));
        }

        [Test]
        public void A_setting_only_the_uploads_own_place_has_is_read_and_carried_over()
        {
            // An earlier build, played at this same address, kept it in PlayerPrefs.
            own.SetInt(DeviceVolumeStorage.SoundKey, 45);
            own.SetFloat("DarknessRises.GrenadeButton.Y", 0.75f);

            Assert.That(settings.TryGetInt(DeviceVolumeStorage.SoundKey, out var sound), Is.True);
            Assert.That(sound, Is.EqualTo(45));
            Assert.That(settings.TryGetFloat("DarknessRises.GrenadeButton.Y", out var y), Is.True);
            Assert.That(y, Is.EqualTo(0.75f));

            Assert.That(browser["DarknessRises.settings.volume.sound"], Is.EqualTo("45"));
            Assert.That(browser["DarknessRises.settings.GrenadeButton.Y"], Is.EqualTo("0.75"));
        }

        [Test]
        public void The_browsers_value_wins_over_an_older_one_in_the_uploads_own_place()
        {
            own.SetInt(DeviceVolumeStorage.SoundKey, 45);
            settings.SetInt(DeviceVolumeStorage.SoundKey, 90);

            Assert.That(settings.TryGetInt(DeviceVolumeStorage.SoundKey, out var sound), Is.True);
            Assert.That(sound, Is.EqualTo(90));
        }

        [Test]
        public void A_setting_nobody_has_isnt_made_up()
        {
            Assert.That(settings.TryGetInt(DeviceVolumeStorage.SoundKey, out _), Is.False);
            Assert.That(settings.TryGetFloat("DarknessRises.GrenadeButton.X", out _), Is.False);
            Assert.That(browser.Count, Is.Zero);
        }

        [Test]
        public void With_the_browsers_storage_blocked_settings_are_kept_in_the_uploads_own_place_as_before()
        {
            browser.IsBlocked = true;

            Assert.That(settings.SetInt(DeviceVolumeStorage.SoundKey, 70), Is.True);
            Assert.That(settings.SetFloat("DarknessRises.GrenadeButton.X", 0.4f), Is.True);

            Assert.That(own.Has(DeviceVolumeStorage.SoundKey), Is.True);
            Assert.That(settings.TryGetInt(DeviceVolumeStorage.SoundKey, out var sound), Is.True);
            Assert.That(sound, Is.EqualTo(70));
            Assert.That(settings.TryGetFloat("DarknessRises.GrenadeButton.X", out var x), Is.True);
            Assert.That(x, Is.EqualTo(0.4f));
        }

        [Test]
        public void With_the_browsers_storage_full_the_newer_value_is_the_one_read_next_time()
        {
            settings.SetInt(DeviceVolumeStorage.SoundKey, 90);
            browser.FailsWrites = true;

            Assert.That(settings.SetInt(DeviceVolumeStorage.SoundKey, 20), Is.True);

            Assert.That(browser["DarknessRises.settings.volume.sound"], Is.Null, "The older value is removed.");
            browser.FailsWrites = false;
            Assert.That(DeviceSettings.Choose(browser, own).TryGetInt(DeviceVolumeStorage.SoundKey, out var sound), Is.True);
            Assert.That(sound, Is.EqualTo(20));
        }

        [Test]
        public void When_nothing_can_be_kept_setting_says_so_and_what_was_kept_stays()
        {
            settings.SetInt(DeviceVolumeStorage.SoundKey, 90);
            browser.FailsWrites = true;
            own.FailsWrites = true;

            Assert.That(settings.SetInt(DeviceVolumeStorage.SoundKey, 20), Is.False);
            Assert.That(settings.SetFloat("DarknessRises.GrenadeButton.X", 0.4f), Is.False);

            Assert.That(browser["DarknessRises.settings.volume.sound"], Is.EqualTo("90"));
        }

        [Test]
        public void Removing_a_setting_forgets_it_everywhere()
        {
            own.SetInt(HintShown, 1);
            settings.SetInt(HintShown, 1);

            settings.Remove(HintShown);

            Assert.That(settings.TryGetInt(HintShown, out _), Is.False);
            Assert.That(own.Has(HintShown), Is.False);
        }

        [Test]
        public void While_every_setting_goes_to_the_browser_the_uploads_own_place_is_never_flushed()
        {
            settings.SetInt(DeviceVolumeStorage.SoundKey, 60);
            settings.TryGetInt(DeviceVolumeStorage.SoundKey, out _);

            settings.Flush();

            Assert.That(own.Flushes, Is.Zero, "Nothing was changed there, so there is nothing to put on disk.");
        }

        [Test]
        public void A_setting_that_fell_back_to_the_uploads_own_place_is_flushed_there_once()
        {
            browser.IsBlocked = true;
            settings.SetInt(DeviceVolumeStorage.SoundKey, 60);

            settings.Flush();
            settings.Flush();

            Assert.That(own.Flushes, Is.EqualTo(1));
        }

        [Test]
        public void A_removed_setting_is_flushed_from_the_uploads_own_place()
        {
            own.SetInt(HintShown, 1);

            settings.Remove(HintShown);
            settings.Flush();

            Assert.That(own.Flushes, Is.EqualTo(1));
        }

        [Test]
        public void The_volume_sliders_follow_the_game_to_a_new_upload()
        {
            var curve = new VolumeCurve(-40f);
            var defaults = new VolumeDefaults(sound: 100, ambient: 100, music: 80);
            var volumes = new AudioVolumes(curve, defaults, new DeviceVolumeStorage(settings));
            volumes.Set(AudioCategory.Music, 30);
            volumes.Save();

            var nextUpload = DeviceSettings.Choose(browser, new MemoryDeviceSettings());
            var reopened = new AudioVolumes(curve, defaults, new DeviceVolumeStorage(nextUpload));

            Assert.That(reopened.Percent(AudioCategory.Music), Is.EqualTo(30));
            Assert.That(reopened.Percent(AudioCategory.Sound), Is.EqualTo(100), "A slider never moved keeps its default.");
        }

        [Test]
        public void A_fallback_needs_both_places()
        {
            Assert.That(() => new FallbackDeviceSettings(null, own), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new FallbackDeviceSettings(own, null), Throws.InstanceOf<ArgumentNullException>());
            Assert.That(() => new DeviceVolumeStorage(null), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
