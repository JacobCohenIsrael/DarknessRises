using System;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Pausing
{
    /// <summary>
    /// The pause of one Level: the player pauses (the Pause action or the on-screen button), or the app loses focus,
    /// and the pause menu offers Resume, Restart and Exit. Restart and Exit go through the <see cref="RunFlow"/>.
    /// Plain C# (ADR 0003): whoever shows the Level freezes it while <see cref="IsFrozen"/>.
    /// </summary>
    /// <remarks>
    /// The game pauses only while the character is alive in a Level: the death screen is its own stop, and there is
    /// nothing to pause before a Run or after it ends. Getting focus back doesn't resume: the player does, from the menu.
    /// After Restart or Exit the Level stays frozen (<see cref="PauseState.Left"/>) until its scene goes, so nothing
    /// in it moves or sounds behind the end screen or while the next Run loads.
    /// </remarks>
    public sealed class GamePause
    {
        private readonly RunFlow run;

        public GamePause(RunFlow runFlow)
        {
            run = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
        }

        /// <summary>Raised whenever <see cref="State"/> changes.</summary>
        public event Action<PauseState> StateChanged;

        public PauseState State { get; private set; } = PauseState.Playing;

        /// <summary>Whether the pause menu is up.</summary>
        public bool IsPaused => State == PauseState.Paused;

        /// <summary>Whether everything in the Level is frozen: paused, or left from the pause menu.</summary>
        public bool IsFrozen => State != PauseState.Playing;

        /// <summary>Whether <see cref="Pause"/> would pause now: the Level plays and the character is alive.</summary>
        public bool CanPause => State == PauseState.Playing && run.IsInRun && run.Phase != RunPhase.Dead;

        /// <summary>Freezes the Level and brings up the pause menu.</summary>
        public bool Pause()
        {
            if (!CanPause)
            {
                return false;
            }

            Enter(PauseState.Paused);
            return true;
        }

        /// <summary>The app lost focus or went to the background: pause, so the character doesn't die unwatched.</summary>
        public bool LoseFocus() => Pause();

        /// <summary>The Pause action: pauses while playing, resumes while paused.</summary>
        public bool Toggle() => IsPaused ? Resume() : Pause();

        /// <summary>Resume: the Level carries on where it froze.</summary>
        public bool Resume()
        {
            if (!IsPaused)
            {
                return false;
            }

            Enter(PauseState.Playing);
            return true;
        }

        /// <summary>Restart from the pause menu: the Run ends and a new one starts from Level 1.</summary>
        public bool Restart()
        {
            if (!IsPaused)
            {
                return false;
            }

            Enter(PauseState.Left);
            run.Restart();
            return true;
        }

        /// <summary>Exit from the pause menu: the Run ends, and the end screen shows its record.</summary>
        public bool Exit()
        {
            if (!IsPaused)
            {
                return false;
            }

            Enter(PauseState.Left);
            run.Exit();
            return true;
        }

        private void Enter(PauseState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
