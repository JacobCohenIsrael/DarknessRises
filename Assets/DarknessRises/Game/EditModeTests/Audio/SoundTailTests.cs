using System;
using DarknessRises.Game.Audio;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Audio
{
    public sealed class SoundTailTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void A_sound_lasts_the_clip_left_after_its_start_divided_by_pitch()
        {
            // Grenade3: 3.687 s, played from 0.45 s.
            Assert.That(SoundTail.Seconds(3.687f, 0.45f, 1f), Is.EqualTo(3.237f).Within(Tolerance));
            Assert.That(SoundTail.Seconds(3.687f, 0.45f, 2f), Is.EqualTo(1.6185f).Within(Tolerance));
            Assert.That(SoundTail.Seconds(3.687f, 0.45f, 0.5f), Is.EqualTo(6.474f).Within(Tolerance));
            Assert.That(SoundTail.Seconds(1f, 2f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void The_Grenade_bang_outlives_the_explosions_light()
        {
            var tail = new SoundTail();
            tail.Play(3.687f, 0.45f);

            // The light is gone at 0.9 s; the bang is still playing at 0.9 s, 2 s and 3 s.
            Tick(tail, 0.9f, 1f);
            Assert.That(tail.IsPlaying, Is.True);
            Tick(tail, 1.1f, 1f);
            Assert.That(tail.IsPlaying, Is.True);
            Tick(tail, 1f, 1f);
            Assert.That(tail.IsPlaying, Is.True);

            Tick(tail, 0.25f, 1f);
            Assert.That(tail.IsPlaying, Is.False);
        }

        [Test]
        public void A_lower_pitch_keeps_it_playing_longer()
        {
            var tail = new SoundTail();
            tail.Play(1f);
            tail.Tick(1.5f, 0.5f);

            Assert.That(tail.IsPlaying, Is.True);
            Assert.That(tail.ClipSecondsLeft, Is.EqualTo(0.25f).Within(Tolerance));

            tail.Tick(0.5f, 0.5f);
            Assert.That(tail.IsPlaying, Is.False);
        }

        [Test]
        public void Overlapping_sounds_last_as_long_as_the_longest()
        {
            var tail = new SoundTail();
            tail.Play(2f);
            tail.Tick(0.5f, 1f);
            tail.Play(0.5f);

            Assert.That(tail.ClipSecondsLeft, Is.EqualTo(1.5f).Within(Tolerance));

            tail.Play(3f);
            Assert.That(tail.ClipSecondsLeft, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void The_pause_holds_it()
        {
            var tail = new SoundTail();
            tail.Play(1f);
            tail.Tick(0f, 1f);
            tail.Tick(5f, 0f);

            Assert.That(tail.ClipSecondsLeft, Is.EqualTo(1f));
        }

        [Test]
        public void Clearing_forgets_every_sound()
        {
            var tail = new SoundTail();
            tail.Play(1f);
            tail.Clear();

            Assert.That(tail.IsPlaying, Is.False);
        }

        [Test]
        public void Rejects_backwards_time_and_pitch()
        {
            var tail = new SoundTail();
            Assert.Throws<ArgumentOutOfRangeException>(() => tail.Tick(-1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => tail.Tick(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => tail.Play(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => tail.Play(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => SoundTail.Seconds(1f, 0f, 0f));
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            var tail = new SoundTail();
            AllocationAssert.DoesNotAllocate(() =>
            {
                tail.Play(10f);
                for (var i = 0; i < 1000; i++)
                {
                    tail.Tick(1f / 60f, 1f);
                }
            });
        }

        // In frame-sized steps, as the game ticks it.
        private static void Tick(SoundTail tail, float seconds, float pitch)
        {
            const float frame = 1f / 60f;
            var steps = (int)Math.Round(seconds / frame);
            for (var i = 0; i < steps; i++)
            {
                tail.Tick(frame, pitch);
            }
        }
    }
}
