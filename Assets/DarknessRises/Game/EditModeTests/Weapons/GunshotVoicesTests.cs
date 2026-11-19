using System;
using DarknessRises.Game.Weapons;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Weapons
{
    public sealed class GunshotVoicesTests
    {
        private const float Frame = 1f / 60f;
        private const float Tolerance = 1e-4f;

        [Test]
        public void Each_Gunshot_takes_a_free_voice_so_they_overlap()
        {
            var voices = new GunshotVoices(3);
            var first = voices.Start(1f);
            var second = voices.Start(1f);
            var third = voices.Start(1f);

            Assert.That(new[] { first, second, third }, Is.Unique);
            Assert.That(voices.PlayingCount, Is.EqualTo(3));
            Assert.That(voices.StolenCount, Is.EqualTo(0));
        }

        [Test]
        public void With_every_voice_busy_the_oldest_is_cut_short()
        {
            var voices = new GunshotVoices(2);
            var oldest = voices.Start(1f);
            voices.Tick(0.1f);
            var newer = voices.Start(1f);
            voices.Tick(0.1f);

            var third = voices.Start(1f);

            Assert.That(third, Is.EqualTo(oldest));
            Assert.That(voices.StolenCount, Is.EqualTo(1));
            Assert.That(voices.SecondsLeft(newer), Is.EqualTo(0.9f).Within(Tolerance));
            Assert.That(voices.PlayingCount, Is.EqualTo(2));

            // And next time the one that's now oldest.
            Assert.That(voices.Start(1f), Is.EqualTo(newer));
        }

        [Test]
        public void A_free_voice_goes_to_the_one_free_longest()
        {
            var voices = new GunshotVoices(3);
            var a = voices.Start(0.1f);
            var b = voices.Start(0.2f);
            var c = voices.Start(0.3f);
            voices.Tick(0.5f);

            Assert.That(voices.Start(1f), Is.EqualTo(a));
            Assert.That(voices.Start(1f), Is.EqualTo(b));
            Assert.That(voices.Start(1f), Is.EqualTo(c));
        }

        [Test]
        public void A_voice_ends_after_its_Gunshot_fading_over_its_last_moment()
        {
            var voices = new GunshotVoices(1);
            var voice = voices.Start(0.5f);

            Assert.That(voices.Gain(voice, 0.1f), Is.EqualTo(1f));
            voices.Tick(0.45f);
            Assert.That(voices.Gain(voice, 0.1f), Is.EqualTo(0.5f).Within(Tolerance));
            voices.Tick(0.06f);
            Assert.That(voices.IsPlaying(voice), Is.False);
            Assert.That(voices.Gain(voice, 0.1f), Is.EqualTo(0f));
        }

        [Test]
        public void A_Machine_gun_burst_overlaps_within_the_cap_and_cuts_nothing_early()
        {
            // 20 Shots at 10 a second, each 0.4 s at the highest pitch 1.05.
            var voices = new GunshotVoices(6);
            var mostPlaying = 0;
            for (var shot = 0; shot < 20; shot++)
            {
                voices.Start(0.4f / 1.05f);
                mostPlaying = Math.Max(mostPlaying, voices.PlayingCount);
                for (var frame = 0; frame < 6; frame++)
                {
                    voices.Tick(Frame);
                }
            }

            Assert.That(mostPlaying, Is.GreaterThan(1));
            Assert.That(mostPlaying, Is.LessThanOrEqualTo(6));
            Assert.That(voices.StolenCount, Is.EqualTo(0));
        }

        [Test]
        public void Never_plays_more_than_its_voices()
        {
            var voices = new GunshotVoices(4);
            for (var shot = 0; shot < 50; shot++)
            {
                voices.Start(10f);
                Assert.That(voices.PlayingCount, Is.LessThanOrEqualTo(4));
            }

            Assert.That(voices.StolenCount, Is.EqualTo(46));
        }

        [Test]
        public void The_pause_holds_every_voice()
        {
            var voices = new GunshotVoices(2);
            var voice = voices.Start(0.3f);
            voices.Tick(0f);

            Assert.That(voices.SecondsLeft(voice), Is.EqualTo(0.3f));
        }

        [Test]
        public void Rejects_nonsense()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GunshotVoices(0));
            var voices = new GunshotVoices(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => voices.Start(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => voices.Tick(-1f));
        }

        [Test]
        public void Trims_stay_inside_the_clip()
        {
            GunshotClip.Trim(1.115f, 0.4395f, 0.4f, out var start, out var length);
            Assert.That(start, Is.EqualTo(0.4395f).Within(Tolerance));
            Assert.That(length, Is.EqualTo(0.4f).Within(Tolerance));

            GunshotClip.Trim(1.115f, 0.4395f, 0f, out start, out length);
            Assert.That(length, Is.EqualTo(0.6755f).Within(Tolerance));

            GunshotClip.Trim(1.115f, 1f, 0.4f, out start, out length);
            Assert.That(length, Is.EqualTo(0.115f).Within(Tolerance));

            GunshotClip.Trim(0.5f, 2f, 0.4f, out start, out length);
            Assert.That(start, Is.EqualTo(0.5f));
            Assert.That(length, Is.EqualTo(0f));
        }

        [Test]
        public void Firing_does_not_allocate()
        {
            var voices = new GunshotVoices(6);
            AllocationAssert.DoesNotAllocate(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    var voice = voices.Start(0.38f);
                    voices.Tick(Frame);
                    voices.Gain(voice, 0.04f);
                }
            });
        }
    }
}
