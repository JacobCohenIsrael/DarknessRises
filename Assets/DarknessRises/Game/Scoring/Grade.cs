using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarknessRises.Game.Scoring
{
    /// <summary>The mark a Station's fight earns, from worst to best.</summary>
    public enum Grade
    {
        C,
        B,
        A,
        S,
    }

    /// <summary>What a Station's fight must manage to earn a Grade. Authored, one per Grade above the lowest.</summary>
    [Serializable]
    public sealed class GradeRequirement
    {
        [SerializeField, Tooltip("The Grade these numbers earn.")]
        private Grade grade = Grade.A;

        [SerializeField, Range(0f, 1f), Tooltip("The least share of the Shots that must have hurt an Enemy.")]
        private float minAccuracy = 0.65f;

        [SerializeField, Min(0f), Tooltip("The most seconds the fight may take per kill, from its first Wave to its last death. 0: any time will do.")]
        private float maxSecondsPerKill;

        [SerializeField, Min(-1), Tooltip("The most hits the character may take during the fight. -1: any number will do.")]
        private int maxHitsTaken = -1;

        public GradeRequirement()
        {
        }

        public GradeRequirement(Grade grade, float minAccuracy, float maxSecondsPerKill, int maxHitsTaken)
        {
            this.grade = grade;
            this.minAccuracy = minAccuracy;
            this.maxSecondsPerKill = maxSecondsPerKill;
            this.maxHitsTaken = maxHitsTaken;
        }

        public Grade Grade => grade;
        public float MinAccuracy => minAccuracy;
        public float MaxSecondsPerKill => maxSecondsPerKill;
        public int MaxHitsTaken => maxHitsTaken;

        /// <summary>Whether a fight with these numbers meets this requirement.</summary>
        public bool IsMetBy(float accuracy, float secondsPerKill, int hitsTaken)
        {
            return accuracy >= minAccuracy
                && (maxSecondsPerKill <= 0f || secondsPerKill <= maxSecondsPerKill)
                && (maxHitsTaken < 0 || hitsTaken <= maxHitsTaken);
        }
    }

    /// <summary>Works out the Grade of a Station's fight from the authored requirements.</summary>
    public static class Grading
    {
        /// <summary>
        /// The best Grade among <paramref name="requirements"/> that the fight meets, whatever order they're listed in,
        /// or <paramref name="lowest"/> when it meets none.
        /// </summary>
        public static Grade For(float accuracy, float secondsPerKill, int hitsTaken, IReadOnlyList<GradeRequirement> requirements, Grade lowest)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            var best = lowest;
            for (var i = 0; i < requirements.Count; i++)
            {
                var requirement = requirements[i];
                if (requirement != null && requirement.Grade > best && requirement.IsMetBy(accuracy, secondsPerKill, hitsTaken))
                {
                    best = requirement.Grade;
                }
            }

            return best;
        }
    }
}
