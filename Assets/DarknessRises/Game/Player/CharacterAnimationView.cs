using System;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// Shows the character's animations: walking along the Route, shooting, throwing and dying, with the upper body
    /// turned to face the aim point and both hands on the Weapon. The rules live in <see cref="CharacterFacing"/> and
    /// <see cref="OneShotMotion"/>; this feeds them frame time, drives the Animator, and after it twists the upper
    /// body and puts the hands on the Weapon's grips.
    /// </summary>
    /// <remarks>
    /// The Grenade and death tickets call <see cref="Throw"/> and <see cref="Die"/>, and listen to
    /// <see cref="GrenadeReleased"/> and <see cref="DeathFinished"/> for the moments gameplay waits on.
    ///
    /// Runs after the Primary weapon, so it reads this frame's aim and shots.
    /// </remarks>
    [DefaultExecutionOrder(AfterPrimaryWeapon)]
    public sealed class CharacterAnimationView : MonoBehaviour
    {
        private const int AfterPrimaryWeapon = 200;

        private static readonly int MoveForwardId = Animator.StringToHash("MoveForward");
        private static readonly int MoveRightId = Animator.StringToHash("MoveRight");
        private static readonly int ShootId = Animator.StringToHash("Shoot");
        private static readonly int ThrowId = Animator.StringToHash("Throw");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;

        [SerializeField, Tooltip("The model's root, turned to where the legs face. Holds the Animator.")]
        private Transform model;

        [SerializeField, Tooltip("Faces where the player aims. The Primary weapon's mount.")]
        private Transform aim;

        [SerializeField, Tooltip("Bones that share the upper body's twist toward the aim, e.g. spine, chest and upper chest.")]
        private TwistBone[] twistBones = Array.Empty<TwistBone>();

        [SerializeField, Tooltip("The Animator layer that plays aiming, shooting and throwing over the walk.")]
        private int upperBodyLayer = 1;

        [SerializeField] private ArmIk rightArm;
        [SerializeField] private ArmIk leftArm;

        [SerializeField, Tooltip("Holds the Weapon's model and the grips, and kicks back with each shot. Not the muzzle.")]
        private Transform recoil;

        private CharacterAnimationSettings settings;
        private RouteProgress route;
        private PrimaryWeapon weapon;
        private CharacterFacing facing;
        private OneShotMotion throwMotion;
        private OneShotMotion deathMotion;
        private float handsWeight;
        private float recoilSecondsLeft;
        private HumanPoseHandler poseHandler;
        private HumanPose pose;
        private float deadSeconds;
        private bool isFalling;
        private Vector3 fallStartPosition;
        private Quaternion fallStartRotation;

        /// <summary>Raised when the Grenade leaves the hand, partway through the throw.</summary>
        public event Action GrenadeReleased;

        /// <summary>Raised when the throw animation ends.</summary>
        public event Action ThrowFinished;

        /// <summary>Raised when the death animation ends and the character lies still.</summary>
        public event Action DeathFinished;

        public bool IsThrowing => throwMotion != null && throwMotion.IsPlaying;
        public bool IsDead { get; private set; }

        /// <summary>Starts animating the character as it walks <paramref name="routeProgress"/> and fires <paramref name="primaryWeapon"/>.</summary>
        public void Show(CharacterAnimationSettings animationSettings, RouteProgress routeProgress, PrimaryWeapon primaryWeapon)
        {
            Unsubscribe();

            settings = animationSettings;
            route = routeProgress;
            weapon = primaryWeapon;
            IsDead = false;

            CharacterFacing.TryGetYaw(route.Direction, out var startYaw);
            facing = new CharacterFacing(
                settings.BodyTurnSpeed,
                settings.UpperBodyTurnSpeed,
                settings.MaxUpperBodyTwist,
                startYaw);

            throwMotion = new OneShotMotion(settings.GrenadeReleaseSeconds, settings.ThrowSeconds);
            throwMotion.Cued += OnGrenadeReleased;
            throwMotion.Finished += OnThrowFinished;
            deathMotion = new OneShotMotion(settings.DeathSeconds, settings.DeathSeconds);
            deathMotion.Finished += OnDeathFinished;
            weapon.Fired += OnFired;

            handsWeight = 1f;
            recoilSecondsLeft = 0f;
            deadSeconds = 0f;
            isFalling = false;
            poseHandler?.Dispose();
            poseHandler = animator.isHuman ? new HumanPoseHandler(animator.avatar, animator.transform) : null;
            animator.Rebind();
            animator.SetLayerWeight(upperBodyLayer, 1f);
        }

        /// <summary>Follows the shots of <paramref name="primaryWeapon"/> from now on, after the player switches Primaries.</summary>
        public void ShowWeapon(PrimaryWeapon primaryWeapon)
        {
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
            }

            weapon = primaryWeapon;
            weapon.Fired += OnFired;
        }

        /// <summary>Starts the Grenade throw. Does nothing while a throw is under way or after death.</summary>
        /// <returns>Whether the throw started; <see cref="GrenadeReleased"/> follows partway through it.</returns>
        [ContextMenu("Throw")]
        public bool Throw()
        {
            if (IsDead || throwMotion == null || !throwMotion.TryPlay())
            {
                return false;
            }

            animator.SetTrigger(ThrowId);
            return true;
        }

        /// <summary>Plays the death animation once. Shooting, throwing and aiming stop.</summary>
        [ContextMenu("Die")]
        public void Die()
        {
            if (IsDead || deathMotion == null)
            {
                return;
            }

            IsDead = true;
            animator.ResetTrigger(ShootId);
            animator.ResetTrigger(ThrowId);
            animator.SetBool(DeadId, true);
            animator.SetLayerWeight(upperBodyLayer, 0f);
            deathMotion.TryPlay();
        }

        private void Update()
        {
            if (facing == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            throwMotion.Tick(deltaSeconds);
            deathMotion.Tick(deltaSeconds);
            recoilSecondsLeft = Mathf.Max(0f, recoilSecondsLeft - deltaSeconds);

            // The hands leave the Weapon to throw or to fall, and go back once the throw is over.
            var handsTarget = IsDead || IsThrowing ? 0f : 1f;
            handsWeight = settings.HandsBlendSeconds > 0f
                ? Mathf.MoveTowards(handsWeight, handsTarget, deltaSeconds / settings.HandsBlendSeconds)
                : handsTarget;

            if (IsDead)
            {
                deadSeconds += deltaSeconds;
                animator.SetFloat(MoveForwardId, 0f, settings.LocomotionBlendSeconds, deltaSeconds);
                animator.SetFloat(MoveRightId, 0f, settings.LocomotionBlendSeconds, deltaSeconds);
                return;
            }

            var travel = route.IsAtStation || route.IsFinished ? Vector3.zero : route.Direction;
            facing.Tick(deltaSeconds, travel, aim.forward);

            var move = facing.ToBodySpace(travel);
            animator.SetFloat(MoveForwardId, move.y, settings.LocomotionBlendSeconds, deltaSeconds);
            animator.SetFloat(MoveRightId, move.x, settings.LocomotionBlendSeconds, deltaSeconds);
        }

        // After the Animator has posed the bones: the twist adds to the animated pose, then the hands go on the grips.
        private void LateUpdate()
        {
            if (facing == null)
            {
                return;
            }

            model.rotation = Quaternion.Euler(0f, facing.BodyYaw, 0f);

            var kick = settings.RecoilSeconds > 0f ? recoilSecondsLeft / settings.RecoilSeconds : 0f;
            recoil.localPosition = Vector3.back * (settings.RecoilDistance * kick);

            // The Weapon's model goes away while the hands are off it, so it doesn't hang in the air. Shots still fire.
            var weaponShown = handsWeight > 0.5f;
            if (recoil.gameObject.activeSelf != weaponShown)
            {
                recoil.gameObject.SetActive(weaponShown);
            }

            if (IsDead)
            {
                Fall();
            }
            else
            {
                var twist = facing.Twist;
                for (var i = 0; i < twistBones.Length; i++)
                {
                    var bone = twistBones[i].Bone;
                    bone.rotation = Quaternion.AngleAxis(twist * twistBones[i].Share, Vector3.up) * bone.rotation;
                }
            }

            rightArm.Solve(aim, handsWeight);
            leftArm.Solve(aim, handsWeight);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            poseHandler?.Dispose();
            poseHandler = null;
        }

        // Tips the body over backwards onto the floor, on top of whatever the death clip does to the limbs.
        private void Fall()
        {
            if (!settings.FallOnDeath || poseHandler == null)
            {
                return;
            }

            poseHandler.GetHumanPose(ref pose);
            if (!isFalling)
            {
                isFalling = true;
                fallStartPosition = pose.bodyPosition;
                fallStartRotation = pose.bodyRotation;
            }

            // It gathers speed as it goes over, like a body falling.
            var progress = settings.DeathFallSeconds > 0f ? Mathf.Clamp01(deadSeconds / settings.DeathFallSeconds) : 1f;
            var eased = progress * progress;
            var lying = Quaternion.Euler(-settings.DeathFallDegrees, 0f, 0f) * fallStartRotation;
            pose.bodyRotation = Quaternion.Slerp(fallStartRotation, lying, eased);
            pose.bodyPosition = new Vector3(
                fallStartPosition.x,
                Mathf.Lerp(fallStartPosition.y, settings.DeathLyingHeight, eased),
                fallStartPosition.z);
            poseHandler.SetHumanPose(ref pose);
        }

        private void OnFired()
        {
            // The throw has the arms; a recoil queued now would play late, after it.
            if (!IsDead && !IsThrowing)
            {
                animator.SetTrigger(ShootId);
                recoilSecondsLeft = settings.RecoilSeconds;
            }
        }

        private void OnGrenadeReleased()
        {
            GrenadeReleased?.Invoke();
        }

        private void OnThrowFinished()
        {
            ThrowFinished?.Invoke();
        }

        private void OnDeathFinished()
        {
            DeathFinished?.Invoke();
        }

        private void Unsubscribe()
        {
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
            }

            if (throwMotion != null)
            {
                throwMotion.Cued -= OnGrenadeReleased;
                throwMotion.Finished -= OnThrowFinished;
            }

            if (deathMotion != null)
            {
                deathMotion.Finished -= OnDeathFinished;
            }
        }

        /// <summary>A bone that takes a share of the upper body's twist. The shares of all twist bones add up to 1.</summary>
        [Serializable]
        public struct TwistBone
        {
            [SerializeField] private Transform bone;
            [SerializeField, Range(0f, 1f)] private float share;

            public TwistBone(Transform bone, float share)
            {
                this.bone = bone;
                this.share = share;
            }

            public Transform Bone => bone;
            public float Share => share;
        }
    }
}
