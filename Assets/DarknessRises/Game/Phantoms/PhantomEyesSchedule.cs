using System;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>What the dark does next: a pair of Phantom eyes looks out, or a voice comes from it alone.</summary>
    public enum PhantomEvent
    {
        /// <summary>Nothing now.</summary>
        None,

        /// <summary>A pair of Phantom eyes opens (perhaps with a voice).</summary>
        Eyes,

        /// <summary>A whisper, a breath or a hush from a dark spot, with no eyes.</summary>
        Voice,
    }

    /// <summary>
    /// When Phantom eyes and lone voices come out of the dark. They may only while the character walks or in the quiet
    /// after a Station's clear (never in a Wave fight, a Boss fight, the title card or after death), and only while no
    /// real Enemy is within the clearance, so they never compete with a real target. Only such safe time counts: after
    /// a random gap of it, the next event is due. Each event is a lone voice by an authored share, else a pair of eyes,
    /// which waits while the most pairs are already out. Plain C# (ADR 0003).
    /// </summary>
    public sealed class PhantomEyesSchedule
    {
        private readonly float minGapSeconds;
        private readonly float maxGapSeconds;
        private readonly float voiceOnlyShare;
        private readonly float retrySeconds;
        private readonly float clearanceMetres;
        private readonly int maxPairs;
        private readonly IRandom random;

        /// <param name="minGapSeconds">Fewest seconds of safe time between two events.</param>
        /// <param name="maxGapSeconds">Most seconds of safe time between two events.</param>
        /// <param name="voiceOnlyShare">Share of events, from 0 to 1, that are a lone voice rather than eyes.</param>
        /// <param name="firstDelaySeconds">Seconds of safe time from the Level's start before the first event.</param>
        /// <param name="retrySeconds">Seconds of safe time before trying again when an event found no spot.</param>
        /// <param name="clearanceMetres">Nothing while a living Enemy is this near the character.</param>
        /// <param name="maxPairs">Most pairs of eyes out at once.</param>
        public PhantomEyesSchedule(
            float minGapSeconds,
            float maxGapSeconds,
            float voiceOnlyShare,
            float firstDelaySeconds,
            float retrySeconds,
            float clearanceMetres,
            int maxPairs,
            IRandom random)
        {
            this.minGapSeconds = NotNegative(minGapSeconds, nameof(minGapSeconds));
            this.maxGapSeconds = maxGapSeconds >= minGapSeconds
                ? maxGapSeconds
                : throw new ArgumentOutOfRangeException(nameof(maxGapSeconds), maxGapSeconds, "The most can't be below the fewest.");
            this.voiceOnlyShare = voiceOnlyShare >= 0f && voiceOnlyShare <= 1f
                ? voiceOnlyShare
                : throw new ArgumentOutOfRangeException(nameof(voiceOnlyShare), voiceOnlyShare, "A share runs from 0 to 1.");
            this.retrySeconds = NotNegative(retrySeconds, nameof(retrySeconds));
            this.clearanceMetres = NotNegative(clearanceMetres, nameof(clearanceMetres));
            this.maxPairs = maxPairs >= 1 ? maxPairs : throw new ArgumentOutOfRangeException(nameof(maxPairs), maxPairs, "At least one pair.");
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            GapLeft = NotNegative(firstDelaySeconds, nameof(firstDelaySeconds));
            Next = PickKind();
        }

        /// <summary>Seconds of safe time left before the next event is due.</summary>
        public float GapLeft { get; private set; }

        /// <summary>What the next event will be.</summary>
        public PhantomEvent Next { get; private set; }

        /// <summary>
        /// Whether Phantom eyes (or their voices) may come in <paramref name="moment"/> with the nearest living Enemy
        /// <paramref name="nearestEnemyMetres"/> away (infinity when there's none): a quiet moment, and no Enemy within
        /// <paramref name="clearanceMetres"/>.
        /// </summary>
        public static bool MayShow(CritterMoment moment, float nearestEnemyMetres, float clearanceMetres)
        {
            return (moment == CritterMoment.Walking || moment == CritterMoment.AfterClear) && nearestEnemyMetres > clearanceMetres;
        }

        /// <summary>
        /// Moves time on by <paramref name="deltaSeconds"/>, of which only safe time counts. Returns the event due now,
        /// if any; the next gap then starts. <paramref name="pairsShowing"/> is how many pairs of eyes are out (or about
        /// to reopen): eyes wait while it's at the most.
        /// </summary>
        public PhantomEvent Tick(float deltaSeconds, CritterMoment moment, float nearestEnemyMetres, int pairsShowing)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!MayShow(moment, nearestEnemyMetres, clearanceMetres))
            {
                return PhantomEvent.None;
            }

            GapLeft = Math.Max(0f, GapLeft - deltaSeconds);
            if (GapLeft > 0f || deltaSeconds <= 0f || (Next == PhantomEvent.Eyes && pairsShowing >= maxPairs))
            {
                return PhantomEvent.None;
            }

            var due = Next;
            GapLeft = random.Range(minGapSeconds, maxGapSeconds);
            Next = PickKind();
            return due;
        }

        /// <summary>
        /// The <paramref name="missed"/> event found no spot (the Room was lit, or open floor was out of view): it comes
        /// again after the retry time instead of a whole gap.
        /// </summary>
        public void Missed(PhantomEvent missed)
        {
            if (missed == PhantomEvent.None)
            {
                return;
            }

            Next = missed;
            GapLeft = retrySeconds;
        }

        private PhantomEvent PickKind()
        {
            return voiceOnlyShare > 0f && random.Range(0f, 1f) < voiceOnlyShare ? PhantomEvent.Voice : PhantomEvent.Eyes;
        }

        private static float NotNegative(float value, string name)
        {
            return value >= 0f ? value : throw new ArgumentOutOfRangeException(name, value, "This can't be negative.");
        }
    }
}
