using System;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// How far the Ride camera swings off north to look where the Route heads, and how it eases there, so it swings
    /// round a corner instead of snapping. The Rooms are built to be seen from the south (their south walls are cut
    /// down), so the camera never goes all the way round behind the character: heading east it swings its most to the
    /// east, heading west its most to the west, and heading north or south it looks straight north.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class RideSwing
    {
        private const float MinFlatLength = 1e-4f;

        private readonly float easeSeconds;

        /// <param name="easeSeconds">
        /// Seconds the swing takes to close about two thirds of the way to a new heading. 0 snaps.
        /// </param>
        public RideSwing(float easeSeconds)
        {
            if (easeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(easeSeconds), easeSeconds, "Easing can't take less than no time.");
            }

            this.easeSeconds = easeSeconds;
        }

        /// <summary>Degrees the camera is swung off north right now: positive to the east.</summary>
        public float Degrees { get; private set; }

        /// <summary>
        /// The swing that looks along <paramref name="heading"/> as far as <paramref name="maxSwingDegrees"/> allow:
        /// the full swing for a heading due east or west, none for one due north or south. A heading with no flat
        /// length swings nothing.
        /// </summary>
        public static float TowardsDegrees(Vector3 heading, float maxSwingDegrees)
        {
            var flatLength = Mathf.Sqrt(heading.x * heading.x + heading.z * heading.z);
            return flatLength > MinFlatLength ? maxSwingDegrees * heading.x / flatLength : 0f;
        }

        /// <summary>Puts the swing at <paramref name="degrees"/> at once, with no easing.</summary>
        public void Snap(float degrees)
        {
            Degrees = degrees;
        }

        /// <summary>Eases the swing towards <paramref name="targetDegrees"/>, the same however the time is cut into steps.</summary>
        public void Tick(float deltaSeconds, float targetDegrees)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (easeSeconds <= 0f)
            {
                Degrees = targetDegrees;
                return;
            }

            Degrees = Mathf.Lerp(targetDegrees, Degrees, Mathf.Exp(-deltaSeconds / easeSeconds));
        }
    }
}
