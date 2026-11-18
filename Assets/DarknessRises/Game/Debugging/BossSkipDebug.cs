#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Levels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the <c>Debug/SkipToBoss</c> action drops the current Station's fight and puts the character straight
    /// on the Boss's Station. Compiled out of release builds.
    /// </summary>
    public sealed class BossSkipDebug : IDisposable
    {
        public const string ActionName = "SkipToBoss";
        public const string ActionPath = LightningDebugToggle.MapName + "/" + ActionName;

        private readonly InputAction action;
        private readonly LevelView level;

        public BossSkipDebug(InputActionReference skip, LevelView level)
        {
            action = skip.action;
            this.level = level;
            action.performed += OnSkip;
            action.Enable();
        }

        public void Dispose()
        {
            action.performed -= OnSkip;
            action.Disable();
        }

        private void OnSkip(InputAction.CallbackContext context)
        {
            if (level.SkipToBoss())
            {
                Debug.Log("[Debug] Skipping to the Boss.");
            }
        }
    }
}
#endif
