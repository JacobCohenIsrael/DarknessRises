using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace DarknessRises.Game.Ads
{
    /// <summary>
    /// A stand-in for a real rewarded ad: a placeholder screen with a countdown, shown for an authored time, after which
    /// it reports the ad as watched. It counts real (unscaled) time, so nothing that slows or stops game time holds it up.
    /// Once a real ad network plugs in behind <see cref="IRewardedAd"/>, this goes.
    /// </summary>
    public sealed class FakeAdView : MonoBehaviour, IRewardedAd
    {
        [SerializeField, Tooltip("Shows the whole seconds left until the Run resumes.")]
        private TextMeshProUGUI countdown;

        [SerializeField, Tooltip("The countdown's text. {0} is the whole seconds left.")]
        private string countdownFormat = "Back to the Run in {0}";

        private float seconds;

        /// <summary>Shows the placeholder for <paramref name="adSeconds"/> each time.</summary>
        public void Use(float adSeconds)
        {
            seconds = adSeconds;
            gameObject.SetActive(false);
        }

        public async UniTask<AdOutcome> ShowAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            try
            {
                var left = seconds;
                var shown = -1;
                while (left > 0f)
                {
                    var whole = Mathf.CeilToInt(left);
                    if (whole != shown)
                    {
                        shown = whole;
                        countdown.SetText(countdownFormat, whole);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                    // Capped like game time, so a hitch (or the app coming back to the front) doesn't skip the ad.
                    left -= Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
                }

                return AdOutcome.Watched;
            }
            finally
            {
                if (this != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
