using DarknessRises.Game.Hud;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Hud
{
    public sealed class VersionLabelTests
    {
        [Test]
        public void A_release_build_shows_the_version()
        {
            Assert.That(VersionLabel.Text("0.1.8", isDevelopmentBuild: false), Is.EqualTo("v0.1.8"));
        }

        [Test]
        public void A_development_build_adds_the_dev_suffix()
        {
            Assert.That(VersionLabel.Text("0.1.8", isDevelopmentBuild: true), Is.EqualTo("v0.1.8-dev"));
        }
    }
}
