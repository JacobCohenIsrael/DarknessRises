using System;
using System.Collections.Generic;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Gestures;
using DarknessRises.Game.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The Weapon button, beside the magazine count on the HUD: it shows the equipped Primary weapon's icon (its
    /// silhouette), its name and its ammo reserve, and a tap or click on it switches to the next unlocked Primary that has ammo. It's there only while
    /// the character carries more than one Primary; with the Pistol alone there is nothing to switch to. When a
    /// Weapon runs dry and the Pistol takes over, its face flashes, so the player sees why the Weapon changed.
    /// </summary>
    /// <remarks>
    /// The tap presses the Next Weapon command on the Gesture device, so it drives the same named action as the mouse
    /// wheel. The button is an interactable UI control, so a press on it never fires and never becomes a Gesture
    /// (<see cref="UiPointerFilter"/>); it's a small tile in the HUD's ammo corner, away from the Grenade button, and
    /// nothing on it looks like a movement control. Its texts are rewritten only when the Weapon or its reserve changes,
    /// and it runs every frame only while the run-dry flash fades.
    /// </remarks>
    public sealed class WeaponButtonView : MonoBehaviour
    {
        [SerializeField, Tooltip("The button itself: the whole tile.")]
        private Button button;

        [SerializeField, Tooltip("The button's face.")]
        private Image face;

        [SerializeField, Tooltip("The equipped Primary weapon's icon, beside its name. Hidden for a Weapon without one.")]
        private Image icon;

        [SerializeField, Tooltip("The equipped Primary weapon's name.")]
        private TMP_Text weaponName;

        [SerializeField, Tooltip("The rounds left in the equipped Weapon's ammo reserve.")]
        private TMP_Text reserve;

        [SerializeField, Tooltip("Presses the Next Weapon command when the button is tapped.")]
        private GestureInput gestures;

        private WeaponButtonSettings settings;
        private PrimaryWeaponLoadout loadout;
        private IReadOnlyList<string> names;
        private IReadOnlyList<WeaponDefinition> definitions;
        private AmmoReserve shownReserve;
        private RunDryFlash flash;
        private Action<PrimaryWeapon> onSwitched;
        private Action onReserveChanged;
        private Action<int> onRanDry;

        /// <summary>Whether the button is on the HUD: the character carries more than one Primary.</summary>
        public bool IsShown => loadout != null && loadout.Count > 1;

        /// <summary>
        /// Shows <paramref name="carried"/>'s equipped Primary, named by <paramref name="weaponNames"/> and drawn with the
        /// icon of its definition in <paramref name="weaponDefinitions"/> (one of each for each Primary carried, in the
        /// same order).
        /// </summary>
        public void Show(
            PrimaryWeaponLoadout carried,
            IReadOnlyList<string> weaponNames,
            IReadOnlyList<WeaponDefinition> weaponDefinitions,
            WeaponButtonSettings buttonSettings)
        {
            Unbind();

            loadout = carried ?? throw new ArgumentNullException(nameof(carried));
            names = weaponNames ?? throw new ArgumentNullException(nameof(weaponNames));
            definitions = weaponDefinitions ?? throw new ArgumentNullException(nameof(weaponDefinitions));
            settings = buttonSettings ?? throw new ArgumentNullException(nameof(buttonSettings));
            flash = new RunDryFlash(settings.RunDryFlashSeconds);
            onSwitched ??= OnSwitched;
            onReserveChanged ??= ShowReserve;
            onRanDry ??= OnRanDry;
            loadout.Switched += onSwitched;
            loadout.RanDry += onRanDry;
            button.onClick.AddListener(OnPressed);

            // Nothing to do each frame until a Weapon runs dry.
            enabled = false;
            face.color = settings.FaceColor;
            weaponName.color = settings.TextColor;
            if (icon != null)
            {
                icon.color = settings.IconColor;
            }

            gameObject.SetActive(IsShown);
            ShowEquipped();
        }

        // Runs only while the run-dry flash fades.
        private void Update()
        {
            // Before the button is shown there is nothing to fade.
            if (flash == null)
            {
                enabled = false;
                return;
            }

            flash.Tick(Time.deltaTime);
            face.color = Color.Lerp(settings.FaceColor, settings.RunDryFlashColor, flash.Strength);
            if (!flash.IsFlashing)
            {
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (loadout == null)
            {
                return;
            }

            loadout.Switched -= onSwitched;
            loadout.RanDry -= onRanDry;
            button.onClick.RemoveListener(OnPressed);
            enabled = false;
            WatchReserve(null);
            loadout = null;
        }

        private void OnPressed()
        {
            if (gestures != null)
            {
                gestures.PressNextWeapon();
            }
        }

        private void OnSwitched(PrimaryWeapon _) => ShowEquipped();

        // A Weapon fired its last round: the face flashes as the Pistol takes over.
        private void OnRanDry(int _)
        {
            flash.Start();
            enabled = flash.IsFlashing;
            if (enabled)
            {
                face.color = settings.RunDryFlashColor;
            }
        }

        private void ShowEquipped()
        {
            var index = loadout.EquippedIndex;
            weaponName.text = index < names.Count ? names[index] : string.Empty;
            if (icon != null)
            {
                var sprite = index < definitions.Count && definitions[index] != null ? definitions[index].Icon : null;
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            WatchReserve(loadout.Equipped.Reserve);
            ShowReserve();
        }

        private void WatchReserve(AmmoReserve watched)
        {
            if (shownReserve != null)
            {
                shownReserve.Changed -= onReserveChanged;
            }

            shownReserve = watched;
            if (shownReserve != null)
            {
                shownReserve.Changed += onReserveChanged;
            }
        }

        private void ShowReserve()
        {
            if (shownReserve.IsUnlimited)
            {
                reserve.text = settings.UnlimitedText;
                reserve.color = settings.TextColor;
                return;
            }

            // TMP's numeric SetText formats without allocating.
            reserve.SetText(settings.ReserveFormat, (float)shownReserve.Rounds);
            reserve.color = shownReserve.IsEmpty ? settings.EmptyReserveColor : settings.TextColor;
        }
    }
}
