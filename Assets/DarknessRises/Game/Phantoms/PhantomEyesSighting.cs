using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;

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

        /// <summary>A real Enemy came within the clearance of the character or of their own spot: gone at once, so they never compete with or sit beside a real target.</summary>
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
        public PhantomConditions(
            CritterMoment moment,
            float nearestEnemyMetres,
            bool isLit,
            bool isAimedAt,
            float metresAway = float.PositiveInfinity,
            float spotEnemyMetres = float.PositiveInfinity)
        {
            Moment = moment;
            SpotEnemyMetres = spotEnemyMetres;
            NearestEnemyMetres = nearestEnemyMetres;
            IsLit = isLit;
            IsAimedAt = isAimedAt;
            MetresAway = metresAway;
        }

        public CritterMoment Moment { get; }

        /// <summary>Metres from the character to the nearest living Enemy; infinity when there's none.</summary>
        public float NearestEnemyMetres { get; }

        /// <summary>Metres from the eyes' own spot to the nearest living Enemy; infinity when there's none.</summary>
        public float SpotEnemyMetres { get; }

        /// <summary>Whether any light reaches their spot now.</summary>
        public bool IsLit { get; }

        /// <summary>Whether the player aims at them now (<see cref="PhantomAim"/>).</summary>
        public bool IsAimedAt { get; }

        /// <summary>Metres across the floor from the character to them.</summary>
        public float MetresAway { get; }
    }

    /// <summary>
    /// How a holding pair of Phantom eyes blinks: a quick close and reopen, at random gaps. Default: never.
    /// </summary>
    public readonly struct PhantomBlinking
    {
        /// <param name="seconds">Seconds a blink takes, closing and reopening. 0: they never blink.</param>
        /// <param name="minGapSeconds">Fewest seconds of open eyes before a blink.</param>
        /// <param name="maxGapSeconds">Most seconds of open eyes before a blink.</param>
        public PhantomBlinking(float seconds, float minGapSeconds, float maxGapSeconds)
        {
            if (seconds < 0f || minGapSeconds < 0f || maxGapSeconds < minGapSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "A blink's time and its gaps can't be negative, and the most gap can't be below the fewest.");
            }

            Seconds = seconds;
            MinGapSeconds = minGapSeconds;
            MaxGapSeconds = maxGapSeconds;
        }

        public float Seconds { get; }
        public float MinGapSeconds { get; }
        public float MaxGapSeconds { get; }

        /// <summary>Whether they blink at all.</summary>
        public bool IsOn => Seconds > 0f;

        /// <summary>How open the eyes are <paramref name="share"/> of the way through a blink: shut halfway, open at both ends.</summary>
        public static float Openness(float share)
        {
            return Math.Min(1f, Math.Abs(1f - 2f * share));
        }
    }

    /// <summary>
    /// One sighting of Phantom eyes: they open over a moment, hold for a few seconds at most (blinking now and then while
    /// they do), then blink out. Light on
    /// their spot, a real Enemy coming near, or a fight starting makes them vanish at once; the player aiming at them, or
    /// the character walking nearer than they ever appear, makes them blink out. Plain C# (ADR 0003); time comes in as game time, so the pause holds them.
    /// </summary>
    public sealed class PhantomEyesSighting
    {
        private readonly float openSeconds;
        private readonly float blinkOutSeconds;
        private readonly float clearanceMetres;
        private readonly float spotClearanceMetres;
        private readonly float keepAwayMetres;
        private readonly PhantomBlinking blinking;
        private readonly IRandom random;
        private float holdSeconds;
        private float nextBlinkIn;
        private float blinkSecondsIn = -1f;
        private float secondsIn;
        private float blinkLeft;

        /// <param name="openSeconds">Seconds the eyes take to open.</param>
        /// <param name="blinkOutSeconds">Seconds a blink out takes. Light makes them vanish at once instead.</param>
        /// <param name="clearanceMetres">They vanish the moment a living Enemy is this near the character.</param>
        /// <param name="keepAwayMetres">They blink out once the character is nearer to them than this: the least they appear at.</param>
        /// <param name="blinking">How they blink while they hold; by default, never.</param>
        /// <param name="random">Picks the gaps between blinks. Needed only when they blink.</param>
        /// <param name="spotClearanceMetres">They vanish the moment a living Enemy is this near their own spot, in a fight too.</param>
        public PhantomEyesSighting(
            float openSeconds,
            float blinkOutSeconds,
            float clearanceMetres,
            float keepAwayMetres = 0f,
            PhantomBlinking blinking = default,
            IRandom random = null,
            float spotClearanceMetres = 0f)
        {
            if (blinking.IsOn && random == null)
            {
                throw new ArgumentNullException(nameof(random), "Blinking eyes need a random source for their gaps.");
            }

            this.blinking = blinking;
            this.random = random;
            this.openSeconds = NotNegative(openSeconds, nameof(openSeconds));
            this.blinkOutSeconds = NotNegative(blinkOutSeconds, nameof(blinkOutSeconds));
            this.clearanceMetres = NotNegative(clearanceMetres, nameof(clearanceMetres));
            this.keepAwayMetres = NotNegative(keepAwayMetres, nameof(keepAwayMetres));
            this.spotClearanceMetres = NotNegative(spotClearanceMetres, nameof(spotClearanceMetres));
        }

        /// <summary>Whether these eyes opened during a Wave fight: then only a fight's rules keep them, not safe time's.</summary>
        public bool IsInFight { get; private set; }

        /// <summary>Whether the eyes are out: opening, holding or blinking out.</summary>
        public bool IsShowing { get; private set; }

        /// <summary>Whether they are blinking out.</summary>
        public bool IsBlinkingOut { get; private set; }

        /// <summary>Whether they are in the middle of a blink while they hold.</summary>
        public bool IsBlinking => blinkSecondsIn >= 0f;

        /// <summary>How strongly the eyes show now, from 0 (not at all) to 1.</summary>
        public float Strength { get; private set; }

        /// <summary>Why the last sighting went away, or <see cref="PhantomVanish.None"/> while it's out.</summary>
        public PhantomVanish Vanish { get; private set; }

        /// <summary>
        /// The eyes appear, to hold for <paramref name="seconds"/> (their opening included) unless something sends them
        /// away. <paramref name="inFight"/>: they open during a Wave fight (<see cref="PhantomFightSchedule"/>), so they
        /// stay through it, and go when it ends.
        /// </summary>
        public void Show(float seconds, bool inFight = false)
        {
            IsInFight = inFight;
            holdSeconds = NotNegative(seconds, nameof(seconds));
            secondsIn = 0f;
            IsShowing = true;
            IsBlinkingOut = false;
            Vanish = PhantomVanish.None;
            Strength = openSeconds > 0f ? 0f : 1f;
            blinkSecondsIn = -1f;
            nextBlinkIn = NextBlinkGap();
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

            // A real Enemy near their own spot: never a phantom next to a real threat, in a fight or not.
            if (conditions.SpotEnemyMetres <= spotClearanceMetres)
            {
                return End(PhantomVanish.EnemyNear);
            }

            if (IsInFight)
            {
                if (!PhantomFightSchedule.MayShow(conditions.Moment))
                {
                    return End(PhantomVanish.MomentEnded);
                }
            }
            else if (conditions.NearestEnemyMetres <= clearanceMetres)
            {
                return End(PhantomVanish.EnemyNear);
            }
            else if (!PhantomEyesSchedule.MayShow(conditions.Moment, conditions.NearestEnemyMetres, clearanceMetres))
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
            var openness = openSeconds > 0f ? Math.Min(1f, secondsIn / openSeconds) : 1f;
            Strength = openness * Blink(deltaSeconds, openness >= 1f);
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

        // How open a blink leaves them now, from 0 to 1, moving the blinks on. Only fully open eyes blink.
        private float Blink(float deltaSeconds, bool isOpen)
        {
            if (!blinking.IsOn || !isOpen)
            {
                return 1f;
            }

            if (blinkSecondsIn >= 0f)
            {
                blinkSecondsIn += deltaSeconds;
                if (blinkSecondsIn < blinking.Seconds)
                {
                    return PhantomBlinking.Openness(blinkSecondsIn / blinking.Seconds);
                }

                blinkSecondsIn = -1f;
                nextBlinkIn = NextBlinkGap();
                return 1f;
            }

            nextBlinkIn -= deltaSeconds;
            if (nextBlinkIn > 0f)
            {
                return 1f;
            }

            blinkSecondsIn = 0f;
            return 1f;
        }

        private float NextBlinkGap()
        {
            return blinking.IsOn ? random.Range(blinking.MinGapSeconds, blinking.MaxGapSeconds) : 0f;
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
            blinkSecondsIn = -1f;
            blinkLeft = blinkOutSeconds;
            return PhantomVanish.None;
        }

        private PhantomVanish End(PhantomVanish reason)
        {
            blinkSecondsIn = -1f;
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
