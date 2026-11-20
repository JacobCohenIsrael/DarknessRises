using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Critters
{
    /// <summary>What the Level is doing, as far as Critters care: whether one may dart through the dark now, and how often.</summary>
    public enum CritterMoment
    {
        /// <summary>No Critters: the Level's title card or its end, death, or no Run.</summary>
        None,

        /// <summary>The character walks between Stations: the player relaxes, and a false alarm works best.</summary>
        Walking,

        /// <summary>The Clear banner, the quiet after it and the Go cue: the other moment a false alarm works best.</summary>
        AfterClear,

        /// <summary>A Station's Ready banner and its Waves: Critters are rare, so they never crowd a real fight.</summary>
        WaveFight,

        /// <summary>The Boss fight: never any Critters.</summary>
        BossFight,
    }

    /// <summary>Which <see cref="CritterMoment"/> the Run's phase and the Station rhythm's beat make. Plain C# (ADR 0003).</summary>
    public static class CritterMoments
    {
        public static CritterMoment Of(RunPhase phase, StationBeat beat)
        {
            switch (phase)
            {
                case RunPhase.BossFight:
                    return CritterMoment.BossFight;
                case RunPhase.Walking:
                case RunPhase.StationFight:
                    break;
                default:
                    return CritterMoment.None;
            }

            switch (beat)
            {
                case StationBeat.Ready:
                case StationBeat.Fight:
                    return CritterMoment.WaveFight;
                case StationBeat.Clear:
                case StationBeat.Quiet:
                case StationBeat.Go:
                    return CritterMoment.AfterClear;
                default:
                    return CritterMoment.Walking;
            }
        }
    }
}
