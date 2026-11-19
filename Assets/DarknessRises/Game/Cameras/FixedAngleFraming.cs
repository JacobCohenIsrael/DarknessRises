using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Where a tilted top-down camera sits relative to the point it looks at, and which way it faces, so that the
    /// point is always at the centre of the view. Without a swing the camera looks north: "up" on screen is world
    /// forward (+Z), which is how the Rooms are built to be seen. A swing turns it about the point, to the east
    /// (positive) or the west.
    /// </summary>
    public static class FixedAngleFraming
    {
        /// <summary>World-space offset from the point looked at to a camera that looks north.</summary>
        public static Vector3 FollowOffset(float pitchDegrees, float distance)
        {
            var pitchRadians = pitchDegrees * Mathf.Deg2Rad;
            return new Vector3(0f, Mathf.Sin(pitchRadians) * distance, -Mathf.Cos(pitchRadians) * distance);
        }

        /// <summary>
        /// World-space offset from the point looked at to a camera swung <paramref name="swingDegrees"/> off north:
        /// swung east, it stands to the south-west of the point.
        /// </summary>
        public static Vector3 FollowOffset(float pitchDegrees, float swingDegrees, float distance)
        {
            return Quaternion.Euler(0f, swingDegrees, 0f) * FollowOffset(pitchDegrees, distance);
        }

        /// <summary>World rotation of a camera that looks north and down, from <see cref="FollowOffset(float,float)"/>.</summary>
        public static Quaternion Rotation(float pitchDegrees)
        {
            return Quaternion.Euler(pitchDegrees, 0f, 0f);
        }

        /// <summary>World rotation of a camera swung <paramref name="swingDegrees"/> off north, looking down at the point.</summary>
        public static Quaternion Rotation(float pitchDegrees, float swingDegrees)
        {
            return Quaternion.Euler(pitchDegrees, swingDegrees, 0f);
        }
    }
}
