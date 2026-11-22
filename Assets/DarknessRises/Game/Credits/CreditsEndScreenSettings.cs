using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// How the end screen pays a Run's Credits out, arcade-style: the Run's Score is turned into Credits
    /// ("SCORE 42300 &gt; +846 CREDITS"), the Level clear bonus counts up beside it, and the new total in the bank counts
    /// up with them, then lands with a jump and a sound.
    /// </summary>
    [Serializable]
    public sealed class CreditsEndScreenSettings
    {
        [SerializeField, Min(0f), Tooltip("Seconds the Credits the Score paid, the clear bonus and the total take to count up. Real time: the Level behind the screen may be frozen.")]
        private float countUpSeconds = 1.2f;

        [SerializeField, Tooltip("The Score being paid out, left of the arrow. {0} is the Run's Score.")]
        private string scoreFormat = "SCORE  {0}";

        [SerializeField, Tooltip("Between the Score and its Credits. Only characters the UI font has.")]
        private string arrowText = ">";

        [SerializeField, Tooltip("The Credits the Score paid, right of the arrow. {0} is the Credits counted so far.")]
        private string creditsFormat = "+{0}  CREDITS";

        [SerializeField, Tooltip("The small line under the payout that tells the rate. {0} is the Score that pays one Credit.")]
        private string rateFormat = "every {0} Score pays 1 Credit";

        [SerializeField, Min(1f), FormerlySerializedAs("bonusKickScale"), Tooltip("How many times its size the total jumps to when the payout lands.")]
        private float landKickScale = 1.35f;

        [SerializeField, Min(0f), FormerlySerializedAs("bonusKickSeconds"), Tooltip("Seconds that jump takes to settle back.")]
        private float landKickSeconds = 0.25f;

        [SerializeField, FormerlySerializedAs("bonusClip"), Tooltip("Played as the payout starts counting. Empty for no sound.")]
        private AssetReferenceT<AudioClip> payoutClip = new(string.Empty);

        [SerializeField, Min(0f), FormerlySerializedAs("bonusClipStartSeconds"), Tooltip("Seconds into the clip where the sound starts, to skip a quiet lead-in.")]
        private float payoutClipStartSeconds;

        [SerializeField, Range(0f, 1f), FormerlySerializedAs("bonusVolume")]
        private float payoutVolume = 0.8f;

        public float CountUpSeconds => countUpSeconds;
        public string ScoreFormat => scoreFormat;
        public string ArrowText => arrowText;
        public string CreditsFormat => creditsFormat;
        public string RateFormat => rateFormat;
        public float LandKickScale => landKickScale;
        public float LandKickSeconds => landKickSeconds;
        public AssetReferenceT<AudioClip> PayoutClip => payoutClip;
        public float PayoutClipStartSeconds => payoutClipStartSeconds;
        public float PayoutVolume => payoutVolume;
    }
}
