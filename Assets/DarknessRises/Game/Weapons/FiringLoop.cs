using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Where in its clip a Firing loop plays, and how it fades. Every time is in clip seconds; the loop plays at pitch 1.
    /// </summary>
    public readonly struct FiringLoopTiming
    {
        /// <exception cref="ArgumentOutOfRangeException">Times that don't fit the clip or each other.</exception>
        public FiringLoopTiming(
            float clipLengthSeconds,
            float startSeconds,
            float loopStartSeconds,
            float loopEndSeconds,
            float crossfadeSeconds,
            float fadeInSeconds,
            float fadeOutSeconds,
            float stopAfterSeconds)
        {
            if (crossfadeSeconds < 0f || fadeInSeconds < 0f || fadeOutSeconds < 0f || stopAfterSeconds < 0f || startSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(crossfadeSeconds), "A Firing loop's times can't be negative.");
            }

            if (loopEndSeconds > clipLengthSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(loopEndSeconds), loopEndSeconds, $"The loop ends past the clip's end ({clipLengthSeconds} s).");
            }

            if (loopStartSeconds < 0f || loopEndSeconds - loopStartSeconds < 2f * crossfadeSeconds || loopEndSeconds <= loopStartSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(loopStartSeconds), loopStartSeconds,
                    "The looped part must be longer than two crossfades, so each pass is heard on its own.");
            }

            if (startSeconds >= loopEndSeconds - crossfadeSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(startSeconds), startSeconds, "The first pass must start before the first crossfade.");
            }

            ClipLengthSeconds = clipLengthSeconds;
            StartSeconds = startSeconds;
            LoopStartSeconds = loopStartSeconds;
            LoopEndSeconds = loopEndSeconds;
            CrossfadeSeconds = crossfadeSeconds;
            FadeInSeconds = fadeInSeconds;
            FadeOutSeconds = fadeOutSeconds;
            StopAfterSeconds = stopAfterSeconds;
        }

        public float ClipLengthSeconds { get; }

        /// <summary>Where the first pass starts when firing starts.</summary>
        public float StartSeconds { get; }

        public float LoopStartSeconds { get; }
        public float LoopEndSeconds { get; }

        /// <summary>How long one pass fades into the next, ending at <see cref="LoopEndSeconds"/>.</summary>
        public float CrossfadeSeconds { get; }

        public float FadeInSeconds { get; }
        public float FadeOutSeconds { get; }

        /// <summary>Seconds without a Shot after which the loop fades out by itself, as when the Weapon is put away.</summary>
        public float StopAfterSeconds { get; }
    }

    /// <summary>
    /// The sound of a Weapon that fires continuously, such as the Flamethrower: one loop that starts with the first Shot
    /// and fades out when firing stops, instead of a Gunshot per Shot. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// The loop plays the steady part of a longer clip on two voices: each pass crossfades into the next, started at the
    /// loop's start, before it reaches the loop's end, so no seek is ever heard and the build-up and tail outside the
    /// looped part are only heard when firing starts and stops. A view starts and stops its two AudioSources on
    /// <see cref="VoiceStarted"/> and <see cref="VoiceStopped"/> and applies <see cref="Volume"/> to them every frame.
    /// </remarks>
    public sealed class FiringLoop
    {
        /// <summary>Two voices: one pass fading out while the next fades in.</summary>
        public const int VoiceCount = 2;

        private const int None = -1;

        private readonly FiringLoopTiming timing;
        private readonly bool[] playing = new bool[VoiceCount];
        private readonly float[] clipSeconds = new float[VoiceCount];
        private readonly float[] gain = new float[VoiceCount];
        private readonly float[] gainPerSecond = new float[VoiceCount];
        private int current = None;
        private float secondsSinceShot;

        public FiringLoop(FiringLoopTiming loopTiming)
        {
            timing = loopTiming;
        }

        /// <summary>A voice starts playing the clip from the given clip second.</summary>
        public event Action<int, float> VoiceStarted;

        /// <summary>A voice has faded out, or reached the clip's end, and stops.</summary>
        public event Action<int> VoiceStopped;

        /// <summary>Whether the Weapon is firing, so the loop is playing rather than fading out.</summary>
        public bool IsFiring { get; private set; }

        public bool IsPlaying(int voice)
        {
            return playing[voice];
        }

        /// <summary>Where <paramref name="voice"/> is in the clip, in seconds.</summary>
        public float ClipSeconds(int voice)
        {
            return clipSeconds[voice];
        }

        /// <summary>How loud <paramref name="voice"/> is, from 0 to 1, with an equal-power curve so a crossfade doesn't dip.</summary>
        public float Volume(int voice)
        {
            return playing[voice] ? (float)Math.Sin(gain[voice] * Math.PI * 0.5) : 0f;
        }

        /// <summary>A Shot was fired: starts the loop if it isn't running, and keeps it running.</summary>
        public void Shot()
        {
            secondsSinceShot = 0f;
            if (IsFiring)
            {
                return;
            }

            IsFiring = true;

            // A voice still fading out from the last burst keeps fading; the quieter one makes way if both are.
            var voice = !playing[0] ? 0 : !playing[1] ? 1 : gain[0] <= gain[1] ? 0 : 1;
            StartVoice(voice, timing.StartSeconds, timing.FadeInSeconds);
        }

        /// <summary>Firing stopped: every voice fades out.</summary>
        public void Release()
        {
            if (!IsFiring)
            {
                return;
            }

            IsFiring = false;
            current = None;
            for (var voice = 0; voice < VoiceCount; voice++)
            {
                if (playing[voice])
                {
                    FadeOut(voice, timing.FadeOutSeconds);
                }
            }
        }

        /// <summary>Advances the loop by <paramref name="deltaSeconds"/> of real time.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            if (IsFiring)
            {
                secondsSinceShot += deltaSeconds;
                if (secondsSinceShot > timing.StopAfterSeconds)
                {
                    Release();
                }
            }

            for (var voice = 0; voice < VoiceCount; voice++)
            {
                if (playing[voice])
                {
                    Advance(voice, deltaSeconds);
                }
            }

            if (current != None && clipSeconds[current] >= timing.LoopEndSeconds - timing.CrossfadeSeconds)
            {
                var next = 1 - current;
                FadeOut(current, timing.CrossfadeSeconds);
                StartVoice(next, timing.LoopStartSeconds, timing.CrossfadeSeconds);
            }
        }

        private void Advance(int voice, float deltaSeconds)
        {
            clipSeconds[voice] += deltaSeconds;
            gain[voice] += gainPerSecond[voice] * deltaSeconds;
            if (gain[voice] >= 1f)
            {
                gain[voice] = 1f;
                gainPerSecond[voice] = 0f;
            }

            if ((gainPerSecond[voice] < 0f && gain[voice] <= 0f) || clipSeconds[voice] >= timing.ClipLengthSeconds)
            {
                Stop(voice);
            }
        }

        private void StartVoice(int voice, float fromClipSeconds, float fadeInSeconds)
        {
            if (playing[voice])
            {
                Stop(voice);
            }

            playing[voice] = true;
            clipSeconds[voice] = fromClipSeconds;
            gain[voice] = fadeInSeconds > 0f ? 0f : 1f;
            gainPerSecond[voice] = fadeInSeconds > 0f ? 1f / fadeInSeconds : 0f;
            current = voice;
            VoiceStarted?.Invoke(voice, fromClipSeconds);
        }

        // From wherever it is now down to silence over the given time.
        private void FadeOut(int voice, float seconds)
        {
            if (seconds <= 0f || gain[voice] <= 0f)
            {
                Stop(voice);
                return;
            }

            gainPerSecond[voice] = -gain[voice] / seconds;
        }

        private void Stop(int voice)
        {
            playing[voice] = false;
            gain[voice] = 0f;
            gainPerSecond[voice] = 0f;
            if (current == voice)
            {
                current = None;
            }

            VoiceStopped?.Invoke(voice);
        }
    }
}
