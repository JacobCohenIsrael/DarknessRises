#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Puts the Web player's safety limiter (<see cref="WebAudioLimiterSettings"/>) between the game's sounds and the
    /// browser's output, over <c>WebAudioLimiter.jslib</c>. Unity's Web audio connects each sound to the page's audio
    /// output every time it plays; from now on that output is the limiter, which feeds the real one. The boot installs it
    /// once, before any sound plays. Does nothing off the Web.
    /// </summary>
    public static class WebAudioLimiter
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int DarknessRisesAudioLimiterInstall(float thresholdDecibels, float ratio, float attackSeconds, float releaseSeconds, float compensationGain);
#endif

        /// <summary>Installs the limiter with <paramref name="settings"/>.</summary>
        /// <returns>Whether the game's output is limited now.</returns>
        public static bool Install(WebAudioLimiterSettings settings)
        {
            if (settings == null || !settings.Enabled)
            {
                return false;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            var curve = settings.Curve;
            return DarknessRisesAudioLimiterInstall(
                curve.ThresholdDecibels, curve.Ratio, settings.AttackSeconds, settings.ReleaseSeconds, curve.CompensationGain) == 1;
#else
            return false;
#endif
        }
    }
}
