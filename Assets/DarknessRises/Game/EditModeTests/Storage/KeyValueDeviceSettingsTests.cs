using System;
using System.Globalization;
using System.Threading;
using DarknessRises.Game.Storage;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Storage
{
    /// <summary>Device settings as keyed text: what the Web player keeps in the browser's storage.</summary>
    public sealed class KeyValueDeviceSettingsTests
    {
        private MemoryKeyValueStorage storage;
        private KeyValueDeviceSettings settings;

        [SetUp]
        public void SetUp()
        {
            storage = new MemoryKeyValueStorage();
            settings = new KeyValueDeviceSettings(storage);
        }

        [Test]
        public void Nothing_is_kept_until_it_is_set()
        {
            Assert.That(settings.TryGetInt("volume.sound", out var percent), Is.False);
            Assert.That(percent, Is.Zero);
            Assert.That(settings.TryGetFloat("GrenadeButton.X", out var x), Is.False);
            Assert.That(x, Is.Zero);
        }

        [Test]
        public void A_whole_number_is_kept_under_its_namespaced_key_and_read_back()
        {
            Assert.That(settings.SetInt("volume.sound", 80), Is.True);

            Assert.That(storage["DarknessRises.settings.volume.sound"], Is.EqualTo("80"));
            Assert.That(settings.TryGetInt("volume.sound", out var percent), Is.True);
            Assert.That(percent, Is.EqualTo(80));
        }

        [Test]
        public void A_number_with_a_fraction_reads_back_exactly()
        {
            const float x = 0.123456789f;

            settings.SetFloat("GrenadeButton.X", x);

            Assert.That(settings.TryGetFloat("GrenadeButton.X", out var read), Is.True);
            Assert.That(read, Is.EqualTo(x));
        }

        [Test]
        public void Numbers_are_written_the_same_way_whatever_the_devices_language()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                // German writes one half as "0,5".
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                settings.SetFloat("GrenadeButton.X", 0.5f);
                Assert.That(storage["DarknessRises.settings.GrenadeButton.X"], Is.EqualTo("0.5"));

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                Assert.That(settings.TryGetFloat("GrenadeButton.X", out var read), Is.True);
                Assert.That(read, Is.EqualTo(0.5f));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Test]
        public void Text_that_isnt_a_number_counts_as_nothing_kept()
        {
            storage["DarknessRises.settings.volume.sound"] = "loud";
            storage["DarknessRises.settings.GrenadeButton.X"] = "NaN";
            storage["DarknessRises.settings.GrenadeButton.Y"] = string.Empty;

            Assert.That(settings.TryGetInt("volume.sound", out _), Is.False);
            Assert.That(settings.TryGetFloat("GrenadeButton.X", out _), Is.False);
            Assert.That(settings.TryGetFloat("GrenadeButton.Y", out _), Is.False);
        }

        [Test]
        public void Removing_a_setting_forgets_it_and_no_other()
        {
            settings.SetInt("volume.sound", 80);
            settings.SetInt("volume.music", 40);

            settings.Remove("volume.sound");

            Assert.That(settings.TryGetInt("volume.sound", out _), Is.False);
            Assert.That(settings.TryGetInt("volume.music", out var music), Is.True);
            Assert.That(music, Is.EqualTo(40));
        }

        [Test]
        public void Storage_that_is_full_or_blocked_says_so_and_never_throws()
        {
            settings.SetInt("volume.sound", 80);
            storage.FailsWrites = true;
            Assert.That(settings.SetInt("volume.sound", 20), Is.False);
            Assert.That(settings.TryGetInt("volume.sound", out var kept), Is.True);
            Assert.That(kept, Is.EqualTo(80));

            storage.IsBlocked = true;
            Assert.That(settings.TryGetInt("volume.sound", out _), Is.False);
            Assert.That(settings.SetFloat("GrenadeButton.X", 0.5f), Is.False);
            Assert.That(() => settings.Remove("volume.sound"), Throws.Nothing);
            Assert.That(() => settings.Flush(), Throws.Nothing);
        }

        [Test]
        public void It_needs_a_storage()
        {
            Assert.That(() => new KeyValueDeviceSettings(null), Throws.InstanceOf<ArgumentNullException>());
        }
    }
}
