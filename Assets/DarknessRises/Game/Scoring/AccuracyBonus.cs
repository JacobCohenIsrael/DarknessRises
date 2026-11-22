using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Scoring
{
    /// <summary>One step of the accuracy bonus: from this accuracy up, a Station's Score is multiplied by this much.</summary>
    [Serializable]
    public sealed class AccuracyTier
    {
        [SerializeField, Range(0f, 1f), Tooltip("The least share of the fight's Shots that must have hurt an Enemy.")]
        private float minAccuracy = 0.5f;

        [SerializeField, Min(1f), Tooltip("What the Station's Score is multiplied by from that accuracy up. 1 adds nothing.")]
        private float multiplier = 1f;

        public AccuracyTier()
        {
        }

        public AccuracyTier(float minAccuracy, float multiplier)
        {
            this.minAccuracy = minAccuracy;
            this.multiplier = multiplier;
        }

        public float MinAccuracy => minAccuracy;
        public float Multiplier => multiplier;
    }

    /// <summary>
    /// The accuracy bonus: each Station's Score is multiplied by a bonus from how well its fight was shot. It takes the
    /// best authored tier the fight's accuracy reaches. A fight with too few Shots to say anything (none at all, a fight
    /// won by Grenades, or Shots that only killed Critters, which count as neither hit nor miss) gets ×1.
    /// Plain C# (ADR 0003).
    /// </summary>
    public sealed class AccuracyBonus
    {
        /// <summary>The bonus that adds nothing: every fight gets ×1.</summary>
        public static readonly AccuracyBonus None = new(Array.Empty<AccuracyTier>(), 0);

        private readonly IReadOnlyList<AccuracyTier> tiers;
        private readonly int minShots;

        /// <param name="tiers">The steps of the bonus, in any order.</param>
        /// <param name="minShots">The fewest Shots (hits and misses) a fight needs for its accuracy to count.</param>
        public AccuracyBonus(IReadOnlyList<AccuracyTier> tiers, int minShots)
        {
            this.tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            if (minShots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minShots), minShots, "A fight can't need fewer than no Shots.");
            }

            this.minShots = minShots;
        }

        /// <summary>What a fight with <paramref name="hits"/> and <paramref name="misses"/> multiplies its Score by.</summary>
        public float MultiplierFor(int hits, int misses)
        {
            var shots = hits + misses;
            if (shots <= 0 || shots < minShots)
            {
                return 1f;
            }

            var accuracy = StationScore.Accuracy(hits, misses);
            var best = 1f;
            for (var i = 0; i < tiers.Count; i++)
            {
                var tier = tiers[i];
                if (tier != null && accuracy >= tier.MinAccuracy && tier.Multiplier > best)
                {
                    best = tier.Multiplier;
                }
            }

            return best;
        }

        /// <summary>
        /// The Score <paramref name="multiplier"/> adds to <paramref name="points"/>: the multiplied Score, rounded to
        /// the nearest whole point, less the points. Never negative.
        /// </summary>
        public static int BonusOf(int points, float multiplier)
        {
            if (points <= 0 || multiplier <= 1f)
            {
                return 0;
            }

            var total = Math.Round(points * (double)multiplier, MidpointRounding.AwayFromZero);
            return (int)Math.Min(int.MaxValue, Math.Max(0d, total - points));
        }
    }
}
