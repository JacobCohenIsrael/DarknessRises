using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Authored Flare: how long the character gives off strong light, the cooldown after, and how strong and wide
    /// the light is. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Flare Definition", fileName = "FlareDefinition", order = 60)]
    public sealed class FlareDefinition : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Min(0.1f), Tooltip("Seconds the Flare burns.")]
        private float durationSeconds = 4f;

        [SerializeField, Min(0f), Tooltip("Real-time seconds from the end of the Flare until it can be used again. Adrenaline doesn't slow it.")]
        private float cooldownSeconds = 15f;

        [SerializeField, Min(0f), Tooltip("Seconds the light takes to reach full strength.")]
        private float fadeInSeconds = 0.15f;

        [SerializeField, Min(0f), Tooltip("Seconds the light takes to die out at the end of the Flare.")]
        private float fadeOutSeconds = 0.8f;

        [Header("Light (shadowless)")]
        [SerializeField] private Color color = new(1f, 0.55f, 0.35f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at full strength.")]
        private float intensity = 14f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches at full strength.")]
        private float radius = 11f;

        [SerializeField, Min(0f), Tooltip("Metres above the character's feet.")]
        private float height = 2.2f;

        [Header("HUD")]
        [SerializeField, Tooltip("The Flare's button on the HUD: its icon, colour, name and key badge.")]
        private SkillButtonLook button = new(new Color(1f, 0.62f, 0.35f, 0.95f), "FLARE", "Q");

        public SkillTiming Timing => new(durationSeconds, cooldownSeconds);
        public float FadeInSeconds => fadeInSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
        public Color Color => color;
        public float Intensity => intensity;
        public float Radius => radius;
        public float Height => height;
        public SkillButtonLook Button => button;
    }
}
