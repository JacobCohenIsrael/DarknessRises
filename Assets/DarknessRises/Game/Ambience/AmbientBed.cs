using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Ambience
{
    /// <summary>
    /// When the ambient bed plays: now and then, a random gap after the last one ended, while the character walks, fights
    /// a Wave or takes the quiet after a clear; never in a Boss fight (the Boss has the stage) or on the title card or
    /// after death. Plain C# (ADR 0003).
    /// </summary>
    public sealed class AmbientBedSchedule
    {
        private readonly float minGapSeconds;
        private readonly float maxGapSeconds;
        private readonly IRandom random;
        private float gapLeft;

        /// <param name="minGapSeconds">Fewest seconds of silence between two beds.</param>
        /// <param name="maxGapSeconds">Most seconds of silence between two beds.</param>
        /// <param name="firstDelaySeconds">Seconds from the Level's start before the first.</param>
        public AmbientBedSchedule(float minGapSeconds, float maxGapSeconds, float firstDelaySeconds, IRandom random)
        {
            if (minGapSeconds < 0f || maxGapSeconds < minGapSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGapSeconds), maxGapSeconds, "Gaps run from a non-negative least to a most at least as long.");
            }

            if (firstDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(firstDelaySeconds), firstDelaySeconds, "A delay can't be negative.");
            }

            this.minGapSeconds = minGapSeconds;
            this.maxGapSeconds = maxGapSeconds;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            gapLeft = firstDelaySeconds;
        }

        public float GapLeft => gapLeft;

        /// <summary>Whether a bed may play in <paramref name="moment"/>.</summary>
        public static bool Allows(CritterMoment moment)
        {
            return moment == CritterMoment.Walking || moment == CritterMoment.AfterClear || moment == CritterMoment.WaveFight;
        }

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/>. The gap only counts down while no bed plays. True when a bed
        /// should start now; the next gap is then drawn, to count from when it ends.
        /// </summary>
        public bool Tick(float deltaSeconds, CritterMoment moment, bool isPlaying)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (isPlaying)
            {
                return false;
            }

            gapLeft = Math.Max(0f, gapLeft - deltaSeconds);
            if (gapLeft > 0f || !Allows(moment) || deltaSeconds <= 0f)
            {
                return false;
            }

            gapLeft = random.Range(minGapSeconds, maxGapSeconds);
            return true;
        }
    }

    /// <summary>
    /// The stretch of a long ambient clip one bed plays: from a random point, for a random few seconds that fit in the
    /// clip, fading in at the start and out at the end, so a 24-second recording gives many different, short, soft beds.
    /// Plain C# (ADR 0003).
    /// </summary>
    public readonly struct AmbientSlice
    {
        public AmbientSlice(float startSeconds, float seconds, float fadeSeconds)
        {
            if (startSeconds < 0f || seconds < 0f || fadeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A slice's start, length and fade can't be negative.");
            }

            StartSeconds = startSeconds;
            Seconds = seconds;
            FadeSeconds = Math.Min(fadeSeconds, seconds * 0.5f);
        }

        /// <summary>Where in the clip it starts, in clip seconds.</summary>
        public float StartSeconds { get; }

        /// <summary>How long it plays, in clip seconds.</summary>
        public float Seconds { get; }

        /// <summary>How long it fades in, and out, at most half of it each.</summary>
        public float FadeSeconds { get; }

        /// <summary>
        /// A slice of a clip <paramref name="clipSeconds"/> long, between <paramref name="minSeconds"/> and
        /// <paramref name="maxSeconds"/> (or the whole clip, if shorter), starting where it still fits.
        /// </summary>
        public static AmbientSlice Plan(float clipSeconds, float minSeconds, float maxSeconds, float fadeSeconds, IRandom random)
        {
            if (minSeconds < 0f || maxSeconds < minSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSeconds), maxSeconds, "A slice runs from a non-negative least to a most at least as long.");
            }

            var clip = Math.Max(0f, clipSeconds);
            var seconds = Math.Min(clip, random.Range(minSeconds, maxSeconds));
            var start = random.Range(0f, clip - seconds);
            return new AmbientSlice(start, seconds, fadeSeconds);
        }

        /// <summary>The share of full volume <paramref name="secondsIn"/> clip seconds into the slice: up, hold, down.</summary>
        public float Volume(float secondsIn)
        {
            if (secondsIn <= 0f || secondsIn >= Seconds)
            {
                return 0f;
            }

            if (FadeSeconds <= 0f)
            {
                return 1f;
            }

            return Math.Min(1f, Math.Min(secondsIn, Seconds - secondsIn) / FadeSeconds);
        }

        public bool IsOver(float secondsIn)
        {
            return secondsIn >= Seconds;
        }
    }

    /// <summary>Where a bed comes from: somewhere around the character, out in the dark, up near the ceiling. Plain C# (ADR 0003).</summary>
    public static class AmbientBedPlace
    {
        public static Vector3 Pick(Vector3 character, float minMetres, float maxMetres, float heightMetres, IRandom random)
        {
            if (minMetres < 0f || maxMetres < minMetres)
            {
                throw new ArgumentOutOfRangeException(nameof(maxMetres), maxMetres, "A range runs from a non-negative least to a most at least as big.");
            }

            var direction = Quaternion.AngleAxis(random.Range(0f, 360f), Vector3.up) * Vector3.forward;
            var place = character + direction * random.Range(minMetres, maxMetres);
            place.y = character.y + heightMetres;
            return place;
        }
    }
}
