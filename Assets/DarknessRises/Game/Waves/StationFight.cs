using System;
using DarknessRises.Game.Routes;

namespace DarknessRises.Game.Waves
{
    /// <summary>
    /// The character's stop at one Station, run to the Station rhythm: the Ready beat, then the Station's Waves, then
    /// the clear, the quiet and the go, after which the Station is cleared and the character sets off.
    /// A Station without Waves has no rhythm: it is cleared at once and the character walks straight through.
    /// One of these serves every Station of a Route, one stop at a time, so reaching a Station allocates nothing.
    /// </summary>
    public sealed class StationFight<TEnemyType>
    {
        private readonly StationRhythm rhythm;
        private WaveSchedule<TEnemyType> waves;
        private Station station;

        public StationFight(StationRhythm rhythm)
        {
            this.rhythm = rhythm ?? throw new ArgumentNullException(nameof(rhythm));
        }

        /// <summary>The Waves of the Station the character is stopped at, or null while it walks.</summary>
        public WaveSchedule<TEnemyType> Waves => waves;

        /// <summary>The Station the character is stopped at for its Waves, or null while it walks.</summary>
        public Station Station => station;

        /// <summary>
        /// The character reached <paramref name="atStation"/>, where <paramref name="stationWaves"/> are fought.
        /// With Waves, the Station rhythm starts; without, the Station is cleared at once.
        /// </summary>
        /// <returns>Whether a stop began.</returns>
        public bool Begin(Station atStation, WaveSchedule<TEnemyType> stationWaves)
        {
            if (atStation == null)
            {
                throw new ArgumentNullException(nameof(atStation));
            }

            if (stationWaves == null)
            {
                throw new ArgumentNullException(nameof(stationWaves));
            }

            if (waves != null)
            {
                throw new InvalidOperationException("The character fights at one Station at a time.");
            }

            if (stationWaves.WaveCount == 0)
            {
                atStation.Clear();
                return false;
            }

            if (!rhythm.Arrive(atStation))
            {
                throw new InvalidOperationException("The Station rhythm is still playing another Station's beats.");
            }

            station = atStation;
            waves = stationWaves;
            return true;
        }

        /// <summary>Runs the beats and, during the fight, the Waves.</summary>
        /// <returns>Whether the stop ended on this tick: the Station is cleared and the character sets off.</returns>
        public bool Tick(float deltaSeconds)
        {
            if (waves == null)
            {
                return false;
            }

            var wasFighting = rhythm.Beat == StationBeat.Fight;
            rhythm.Tick(deltaSeconds);
            if (rhythm.Beat == StationBeat.Fight)
            {
                if (!waves.HasBegun)
                {
                    waves.Begin();
                }

                // On the tick the Ready beat ends, the Waves get only what's left of the step.
                waves.Tick(wasFighting ? deltaSeconds : rhythm.SecondsInBeat);
                if (waves.IsDone)
                {
                    // The beats after the fight start now; ones of no length pass at once.
                    rhythm.WinFight();
                    rhythm.Tick(0f);
                }
            }

            if (rhythm.Beat != StationBeat.Walking)
            {
                return false;
            }

            waves = null;
            station = null;
            return true;
        }

        /// <summary>
        /// Debug: gives up on the stop where it is, leaving its Enemies to whoever holds them and the Station uncleared.
        /// </summary>
        /// <returns>The Station that was abandoned, or null if the character was walking.</returns>
        public Station Abandon()
        {
            if (waves == null)
            {
                return null;
            }

            var abandoned = station;
            waves = null;
            station = null;
            rhythm.Abandon();
            return abandoned;
        }
    }
}
