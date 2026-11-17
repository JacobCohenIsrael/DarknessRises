using System;
using DarknessRises.Game.Routes;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// The point the camera follows: a little way ahead of the character along its Route while it walks, easing back
    /// while it is stopped. Following the Route rather than a straight line lets the camera look round corners.
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

        public void Tick(float deltaSeconds, RouteProgress progress)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            var stopped = progress.IsAtStation || progress.IsFinished;
            var desired = stopped ? stoppedDistance : walkingDistance;
            Distance = Mathf.MoveTowards(Distance, desired, changeSpeed * deltaSeconds);
            Target = progress.Path.PositionAt(progress.Distance + Distance);
        }
    }
}
