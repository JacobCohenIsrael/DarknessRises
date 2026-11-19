using System;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Hud
{
    /// <summary>How often the shoot hint is shown.</summary>
    public enum ShootHintFrequency
    {
        /// <summary>Never shown.</summary>
        Never,

        /// <summary>Over the first Enemy of every Run.</summary>
        OncePerRun,

        /// <summary>Over the first Enemy the game ever shows on this device, and never again.</summary>
        OncePerInstall,
    }

    /// <summary>
    /// The first-run hint: "tap to shoot", over the first Enemy that comes at the character in the first Level of a
    /// Run. It tells a new player in one line what their whole job is. It goes away as soon as they fire, or when
    /// that Enemy dies. Plain C# (ADR 0003): whoever shows it tells it what happens.
    /// </summary>
    public sealed class ShootHint
    {
        private readonly ShootHintFrequency frequency;
        private readonly bool wasShownOnThisInstall;

        /// <param name="frequency">How often the hint is shown.</param>
        /// <param name="wasShownOnThisInstall">Whether this device has shown the hint before, in any Run.</param>
        public ShootHint(ShootHintFrequency frequency, bool wasShownOnThisInstall)
        {
            this.frequency = frequency;
            this.wasShownOnThisInstall = wasShownOnThisInstall;
        }

        /// <summary>Whether the hint is on screen right now.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>Whether the hint has been shown in this Level, showing still or not.</summary>
        public bool WasShown { get; private set; }

        /// <summary>
        /// An Enemy came at the character in Level <paramref name="levelIndex"/> of the Run. The hint shows over the
        /// first one of the Run's first Level, as often as its frequency allows.
        /// </summary>
        /// <returns>Whether the hint shows over this Enemy.</returns>
        public bool TryShow(int levelIndex)
        {
            if (levelIndex < RunRecord.FirstLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), levelIndex, "Levels count from 1.");
            }

            if (WasShown || levelIndex != RunRecord.FirstLevel || frequency == ShootHintFrequency.Never)
            {
                return false;
            }

            if (frequency == ShootHintFrequency.OncePerInstall && wasShownOnThisInstall)
            {
                return false;
            }

            WasShown = true;
            IsShowing = true;
            return true;
        }

        /// <summary>The player fired, or the Enemy under the hint died: the hint has done its job.</summary>
        /// <returns>Whether it was showing.</returns>
        public bool Dismiss()
        {
            if (!IsShowing)
            {
                return false;
            }

            IsShowing = false;
            return true;
        }
    }
}
