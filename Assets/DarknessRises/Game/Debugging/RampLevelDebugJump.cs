#if UNITY_EDITOR || DEBUG
using System;
using DarknessRises.Game.Runs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Debugging
{
    /// <summary>
    /// Debug only: the <c>Debug/JumpToRampLevel</c> action leaves the current Level for Ramp level N of the same Run
    /// (N is authored on the Ramp definition). Pressed again at or past N, it jumps on to the next Level, so the Ramp
    /// can be climbed a Level at a time. Compiled out of release builds.
    /// </summary>
    public sealed class RampLevelDebugJump : IDisposable
    {
        public const string ActionName = "JumpToRampLevel";
        public const string ActionPath = LightningDebugToggle.MapName + "/" + ActionName;

        private readonly InputAction action;
        private readonly RunFlow run;
        private readonly int jumpLevel;

        public RampLevelDebugJump(InputActionReference jump, RunFlow run, int jumpLevel)
        {
            action = jump.action;
            this.run = run;
            this.jumpLevel = jumpLevel;
            action.performed += OnJump;
            action.Enable();
        }

        public void Dispose()
        {
            action.performed -= OnJump;
            action.Disable();
        }

        /// <summary>Leaves for Ramp level N, or for the next Level once at or past it.</summary>
        public bool Jump()
        {
            var target = run.LevelIndex < jumpLevel ? jumpLevel : run.LevelIndex + 1;
            if (!run.JumpToLevel(target))
            {
                return false;
            }

            Debug.Log($"[Debug] Jumping to Ramp level {target}.");
            return true;
        }

        private void OnJump(InputAction.CallbackContext context) => Jump();
    }
}
#endif
