using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// When a pair of Phantom eyes may look out during a Wave fight: rarely, after a random gap of fight time, and never
    /// more than an authored few at once. Never in the Boss fight, whose dark already holds the one real threat. The eyes
    /// themselves only open at the far edge of the dark and away from every real Enemy (the view's placement rules), so
    /// eyes near the character, or next to a real threat, are always real. Plain C# (ADR 0003).
    /// </summary>
    public sealed class PhantomFightSchedule
    {
        private readonly float minGapSeconds;
        private readonly float maxGapSeconds;
        private readonly float retrySeconds;
        private readonly int maxPairs;
        private readonly IRandom random;

        /// <param name="minGapSeconds">Fewest seconds of Wave fight between two pairs.</param>
        /// <param name="maxGapSeconds">Most seconds of Wave fight between two pairs.</param>
        /// <param name="retrySeconds">Seconds of fight before trying again when a pair found no spot.</param>
        /// <param name="maxPairs">Most pairs out at once during a fight. 0: none in fights.</param>
        public PhantomFightSchedule(float minGapSeconds, float maxGapSeconds, float retrySeconds, int maxPairs, IRandom random)
        {
            if (minGapSeconds < 0f || maxGapSeconds < minGapSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(minGapSeconds), minGapSeconds, "The gaps can't be negative, and the most can't be below the fewest.");
            }

            this.minGapSeconds = minGapSeconds;
            this.maxGapSeconds = maxGapSeconds;
            this.retrySeconds = retrySeconds >= 0f
                ? retrySeconds
                : throw new ArgumentOutOfRangeException(nameof(retrySeconds), retrySeconds, "This can't be negative.");
            this.maxPairs = maxPairs >= 0
                ? maxPairs
                : throw new ArgumentOutOfRangeException(nameof(maxPairs), maxPairs, "This can't be negative.");
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            GapLeft = random.Range(minGapSeconds, maxGapSeconds);
        }

        /// <summary>Seconds of Wave fight left before the next pair is due.</summary>
        public float GapLeft { get; private set; }

        /// <summary>Whether fight eyes may show in <paramref name="moment"/>: a Wave fight only, never the Boss fight.</summary>
        public static bool MayShow(CritterMoment moment)
        {
            return moment == CritterMoment.WaveFight;
        }

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/>, of which only Wave fight time counts. True when a pair is due
        /// now; the next gap then starts. <paramref name="pairsOut"/> is how many fight pairs are out: none is due while
        /// that's at the most.
        /// </summary>
        public bool Tick(float deltaSeconds, CritterMoment moment, int pairsOut)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!MayShow(moment) || maxPairs == 0)
            {
                return false;
            }

            GapLeft = Math.Max(0f, GapLeft - deltaSeconds);
            if (GapLeft > 0f || deltaSeconds <= 0f || pairsOut >= maxPairs)
            {
                return false;
            }

            GapLeft = random.Range(minGapSeconds, maxGapSeconds);
            return true;
        }

        /// <summary>The due pair found no spot (the Room lit, or every far spot near a real Enemy): it tries again soon.</summary>
        public void Missed()
        {
            GapLeft = retrySeconds;
        }
    }
}
