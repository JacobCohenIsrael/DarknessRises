using System;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// When a due pair of Phantom eyes opens: just as a flash that lit the whole Room (Lightning) fades, so the eyes get
    /// the whole stretch of dark until the next strike. The flash showed nothing there; then they're there. A storm
    /// strikes every few seconds, so eyes that opened at any moment would mostly be lit away before they could blink.
    /// If no flash comes within the longest wait (a calm Level), they open anyway. Plain C# (ADR 0003).
    /// </summary>
    public sealed class PhantomAfterFlash
    {
        private readonly float windowSeconds;
        private readonly float longestWaitSeconds;
        private float darkSeconds = float.PositiveInfinity;
        private float waitedSeconds;

        /// <param name="windowSeconds">Seconds after the Room goes dark again in which a waiting pair opens.</param>
        /// <param name="longestWaitSeconds">Most seconds a pair waits for a flash before it opens anyway.</param>
        public PhantomAfterFlash(float windowSeconds, float longestWaitSeconds)
        {
            this.windowSeconds = windowSeconds >= 0f
                ? windowSeconds
                : throw new ArgumentOutOfRangeException(nameof(windowSeconds), windowSeconds, "This can't be negative.");
            this.longestWaitSeconds = longestWaitSeconds >= 0f
                ? longestWaitSeconds
                : throw new ArgumentOutOfRangeException(nameof(longestWaitSeconds), longestWaitSeconds, "This can't be negative.");
        }

        /// <summary>Whether a pair is waiting to open.</summary>
        public bool IsWaiting => Waiting > 0;

        /// <summary>How many due pairs are waiting to open: all of them open as the same flash fades, one a frame.</summary>
        public int Waiting { get; private set; }

        /// <summary>A pair is due: it waits for the next flash to fade, with any others already waiting.</summary>
        public void Wait()
        {
            if (Waiting == 0)
            {
                waitedSeconds = 0f;
            }

            Waiting++;
        }

        /// <summary>The wait is called off for every waiting pair (a fight began, an Enemy came near).</summary>
        public void Cancel()
        {
            Waiting = 0;
        }

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/>; <paramref name="isRoomLit"/> says whether a light reaches the
        /// whole Room now. True when one waiting pair should open now; the others open in the next frames of the same
        /// moment, so a flash that fades opens them all.
        /// </summary>
        public bool Tick(float deltaSeconds, bool isRoomLit)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            darkSeconds = isRoomLit ? 0f : darkSeconds + deltaSeconds;
            if (!IsWaiting)
            {
                return false;
            }

            waitedSeconds += deltaSeconds;
            var justFaded = !isRoomLit && darkSeconds > 0f && darkSeconds <= windowSeconds;
            if (!justFaded && (isRoomLit || waitedSeconds < longestWaitSeconds))
            {
                return false;
            }

            Waiting--;
            return true;
        }
    }
}
