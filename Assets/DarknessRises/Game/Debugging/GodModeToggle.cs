#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Health;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the <c>Debug/GodMode</c> action switches god mode on and off, so no hit lands on the character.
    /// Compiled out of release builds.
    /// </summary>
    public sealed class GodModeToggle : IDisposable
    {
        public const string ActionName = "GodMode";
        public const string ActionPath = LightningDebugToggle.MapName + "/" + ActionName;

        private readonly InputAction action;
        private readonly CharacterHealth health;

        public GodModeToggle(InputActionReference toggle, CharacterHealth health)
        {
            action = toggle.action;
            this.health = health;
            action.performed += OnToggle;
            action.Enable();
        }

        public void Dispose()
        {
            action.performed -= OnToggle;
            action.Disable();
        }

        /// <summary>Whether god mode is on.</summary>
        public bool IsOn => health.IsGodMode;

        /// <summary>Switches god mode on or off.</summary>
        public void Toggle()
        {
            health.IsGodMode = !health.IsGodMode;
            Debug.Log($"[Debug] God mode {(health.IsGodMode ? "on" : "off")}.");
        }

        private void OnToggle(InputAction.CallbackContext context) => Toggle();
    }
}
#endif
