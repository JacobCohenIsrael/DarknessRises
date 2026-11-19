using System;
using System.Collections.Generic;
using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Visibility;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Holds the character's Primary weapons in the scene: forwards the player's Fire and Reload commands to the
    /// equipped one in the <see cref="PrimaryWeaponLoadout"/>, turns the weapon mount toward the aim point,
    /// and shows each shot through that Weapon's <see cref="PrimaryWeaponEffects"/>.
    /// </summary>
    /// <remarks>
    /// Runs after the default execution order so the character has already walked this frame,
    /// and Shots leave from where the character is now.
    /// </remarks>
    [DefaultExecutionOrder(AfterCharacterMovement)]
    public sealed class PrimaryWeaponView : MonoBehaviour
    {
        private const int AfterCharacterMovement = 100;

        [SerializeField] private PlayerControls controls;

        [SerializeField, Tooltip("Bends the aim toward a visible Enemy near the finger, on touch only.")]
        private AimAssistView aimAssist;

        [SerializeField, Tooltip("Turns (yaw only) to face the aim point.")]
        private Transform mount;

        [SerializeField, Tooltip("Where Shots and the Muzzle flash start. A child of the mount.")]
        private Transform muzzle;

        [SerializeField, Tooltip("What a Shot stops at.")]
        private LayerMask shotHitLayers = Physics.DefaultRaycastLayers;

        private PrimaryWeaponEffects[] effects;
        private Action[] onFired;
        private Action<bool, Vector3> onShotLanded;

        public PrimaryWeaponLoadout Loadout { get; private set; }

        /// <summary>The equipped Primary weapon, or null before <see cref="Arm"/>.</summary>
        public PrimaryWeapon Weapon => Loadout?.Equipped;

        /// <summary>Raised after a different Primary is equipped, with the newly equipped one.</summary>
        public event Action<PrimaryWeapon> Switched;

        /// <summary>
        /// Raised when a Shot of any of the character's Primaries ends: whether it hurt an Enemy (a hit) or not (a miss),
        /// and where.
        /// </summary>
        public event Action<bool, Vector3> ShotLanded;

        /// <summary>
        /// Arms the character with a fresh, fully loaded Primary for each of <paramref name="definitions"/>,
        /// in switching order, and equips the first. Their Shots and hit sparks come from <paramref name="content"/>, already loaded.
        /// Their lights are added to <paramref name="lights"/>, so what Gunfire lights counts as visible.
        /// </summary>
        public PrimaryWeaponLoadout Arm(IReadOnlyList<WeaponDefinition> definitions, ILoadedContent content, SceneLights lights)
        {
            Disarm();

            onShotLanded ??= OnShotLanded;
            var stats = new PrimaryWeaponStats[definitions.Count];
            effects = new PrimaryWeaponEffects[definitions.Count];
            onFired = new Action[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                stats[i] = definitions[i].PrimaryStats;
                effects[i] = new PrimaryWeaponEffects(definitions[i], shotHitLayers, content, lights);
                effects[i].ShotLanded += onShotLanded;
            }

            Loadout = new PrimaryWeaponLoadout(stats);
            for (var i = 0; i < effects.Length; i++)
            {
                var weaponEffects = effects[i];
                onFired[i] = () => weaponEffects.Fire(muzzle.position, mount.forward);
                Loadout[i].Fired += onFired[i];
            }

            Loadout.Switched += OnSwitched;
            return Loadout;
        }

        /// <summary>Equips the Primary at <paramref name="index"/> in the loadout.</summary>
        /// <returns>Whether the equipped Primary changed.</returns>
        public bool Switch(int index)
        {
            return Loadout != null && Loadout.Switch(index);
        }

        public void Disarm()
        {
            if (Loadout == null)
            {
                return;
            }

            Loadout.Switched -= OnSwitched;
            for (var i = 0; i < effects.Length; i++)
            {
                Loadout[i].Fired -= onFired[i];
                effects[i].ShotLanded -= onShotLanded;
                effects[i].Dispose();
            }

            Loadout = null;
            effects = null;
            onFired = null;
        }

        private void Update()
        {
            if (Loadout == null)
            {
                return;
            }

            if (controls.TryGetAimPoint(muzzle.position.y, out var aimPoint))
            {
                if (aimAssist != null && controls.IsTouchActive)
                {
                    aimPoint = aimAssist.Assist(mount.position, aimPoint);
                }

                if (AimPlane.TryGetFlatDirection(mount.position, aimPoint, out var aimDirection))
                {
                    mount.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
                }
            }

            var weapon = Loadout.Equipped;
            if (controls.WasReloadPressedThisFrame)
            {
                weapon.Reload();
            }

            if (controls.IsFireHeld)
            {
                weapon.PullTrigger();
            }
            else
            {
                weapon.ReleaseTrigger();
            }

            Loadout.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Disarm();
        }

        private void OnSwitched(PrimaryWeapon equipped)
        {
            Switched?.Invoke(equipped);
        }

        private void OnShotLanded(bool hurtEnemy, Vector3 point)
        {
            ShotLanded?.Invoke(hurtEnemy, point);
        }
    }
}
