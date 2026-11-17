using UnityEngine;

namespace DarknessRises.Game.Cameras
{
    /// <summary>
    /// Where a fixed-angle top-down camera sits relative to the character, and which way it faces,
    /// so that the character is always at the centre of the view.
    /// The camera never turns with the character: "up" on screen is always world forward (+Z).
    /// </summary>
    public static class FixedAngleFraming
    {
        /// <summary>World-space offset from the character to the camera.</summary>
        public static Vector3 FollowOffset(float pitchDegrees, float distance)
        {
            var pitchRadians = pitchDegrees * Mathf.Deg2Rad;
            return new Vector3(0f, Mathf.Sin(pitchRadians) * distance, -Mathf.Cos(pitchRadians) * distance);
        }

        /// <summary>World rotation of the camera, looking down at the character from <see cref="FollowOffset"/>.</summary>
        public static Quaternion Rotation(float pitchDegrees)
        {
            return Quaternion.Euler(pitchDegrees, 0f, 0f);
        }
    }
}
