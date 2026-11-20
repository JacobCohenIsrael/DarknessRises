using System;
using DarknessRises.Game.Critters;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>Why Phantom eyes went away.</summary>
    public enum PhantomVanish
    {
        /// <summary>They're still there.</summary>
        None,

        /// <summary>Light reached their spot: Lightning, Gunfire, a Hit spark, the Flare, a Grenade's blast. Gone at once: nothing is there.</summary>
        Lit,

        /// <summary>The player aimed at them: they blink out.</summary>
        AimedAt,

        /// <summary>A real Enemy came within the clearance: gone at once, so they never compete with a real target.</summary>
        EnemyNear,

        /// <summary>A fight or anything else that allows no Phantom eyes began: gone at once.</summary>
        MomentEnded,

        /// <summary>They held as long as they hold: they blink out.</summary>
        TimeUp,

        /// <summary>
        /// The character came nearer than they ever appear: they blink out, so eyes that near are always real.
        /// </summary>
        TooNear,
    }

    /// <summary>What a showing pair of Phantom eyes checks every frame.</summary>
    public readonly struct PhantomConditions
    {
        public PhantomConditions(CritterMoment moment, float nearestEnemyMetres, bool isLit, bool isAimedAt, float metresAway = float.PositiveInfinity)
        {
            Moment = moment;
            NearestEnemyMetres = nearestEnemyMetres;
            IsLit = isLit;
            IsAimedAt = isAimedAt;
            MetresAway = metresAway;
        }

        public CritterMoment Moment { get; }

        /// <summary>Metres to the nearest living Enemy; infinity when there's none.</summary>
        public float NearestEnemyMetres { get; }

        /// <summary>Whether any light reaches their spot now.</summary>
        public bool IsLit { get; }

        /// <summary>Whether the player aims at them now (<see cref="PhantomAim"/>).</summary>
        public bool IsAimedAt { get; }

        /// <summary>Metres across the floor from the character to them.</summary>
        public float MetresAway { get; }
    }

    /// <summary>
    /// One sighting of Phantom eyes: they open over a moment, hold for a few seconds at most, then blink out. Light on
    /// their spot, a real Enemy coming near, or a fight starting makes them vanish at once; the player aiming at them, or
    /// the character walking nearer than they ever appear, makes them blink out. Plain C# (ADR 0003); time comes in as game time, so the pause holds them.
    /// </summary>
    public sealed class PhantomEyesSighting
    {
        private readonly float openSeconds;
        private readonly float blinkOutSeconds;
        private readonly float clearanceMetres;
        private readonly float keepAwayMetres;
        private float holdSeconds;
        private float secondsIn;
        private float blinkLeft;

        /// <param name="openSeconds">Seconds the eyes take to open.</param>
        /// <param name="blinkOutSeconds">Seconds a blink out takes. Light makes them vanish at once instead.</param>
        /// <param name="clearanceMetres">They vanish the moment a living Enemy is this near the character.</param>
        /// <param name="keepAwayMetres">They blink out once the character is nearer to them than this: the least they appear at.</param>
        public PhantomEyesSighting(float openSeconds, float blinkOutSeconds, float clearanceMetres, float keepAwayMetres = 0f)
        {
            this.openSeconds = NotNegative(openSeconds, nameof(openSeconds));
            this.blinkOutSeconds = NotNegative(blinkOutSeconds, nameof(blinkOutSeconds));
            this.clearanceMetres = NotNegative(clearanceMetres, nameof(clearanceMetres));
            this.keepAwayMetres = NotNegative(keepAwayMetres, nameof(keepAwayMetres));
        }

        /// <summary>Whether the eyes are out: opening, holding or blinking out.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>Whether they are blinking out.</summary>
        public bool IsBlinkingOut { get; private set; }

        /// <summary>How strongly the eyes show now, from 0 (not at all) to 1.</summary>
        public float Strength { get; private set; }

        /// <summary>Why the last sighting went away, or <see cref="PhantomVanish.None"/> while it's out.</summary>
        public PhantomVanish Vanish { get; private set; }

        /// <summary>The eyes appear, to hold for <paramref name="seconds"/> (their opening included) unless something sends them away.</summary>
        public void Show(float seconds)
        {
            holdSeconds = NotNegative(seconds, nameof(seconds));
            secondsIn = 0f;
            IsShowing = true;
            IsBlinkingOut = false;
            Vanish = PhantomVanish.None;
            Strength = openSeconds > 0f ? 0f : 1f;
        }

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/> under <paramref name="conditions"/>.
        /// Returns why they went away in this tick, or <see cref="PhantomVanish.None"/>.
        /// </summary>
        public PhantomVanish Tick(float deltaSeconds, in PhantomConditions conditions)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsShowing)
            {
                return PhantomVanish.None;
            }

            // Whatever they're doing, even blinking out: these end them in this very frame.
            if (conditions.IsLit)
            {
                return End(PhantomVanish.Lit);
            }

            if (conditions.NearestEnemyMetres <= clearanceMetres)
            {
                return End(PhantomVanish.EnemyNear);
            }

            if (!PhantomEyesSchedule.MayShow(conditions.Moment, conditions.NearestEnemyMetres, clearanceMetres))
            {
                return End(PhantomVanish.MomentEnded);
            }

            if (IsBlinkingOut)
            {
                blinkLeft -= deltaSeconds;
                if (blinkLeft <= 0f)
                {
                    return End(Vanish);
                }

                Strength = Math.Min(Strength, blinkLeft / blinkOutSeconds);
                return PhantomVanish.None;
            }

            secondsIn += deltaSeconds;
            Strength = openSeconds > 0f ? Math.Min(1f, secondsIn / openSeconds) : 1f;
            if (conditions.IsAimedAt)
            {
                return BlinkOut(PhantomVanish.AimedAt);
            }

            if (conditions.MetresAway < keepAwayMetres)
            {
                return BlinkOut(PhantomVanish.TooNear);
            }

            return secondsIn >= holdSeconds ? BlinkOut(PhantomVanish.TimeUp) : PhantomVanish.None;
        }

        // Starts blinking out. The reason is reported when the blink ends; a blink of no time ends now.
        private PhantomVanish BlinkOut(PhantomVanish reason)
        {
            Vanish = reason;
            if (blinkOutSeconds <= 0f)
            {
                return End(reason);
            }

            IsBlinkingOut = true;
            blinkLeft = blinkOutSeconds;
            return PhantomVanish.None;
        }

        private PhantomVanish End(PhantomVanish reason)
        {
            IsShowing = false;
            IsBlinkingOut = false;
            Strength = 0f;
            Vanish = reason;
            return reason;
        }

        private static float NotNegative(float value, string name)
        {
            return value >= 0f ? value : throw new ArgumentOutOfRangeException(name, value, "This can't be negative.");
        }
    }
}
