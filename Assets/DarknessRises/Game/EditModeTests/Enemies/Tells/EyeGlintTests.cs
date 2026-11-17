using System;
using DarknessRises.Game.Enemies.Tells;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Enemies.Tells
{
    public sealed class EyeGlintTests
    {
        [TestCase(0f)]
        [TestCase(3f)]
        [TestCase(5.5f)]
        public void Eyes_glint_fully_well_within_range(float distance)
        {
            Assert.That(EyeGlint.Strength(distance, 7f, 1.5f), Is.EqualTo(1f));
        }

        [TestCase(7f)]
        [TestCase(7.01f)]
        [TestCase(40f)]
        public void Eyes_do_not_glint_at_or_beyond_range(float distance)
        {
            Assert.That(EyeGlint.Strength(distance, 7f, 1.5f), Is.EqualTo(0f));
        }

        [Test]
        public void Glint_fades_out_across_the_edge_of_the_range()
        {
            Assert.That(EyeGlint.Strength(6.25f, 7f, 1.5f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(EyeGlint.Strength(6.5f, 7f, 1.5f), Is.LessThan(EyeGlint.Strength(6f, 7f, 1.5f)));
        }

        [Test]
        public void Without_a_fade_the_glint_switches_at_the_range()
        {
            Assert.That(EyeGlint.Strength(6.99f, 7f, 0f), Is.EqualTo(1f));
            Assert.That(EyeGlint.Strength(7f, 7f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void A_fade_longer_than_the_range_fades_from_the_character_outwards()
        {
            Assert.That(EyeGlint.Strength(0f, 2f, 5f), Is.EqualTo(1f));
            Assert.That(EyeGlint.Strength(1f, 2f, 5f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void A_zero_range_never_glints()
        {
            Assert.That(EyeGlint.Strength(0f, 0f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void Negative_settings_are_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => EyeGlint.Strength(1f, -1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => EyeGlint.Strength(1f, 7f, -1f));
        }
    }
}
