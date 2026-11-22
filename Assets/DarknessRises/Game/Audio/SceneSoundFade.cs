using System;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// How loud a whole scene's own sounds are on their way out: 1 while the scene plays, falling to 0 over an authored
    /// time once it's told to fade out, before the scene goes. Every source of the scene plays at its own volume times
    /// its category's gain times this (<see cref="SceneAudioVolumes"/>), so a sound still playing when the scene unloads
    /// (a long one-shot like the Colossus's defeat, a footstep, a heartbeat loop) has already faded to silence instead of
    /// stopping dead (issue #121). A scene never fades back in: it is about to unload.
    /// Plain C# (ADR 0003).
    /// </summary>
    public sealed class SceneSoundFade
    {
        private float fadeSeconds;
        private float fadeFrom = 1f;
        private float fadedSeconds;

        /// <summary>The factor the scene's sounds play at: 1 until the fade starts, then down to 0.</summary>
        public float Level { get; private set; } = 1f;

        /// <summary>Whether the fade has started.</summary>
        public bool IsFading { get; private set; }

        /// <summary>Whether the scene's sounds have faded all the way to silence.</summary>
        public bool IsSilent => Level <= 0f;

        /// <summary>
        /// Starts fading the scene's sounds to silence over <paramref name="seconds"/>, from where they are. A shorter fade
        /// asked for during a longer one takes over from the current level, so the scene is silent by the earlier of the two.
        /// </summary>
        public void FadeOut(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds))
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A fade can't take negative time.");
            }

            if (IsFading && seconds >= fadeSeconds - fadedSeconds)
            {
                return;
            }

            IsFading = true;
            fadeFrom = Level;
            fadeSeconds = seconds;
            fadedSeconds = 0f;
            if (seconds <= 0f)
            {
                Level = 0f;
            }
        }

        /// <summary>Advances the fade by <paramref name="deltaSeconds"/>.</summary>
        /// <returns>Whether <see cref="Level"/> changed.</returns>
        public bool Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsFading || IsSilent)
            {
                return false;
            }

            fadedSeconds += deltaSeconds;
            Level = fadedSeconds >= fadeSeconds ? 0f : fadeFrom * (1f - fadedSeconds / fadeSeconds);
            return true;
        }
    }
}
