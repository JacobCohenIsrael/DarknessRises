using System;
using DarknessRises.Game.Routes;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Which way the Route heads from where the character is, read some way ahead rather than off the leg underfoot,
    /// so a camera that follows it starts turning before a corner and doesn't twitch through a zigzag.
    /// </summary>
    public static class RouteHeading
    {
        private const float MinLength = 1e-4f;

        /// <summary>
        /// The flat unit direction from the point <paramref name="distance"/> metres along <paramref name="path"/> to
        /// the point <paramref name="metresAhead"/> further on. Near the end of the Route, where there is nothing
        /// further on, it's the direction of the last leg.
        /// </summary>
        public static Vector3 Ahead(RoutePath path, float distance, float metresAhead)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (metresAhead < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(metresAhead), metresAhead, "The Route is read ahead, not behind.");
            }

            var chord = path.PositionAt(distance + metresAhead) - path.PositionAt(distance);
            chord.y = 0f;
            var length = chord.magnitude;
            if (length > MinLength)
            {
                return chord / length;
            }

            var leg = path.DirectionAt(distance);
            leg.y = 0f;
            return leg.sqrMagnitude > MinLength * MinLength ? leg.normalized : Vector3.forward;
        }
    }
}
