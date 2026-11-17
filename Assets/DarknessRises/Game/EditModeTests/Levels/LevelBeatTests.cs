using DarknessRises.Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Levels
{
    public sealed class LevelBeatTests
    {
        [Test]
        public void A_fade_rises_holds_and_falls()
        {
            Assert.That(BeatFade.InHoldOut(0f, 1f, 2f, 1f), Is.EqualTo(0f));
            Assert.That(BeatFade.InHoldOut(0.5f, 1f, 2f, 1f), Is.EqualTo(0.5f));
            Assert.That(BeatFade.InHoldOut(2f, 1f, 2f, 1f), Is.EqualTo(1f));
            Assert.That(BeatFade.InHoldOut(3.5f, 1f, 2f, 1f), Is.EqualTo(0.5f));
            Assert.That(BeatFade.InHoldOut(10f, 1f, 2f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void The_default_start_beat_shows_the_title_and_ends_in_Darkness_within_three_to_five_seconds()
        {
            var start = new LevelStartSettings();

            Assert.That(start.DurationSeconds, Is.InRange(3f, 5f));
            Assert.That(start.DoorLightIntensityAt(0f), Is.GreaterThan(0f));
            Assert.That(start.DoorLightIntensityAt(start.DurationSeconds / 2f), Is.LessThan(start.DoorLightIntensityAt(0f)));
            Assert.That(start.DoorLightIntensityAt(start.DurationSeconds), Is.EqualTo(0f));
            Assert.That(start.TitleAlpha(start.DurationSeconds / 2f), Is.EqualTo(1f));
            Assert.That(start.TitleAlpha(start.DurationSeconds), Is.EqualTo(0f));
        }

        [Test]
        public void The_default_end_beat_is_a_short_calm_then_a_fade_to_black()
        {
            var end = new LevelEndSettings();

            Assert.That(end.CalmSeconds + end.FadeOutSeconds, Is.InRange(3f, 5f));
            Assert.That(end.CalmLightIntensityAt(0f), Is.EqualTo(0f));
            Assert.That(end.CalmLightIntensityAt(end.CalmSeconds), Is.GreaterThan(0f));
            Assert.That(end.ScreenFadeAt(0f), Is.EqualTo(0f));
            Assert.That(end.ScreenFadeAt(end.FadeOutSeconds), Is.EqualTo(1f));
        }

        [Test]
        public void Stragglers_enter_only_ahead_of_the_character()
        {
            var character = new Vector3(5f, 0f, 0f);
            var east = Vector3.right;

            Assert.That(StragglerEntrances.IsAhead(new Vector3(9f, 0f, 3f), character, east, 2f), Is.True);
            Assert.That(StragglerEntrances.IsAhead(new Vector3(6f, 0f, 3f), character, east, 2f), Is.False, "Too close.");
            Assert.That(StragglerEntrances.IsAhead(new Vector3(1f, 0f, 0f), character, east, 2f), Is.False, "Behind.");
        }
    }
}
