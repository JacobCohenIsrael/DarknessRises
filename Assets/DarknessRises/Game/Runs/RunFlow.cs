using System;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// The state machine of a Run (ADR 0005): each Level's start beat, walking, Station fights, the Boss fight and the
    /// end beat, then straight into the next Level, endlessly, until the character dies. Death leads to Continue
    /// (back into the same Level, a limited number of times per Run), Restart (a new Run from Level 1) or Exit (the end
    /// screen). It tracks the Level index, which drives the Ramp, and the Run's <see cref="RunRecord"/>.
    /// Plain C# (ADR 0003): whoever shows the Level reports what happens there.
    /// </summary>
    /// <remarks>
    /// A report that doesn't fit the current phase is ignored and returns false, so late news never breaks the flow.
    /// While the character is dead the Run still follows the Level's own steps (a Station cleared by a Grenade still in
    /// the air, say), so a Continue goes back to where the Level is by then.
    /// The pause menu calls <see cref="Restart"/> and <see cref="Exit"/>, which work from any phase of a Level.
    /// </remarks>
    public sealed class RunFlow
    {
        /// <summary>Raised when a Level starts: the first of a new Run, the next one in the chain, or a jump. Carries its index.</summary>
        public event Action<int> LevelStarted;

        /// <summary>Raised whenever <see cref="Phase"/> changes.</summary>
        public event Action<RunPhase> PhaseChanged;

        /// <summary>Raised when the Run ends with Exit, before the end screen shows its record.</summary>
        public event Action<RunRecord> RunEnded;

        public RunPhase Phase { get; private set; } = RunPhase.NotStarted;

        /// <summary>The current Level, counted from 1. It drives the Ramp.</summary>
        public int LevelIndex { get; private set; }

        /// <summary>The current Run's Level reached and Enemies killed. A new one for every Run.</summary>
        public RunRecord Record { get; private set; } = new();

        /// <summary>How many times the character has Continued in this Run. Back to 0 with every new Run.</summary>
        public int ContinuesUsed { get; private set; }

        // The phase of the Level the character is dead in: where a Continue goes back to.
        private RunPhase resumePhase;

        /// <summary>Whether a Level of the Run is in play: from its start beat to its end beat, alive or dead.</summary>
        public bool IsInRun => Phase != RunPhase.NotStarted && Phase != RunPhase.Ended;

        /// <summary>Starts a new Run from Level 1. Only before the first Run or after Exit.</summary>
        public bool StartNewRun()
        {
            if (IsInRun)
            {
                return false;
            }

            BeginRun();
            return true;
        }

        /// <summary>Ends the current Run and starts a new one from Level 1: after death, or from the pause menu.</summary>
        public bool Restart()
        {
            if (!IsInRun)
            {
                return false;
            }

            BeginRun();
            return true;
        }

        /// <summary>Ends the Run: after death, or from the pause menu. The end screen shows its <see cref="Record"/>.</summary>
        public bool Exit()
        {
            if (!IsInRun)
            {
                return false;
            }

            Enter(RunPhase.Ended);
            RunEnded?.Invoke(Record);
            return true;
        }

        /// <summary>The Level's start beat is over: the character walks the Route.</summary>
        public bool FinishStartBeat() => Move(RunPhase.LevelStart, RunPhase.Walking);

        /// <summary>The character reached a Station with Waves to fight.</summary>
        public bool BeginStationFight() => Move(RunPhase.Walking, RunPhase.StationFight);

        /// <summary>The Station's last Wave is dead: the character walks on.</summary>
        public bool ClearStationFight() => Move(RunPhase.StationFight, RunPhase.Walking);

        /// <summary>The character reached the Boss.</summary>
        public bool BeginBossFight() => Move(RunPhase.Walking, RunPhase.BossFight);

        /// <summary>The last fight of the Level is over: its end beat plays.</summary>
        public bool BeginLevelEnd()
        {
            return Move(RunPhase.Walking, RunPhase.LevelEnd) || Move(RunPhase.BossFight, RunPhase.LevelEnd);
        }

        /// <summary>The character walked out the far door: the next, harder Level starts.</summary>
        public bool FinishLevel()
        {
            if (Phase != RunPhase.LevelEnd)
            {
                return false;
            }

            StartLevel(LevelIndex + 1);
            return true;
        }

        /// <summary>The character died.</summary>
        public bool Die()
        {
            if (!IsInRun || Phase == RunPhase.Dead)
            {
                return false;
            }

            resumePhase = Phase;
            Enter(RunPhase.Dead);
            return true;
        }

        /// <summary>
        /// Whether the dead character may Continue: it's dead and the Run has used fewer than
        /// <paramref name="continuesPerRun"/> Continues (the authored allowance).
        /// </summary>
        public bool CanContinue(int continuesPerRun)
        {
            if (continuesPerRun < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(continuesPerRun), continuesPerRun, "A Run can't allow fewer than no Continues.");
            }

            return Phase == RunPhase.Dead && ContinuesUsed < continuesPerRun;
        }

        /// <summary>
        /// The character comes back to life where it died: the Run goes back to the phase of the Level it died in and
        /// uses up one of its <paramref name="continuesPerRun"/> Continues. Only while <see cref="CanContinue"/>.
        /// </summary>
        public bool Continue(int continuesPerRun)
        {
            if (!CanContinue(continuesPerRun))
            {
                return false;
            }

            ContinuesUsed++;
            Enter(resumePhase);
            return true;
        }

        /// <summary>An Enemy died. Counts while a Run is on, including after the character's death (a Grenade still in the air).</summary>
        public bool CountKill()
        {
            if (!IsInRun)
            {
                return false;
            }

            Record.CountKill();
            return true;
        }

        /// <summary>A kill earned <paramref name="points"/> of Score. Counts while a Run is on, like the kill itself.</summary>
        public bool AddScore(int points)
        {
            if (!IsInRun)
            {
                return false;
            }

            Record.AddScore(points);
            return true;
        }

        /// <summary>
        /// Debug: leaves the current Level for Level <paramref name="levelIndex"/> of the same Run, with its Ramp.
        /// The key that calls it is compiled out of release builds.
        /// </summary>
        public bool JumpToLevel(int levelIndex)
        {
            if (levelIndex < RunRecord.FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Levels count from 1.");
            }

            if (!IsInRun)
            {
                return false;
            }

            StartLevel(levelIndex);
            return true;
        }

        private void BeginRun()
        {
            Record = new RunRecord();
            ContinuesUsed = 0;
            StartLevel(RunRecord.FirstLevel);
        }

        private void StartLevel(int levelIndex)
        {
            LevelIndex = levelIndex;
            Record.Reach(levelIndex);
            Enter(RunPhase.LevelStart);
            LevelStarted?.Invoke(levelIndex);
        }

        private bool Move(RunPhase from, RunPhase to)
        {
            // Dead, the Run keeps up with the Level, for a Continue to go back to.
            if (Phase == RunPhase.Dead && resumePhase == from)
            {
                resumePhase = to;
                return true;
            }

            if (Phase != from)
            {
                return false;
            }

            Enter(to);
            return true;
        }

        private void Enter(RunPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
