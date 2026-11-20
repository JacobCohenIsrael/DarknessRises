using System;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// The ammo a Primary weapon carries beyond its magazine: reloads take from it. A finite reserve starts full, runs
    /// down as the Weapon reloads, and is topped up by refills (a new Level, a Continue), never above its size. The
    /// Pistol's is unlimited: it reloads forever, so the player is never helpless. Plain C# (ADR 0003).
    /// </summary>
    public sealed class AmmoReserve
    {
        private AmmoReserve(int max, bool isUnlimited)
        {
            Max = max;
            Rounds = max;
            IsUnlimited = isUnlimited;
        }

        /// <param name="max">The rounds a full reserve holds.</param>
        public AmmoReserve(int max) : this(max, false)
        {
            if (max < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "A reserve can't hold a negative number of rounds.");
            }
        }

        /// <summary>A reserve that never runs out.</summary>
        public static AmmoReserve Unlimited() => new(0, true);

        /// <summary>Raised when <see cref="Rounds"/> changes.</summary>
        public event Action Changed;

        /// <summary>Whether the reserve never runs out. Its <see cref="Rounds"/> and <see cref="Max"/> mean nothing then.</summary>
        public bool IsUnlimited { get; }

        /// <summary>The rounds a full reserve holds.</summary>
        public int Max { get; }

        /// <summary>The rounds left in the reserve.</summary>
        public int Rounds { get; private set; }

        /// <summary>Whether a reload would find nothing here.</summary>
        public bool IsEmpty => !IsUnlimited && Rounds <= 0;

        /// <summary>Forgets whoever listened: the Level that showed this reserve is gone.</summary>
        public void DropListeners()
        {
            Changed = null;
        }

        /// <summary>Takes up to <paramref name="wanted"/> rounds out for a reload.</summary>
        /// <returns>The rounds taken: all of them from an unlimited reserve, otherwise as many as are left.</returns>
        public int Take(int wanted)
        {
            if (wanted < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "A reload can't take a negative number of rounds.");
            }

            if (IsUnlimited)
            {
                return wanted;
            }

            var taken = Math.Min(wanted, Rounds);
            if (taken > 0)
            {
                Rounds -= taken;
                Changed?.Invoke();
            }

            return taken;
        }

        /// <summary>
        /// Puts back <paramref name="share"/> of a full reserve (0.5 is half), rounded to whole rounds, never above full.
        /// </summary>
        /// <returns>The rounds added.</returns>
        public int Refill(float share)
        {
            if (!(share >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(share), share, "A refill never takes ammo away.");
            }

            if (IsUnlimited)
            {
                return 0;
            }

            var wanted = (long)Math.Round(Max * (double)share, MidpointRounding.AwayFromZero);
            var added = (int)Math.Min(Max - Rounds, wanted);
            if (added > 0)
            {
                Rounds += added;
                Changed?.Invoke();
            }

            return added;
        }
    }
}
