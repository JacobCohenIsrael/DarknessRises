using System;
using UnityEngine;

namespace DarknessRises.Game.Controls
{
    /// <summary>How Aim assist bends Shots on touch.</summary>
    [Serializable]
    public sealed class AimAssistSettings
    {
        [SerializeField, Min(0f), Tooltip("Metres from the finger's aim point (on the floor plane) an Enemy may be to pull the aim.")]
        private float radius = 1.5f;

        [SerializeField, Range(0f, 1f), Tooltip("How far the aim turns toward the Enemy: 0 not at all, 1 straight at it.")]
        private float strength = 0.6f;

        public AimAssistSettings()
        {
        }

        public AimAssistSettings(float radius, float strength)
        {
            this.radius = radius;
            this.strength = strength;
        }

        public float Radius => radius;
        public float Strength => strength;
    }
}
