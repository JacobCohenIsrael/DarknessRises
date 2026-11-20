using System;
using System.Collections.Generic;
using DarknessRises.Game.Weapons;

namespace DarknessRises.Game.Scoring
{
    /// <summary>What one Station's fight came to: the numbers on its result card.</summary>
    public readonly struct StationResult
    {
        public StationResult(int kills, int hits, int misses, int hitsTaken, float seconds, int points, Grade grade)
        {
            Kills = kills;
            Hits = hits;
            Misses = misses;
            HitsTaken = hitsTaken;
            Seconds = seconds;
            Points = points;
            Grade = grade;
        }

        /// <summary>Enemies killed during the fight.</summary>
        public int Kills { get; }

        /// <summary>Shots that hurt an Enemy.</summary>
        public int Hits { get; }

        /// <summary>Shots that hurt nothing.</summary>
        public int Misses { get; }

        /// <summary>Hits the character took.</summary>
        public int HitsTaken { get; }

        /// <summary>Seconds from the fight's start to its last death.</summary>
        public float Seconds { get; }

        /// <summary>Score earned during the fight.</summary>
        public int Points { get; }

        public Grade Grade { get; }

        /// <summary>The share of the Shots that hurt an Enemy, from 0 to 1. A fight won without a single Shot missed nothing.</summary>
        public float Accuracy => StationScore.Accuracy(Hits, Misses);
    }

    /// <summary>The sums behind a Station's result.</summary>
    public static class StationScore
    {
        /// <summary>The share of the Shots that hurt an Enemy. With no Shots at all (a fight won by Grenades), 1.</summary>
        public static float Accuracy(int hits, int misses)
        {
            var shots = hits + misses;
            return shots > 0 ? (float)hits / shots : 1f;
        }

        /// <summary>Seconds the fight took for each kill. With no kills, the whole fight.</summary>
        public static float SecondsPerKill(float seconds, int kills)
        {
            return kills > 0 ? seconds / kills : seconds;
        }
    }

    /// <summary>
    /// Keeps the Score of a Level as it's played: every kill earns its Enemy's points times the Combo's multiplier, hits
    /// and misses drive the Combo, and from the start of a Station's fight to its clear it tallies what the result card
    /// shows and grades it. The Run's total is kept by the Run; this only says what each kill earned.
    /// Plain C# (ADR 0003): whoever shows the Level tells it what happens.
    /// </summary>
    public sealed class ScoreKeeper
    {
        private readonly IReadOnlyList<GradeRequirement> grades;
        private readonly Grade lowestGrade;
        private bool isTallying;
        private int kills;
        private int hits;
        private int misses;
        private int hitsTaken;
        private int points;

        /// <param name="combo">The Combo that multiplies every kill.</param>
        /// <param name="grades">What each Grade above <paramref name="lowestGrade"/> requires.</param>
        /// <param name="lowestGrade">The Grade of a fight that meets none of them.</param>
        public ScoreKeeper(Combo combo, IReadOnlyList<GradeRequirement> grades, Grade lowestGrade)
        {
            Combo = combo ?? throw new ArgumentNullException(nameof(combo));
            this.grades = grades ?? throw new ArgumentNullException(nameof(grades));
            this.lowestGrade = lowestGrade;
        }

        /// <summary>Raised when a kill earns Score, with the points it earned (already multiplied).</summary>
        public event Action<int> Scored;

        public Combo Combo { get; }

        /// <summary>Whether a Station's fight is being tallied right now.</summary>
        public bool IsTallying => isTallying;

        /// <summary>
        /// A Shot ended as <paramref name="outcome"/>: a hit or a miss drives the Combo and the tally, and a Shot that
        /// counts as neither (it killed a Critter) changes nothing.
        /// </summary>
        public void ShotLanded(ShotOutcome outcome)
        {
            switch (outcome)
            {
                case ShotOutcome.HurtEnemy:
                    ShotHit();
                    break;
                case ShotOutcome.Missed:
                    ShotMissed();
                    break;
            }
        }

        /// <summary>A Shot hurt an Enemy.</summary>
        public void ShotHit()
        {
            Combo.Hit();
            if (isTallying)
            {
                hits++;
            }
        }

        /// <summary>A Shot hurt nothing: it hit a wall, ran out of range, or struck something it can't hurt.</summary>
        public void ShotMissed()
        {
            Combo.Miss();
            if (isTallying)
            {
                misses++;
            }
        }

        /// <summary>
        /// An Enemy worth <paramref name="basePoints"/> died, however it died (a Grenade's kill counts too).
        /// </summary>
        /// <returns>The Score it earned: its points times the Combo's multiplier.</returns>
        public int EnemyKilled(int basePoints)
        {
            if (basePoints < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(basePoints), basePoints, "A kill can't cost Score.");
            }

            var earned = basePoints * Combo.Multiplier;
            if (isTallying)
            {
                kills++;
                points += earned;
            }

            if (earned > 0)
            {
                Scored?.Invoke(earned);
            }

            return earned;
        }

        /// <summary>The character took a hit.</summary>
        public void CharacterHit()
        {
            if (isTallying)
            {
                hitsTaken++;
            }
        }

        /// <summary>A Station's fight starts: its tally starts from nothing.</summary>
        public void BeginStation()
        {
            isTallying = true;
            kills = 0;
            hits = 0;
            misses = 0;
            hitsTaken = 0;
            points = 0;
        }

        /// <summary>
        /// The Station's last Wave is dead after <paramref name="fightSeconds"/> of fighting: the tally stops and is graded.
        /// </summary>
        public StationResult EndStation(float fightSeconds)
        {
            if (fightSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fightSeconds), fightSeconds, "A fight can't take less than no time.");
            }

            isTallying = false;
            var grade = Grading.For(
                StationScore.Accuracy(hits, misses), StationScore.SecondsPerKill(fightSeconds, kills), hitsTaken, grades, lowestGrade);
            return new StationResult(kills, hits, misses, hitsTaken, fightSeconds, points, grade);
        }

        public void Tick(float deltaSeconds)
        {
            Combo.Tick(deltaSeconds);
        }
    }
}
