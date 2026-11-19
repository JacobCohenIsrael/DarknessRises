using System;

namespace DarknessRises.Game.Routes
{
    /// <summary>How long each timed beat of the Station rhythm lasts, copied out of the authored settings.</summary>
    public readonly struct StationRhythmTiming
    {
        public StationRhythmTiming(float readySeconds, float clearSeconds, float quietSeconds, float goSeconds)
        {
            if (readySeconds < 0f || clearSeconds < 0f || quietSeconds < 0f || goSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(readySeconds), "A beat can't last less than no time.");
            }

            ReadySeconds = readySeconds;
            ClearSeconds = clearSeconds;
            QuietSeconds = quietSeconds;
            GoSeconds = goSeconds;
        }

        /// <summary>Seconds from the character stopping at a Station until its Waves start.</summary>
        public float ReadySeconds { get; }

        /// <summary>Seconds the clear is called for, from the last Wave's death.</summary>
        public float ClearSeconds { get; }

        /// <summary>Seconds of quiet after the clear.</summary>
        public float QuietSeconds { get; }

        /// <summary>Seconds the cue to move on shows before the character sets off.</summary>
        public float GoSeconds { get; }

        /// <summary>How long <paramref name="beat"/> lasts, or 0 for a beat that isn't timed (walking and the fight).</summary>
        public float SecondsOf(StationBeat beat)
        {
            return beat switch
            {
                StationBeat.Ready => ReadySeconds,
                StationBeat.Clear => ClearSeconds,
                StationBeat.Quiet => QuietSeconds,
                StationBeat.Go => GoSeconds,
                _ => 0f,
            };
        }
    }
}
