using System;
using UnityEngine;

namespace DarknessRises.Game.Routes
{
    /// <summary>
    /// How the character's walk eases into and out of a Station, so a stop reads as a deliberate plant and a start as
    /// setting off, never as a walk cut dead: it slows over the last metres before a Station and picks its pace back up
    /// over the first moments after one. The default value doesn't ease at all.
    /// </summary>
    public readonly struct WalkEasing
    {
        /// <param name="brakingMetres">Metres before a Station over which the walk slows. 0 stops it dead.</param>
        /// <param name="setOffSeconds">Seconds after leaving a Station over which the walk picks up. 0 starts at full pace.</param>
        /// <param name="slowestFraction">
        /// The share of the walking speed the character arrives at and sets off from. Above 0, so it always arrives.
        /// </param>
        public WalkEasing(float brakingMetres, float setOffSeconds, float slowestFraction)
        {
            if (brakingMetres < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(brakingMetres), brakingMetres, "A braking distance can't be negative.");
            }

            if (setOffSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(setOffSeconds), setOffSeconds, "Setting off can't take less than no time.");
            }

            if (slowestFraction <= 0f || slowestFraction > 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slowestFraction), slowestFraction, "The slowest pace is a share of the walking speed, above 0 and at most 1.");
            }

            BrakingMetres = brakingMetres;
            SetOffSeconds = setOffSeconds;
            SlowestFraction = slowestFraction;
        }

        public float BrakingMetres { get; }
        public float SetOffSeconds { get; }
        public float SlowestFraction { get; }

        /// <summary>
        /// The share of the walking speed the character walks at, <paramref name="metresToStation"/> short of the next
        /// Station and <paramref name="secondsSinceSetOff"/> after leaving the last one.
        /// </summary>
        public float SpeedFraction(float metresToStation, float secondsSinceSetOff)
        {
            var fraction = 1f;
            if (BrakingMetres > 0f && metresToStation < BrakingMetres)
            {
                fraction = Mathf.Lerp(SlowestFraction, 1f, Mathf.Max(metresToStation, 0f) / BrakingMetres);
            }

            if (SetOffSeconds > 0f && secondsSinceSetOff < SetOffSeconds)
            {
                fraction = Mathf.Min(fraction, Mathf.Lerp(SlowestFraction, 1f, Mathf.Max(secondsSinceSetOff, 0f) / SetOffSeconds));
            }

            return fraction;
        }
    }
}
