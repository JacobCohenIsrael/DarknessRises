using System;
using DarknessRises.Game.Ads;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// The end-of-Run ad: once a Run has ended, the end screen offers one rewarded ad that multiplies the Credits the
    /// Run earned. It's optional, it pays once per Run, and an ad that wasn't watched through pays nothing and leaves
    /// the offer standing. A Run that earned too little isn't offered one. Plain C# (ADR 0003).
    /// </summary>
    public sealed class EndOfRunAd
    {
        /// <summary>The smallest factor there is: the ad multiplies Credits, it never takes any away.</summary>
        public const float MinFactor = 1f;

        private readonly RunCredits credits;
        private readonly float factor;
        private readonly int minimumCredits;

        /// <param name="runCredits">The Credits of the Run that ended.</param>
        /// <param name="factor">What the Run's earned Credits are multiplied by: 2 doubles them.</param>
        /// <param name="minimumCredits">A Run that earned fewer Credits than this isn't offered the ad.</param>
        public EndOfRunAd(RunCredits runCredits, float factor, int minimumCredits)
        {
            credits = runCredits ?? throw new ArgumentNullException(nameof(runCredits));
            if (!(factor >= MinFactor) || float.IsInfinity(factor))
            {
                throw new ArgumentOutOfRangeException(nameof(factor), factor, "The ad multiplies Credits; it never takes any away.");
            }

            if (minimumCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumCredits), minimumCredits, "The minimum can't be negative.");
            }

            this.factor = factor;
            this.minimumCredits = minimumCredits;
        }

        /// <summary>What the Run's earned Credits are multiplied by.</summary>
        public float Factor => factor;

        /// <summary>
        /// The Credits the ad adds on top of what the Run earned: its earnings times the factor, less the earnings
        /// themselves, rounded to whole Credits.
        /// </summary>
        public int Bonus => (int)Math.Round(credits.Earned * (double)(factor - 1f), MidpointRounding.AwayFromZero);

        /// <summary>
        /// Whether the ad is on offer: the Run earned something, at least the minimum, the ad would add at least one
        /// Credit, and it hasn't paid out for this Run yet.
        /// </summary>
        public bool IsOffered => !credits.HasAdBonus && credits.Earned > 0 && credits.Earned >= minimumCredits && Bonus > 0;

        /// <summary>
        /// The ad ended with <paramref name="outcome"/>. Watched through, it adds <see cref="Bonus"/> to the Run's
        /// Credits and the offer is used up; otherwise nothing changes and the offer stays.
        /// </summary>
        /// <returns>The Credits added: 0 when the ad wasn't watched or wasn't on offer.</returns>
        public int Finish(AdOutcome outcome)
        {
            if (outcome != AdOutcome.Watched || !IsOffered)
            {
                return 0;
            }

            var bonus = Bonus;
            return credits.AddAdBonus(bonus) ? bonus : 0;
        }
    }
}
