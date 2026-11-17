using System;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Whether a Weapon's Shots light their surroundings as they fly, and how. A lit Shot carries a shadowless
    /// point light that dims as the Shot nears the end of its range. Only Lightning casts real-time shadows.
    /// </summary>
    [Serializable]
    public sealed class ShotLightSettings
    {
        [SerializeField, Tooltip("Whether Shots carry light. Off, Shots only glow enough to be seen.")]
        private bool carriesLight;

        [SerializeField] private Color color = new(1f, 0.55f, 0.2f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity as the Shot leaves the muzzle. It fades to 0 at the end of the Shot's range.")]
        private float intensity = 4f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches.")]
        private float range = 4f;

        [SerializeField, Min(1), Tooltip("Only every Nth Shot carries a light, which keeps the number of lights low on mobile. 1 lights every Shot.")]
        private int everyNthShot = 1;

        public bool CarriesLight => carriesLight;
        public Color Color => color;
        public float Intensity => intensity;
        public float Range => range;
        public int EveryNthShot => everyNthShot;
    }
}
