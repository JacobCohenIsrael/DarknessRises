using System;
using UnityEngine;

namespace DarknessRises.Game.Controls
{
    /// <summary>
    /// How Aim assist bends Shots for one kind of input (touch or mouse): how close to the aim point an Enemy must be,
    /// how hard the aim is pulled toward it in the first Level, how much harder each Level after that, and the cap.
    /// </summary>
    [Serializable]
    public sealed class AimAssistSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres from the aim point (on the floor plane) an Enemy may be to pull the aim.")]
        private float radius = 1.5f;

        [SerializeField, Range(0f, 1f), Tooltip("How far the aim turns toward the Enemy in the first Level: 0 not at all, 1 straight at it.")]
        private float strength = 0.6f;

        [SerializeField, Range(0f, 1f), Tooltip("How much the strength grows with each Level after the first, where Enemies are faster and more numerous.")]
        private float strengthPerLevel = 0.02f;

        [SerializeField, Range(0f, 1f), Tooltip("The most the strength grows to, however far the Run goes. Never below the first Level's strength.")]
        private float maxStrength = 0.75f;

        [SerializeField, Range(0f, 1f), Tooltip(
            "How much of the radius pulls at full strength. Beyond it the pull fades to nothing at the radius, "
            + "so an Enemy coming into reach never makes the aim jump. 1 pulls at full strength right up to the radius.")]
        private float fullPullFraction = 0.5f;

        public AimAssistSettings()
        {
        }

        public AimAssistSettings(float radius, float strength, float strengthPerLevel = 0f, float maxStrength = 1f, float fullPullFraction = 1f)
        {
            this.radius = radius;
            this.strength = strength;
            this.strengthPerLevel = strengthPerLevel;
            this.maxStrength = maxStrength;
            this.fullPullFraction = fullPullFraction;
        }

        public float Radius => radius;

        /// <summary>The strength in the first Level.</summary>
        public float Strength => strength;

        public float StrengthPerLevel => strengthPerLevel;
        public float MaxStrength => maxStrength;
        public float FullPullFraction => fullPullFraction;

        /// <summary>
        /// The strength in the Level at <paramref name="levelIndex"/> (0 is the first): the first Level's, grown by the
        /// step for each Level after it, never past the cap (or below the first Level's), and within 0 to 1.
        /// </summary>
        public float StrengthAt(int levelIndex)
        {
            var grown = strength + strengthPerLevel * Mathf.Max(0, levelIndex);
            return Mathf.Clamp01(Mathf.Min(grown, Mathf.Max(maxStrength, strength)));
        }
    }
}
