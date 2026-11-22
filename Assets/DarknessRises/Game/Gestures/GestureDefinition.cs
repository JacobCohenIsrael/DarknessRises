using DarknessRises.Game.Controls;
using DarknessRises.Game.Grenades;
using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// Touch input: what counts as each Gesture, which Two-finger flick directions mean Flare, Adrenaline and the Lure, and how strongly
    /// Aim assist bends Shots on touch, and the Grenade button. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Gesture Definition", fileName = "GestureDefinition")]
    public sealed class GestureDefinition : ScriptableObject
    {
        [SerializeField] private GestureThresholds thresholds = new();

        [SerializeField, Range(0f, 90f), Tooltip(
            "Degrees either side of straight up (Flare), straight down (Adrenaline) and straight left or right (the Lure, "
            + "thrown that way) a Two-finger flick may be. A Two-finger flick in any other direction does nothing.")]
        private float twoFingerFlickAxisHalfAngleDegrees = 45f;

        [SerializeField, Tooltip("On touch, bends Shots toward the nearest visible Enemy close to the finger, more in later Levels. The mouse's is on the root configuration.")]
        private AimAssistSettings aimAssist = new();

        [SerializeField, Tooltip("The on-screen Grenade button, touch only: tap to arm, tap the Room to throw, hold to move it.")]
        private GrenadeButtonSettings grenadeButton = new();

        public GestureThresholds Thresholds => thresholds;
        public float TwoFingerFlickAxisHalfAngleDegrees => twoFingerFlickAxisHalfAngleDegrees;
        public AimAssistSettings AimAssist => aimAssist;
        public GrenadeButtonSettings GrenadeButton => grenadeButton;
    }
}
