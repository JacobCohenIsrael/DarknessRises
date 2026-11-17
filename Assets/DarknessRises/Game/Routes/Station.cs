using System;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// A point on the Route where the character stops. It moves on once the Station is cleared.
    /// Whoever owns the fight at the Station (for now a placeholder timer, later its Waves) calls <see cref="Clear"/>.
    /// </summary>
    public sealed class Station
    {
        public Station(int index, float distance)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "A Station's index can't be negative.");
            }

            if (distance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distance), distance, "A Station can't sit before the start of the Route.");
            }

            Index = index;
            Distance = distance;
        }

        /// <summary>Raised once, when the Station is cleared.</summary>
        public event Action<Station> Cleared;

        /// <summary>Order of this Station along its Route, from 0.</summary>
        public int Index { get; }

        /// <summary>Metres along the Route at which the character stops.</summary>
        public float Distance { get; }

        public bool IsCleared { get; private set; }

        public void Clear()
        {
            if (IsCleared)
            {
                return;
            }

            IsCleared = true;
            Cleared?.Invoke(this);
        }
    }
}
