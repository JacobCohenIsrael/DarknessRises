using System;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// The character's Grenade charges: whether a throw is allowed, and the timer that refills them one at a time.
    /// Plain C#: time arrives as an explicit delta and commands as method calls (ADR 0003).
    /// </summary>
    /// <remarks>
    /// The character starts with every charge. While any charge is missing, the refill timer runs and gives one back
    /// each time it completes; it stops, at zero, once the charges are full. Spending a charge mid-refill doesn't
    /// restart the timer, so throwing never delays the charge that is already on its way.
    /// </remarks>
    public sealed class GrenadeCharges
    {
        private readonly GrenadeChargeStats stats;
        private float refillSecondsElapsed;

        public GrenadeCharges(GrenadeChargeStats stats)
        {
            this.stats = stats;
            Charges = stats.MaxCharges;
        }

        /// <summary>Raised whenever <see cref="Charges"/> changes: a charge spent or a charge refilled.</summary>
        public event Action ChargesChanged;

        public int Charges { get; private set; }
        public int MaxCharges => stats.MaxCharges;
        public bool CanThrow => Charges > 0;
        public bool IsFull => Charges >= stats.MaxCharges;

        /// <summary>How far the next charge is from refilling, from 0 to 1. 0 when the charges are full.</summary>
        public float RefillProgress => IsFull ? 0f : refillSecondsElapsed / stats.RefillSeconds;

        /// <summary>Spends one charge for a throw. Does nothing when there are none.</summary>
        /// <returns>Whether a charge was spent.</returns>
        public bool TrySpend()
        {
            if (!CanThrow)
            {
                return false;
            }

            Charges--;
            ChargesChanged?.Invoke();
            return true;
        }

        /// <summary>Advances the refill timer by <paramref name="deltaSeconds"/>, refilling as many charges as it completes.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time only moves forward.");
            }

            if (IsFull)
            {
                return;
            }

            refillSecondsElapsed += deltaSeconds;
            while (!IsFull && refillSecondsElapsed >= stats.RefillSeconds)
            {
                refillSecondsElapsed -= stats.RefillSeconds;
                Charges++;
                ChargesChanged?.Invoke();
            }

            // Time spent full is never banked toward the next refill.
            if (IsFull)
            {
                refillSecondsElapsed = 0f;
            }
        }
    }
}
