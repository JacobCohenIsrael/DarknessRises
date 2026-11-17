using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// The clips the character's Animator controller is built from. Change a clip and rebuild the Test Room
    /// to regenerate the controller; empty required slots are filled with the project's default clips.
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
        [SerializeField, Tooltip("Optional diagonals; the walk blends without them.")] private AnimationClip walkForwardLeft;
        [SerializeField] private AnimationClip walkForwardRight;
        [SerializeField] private AnimationClip walkBackwardLeft;
        [SerializeField] private AnimationClip walkBackwardRight;
        [SerializeField] private AnimationClip death;

        [Header("Upper body (over the walk)")]
        [SerializeField, Tooltip("Optional. Held while not shooting or throwing. Without it the walk plays and the hands are put on the Weapon by IK.")]
        private AnimationClip aim;

        [SerializeField, Tooltip("Optional. Without it each shot is a procedural recoil.")]
        private AnimationClip shoot;
        [SerializeField] private AnimationClip throwGrenade;

        public AnimationClip Idle => idle;
        public AnimationClip WalkForward => walkForward;
        public AnimationClip WalkBackward => walkBackward;
        public AnimationClip WalkLeft => walkLeft;
        public AnimationClip WalkRight => walkRight;
        public AnimationClip WalkForwardLeft => walkForwardLeft;
        public AnimationClip WalkForwardRight => walkForwardRight;
        public AnimationClip WalkBackwardLeft => walkBackwardLeft;
        public AnimationClip WalkBackwardRight => walkBackwardRight;
        public AnimationClip Death => death;
        public AnimationClip Aim => aim;
        public AnimationClip Shoot => shoot;
        public AnimationClip ThrowGrenade => throwGrenade;
    }
}
