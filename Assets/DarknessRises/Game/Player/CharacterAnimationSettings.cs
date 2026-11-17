using System;
using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// How the character's body moves and when its animations reach the moments gameplay waits for.
    /// The throw and death timings must match the clips in the character's animation set.
    /// </summary>
    [Serializable]
    public sealed class CharacterAnimationSettings
    {
        [SerializeField, Min(0f), Tooltip("Degrees per second the legs turn toward the Route's direction, or toward the aim while stopped.")]
        private float bodyTurnSpeed = 360f;

        [SerializeField, Min(0f), Tooltip("Degrees per second the upper body turns toward the aim point.")]
        private float upperBodyTurnSpeed = 1440f;

        [SerializeField, Range(0f, 180f), Tooltip("Most degrees the upper body twists away from the legs. Past this the legs turn with it.")]
        private float maxUpperBodyTwist = 100f;

        [SerializeField, Min(0f), Tooltip("Seconds the walk blend takes to settle on a new direction or on stopping.")]
        private float locomotionBlendSeconds = 0.12f;

        [SerializeField, Min(0f), Tooltip("Seconds the hands take to move onto the Weapon after a throw, or off it to throw.")]
        private float handsBlendSeconds = 0.15f;

        [SerializeField, Min(0f), Tooltip("Metres the hands and the Weapon's model kick back with each shot. The Shot itself always leaves from the muzzle.")]
        private float recoilDistance = 0.06f;

        [SerializeField, Min(0f), Tooltip("Seconds the recoil takes to settle back.")]
        private float recoilSeconds = 0.12f;

        [SerializeField, Min(0f), Tooltip("Seconds into the throw animation when the Grenade leaves the hand.")]
        private float grenadeReleaseSeconds = 0.67f;

        [SerializeField, Min(0f), Tooltip("Seconds the throw animation lasts, until the character can throw again.")]
        private float throwSeconds = 1.17f;

        [SerializeField, Min(0f), Tooltip("Seconds from the start of the death animation until the character lies still.")]
        private float deathSeconds = 1.5f;

        public float BodyTurnSpeed => bodyTurnSpeed;
        public float UpperBodyTurnSpeed => upperBodyTurnSpeed;
        public float MaxUpperBodyTwist => maxUpperBodyTwist;
        public float LocomotionBlendSeconds => locomotionBlendSeconds;
        public float HandsBlendSeconds => handsBlendSeconds;
        public float RecoilDistance => recoilDistance;
        public float RecoilSeconds => recoilSeconds;
        public float GrenadeReleaseSeconds => grenadeReleaseSeconds;
        public float ThrowSeconds => throwSeconds;
        public float DeathSeconds => deathSeconds;
    }
}
