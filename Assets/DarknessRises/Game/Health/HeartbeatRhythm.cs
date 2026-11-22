using System;

namespace DarknessRises.Game.Health
{
    /// <summary>How the heartbeat in one looping clip is timed: its tempo, when its first strong beat falls, and how soon the soft beat follows.</summary>
    public readonly struct HeartbeatRhythm
    {
        public HeartbeatRhythm(float beatsPerMinute, float firstBeatSeconds, float softBeatDelaySeconds)
        {
            if (beatsPerMinute <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerMinute), beatsPerMinute, "A heartbeat needs a tempo.");
            }

            if (firstBeatSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(firstBeatSeconds), firstBeatSeconds, "The first beat can't come before the clip.");
            }

            var period = 60f / beatsPerMinute;
            if (softBeatDelaySeconds < 0f || softBeatDelaySeconds >= period)
            {
                throw new ArgumentOutOfRangeException(nameof(softBeatDelaySeconds), softBeatDelaySeconds, "The soft beat comes within the beat, after the strong one.");
            }

            BeatsPerMinute = beatsPerMinute;
            FirstBeatSeconds = firstBeatSeconds;
            SoftBeatDelaySeconds = softBeatDelaySeconds;
        }

        public float BeatsPerMinute { get; }
        public float FirstBeatSeconds { get; }
        public float SoftBeatDelaySeconds { get; }

        /// <summary>Seconds from one strong beat to the next.</summary>
        public float PeriodSeconds => 60f / BeatsPerMinute;

        /// <summary>Seconds since the last strong beat, <paramref name="clipSeconds"/> into the looping clip.</summary>
        public float SecondsSinceBeat(float clipSeconds)
        {
            var period = PeriodSeconds;
            var since = (clipSeconds - FirstBeatSeconds) % period;
            return since < 0f ? since + period : since;
        }
    }
}
