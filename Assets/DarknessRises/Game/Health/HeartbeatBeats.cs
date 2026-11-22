namespace DarknessRises.Game.Health
{
    /// <summary>
    /// Finds each strong beat of a looping heartbeat clip as it's heard, from the clip's playback time, so whatever
    /// follows the heartbeat (the low-health Haptics) is in time with it and never drifts. Plain C# (ADR 0003).
    /// </summary>
    public sealed class HeartbeatBeats
    {
        private readonly HeartbeatRhythm rhythm;
        private float lastSince = -1f;

        public HeartbeatBeats(HeartbeatRhythm rhythm)
        {
            this.rhythm = rhythm;
        }

        /// <summary>
        /// The clip is <paramref name="clipSeconds"/> in, <paramref name="deltaSeconds"/> after the last call. True when
        /// a strong beat fell since the last call: the time since the last beat went back down. The first call after
        /// <see cref="Reset"/> counts a beat that fell within <paramref name="deltaSeconds"/>.
        /// </summary>
        public bool Advance(float clipSeconds, float deltaSeconds)
        {
            var since = rhythm.SecondsSinceBeat(clipSeconds);
            var beat = lastSince < 0f ? since <= deltaSeconds : since < lastSince;
            lastSince = since;
            return beat;
        }

        /// <summary>The heartbeat stopped, or another one is heard now: start over.</summary>
        public void Reset() => lastSince = -1f;
    }
}
