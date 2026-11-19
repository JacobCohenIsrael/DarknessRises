using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>Which way the Go cue's arrow points on screen, so it points along the Route as the camera sees it.</summary>
    public static class GoCue
    {
        // A Route direction pointing (almost) straight into the camera has no direction on screen.
        private const float MinLengthOnScreen = 1e-3f;

        /// <summary>
        /// The unit direction on screen (x to the right, y up) of the world direction <paramref name="routeDirection"/>,
        /// seen through a camera whose right and up axes are given. Up, when the Route heads straight into the view.
        /// </summary>
        public static Vector2 ScreenDirection(Vector3 routeDirection, Vector3 cameraRight, Vector3 cameraUp)
        {
            var onScreen = new Vector2(Vector3.Dot(routeDirection, cameraRight), Vector3.Dot(routeDirection, cameraUp));
            var length = onScreen.magnitude;
            return length > MinLengthOnScreen ? onScreen / length : Vector2.up;
        }

        /// <summary>Degrees to turn an arrow that points right so it points along <paramref name="screenDirection"/>.</summary>
        public static float AngleDegrees(Vector2 screenDirection)
        {
            return Mathf.Atan2(screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;
        }
    }
}
