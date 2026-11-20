using System;
using DarknessRises.Game.Runs;
using UnityEngine;

namespace DarknessRises.Game.Credits
{
    /// <summary>Authored numbers and wording of the <see cref="EndOfRunAd"/>.</summary>
    [Serializable]
    public sealed class EndOfRunAdSettings
    {
        [SerializeField, Min(EndOfRunAd.MinFactor), Tooltip("What the ad multiplies the Run's earned Credits by. 2 doubles them.")]
        private float factor = 2f;

        [SerializeField, Min(0), Tooltip("A Run that earned fewer Credits than this isn't offered the ad. A Run that earned none never is.")]
        private int minimumCredits = 20;

        [SerializeField, Min(0.1f), Tooltip("Seconds the fake ad's placeholder screen shows before it reports the ad as watched.")]
        private float fakeAdSeconds = 5f;

        [SerializeField, Tooltip("The offer's button on the end screen. {0} is the factor.")]
        private string offerFormat = "WATCH AD: x{0} CREDITS";

        [SerializeField, Tooltip("The small line under the button. {0} is the Credits the ad would add.")]
        private string bonusHintFormat = "+{0} Credits for this Run";

        public float Factor => factor;
        public int MinimumCredits => minimumCredits;
        public float FakeAdSeconds => fakeAdSeconds;
        public string OfferFormat => offerFormat;
        public string BonusHintFormat => bonusHintFormat;

        /// <summary>The ad's rule for the Run that earned <paramref name="credits"/>, with these numbers.</summary>
        public EndOfRunAd ToAd(RunCredits credits) => new(credits, factor, minimumCredits);
    }
}
