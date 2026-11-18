using DarknessRises.Game.Controls;
using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>
    /// Touch input: what counts as each Gesture, which Flick directions mean Reload and Flare, and how strongly
    /// Aim assist bends Shots on touch. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Gesture Definition", fileName = "GestureDefinition")]
    public sealed class GestureDefinition : ScriptableObject
    {
        [SerializeField] private GestureThresholds thresholds = new();

        [SerializeField, Range(0f, 90f), Tooltip(
            "Degrees either side of straight down (Reload) and straight up (Flare) a Flick may be. "
            + "Any other Flick Relocates in Boss fights.")]
        private float flickAxisHalfAngleDegrees = 30f;

        [SerializeField, Tooltip("On touch only, bends Shots toward the nearest visible Enemy close to the finger.")]
        private AimAssistSettings aimAssist = new();

        public GestureThresholds Thresholds => thresholds;
        public float FlickAxisHalfAngleDegrees => flickAxisHalfAngleDegrees;
        public AimAssistSettings AimAssist => aimAssist;
    }
}
