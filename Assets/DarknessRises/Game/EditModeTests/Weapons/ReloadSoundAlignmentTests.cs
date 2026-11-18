using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class ReloadSoundAlignmentTests
    {
        // The reload sequence clip is 1.96 s long; the Pistol reloads in 1.2 s, the Machine gun and Flamethrower in 2.5 s.
        private const float ClipSeconds = 1.96f;
        private const float Tolerance = 1e-5f;

        [Test]
        public void A_shorter_reload_starts_the_clip_partway_in_with_no_wait()
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, 1.2f, 1f);

            Assert.That(alignment.ClipStartSeconds, Is.EqualTo(0.76f).Within(Tolerance));
            Assert.That(alignment.DelaySeconds, Is.EqualTo(0f));
            Assert.That(alignment.EndSeconds, Is.EqualTo(1.2f).Within(Tolerance));
        }

        [Test]
        public void A_longer_reload_waits_then_plays_the_whole_clip()
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, 2.5f, 1f);

            Assert.That(alignment.ClipStartSeconds, Is.EqualTo(0f));
            Assert.That(alignment.DelaySeconds, Is.EqualTo(0.54f).Within(Tolerance));
            Assert.That(alignment.EndSeconds, Is.EqualTo(2.5f).Within(Tolerance));
        }

        [Test]
        public void An_equal_reload_starts_at_the_top_with_no_wait()
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, ClipSeconds, 1f);

            Assert.That(alignment.ClipStartSeconds, Is.EqualTo(0f));
            Assert.That(alignment.DelaySeconds, Is.EqualTo(0f));
            Assert.That(alignment.EndSeconds, Is.EqualTo(ClipSeconds).Within(Tolerance));
        }

        [TestCase(0.4f)]
        [TestCase(1.2f)]
        [TestCase(1.96f)]
        [TestCase(2.5f)]
        [TestCase(6f)]
        public void The_clip_ends_as_the_reload_finishes(float reloadSeconds)
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, reloadSeconds, 1f);

            Assert.That(alignment.TryClipTimeAt(reloadSeconds - 0.001f, out var nearEnd), Is.True);
            Assert.That(nearEnd, Is.EqualTo(ClipSeconds - 0.001f).Within(1e-4f));
            Assert.That(alignment.TryClipTimeAt(reloadSeconds, out _), Is.False);
        }

        [Test]
        public void Before_the_wait_is_over_there_is_nothing_to_play()
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, 2.5f, 1f);

            Assert.That(alignment.TryClipTimeAt(0f, out _), Is.False);
            Assert.That(alignment.TryClipTimeAt(0.5f, out _), Is.False);
            Assert.That(alignment.TryClipTimeAt(0.6f, out var clipTime), Is.True);
            Assert.That(clipTime, Is.EqualTo(0.06f).Within(Tolerance));
        }

        [Test]
        public void A_reload_that_starts_the_clip_partway_plays_from_there_at_once()
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, 1.2f, 1f);

            Assert.That(alignment.TryClipTimeAt(0f, out var atStart), Is.True);
            Assert.That(atStart, Is.EqualTo(0.76f).Within(Tolerance));
            Assert.That(alignment.TryClipTimeAt(0.5f, out var later), Is.True);
            Assert.That(later, Is.EqualTo(1.26f).Within(Tolerance));
        }

        [Test]
        public void A_higher_pitch_plays_the_clip_faster_and_still_ends_with_the_reload()
        {
            // At pitch 2 the 1.96 s clip takes 0.98 s of the reload.
            var shorter = ReloadSoundAlignment.For(ClipSeconds, 0.5f, 2f);
            Assert.That(shorter.ClipStartSeconds, Is.EqualTo(0.96f).Within(Tolerance));
            Assert.That(shorter.DelaySeconds, Is.EqualTo(0f));

            var longer = ReloadSoundAlignment.For(ClipSeconds, 1.2f, 2f);
            Assert.That(longer.DelaySeconds, Is.EqualTo(0.22f).Within(Tolerance));
            Assert.That(longer.TryClipTimeAt(1.1f, out var clipTime), Is.True);
            Assert.That(clipTime, Is.EqualTo(1.76f).Within(Tolerance));
        }

        [Test]
        public void An_instant_reload_plays_nothing()
        {
            var alignment = ReloadSoundAlignment.For(ClipSeconds, 0f, 1f);

            Assert.That(alignment.TryClipTimeAt(0f, out _), Is.False);
        }

        [Test]
        public void Rejects_impossible_inputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ReloadSoundAlignment.For(-1f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ReloadSoundAlignment.For(1f, -1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ReloadSoundAlignment.For(1f, 1f, 0f));
        }
    }
}
