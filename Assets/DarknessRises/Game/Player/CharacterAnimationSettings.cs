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

        [SerializeField, Tooltip("Lay the body down on death, for a death clip that only bends the limbs. Turn off for a clip that moves the body itself.")]
        private bool fallOnDeath = true;

        [SerializeField, Min(0f), Tooltip("Seconds the body takes to fall onto its back.")]
        private float deathFallSeconds = 0.85f;

        [SerializeField, Range(0f, 90f), Tooltip("Degrees the body tips backwards as it falls.")]
        private float deathFallDegrees = 84f;

        [SerializeField, Min(0f), Tooltip("Metres the body's centre lies above the floor once it has fallen.")]
        private float deathLyingHeight = 0.15f;

        [Header("Plant at a Station")]
        [SerializeField, Min(0f), Tooltip(
            "Seconds the character takes to settle into its plant stance when it stops at a Station, and to stand up out of it " +
            "once the clear is called. 0 snaps.")]
        private float plantSeconds = 0.22f;

        [SerializeField, Range(0f, 60f), Tooltip(
            "Degrees the lead (left) thigh swings forward in the plant stance. The rear shin leans back as far, so both feet stay on the floor. " +
            "0 with the rear angle at 0 switches the stance off.")]
        private float plantLeadDegrees = 26f;

        [SerializeField, Range(0f, 60f), Tooltip(
            "Degrees the rear (right) thigh swings back in the plant stance. Keep it at or under the lead angle. " +
            "The gap between the two is how deep the knees bend.")]
        private float plantRearDegrees = 14f;

        [SerializeField, Range(0f, 30f), Tooltip("Degrees the upper body leans forward over the lead leg in the plant stance.")]
        private float plantLeanDegrees = 7f;

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
        public bool FallOnDeath => fallOnDeath;
        public float DeathFallSeconds => deathFallSeconds;
        public float DeathFallDegrees => deathFallDegrees;
        public float DeathLyingHeight => deathLyingHeight;
        public float PlantSeconds => plantSeconds;
        public float PlantLeadDegrees => plantLeadDegrees;
        public float PlantRearDegrees => plantRearDegrees;
        public float PlantLeanDegrees => plantLeanDegrees;
    }
}
