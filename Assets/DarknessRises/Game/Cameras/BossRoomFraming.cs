using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Where a Boss Room's camera stands to frame the whole fight: looking down at the fixed angle, at the middle of
    /// every point that has to be seen (the Positions), far enough back that each one is in view with a margin.
    /// </summary>
    public static class BossRoomFraming
    {
        /// <summary>The middle of <paramref name="points"/>: the centre of their bounds.</summary>
        public static Vector3 Centre(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                throw new ArgumentException("Nothing to frame.", nameof(points));
            }

            var bounds = new Bounds(points[0], Vector3.zero);
            for (var i = 1; i < points.Count; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds.center;
        }

        /// <summary>
        /// How far back from <paramref name="centre"/> a camera at <paramref name="pitchDegrees"/>, with a vertical
        /// field of view of <paramref name="fieldOfViewDegrees"/> and <paramref name="aspect"/> (width over height),
        /// must stand so every point, padded by <paramref name="marginMetres"/> on each side, is in view.
        /// Never less than <paramref name="minDistance"/>.
        /// </summary>
        public static float Distance(
            IReadOnlyList<Vector3> points,
            Vector3 centre,
            float pitchDegrees,
            float fieldOfViewDegrees,
            float aspect,
            float marginMetres,
            float minDistance)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            var rotation = FixedAngleFraming.Rotation(pitchDegrees);
            var forward = rotation * Vector3.forward;
            var up = rotation * Vector3.up;
            var right = rotation * Vector3.right;
            var tanHalfVertical = Mathf.Tan(fieldOfViewDegrees * 0.5f * Mathf.Deg2Rad);
            var tanHalfHorizontal = tanHalfVertical * aspect;

            var distance = minDistance;
            for (var i = 0; i < points.Count; i++)
            {
                var offset = points[i] - centre;
                var depthPastCentre = Vector3.Dot(offset, forward);
                var vertical = Mathf.Abs(Vector3.Dot(offset, up)) + marginMetres;
                var horizontal = Mathf.Abs(Vector3.Dot(offset, right)) + marginMetres;

                // In view when its sideways and up/down offsets fit the frustum at its depth: distance + depthPastCentre.
                distance = Mathf.Max(distance, vertical / tanHalfVertical - depthPastCentre);
                distance = Mathf.Max(distance, horizontal / tanHalfHorizontal - depthPastCentre);
            }

            return distance;
        }

        /// <summary>Where the camera stands, <paramref name="distance"/> back from <paramref name="centre"/> at the pitch.</summary>
        public static Vector3 CameraPosition(Vector3 centre, float pitchDegrees, float distance)
        {
            return centre + FixedAngleFraming.FollowOffset(pitchDegrees, distance);
        }
    }
}
