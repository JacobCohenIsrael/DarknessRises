using System;
using DarknessRises.Game.Storage;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Storage
{
    /// <summary>The keys the Web player uses in the browser's storage, which other games on the same site share.</summary>
    public sealed class BrowserKeysTests
    {
        [Test]
        public void The_saves_key_is_fixed_and_carries_the_games_name()
        {
            // Changing this key would strand every Web player's save: add a migration, never edit it.
            Assert.That(BrowserKeys.Save, Is.EqualTo("DarknessRises.save.v1"));
        }

        [Test]
        public void A_settings_key_carries_the_games_name()
        {
            Assert.That(BrowserKeys.ForSetting("volume.sound"), Is.EqualTo("DarknessRises.settings.volume.sound"));
        }

        [Test]
        public void A_name_that_already_carries_the_games_name_isnt_given_it_twice()
        {
            Assert.That(BrowserKeys.ForSetting("DarknessRises.ShootHint.Shown"), Is.EqualTo("DarknessRises.settings.ShootHint.Shown"));
            Assert.That(BrowserKeys.ForSetting("DarknessRises.GrenadeButton.X"), Is.EqualTo("DarknessRises.settings.GrenadeButton.X"));
        }

        [Test]
        public void No_key_is_ever_bare()
        {
            foreach (var key in new[] { BrowserKeys.Save, BrowserKeys.ForSetting("x"), BrowserKeys.ForSetting("DarknessRises.x") })
            {
                Assert.That(key, Does.StartWith(BrowserKeys.Namespace), key);
            }
        }

        [Test]
        public void A_setting_is_never_kept_under_the_saves_key()
        {
            Assert.That(BrowserKeys.ForSetting("save.v1"), Is.Not.EqualTo(BrowserKeys.Save));
            Assert.That(BrowserKeys.ForSetting("DarknessRises.save.v1"), Is.Not.EqualTo(BrowserKeys.Save));
        }

        [Test]
        public void A_setting_needs_a_name()
        {
            Assert.That(() => BrowserKeys.ForSetting(null), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => BrowserKeys.ForSetting(string.Empty), Throws.InstanceOf<ArgumentException>());
        }
    }
}
