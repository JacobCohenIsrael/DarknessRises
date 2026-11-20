using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Critters
{
    /// <summary>
    /// When a Critter darts through the dark: by chance each second, at a rate that depends on the moment (mostly while
    /// the character walks and in the quiet after a Station's clear, rarely during a Wave fight, never in a Boss fight),
    /// never while the cap of Critters is out, and not again until a cooldown has passed. Plain C# (ADR 0003).
    /// </summary>
    public sealed class CritterSchedule
    {
        private readonly CritterRates rates;
        private readonly float cooldownSeconds;
        private readonly int maxAlive;
        private readonly IRandom random;
        private float cooldownLeft;

        /// <param name="rates">Chance per second of a Critter in each moment.</param>
        /// <param name="cooldownSeconds">Seconds after a Critter darts before another may.</param>
        /// <param name="firstDelaySeconds">Seconds from the Level's start before the first may.</param>
        /// <param name="maxAlive">Most Critters out at once.</param>
        public CritterSchedule(CritterRates rates, float cooldownSeconds, float firstDelaySeconds, int maxAlive, IRandom random)
        {
            if (cooldownSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cooldownSeconds), cooldownSeconds, "A cooldown can't be negative.");
            }

            if (firstDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(firstDelaySeconds), firstDelaySeconds, "A delay can't be negative.");
            }

            if (maxAlive < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAlive), maxAlive, "The cap can't be negative.");
            }

            this.rates = rates;
            this.cooldownSeconds = cooldownSeconds;
            this.maxAlive = maxAlive;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            cooldownLeft = firstDelaySeconds;
        }

        /// <summary>Seconds until a Critter may dart again, whatever the moment.</summary>
        public float CooldownLeft => cooldownLeft;

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/> in <paramref name="moment"/>, with <paramref name="alive"/>
        /// Critters out. True when a Critter should dart now; the cooldown then starts.
        /// </summary>
        public bool Tick(float deltaSeconds, CritterMoment moment, int alive)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (cooldownLeft > 0f)
            {
                cooldownLeft = Math.Max(0f, cooldownLeft - deltaSeconds);
                return false;
            }

            var chancePerSecond = rates.For(moment);
            if (chancePerSecond <= 0f || alive >= maxAlive || deltaSeconds <= 0f)
            {
                return false;
            }

            // Per frame, the chance of at least one event at this rate: frame-rate independent.
            var chance = 1f - (float)Math.Exp(-chancePerSecond * deltaSeconds);
            if (random.Range(0f, 1f) >= chance)
            {
                return false;
            }

            cooldownLeft = cooldownSeconds;
            return true;
        }
    }

    /// <summary>The chance per second of a Critter in each <see cref="CritterMoment"/>. A Boss fight never has one.</summary>
    public readonly struct CritterRates
    {
        public CritterRates(float walking, float afterClear, float waveFight)
        {
            Walking = NotNegative(walking, nameof(walking));
            AfterClear = NotNegative(afterClear, nameof(afterClear));
            WaveFight = NotNegative(waveFight, nameof(waveFight));
        }

        public float Walking { get; }
        public float AfterClear { get; }
        public float WaveFight { get; }

        public float For(CritterMoment moment)
        {
            return moment switch
            {
                CritterMoment.Walking => Walking,
                CritterMoment.AfterClear => AfterClear,
                CritterMoment.WaveFight => WaveFight,
                _ => 0f,
            };
        }

        private static float NotNegative(float rate, string name)
        {
            return rate >= 0f ? rate : throw new ArgumentOutOfRangeException(name, rate, "A rate can't be negative.");
        }
    }
}
