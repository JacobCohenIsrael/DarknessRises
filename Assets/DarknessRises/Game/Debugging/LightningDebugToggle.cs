#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Lightning;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the <c>Debug/ToggleLightning</c> action switches Lightning on and off.
    /// Compiled out of release builds.
    /// </summary>
    public sealed class LightningDebugToggle : IDisposable
    {
        public const string MapName = "Debug";
        public const string ActionName = "ToggleLightning";
        public const string ActionPath = MapName + "/" + ActionName;

        private readonly InputAction action;
        private readonly LightningScheduler scheduler;
        private readonly LightningView view;

        public LightningDebugToggle(InputActionReference toggle, LightningScheduler scheduler, LightningView view)
        {
            action = toggle.action;
            this.scheduler = scheduler;
            this.view = view;
            action.performed += OnToggle;
            action.Enable();
        }

        public void Dispose()
        {
            action.performed -= OnToggle;
            action.Disable();
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            var enabled = !scheduler.IsEnabled;
            scheduler.SetEnabled(enabled);
            if (!enabled)
            {
                view.StopFlash();
            }

            Debug.Log($"[Debug] Lightning {(enabled ? "on" : "off")}.");
        }
    }
}
#endif
