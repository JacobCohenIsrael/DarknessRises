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

        public PrimaryWeaponDebugSwitch(InputActionReference[] switchActions, PrimaryWeaponView primaryWeapon)
        {
            actions = new InputAction[switchActions.Length];
            handlers = new Action<InputAction.CallbackContext>[switchActions.Length];
            for (var i = 0; i < switchActions.Length; i++)
            {
                var index = i;
                actions[i] = switchActions[i].action;
                handlers[i] = _ =>
                {
                    if (primaryWeapon.Switch(index))
                    {
                        Debug.Log($"[Debug] Primary weapon {index + 1} equipped.");
                    }
                };
                actions[i].performed += handlers[i];
                actions[i].Enable();
            }
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
