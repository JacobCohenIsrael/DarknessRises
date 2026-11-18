using System;
using DarknessRises.Game.Audio;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Audio
{
    public sealed class SoundFadeTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Holds_its_volume_until_told_to_fade()
        {
            var fade = new SoundFade();
            fade.Start(0.8f);
            fade.Tick(5f);

            Assert.That(fade.IsAudible, Is.True);
            Assert.That(fade.IsFading, Is.False);
            Assert.That(fade.Volume, Is.EqualTo(0.8f));
        }

        [Test]
        public void An_interruption_fades_out_instead_of_cutting_dead()
        {
            var fade = new SoundFade();
            fade.Start(0.8f);
            fade.FadeOut(0.2f);

            Assert.That(fade.IsAudible, Is.True);
            Assert.That(fade.Volume, Is.EqualTo(0.8f));

            fade.Tick(0.1f);
            Assert.That(fade.IsAudible, Is.True);
            Assert.That(fade.Volume, Is.EqualTo(0.4f).Within(Tolerance));

            fade.Tick(0.1f);
            Assert.That(fade.IsAudible, Is.False);
            Assert.That(fade.Volume, Is.EqualTo(0f));
        }

        [Test]
        public void A_paused_fade_holds_where_it_was()
        {
            var fade = new SoundFade();
            fade.Start(1f);
            fade.FadeOut(0.2f);
            fade.Tick(0.05f);
            fade.Tick(0f);

            Assert.That(fade.Volume, Is.EqualTo(0.75f).Within(Tolerance));
            Assert.That(fade.IsAudible, Is.True);
        }

        [Test]
        public void A_second_interruption_does_not_restart_the_fade()
        {
            var fade = new SoundFade();
            fade.Start(1f);
            fade.FadeOut(0.2f);
            fade.Tick(0.1f);
            fade.FadeOut(1f);
            fade.Tick(0.1f);

            Assert.That(fade.IsAudible, Is.False);
        }

        [Test]
        public void A_new_sound_ends_a_fade_under_way()
        {
            var fade = new SoundFade();
            fade.Start(1f);
            fade.FadeOut(0.2f);
            fade.Tick(0.1f);
            fade.Start(0.6f);
            fade.Tick(1f);

            Assert.That(fade.IsFading, Is.False);
            Assert.That(fade.Volume, Is.EqualTo(0.6f));
        }

        [Test]
        public void A_silent_sound_has_nothing_to_fade()
        {
            var fade = new SoundFade();
            fade.FadeOut(0.2f);

            Assert.That(fade.IsAudible, Is.False);
            Assert.That(fade.IsFading, Is.False);
        }

        [Test]
        public void A_zero_fade_silences_at_once()
        {
            var fade = new SoundFade();
            fade.Start(1f);
            fade.FadeOut(0f);

            Assert.That(fade.IsAudible, Is.False);
            Assert.That(fade.Volume, Is.EqualTo(0f));
        }

        [Test]
        public void Rejects_negative_volumes_and_times()
        {
            var fade = new SoundFade();
            Assert.Throws<ArgumentOutOfRangeException>(() => fade.Start(-0.1f));
            fade.Start(1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => fade.FadeOut(-0.1f));
        }
    }
}
