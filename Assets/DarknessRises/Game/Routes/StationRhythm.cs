using System;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// The rhythm every Station with Waves plays, so the ride reads as stop, fight, go: the character arrives and gets
    /// ready, the fight runs until whoever owns the Waves says it's won, the clear is called, a quiet moment passes, the
    /// go is cued, and then the Station is cleared and the character sets off. This decides which beat comes next and
    /// when; views listen to <see cref="BeatChanged"/> and show it.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class StationRhythm
    {
        private readonly StationRhythmTiming timing;

        public StationRhythm(StationRhythmTiming timing)
        {
            this.timing = timing;
        }

        /// <summary>Raised whenever <see cref="Beat"/> changes. <see cref="Station"/> already tells where.</summary>
        public event Action<StationBeat> BeatChanged;

        public StationBeat Beat { get; private set; } = StationBeat.Walking;

        /// <summary>The Station the beats are playing at, or null while the character walks.</summary>
        public Station Station { get; private set; }

        /// <summary>Seconds the current beat has run.</summary>
        public float SecondsInBeat { get; private set; }

        /// <summary>How long the current beat lasts, or 0 when it isn't timed (walking and the fight).</summary>
        public float BeatSeconds => timing.SecondsOf(Beat);

        /// <summary>Seconds the last fight took, from the end of its Ready beat to its last death. 0 before the first is won.</summary>
        public float FightSeconds { get; private set; }

        /// <summary>The character stopped at <paramref name="station"/>: the Ready beat starts. Only while walking.</summary>
        /// <returns>Whether the rhythm started.</returns>
        public bool Arrive(Station station)
        {
            if (station == null)
            {
                throw new ArgumentNullException(nameof(station));
            }

            if (Beat != StationBeat.Walking)
            {
                return false;
            }

            Station = station;
            Enter(StationBeat.Ready, 0f);
            return true;
        }

        /// <summary>The Station's last Wave is dead: the clear is called. Only during the fight.</summary>
        /// <returns>Whether the fight was on.</returns>
        public bool WinFight()
        {
            if (Beat != StationBeat.Fight)
            {
                return false;
            }

            FightSeconds = SecondsInBeat;
            Enter(StationBeat.Clear, 0f);
            return true;
        }

        /// <summary>
        /// Runs the timed beats. A beat that ends hands the rest of the step to the next, so beats of no length pass in
        /// one tick. When the Go beat ends, the Station is cleared and the character walks on.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (Beat == StationBeat.Walking)
            {
                return;
            }

            SecondsInBeat += deltaSeconds;
            while (IsTimed(Beat) && SecondsInBeat >= timing.SecondsOf(Beat))
            {
                var left = SecondsInBeat - timing.SecondsOf(Beat);
                switch (Beat)
                {
                    case StationBeat.Ready:
                        Enter(StationBeat.Fight, left);
                        break;
                    case StationBeat.Clear:
                        Enter(StationBeat.Quiet, left);
                        break;
                    case StationBeat.Quiet:
                        Enter(StationBeat.Go, left);
                        break;
                    default:
                        SetOff();
                        break;
                }
            }
        }

        /// <summary>
        /// Debug: drops the beats where they are and goes back to walking, without clearing the Station.
        /// Whoever skips the character ahead moves it on.
        /// </summary>
        /// <returns>Whether any beat was playing.</returns>
        public bool Abandon()
        {
            if (Beat == StationBeat.Walking)
            {
                return false;
            }

            Station = null;
            Enter(StationBeat.Walking, 0f);
            return true;
        }

        private static bool IsTimed(StationBeat beat)
        {
            return beat == StationBeat.Ready || beat == StationBeat.Clear || beat == StationBeat.Quiet || beat == StationBeat.Go;
        }

        // The Station is cleared first, so by the time anyone hears of the walk the character is already free to go.
        private void SetOff()
        {
            var station = Station;
            Station = null;
            Beat = StationBeat.Walking;
            SecondsInBeat = 0f;
            station.Clear();
            BeatChanged?.Invoke(StationBeat.Walking);
        }

        private void Enter(StationBeat beat, float secondsIn)
        {
            Beat = beat;
            SecondsInBeat = secondsIn;
            BeatChanged?.Invoke(beat);
        }
    }
}
