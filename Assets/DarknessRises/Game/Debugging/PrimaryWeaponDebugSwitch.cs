#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the <c>Debug/PrimaryWeapon1</c>, <c>2</c> and <c>3</c> actions (keys 1–3) equip the Pistol,
    /// Machine gun and Flamethrower. The Nth action equips the Nth Primary in the loadout.
    /// Compiled out of release builds.
    /// </summary>
    public sealed class PrimaryWeaponDebugSwitch : IDisposable
    {
        public const string MapName = "Debug";
        public const string ActionPrefix = "PrimaryWeapon";

        private readonly InputAction[] actions;
        private readonly Action<InputAction.CallbackContext>[] handlers;
        private readonly PrimaryWeaponView primaryWeapon;

        public PrimaryWeaponDebugSwitch(InputActionReference[] switchActions, PrimaryWeaponView primaryWeapon)
        {
            this.primaryWeapon = primaryWeapon;
            actions = new InputAction[switchActions.Length];
            handlers = new Action<InputAction.CallbackContext>[switchActions.Length];
            for (var i = 0; i < switchActions.Length; i++)
            {
                var index = i;
                actions[i] = switchActions[i].action;
                handlers[i] = _ => Equip(index);
                actions[i].performed += handlers[i];
                actions[i].Enable();
            }
        }

        /// <summary>The index of the equipped Primary in the loadout, or -1 before it's armed.</summary>
        public int EquippedIndex => primaryWeapon.Loadout?.EquippedIndex ?? -1;

        /// <summary>Equips the Primary at <paramref name="index"/> in the loadout (from 0).</summary>
        public bool Equip(int index)
        {
            if (!primaryWeapon.Switch(index))
            {
                return false;
            }

            Debug.Log($"[Debug] Primary weapon {index + 1} equipped.");
            return true;
        }

        /// <summary>The path of the action that equips the Primary numbered <paramref name="number"/> (from 1).</summary>
        public static string ActionPath(int number)
        {
            return $"{MapName}/{ActionPrefix}{number}";
        }

        public void Dispose()
        {
            for (var i = 0; i < actions.Length; i++)
            {
                actions[i].performed -= handlers[i];
                actions[i].Disable();
            }
        }
    }
}
#endif
