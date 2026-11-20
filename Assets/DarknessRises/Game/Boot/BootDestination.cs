using System;
using DarknessRises.Game.Saving;

namespace DarknessRises.Game.Boot
{
    /// <summary>Where the game goes when the app starts.</summary>
    public enum BootDestination
    {
        /// <summary>Straight into Level 1 of a new Run: the arcade first touch, with nothing in the way.</summary>
        FirstRun,

        /// <summary>The Shop, with a big PLAY that starts a new Run.</summary>
        Shop,
    }

    /// <summary>
    /// Picks the <see cref="BootDestination"/>: a first install boots straight into Level 1, because the first thing a
    /// new player does should be shooting. Once they have finished a Run, however it ended, every later launch opens
    /// the Shop, where their Credits are. Plain C# (ADR 0003).
    /// </summary>
    public static class BootDestinations
    {
        public static BootDestination For(PlayerSave save)
        {
            if (save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }

            return save.RunsFinished > 0 ? BootDestination.Shop : BootDestination.FirstRun;
        }
    }
}
