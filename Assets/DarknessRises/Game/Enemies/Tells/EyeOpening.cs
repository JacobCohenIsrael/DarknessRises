using System;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// How far an Enemy's eyes have opened since it spawned, from 0 (shut) to 1 (open). The eyes stay shut for a moment,
    /// crack open slowly to a peek, blink shut and open again, then open fully, slowly at first. It multiplies the
    /// distance glint (<see cref="EyeGlint"/>), so an Enemy that spawns close to the character doesn't glint at full
    /// strength from its first frame. Plain C# (ADR 0003); time comes in as Enemy time, so it follows Adrenaline and
    /// stops while the game is paused. Pooled with its Enemy, so <see cref="Restart"/> shuts the eyes again.
    /// </summary>
    public sealed class EyeOpening
    {
        private readonly float seconds;
        private readonly float shutEnd;
        private readonly float peekEnd;
        private readonly float blinkEnd;
        private readonly float peekStrength;
        private float elapsedSeconds;

        /// <param name="seconds">From spawning to fully open. 0 opens them at once.</param>
        /// <param name="shutShare">Share of <paramref name="seconds"/> the eyes stay shut.</param>
        /// <param name="peekShare">Share over which they crack open to <paramref name="peekStrength"/>.</param>
        /// <param name="peekStrength">How far they crack open before the blink, from 0 to 1.</param>
        /// <param name="blinkShare">Share taken by one blink, shut and back to the peek. The rest opens them fully.</param>
        public EyeOpening(float seconds, float shutShare, float peekShare, float peekStrength, float blinkShare)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Opening can't take negative time.");
            }

            Share(shutShare, nameof(shutShare));
            Share(peekShare, nameof(peekShare));
            Share(peekStrength, nameof(peekStrength));
            Share(blinkShare, nameof(blinkShare));
            if (shutShare + peekShare + blinkShare > 1f)
            {
                throw new ArgumentException("The shut, peek and blink shares can't add up to more than the whole opening.");
            }

            this.seconds = seconds;
            this.peekStrength = peekStrength;
            shutEnd = shutShare;
            peekEnd = shutShare + peekShare;
            blinkEnd = peekEnd + blinkShare;
        }

        /// <summary>How far open the eyes are now, from 0 (shut) to 1 (open).</summary>
        public float Strength => At(elapsedSeconds);

        /// <summary>Whether the eyes are fully open.</summary>
        public bool IsOpen => elapsedSeconds >= seconds;

        /// <summary>Shuts the eyes, to open again from the start: a freshly spawned Enemy.</summary>
        public void Restart()
        {
            elapsedSeconds = 0f;
        }

        /// <summary>Moves Enemy time on by <paramref name="deltaSeconds"/>. 0 (paused) leaves the eyes as they are.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (elapsedSeconds < seconds)
            {
                elapsedSeconds = Math.Min(seconds, elapsedSeconds + deltaSeconds);
            }
        }

        /// <summary>How far open the eyes are <paramref name="secondsSinceSpawn"/> after spawning.</summary>
        public float At(float secondsSinceSpawn)
        {
            if (seconds <= 0f || secondsSinceSpawn >= seconds)
            {
                return 1f;
            }

            var progress = Math.Max(0f, secondsSinceSpawn) / seconds;
            if (progress < shutEnd)
            {
                return 0f;
            }

            if (progress < peekEnd)
            {
                return peekStrength * SmoothStep(Within(progress, shutEnd, peekEnd));
            }

            if (progress < blinkEnd)
            {
                // Shut and open again: down to 0 halfway through, back to the peek at the end.
                var blink = Within(progress, peekEnd, blinkEnd);
                return peekStrength * (1f - (float)Math.Sin(Math.PI * blink));
            }

            return peekStrength + (1f - peekStrength) * SmoothStep(Within(progress, blinkEnd, 1f));
        }

        // How far through [start, end) the progress is, from 0 to 1.
        private static float Within(float progress, float start, float end)
        {
            return end > start ? (progress - start) / (end - start) : 1f;
        }

        // Slow to start and slow to settle: eyelids ease open rather than snapping.
        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static void Share(float value, string name)
        {
            if (value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(name, value, "A share must be between 0 and 1.");
            }
        }
    }
}
