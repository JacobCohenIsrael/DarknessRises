using System;
using DarknessRises.Game.Lightning;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Lightning
{
    public sealed class WarningVoiceTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void HoldsItsVolumeUntilTheFlash_ThenFadesOverTheFadeTime()
        {
            var voice = new WarningVoice();
            voice.Start(0.8f, 1f, 2f);

            voice.Tick(0.5f);
            Assert.That(voice.Volume, Is.EqualTo(0.8f).Within(Tolerance));

            voice.Tick(1.5f);
            Assert.That(voice.Volume, Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(voice.IsPlaying, Is.True);

            voice.Tick(1f);
            Assert.That(voice.Volume, Is.EqualTo(0f));
            Assert.That(voice.IsPlaying, Is.False);
        }

        [Test]
        public void FadingOutEarly_FadesFromTheCurrentVolumeInsteadOfStopping()
        {
            var voice = new WarningVoice();
            voice.Start(1f, 2f, 4f);
            voice.Tick(0.5f);

            voice.FadeOut(0.5f);
            Assert.That(voice.IsPlaying, Is.True);
            Assert.That(voice.Volume, Is.EqualTo(1f).Within(Tolerance));

            voice.Tick(0.25f);
            Assert.That(voice.Volume, Is.EqualTo(0.5f).Within(Tolerance));

            voice.Tick(0.25f);
            Assert.That(voice.IsPlaying, Is.False);
        }

        [Test]
        public void FadingOutEarly_NeverMakesAnAlmostSilentWarningLast()
        {
            var voice = new WarningVoice();
            voice.Start(1f, 0f, 1f);
            voice.Tick(0.75f);

            voice.FadeOut(2f);
            voice.Tick(0.25f);

            Assert.That(voice.IsPlaying, Is.False);
        }

        [Test]
        public void ASilentVoiceIgnoresFadesAndTicks()
        {
            var voice = new WarningVoice();
            voice.FadeOut(1f);
            voice.Tick(1f);

            Assert.That(voice.IsPlaying, Is.False);
            Assert.That(voice.Volume, Is.EqualTo(0f));
        }

        [Test]
        public void ANewWarning_TakesASilentVoiceFirst()
        {
            var voices = new[] { new WarningVoice(), new WarningVoice() };
            voices[0].Start(1f, 1f, 1f);

            Assert.That(WarningVoice.PickFor(voices), Is.EqualTo(1));
        }

        [Test]
        public void ANewWarning_WithEveryVoiceBusy_TakesTheQuietest()
        {
            var voices = new[] { new WarningVoice(), new WarningVoice() };
            voices[0].Start(1f, 0f, 2f);
            voices[0].Tick(1.5f);
            voices[1].Start(1f, 1f, 2f);

            Assert.That(WarningVoice.PickFor(voices), Is.EqualTo(0));
        }

        [Test]
        public void NegativeTimes_AreRejected()
        {
            var voice = new WarningVoice();
            Assert.Throws<ArgumentOutOfRangeException>(() => voice.Start(1f, -1f, 1f));
            Assert.Throws<ArgumentException>(() => WarningVoice.PickFor(Array.Empty<WarningVoice>()));
        }
    }
}
