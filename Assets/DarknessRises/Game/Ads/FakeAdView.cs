using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Ads
{
    /// <summary>
    /// A stand-in for a real rewarded ad: a placeholder screen with a countdown, shown for an authored time, after which
    /// it reports the ad as watched. Where it has a close button, closing it early reports the ad as not watched, as
    /// skipping a real one would. It counts real (unscaled) time, so nothing that slows or stops game time holds it up:
    /// it plays for a Continue, while the character is dead and the game never pauses, and for the Score boost, over
    /// a Level that has ended and may be frozen.
    /// Once a real ad network plugs in behind <see cref="IRewardedAd"/>, this goes.
    /// </summary>
    public sealed class FakeAdView : MonoBehaviour, IRewardedAd
    {
        [SerializeField, Tooltip("Shows the whole seconds left until the ad is over.")]
        private TextMeshProUGUI countdown;

        [SerializeField, Tooltip("The countdown's text. {0} is the whole seconds left.")]
        private string countdownFormat = "Back to the Run in {0}";

        [SerializeField, Tooltip("Closes the ad early, which earns nothing. None: the ad can only be watched through.")]
        private Button closeButton;

        private float seconds;
        private bool isClosed;

        /// <summary>Shows the placeholder for <paramref name="adSeconds"/> each time.</summary>
        public void Use(float adSeconds)
        {
            seconds = adSeconds;
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public async UniTask<AdOutcome> ShowAsync(CancellationToken cancellationToken)
        {
            isClosed = false;
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
                    if (isClosed)
                    {
                        return AdOutcome.NotWatched;
                    }

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

        private void Close()
        {
            isClosed = true;
        }
    }
}
