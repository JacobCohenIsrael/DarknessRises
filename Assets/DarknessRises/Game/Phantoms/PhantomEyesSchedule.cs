using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// When Phantom eyes may look out of the dark, and when they do. They may only while the character walks or in the
    /// quiet after a Station's clear (never in a Wave fight, a Boss fight, the title card or after death), and only while
    /// no real Enemy is within the clearance, so they never compete with a real target. Then they come by chance, rarely,
    /// with a long cooldown, and never while a sighting is still showing. Plain C# (ADR 0003).
    /// </summary>
    public sealed class PhantomEyesSchedule
    {
        private readonly float chancePerSecond;
        private readonly float cooldownSeconds;
        private readonly float clearanceMetres;
        private readonly IRandom random;
        private float cooldownLeft;

        /// <param name="chancePerSecond">Chance per second of a sighting while one may come.</param>
        /// <param name="cooldownSeconds">Seconds after a sighting appears before another may.</param>
        /// <param name="firstDelaySeconds">Seconds from the Level's start before the first may.</param>
        /// <param name="clearanceMetres">No sighting while a living Enemy is this near the character.</param>
        public PhantomEyesSchedule(float chancePerSecond, float cooldownSeconds, float firstDelaySeconds, float clearanceMetres, IRandom random)
        {
            this.chancePerSecond = NotNegative(chancePerSecond, nameof(chancePerSecond));
            this.cooldownSeconds = NotNegative(cooldownSeconds, nameof(cooldownSeconds));
            this.clearanceMetres = NotNegative(clearanceMetres, nameof(clearanceMetres));
            cooldownLeft = NotNegative(firstDelaySeconds, nameof(firstDelaySeconds));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public float CooldownLeft => cooldownLeft;

        /// <summary>
        /// Whether Phantom eyes may show in <paramref name="moment"/> with the nearest living Enemy
        /// <paramref name="nearestEnemyMetres"/> away (infinity when there's none): a quiet moment, and no Enemy within
        /// <paramref name="clearanceMetres"/>.
        /// </summary>
        public static bool MayShow(CritterMoment moment, float nearestEnemyMetres, float clearanceMetres)
        {
            return (moment == CritterMoment.Walking || moment == CritterMoment.AfterClear) && nearestEnemyMetres > clearanceMetres;
        }

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/>. True when a sighting should appear now; the cooldown then
        /// starts. <paramref name="isShowing"/> says whether one is still out.
        /// </summary>
        public bool Tick(float deltaSeconds, CritterMoment moment, float nearestEnemyMetres, bool isShowing)
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

            if (isShowing || deltaSeconds <= 0f || chancePerSecond <= 0f || !MayShow(moment, nearestEnemyMetres, clearanceMetres))
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

        private static float NotNegative(float value, string name)
        {
            return value >= 0f ? value : throw new ArgumentOutOfRangeException(name, value, "This can't be negative.");
        }
    }
}
