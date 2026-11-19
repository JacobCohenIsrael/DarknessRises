using System;
using DarknessRises.Game.Routes;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// The point the camera follows: a little way ahead of the character along its Route while it leads the walk,
    /// easing back while the character is stopped. Following the Route rather than a straight line lets the camera
    /// look round corners.
    /// </summary>
    public sealed class RouteLookAhead
    {
        private readonly float walkingDistance;
        private readonly float stoppedDistance;
        private readonly float changeSpeed;

        public RouteLookAhead(float walkingDistance, float stoppedDistance, float changeSpeed)
        {
            if (walkingDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(walkingDistance), walkingDistance, "Look-ahead can't be negative.");
            }

            if (stoppedDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stoppedDistance), stoppedDistance, "Look-ahead can't be negative.");
            }

            if (changeSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(changeSpeed), changeSpeed, "Look-ahead must be able to change.");
            }

            this.walkingDistance = walkingDistance;
            this.stoppedDistance = stoppedDistance;
            this.changeSpeed = changeSpeed;

            // Start as if already walking, so the first frame doesn't drift.
            Distance = walkingDistance;
        }

        /// <summary>Current metres ahead of the character along the Route.</summary>
        public float Distance { get; private set; }

        /// <summary>The world point the camera follows.</summary>
        public Vector3 Target { get; private set; }

        /// <summary>Aims ahead while the character walks, and eases back while it's stopped at a Station or the end of the Route.</summary>
        public void Tick(float deltaSeconds, RouteProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            Tick(deltaSeconds, progress, !progress.IsAtStation && !progress.IsFinished);
        }

        /// <summary>
        /// Aims ahead while <paramref name="leading"/>, wherever the character is, and eases back otherwise.
        /// The camera leads a stopped character on the Go cue, just before it sets off.
        /// </summary>
        public void Tick(float deltaSeconds, RouteProgress progress, bool leading)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var desired = leading ? walkingDistance : stoppedDistance;
            Distance = Mathf.MoveTowards(Distance, desired, changeSpeed * deltaSeconds);
            Target = progress.Path.PositionAt(progress.Distance + Distance);
        }
    }
}
