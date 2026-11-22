using System;
using System.Collections.Generic;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Visibility;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Holds the character's Primary weapons in the scene: forwards the player's Fire, Reload and Weapon commands to
    /// the Run's <see cref="PrimaryWeaponLoadout"/>, turns the weapon mount toward the aim point,
    /// and shows each shot through that Weapon's <see cref="PrimaryWeaponEffects"/>. It holds every carried Primary's
    /// model under the mount and shows only the equipped one's, with the hands' grips and the muzzle where that model
    /// has them, and plays the <see cref="WeaponSwitchBeat"/> on a switch: the gun dips, the new one comes up, a click.
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

        [SerializeField, Tooltip("Tips and drops the gun for the switch beat. A child of the mount, at its origin.")]
        private Transform switchPivot;

        [SerializeField, Tooltip("Where the carried Primaries' models go: the recoil pivot, under the switch pivot.")]
        private Transform modelHolder;

        [SerializeField, Tooltip("The right hand's grip, moved to where the equipped model has it. A child of the model holder.")]
        private Transform rightGrip;

        [SerializeField, Tooltip("The left hand's grip, moved to where the equipped model has it. A child of the model holder.")]
        private Transform leftGrip;

        [SerializeField, Tooltip("2D, not played on awake. Plays the switch click.")]
        private AudioSource switchSound;

        [SerializeField, Tooltip("What a Shot stops at.")]
        private LayerMask shotHitLayers = Physics.DefaultRaycastLayers;

        private PrimaryWeaponEffects[] effects;
        private Action[] onFired;

        // For each Primary in the Shop's order, where it is in the loadout, or -1 when it isn't carried.
        private int[] places = Array.Empty<int>();
        private Action<ShotOutcome, Vector3> onShotLanded;
        private WeaponModel[] models = Array.Empty<WeaponModel>();
        private WeaponModel shownModel;
        private WeaponSwitchSettings switchSettings;
        private WeaponSwitchBeat beat;
        private Action onSwapped;
        private AudioClip switchClip;
        private float switchSoundSecondsLeft;

        /// <summary>The switch beat, while the character is armed.</summary>
        public WeaponSwitchBeat Beat => beat;

        /// <summary>The model in the hands: the equipped Primary's, or the one going down in a switch beat.</summary>
        public WeaponModel ShownModel => shownModel;

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
        /// A switch plays the beat in <paramref name="switchBeat"/>.
        /// </summary>
        public PrimaryWeaponLoadout Arm(
            IReadOnlyList<WeaponDefinition> definitions,
            IReadOnlyList<WeaponRunStats> runStats,
            PrimaryWeaponLoadout loadout,
            int[] carriedPlaces,
            ILoadedContent content,
            SceneLights lights,
            WeaponSwitchSettings switchBeat)
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

            // Every carried model is made once, here, and only switched on and off after.
            models = new WeaponModel[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                if (ContentReferences.IsSet(definitions[i].Model))
                {
                    models[i] = Instantiate(content.Prefab<WeaponModel>(definitions[i].Model), modelHolder);
                    models[i].gameObject.SetActive(false);
                }
            }

            switchSettings = switchBeat ?? throw new ArgumentNullException(nameof(switchBeat));
            switchClip = content.Clip(switchSettings.Clip);
            beat = new WeaponSwitchBeat(switchSettings.LowerSeconds, switchSettings.RaiseSeconds);
            onSwapped ??= OnSwapped;
            beat.Swapped += onSwapped;
            ShowModel(Loadout.EquippedIndex);
            PoseSwitch();

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

            if (beat != null)
            {
                beat.Swapped -= onSwapped;
            }

            for (var i = 0; i < models.Length; i++)
            {
                if (models[i] != null)
                {
                    Destroy(models[i].gameObject);
                }
            }

            models = Array.Empty<WeaponModel>();
            shownModel = null;
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

            var deltaSeconds = Time.deltaTime;
            beat.Tick(deltaSeconds);
            Loadout.Tick(deltaSeconds, beat.MayFire);
            PoseSwitch();
            StopClickWhenDone(deltaSeconds);
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
            beat.Start();
            Switched?.Invoke(equipped);
        }

        // The bottom of the beat: the equipped Primary's model takes the old one's place and comes up, with a click.
        private void OnSwapped()
        {
            ShowModel(Loadout.EquippedIndex);
            PlayClick();
        }

        // Only the model in the hands is on. The grips and the muzzle go where it has them.
        private void ShowModel(int index)
        {
            var model = index < models.Length ? models[index] : null;
            if (shownModel != null && shownModel != model)
            {
                shownModel.gameObject.SetActive(false);
            }

            shownModel = model;
            if (model == null)
            {
                return;
            }

            model.gameObject.SetActive(true);
            CopyPose(model.RightGrip, rightGrip);
            CopyPose(model.LeftGrip, leftGrip);
            if (model.Muzzle != null)
            {
                muzzle.localPosition = model.MuzzlePoint;
            }
        }

        private static void CopyPose(Transform from, Transform to)
        {
            if (from == null || to == null)
            {
                return;
            }

            from.GetPositionAndRotation(out var position, out var rotation);
            to.SetPositionAndRotation(position, rotation);
        }

        // Tips the gun down about the mount and drops it, eased in and out, by how far down the beat has it.
        private void PoseSwitch()
        {
            if (switchPivot == null)
            {
                return;
            }

            var down = Mathf.SmoothStep(0f, 1f, beat.Lowered);
            switchPivot.localRotation = Quaternion.Euler(switchSettings.LowerDegrees * down, 0f, 0f);
            switchPivot.localPosition = Vector3.down * (switchSettings.LowerMetres * down);
        }

        // A cut of the clip: it starts partway in and stops after the authored length.
        private void PlayClick()
        {
            if (switchSound == null || switchClip == null || switchClip.loadState != AudioDataLoadState.Loaded)
            {
                return;
            }

            switchSound.clip = switchClip;
            switchSound.pitch = switchSettings.Pitch;
            switchSound.SetVolume(switchSettings.Volume);
            switchSound.Play();
            switchSound.time = Mathf.Min(switchSettings.ClipStartSeconds, switchClip.length);
            switchSoundSecondsLeft = switchSettings.ClipSeconds / switchSettings.Pitch;
        }

        private void StopClickWhenDone(float deltaSeconds)
        {
            if (switchSoundSecondsLeft <= 0f)
            {
                return;
            }

            switchSoundSecondsLeft -= deltaSeconds;
            if (switchSoundSecondsLeft <= 0f)
            {
                switchSound.Stop();
            }
        }

        private void OnShotLanded(ShotOutcome outcome, Vector3 point)
        {
            ShotLanded?.Invoke(outcome, point);
        }
    }
}
