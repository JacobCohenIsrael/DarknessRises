using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// The clips the character's Animator controller is built from. Drop in real clips and rebuild the Test Room
    /// to regenerate the controller; an empty slot gets a generated placeholder clip.
    /// Keep the throw and death timings in <see cref="CharacterAnimationSettings"/> in step with these clips.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Character Animation Set", fileName = "CharacterAnimationSet", order = 11)]
    public sealed class CharacterAnimationSet : ScriptableObject
    {
        [Header("Walk (base layer, blended by direction relative to the legs)")]
        [SerializeField] private AnimationClip idle;
        [SerializeField] private AnimationClip walkForward;
        [SerializeField] private AnimationClip walkBackward;
        [SerializeField] private AnimationClip walkLeft;
        [SerializeField] private AnimationClip walkRight;
        [SerializeField] private AnimationClip death;

        [Header("Upper body (over the walk)")]
        [SerializeField, Tooltip("Held while not shooting or throwing: the Weapon raised toward the aim point.")]
        private AnimationClip aim;

        [SerializeField] private AnimationClip shoot;
        [SerializeField] private AnimationClip throwGrenade;

        public AnimationClip Idle => idle;
        public AnimationClip WalkForward => walkForward;
        public AnimationClip WalkBackward => walkBackward;
        public AnimationClip WalkLeft => walkLeft;
        public AnimationClip WalkRight => walkRight;
        public AnimationClip Death => death;
        public AnimationClip Aim => aim;
        public AnimationClip Shoot => shoot;
        public AnimationClip ThrowGrenade => throwGrenade;
    }
}
