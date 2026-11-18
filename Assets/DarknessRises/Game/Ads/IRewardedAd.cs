using System.Threading;
using Cysharp.Threading.Tasks;

namespace DarknessRises.Game.Ads
{
    /// <summary>How a rewarded ad ended.</summary>
    public enum AdOutcome
    {
        /// <summary>The player watched it through: they earn the reward.</summary>
        Watched,

        /// <summary>It was skipped, closed early or failed to show: no reward.</summary>
        NotWatched,
    }

    /// <summary>
    /// An ad the player watches for a reward, such as a Continue. The prototype uses a fake one that shows a placeholder
    /// screen; a real ad network plugs in behind this interface without touching the Run's rules.
    /// </summary>
    public interface IRewardedAd
    {
        /// <summary>Shows the ad and waits until it's over.</summary>
        UniTask<AdOutcome> ShowAsync(CancellationToken cancellationToken);
    }
}
