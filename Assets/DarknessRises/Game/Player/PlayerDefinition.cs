using DarknessRises.Game.Health;
using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// Authored tuning for the character. Never changed at runtime; rule objects copy what they need.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Player Definition", fileName = "PlayerDefinition", order = 10)]
    public sealed class PlayerDefinition : ScriptableObject
    {
        [SerializeField, Min(1), Tooltip("Discrete hit points. They never regenerate.")]
        private int hitPoints = 5;

        [SerializeField, Min(0f), Tooltip("Seconds of invulnerability after the character is hit.")]
        private float invulnerabilitySeconds = 1f;

        [SerializeField, Min(0f), Tooltip("Metres per second the character walks along the Route.")]
        private float walkingSpeed = 3f;

        [SerializeField, Tooltip("The faint, always-on, shadowless light around the character.")]
        private AuraSettings aura = new();

        [SerializeField, Tooltip("How the body turns and when the throw and death animations reach their gameplay moments.")]
        private CharacterAnimationSettings animation = new();

        [SerializeField, Tooltip("How a landed hit shows: camera shake, hurt sound and blinking while invulnerable.")]
        private HitReactionSettings hitReaction = new();

        public HitReactionSettings HitReaction => hitReaction;
        public int HitPoints => hitPoints;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public float WalkingSpeed => walkingSpeed;
        public AuraSettings Aura => aura;
        public CharacterAnimationSettings Animation => animation;
    }
}
