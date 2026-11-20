using UnityEngine;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// Whether the player aims at a pair of Phantom eyes: the Crosshair (the mouse pointer, or a finger that's down or
    /// has just tapped) is within a share of the screen's height of where the eyes are drawn. Measured on screen, so it
    /// feels the same at any distance and on any device. Plain C# (ADR 0003).
    /// </summary>
    public static class PhantomAim
    {
        /// <param name="pointer">Where the Crosshair is, in screen pixels.</param>
        /// <param name="isPointerAiming">Whether it aims now: always with a mouse; on touch, while a finger is down.</param>
        /// <param name="eyesOnScreen">Where the eyes are drawn, in screen pixels; z is their depth in front of the camera.</param>
        /// <param name="screenHeight">The screen's height in pixels.</param>
        /// <param name="radiusShare">How near counts, as a share of the screen's height.</param>
        public static bool IsAimedAt(Vector2 pointer, bool isPointerAiming, Vector3 eyesOnScreen, float screenHeight, float radiusShare)
        {
            if (!isPointerAiming || eyesOnScreen.z <= 0f || screenHeight <= 0f)
            {
                return false;
            }

            var radius = screenHeight * radiusShare;
            var offset = pointer - new Vector2(eyesOnScreen.x, eyesOnScreen.y);
            return offset.sqrMagnitude <= radius * radius;
        }
    }
}
