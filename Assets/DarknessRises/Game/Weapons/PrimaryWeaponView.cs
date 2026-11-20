using System;
using System.Collections.Generic;
using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Visibility;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Holds the character's Primary weapons in the scene: forwards the player's Fire, Reload and Weapon commands to
    /// the Run's <see cref="PrimaryWeaponLoadout"/>, turns the weapon mount toward the aim point,
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

        [SerializeField, Tooltip("Bends the aim toward a visible Enemy near the aim point, on touch and with the mouse.")]
        private AimAssistView aimAssist;

        [SerializeField, Tooltip("Turns (yaw only) to face the aim point.")]
        private Transform mount;

        [SerializeField, Tooltip("Where Shots and the Muzzle flash start. A child of the mount.")]
        private Transform muzzle;

        [SerializeField, Tooltip("What a Shot stops at.")]
        private LayerMask shotHitLayers = Physics.DefaultRaycastLayers;

        private PrimaryWeaponEffects[] effects;
        private Action[] onFired;

        // For each Primary in the Shop's order, where it is in the loadout, or -1 when it isn't carried.
        private int[] places = Array.Empty<int>();
        private Action<ShotOutcome, Vector3> onShotLanded;

        public PrimaryWeaponLoadout Loadout { get; private set; }

        /// <summary>The equipped Primary weapon, or null before <see cref="Arm"/>.</summary>
        public PrimaryWeapon Weapon => Loadout?.Equipped;

        /// <summary>Raised after a different Primary is equipped, with the newly equipped one.</summary>
        public event Action<PrimaryWeapon> Switched;

        /// <summary>
        /// Raised when a Shot of any of the character's Primaries ends: whether it hurt an Enemy (a hit), hurt nothing (a
        /// miss) or struck something that counts as neither (a Critter), and where.
        /// </summary>
        public event Action<ShotOutcome, Vector3> ShotLanded;

        /// <summary>
        /// Arms the character with the Run's <paramref name="loadout"/>: one Primary for each of
        /// <paramref name="definitions"/>, in switching order, as the Run left them (magazines, reserves, which is
        /// equipped). Each damages by its entry in <paramref name="runStats"/>: the Run's numbers, with the player's
        /// Upgrades applied. <paramref name="carriedPlaces"/> says, for each Primary in the Shop's order, where it is in
        /// the loadout (-1 when it isn't unlocked), for the Weapon 1, 2, 3 commands.
        /// Their Shots and hit sparks come from <paramref name="content"/>, already loaded.
        /// Their lights are added to <paramref name="lights"/>, so what Gunfire lights counts as visible.
        /// </summary>
        public PrimaryWeaponLoadout Arm(
            IReadOnlyList<WeaponDefinition> definitions,
            IReadOnlyList<WeaponRunStats> runStats,
            PrimaryWeaponLoadout loadout,
            int[] carriedPlaces,
            ILoadedContent content,
            SceneLights lights)
        {
            if (runStats == null || runStats.Count != definitions.Count)
            {
                throw new ArgumentException("Each carried Primary needs its numbers for the Run.", nameof(runStats));
            }

            if (loadout == null || loadout.Count != definitions.Count)
            {
                throw new ArgumentException("The Run's loadout holds one Primary for each one carried.", nameof(loadout));
            }

            Disarm();
            places = carriedPlaces ?? Array.Empty<int>();

            onShotLanded ??= OnShotLanded;
            effects = new PrimaryWeaponEffects[definitions.Count];
            onFired = new Action[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                effects[i] = new PrimaryWeaponEffects(definitions[i], runStats[i], shotHitLayers, content, lights, transform);
                effects[i].ShotLanded += onShotLanded;
            }

            // The Run's own, kept from Level to Level: this scene only shows it.
            Loadout = loadout;
            Loadout.Equipped.ReleaseTrigger();
            for (var i = 0; i < effects.Length; i++)
            {
                var weaponEffects = effects[i];
                onFired[i] = () => weaponEffects.Fire(mount.position, muzzle.position, mount.forward);
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
                if (aimAssist != null)
                {
                    aimPoint = aimAssist.Assist(mount.position, aimPoint, controls.IsTouchActive);
                }

                if (AimPlane.TryGetFlatDirection(mount.position, aimPoint, out var aimDirection))
                {
                    mount.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
                }
            }

            // Which Weapon: a place in the Shop's order (keys 1 to 3), or the next or the one before (the mouse wheel,
            // the Weapon button). The loadout refuses one that has run dry.
            if (controls.TryGetSelectedWeapon(out var place))
            {
                if (place < places.Length && places[place] >= 0)
                {
                    Loadout.Switch(places[place]);
                }
            }
            else if (controls.WasNextWeaponPressedThisFrame)
            {
                Loadout.SwitchToNext();
            }
            else if (controls.WasPreviousWeaponPressedThisFrame)
            {
                Loadout.SwitchToPrevious();
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
            // The loadout outlives this scene: it's the Run's. Whatever of this Level still listened to it is dropped.
            var carried = Loadout;
            Disarm();
            carried?.DropListeners();
        }

        private void OnSwitched(PrimaryWeapon equipped)
        {
            Switched?.Invoke(equipped);
        }

        private void OnShotLanded(ShotOutcome outcome, Vector3 point)
        {
            ShotLanded?.Invoke(outcome, point);
        }
    }
}
