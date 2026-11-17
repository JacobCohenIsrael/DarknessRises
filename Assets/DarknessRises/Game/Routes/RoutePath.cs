using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// The shape of a Route: an ordered line of points through the Room, measured in metres along its length.
    /// Immutable once built. Sampling it never allocates.
    /// </summary>
    public sealed class RoutePath
    {
        private readonly Vector3[] points;
        private readonly float[] distances;
        private readonly int lastSegmentWithLength;

        public RoutePath(IReadOnlyList<Vector3> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count < 2)
            {
                throw new ArgumentException("A Route needs at least two points.", nameof(points));
            }

            this.points = new Vector3[points.Count];
            distances = new float[points.Count];
            lastSegmentWithLength = -1;

            for (var i = 0; i < points.Count; i++)
            {
                this.points[i] = points[i];
                if (i == 0)
                {
                    continue;
                }

                var segmentLength = Vector3.Distance(points[i - 1], points[i]);
                distances[i] = distances[i - 1] + segmentLength;
                if (segmentLength > 0f)
                {
                    lastSegmentWithLength = i - 1;
                }
            }

            if (lastSegmentWithLength < 0)
            {
                throw new ArgumentException("A Route needs some length: its points can't all be in one place.", nameof(points));
            }
        }

        /// <summary>Metres from the start of the Route to its end.</summary>
        public float Length => distances[distances.Length - 1];

        public int PointCount => points.Length;

        public Vector3 PointAt(int index) => points[index];

        /// <summary>Metres along the Route at which it passes through the point at <paramref name="index"/>.</summary>
        public float DistanceAtPoint(int index) => distances[index];

        /// <summary>World position the given number of metres along the Route, clamped to its ends.</summary>
        public Vector3 PositionAt(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            var segment = SegmentAt(distance);
            var segmentLength = distances[segment + 1] - distances[segment];
            var t = (distance - distances[segment]) / segmentLength;
            return Vector3.Lerp(points[segment], points[segment + 1], t);
        }

        /// <summary>
        /// Unit direction the Route heads in at the given distance. At a corner it is the direction of the leg ahead;
        /// at the very end, the direction of the last leg.
        /// </summary>
        public Vector3 DirectionAt(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, Length);
            var segment = SegmentAt(distance);
            var segmentLength = distances[segment + 1] - distances[segment];
            return (points[segment + 1] - points[segment]) / segmentLength;
        }

        // The leg that starts at or before the distance and ends after it, skipping legs of zero length.
        private int SegmentAt(float distance)
        {
            for (var i = 0; i < lastSegmentWithLength; i++)
            {
                if (distance < distances[i + 1])
                {
                    return i;
                }
            }

            return lastSegmentWithLength;
        }
    }
}
