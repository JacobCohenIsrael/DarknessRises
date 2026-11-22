using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Ads;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Credits;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Death
{
    /// <summary>
    /// The end screen after Exit: the Run's Score beside the best Score of every Run (with NEW BEST! stamped in when
    /// the Run beat it), the Level reached and the Enemies killed, then the Run's pay-out, counted up arcade-style: its
    /// Score turned into Credits ("SCORE 42300 &gt; +846 CREDITS"), then the flat bonus of the Levels it cleared, then the
    /// new total in the bank. It offers the end-of-Run ad, which multiplies the Run's Credits (<see cref="EndOfRunAd"/>
    /// decides when and how much); once watched, its bonus counts up on its own line and the total climbs again. It never
    /// blocks moving on. It leads to the Shop, where a new Run starts.
    /// </summary>
    /// <remarks>
    /// It counts in real time: the Level behind it may be frozen (Exit from the pause menu). Numbers are written only
    /// when they change, with TMP's number formatting, so nothing is allocated while they count.
    /// </remarks>
    public sealed class EndScreenView : MonoBehaviour
    {
        private const string NumberFormat = "{0}";
        private const string GainFormat = "+{0}";

        // The order the choices are waited on.
        private const int MoveOnPicked = 0;

        // The pay-out's steps, one after the other.
        private enum Step
        {
            Payout,
            ClearBonus,
            AdBonus,
            Total,
            Done,
        }

        [SerializeField] private TMP_Text levelReached;
        [SerializeField] private TMP_Text enemiesKilled;
        [SerializeField] private TMP_Text score;

        [SerializeField, Tooltip("Beside the Score: the best Score of every Run, this one counted.")]
        private TMP_Text bestScore;

        [SerializeField, Tooltip("Stamped in when the Run beat the best Score, then pulsing.")]
        private TMP_Text newBest;

        [Header("Credits")]
        [SerializeField, Tooltip("The pay-out's left side: the Run's Score being paid out.")]
        private TMP_Text payoutScore;

        [SerializeField, Tooltip("Between the Score and the Credits it pays.")]
        private TMP_Text payoutArrow;

        [SerializeField, Tooltip("The pay-out's right side: the Credits the Score paid, counted up.")]
        private TMP_Text payoutCredits;

        [SerializeField, Tooltip("Under the pay-out: the Score that pays one Credit.")]
        private TMP_Text payoutRate;

        [SerializeField, Tooltip("The flat Credits of the Levels the Run cleared: its label and value. Shown only when it cleared one.")]
        private GameObject clearBonusRow;

        [SerializeField] private TMP_Text clearBonus;

        [SerializeField, Tooltip("The end-of-Run ad's bonus: its label and value. Shown once the ad has paid out.")]
        private GameObject adBonusRow;

        [SerializeField] private TMP_Text adBonus;

        [SerializeField, Tooltip("The Credits in the bank, this Run's included.")]
        private TMP_Text creditsTotal;

        [SerializeField, Tooltip("Every text in the Credits' colour.")]
        private TMP_Text[] creditsTexts;

        [Header("End-of-Run ad")]
        [SerializeField, Tooltip("The offer. Hidden when the Run earned too little, and once the ad has paid out.")]
        private Button adButton;

        [SerializeField] private TMP_Text adButtonLabel;

        [SerializeField, Tooltip("Under the button: the Credits the ad would add.")]
        private TMP_Text adHint;

        [SerializeField, Tooltip("Plays the pay-out.")]
        private AudioSource sound;

        [SerializeField, Tooltip("Leaves the end screen for the Shop.")]
        private Button shopButton;

        private readonly CountUp payoutCount = new();
        private readonly CountUp bonusCount = new();
        private readonly CountUp adCount = new();
        private readonly CountUp totalCount = new();

        private CreditsDefinition settings;
        private ScoreHudSettings scoreLook;
        private BonusReveal newBestStamp;
        private string bestFormat;
        private float pulseSeconds;
        private IRewardedAd ad;
        private PlayerSave save;
        private RunCredits shownCredits;
        private Step step = Step.Done;
        private AudioClip payoutClip;
        private float kickSecondsLeft;
        private bool wasCountingTotal;
        private bool isWatching;

        /// <summary>
        /// Gets the screen ready to pay Credits out as <paramref name="credits"/> says, into the bank of
        /// <paramref name="playerSave"/>, with <paramref name="payoutSound"/> as it counts, and to show
        /// <paramref name="rewardedAd"/> for the end-of-Run ad.
        /// </summary>
        public void Use(
            CreditsDefinition credits, IRewardedAd rewardedAd, PlayerSave playerSave, AudioClip payoutSound, ScoreHudSettings scoreHud)
        {
            scoreLook = scoreHud ?? throw new ArgumentNullException(nameof(scoreHud));

            // Made once: the caption and the number's place ("BEST  {0}").
            bestFormat = scoreLook.BestCaption + "  {0}";
            newBest.text = scoreLook.NewBestText;
            newBest.color = scoreLook.NewBestColor;
            newBest.enabled = false;
            newBestStamp = new BonusReveal(scoreLook.NewBestDelaySeconds, scoreLook.StampSeconds, scoreLook.StampScale, 0f, 0f);

            settings = credits ? credits : throw new ArgumentNullException(nameof(credits));
            ad = rewardedAd ?? throw new ArgumentNullException(nameof(rewardedAd));
            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
            payoutClip = payoutSound;
            payoutArrow.text = settings.EndScreen.ArrowText;

            // Exit from the pause menu leaves the Level frozen and the listener paused; the screen's sound still plays.
            sound.ignoreListenerPause = true;

            var color = settings.Color;
            for (var i = 0; i < creditsTexts.Length; i++)
            {
                creditsTexts[i].color = color;
            }

            if (!isWatching)
            {
                isWatching = true;
                payoutCount.ValueChanged += ShowPayout;
                bonusCount.ValueChanged += ShowBonus;
                adCount.ValueChanged += ShowAdBonus;
                totalCount.ValueChanged += ShowTotal;
            }
        }

        /// <summary>Shows <paramref name="record"/> and pays out the Credits it earned until the player moves on.</summary>
        public async UniTask ShowAsync(RunRecord record, CancellationToken cancellationToken)
        {
            // TMP's numeric SetText formats without allocating; {0} prints no decimals.
            levelReached.SetText(NumberFormat, (float)record.LevelReached);
            enemiesKilled.SetText(NumberFormat, (float)record.EnemiesKilled);
            score.SetText(NumberFormat, (float)record.Score);
            bestScore.SetText(bestFormat, (float)Math.Max(record.BestScore, save.BestScore));
            newBest.enabled = false;
            pulseSeconds = 0f;
            if (record.IsNewBest)
            {
                newBestStamp.Begin();
            }
            else
            {
                newBestStamp.Stop();
            }

            // The Run's Credits were banked when it ended; this only catches a Run nobody banks.
            var credits = record.Credits;
            save.Bank(credits);

            var look = settings.EndScreen;
            shownCredits = credits;
            payoutScore.SetText(look.ScoreFormat, (float)credits.Score);
            payoutRate.SetText(look.RateFormat, (float)credits.ScorePerCredit);
            Begin(payoutCount, payoutCredits, look.CreditsFormat, 0);
            Begin(bonusCount, clearBonus, GainFormat, 0);
            Begin(adCount, adBonus, GainFormat, credits.AdBonus);
            Begin(totalCount, creditsTotal, NumberFormat, save.Credits - credits.Total);
            clearBonusRow.SetActive(credits.Earned > 0);
            adBonusRow.SetActive(credits.AdBonus > 0);
            wasCountingTotal = false;
            kickSecondsLeft = 0f;

            // Score to Credits first; the clear bonus, then the total, follow in Update.
            step = Step.Payout;
            payoutCount.CountTo(credits.FromScore, look.CountUpSeconds);
            if (credits.Total > 0)
            {
                PlayPayout();
            }

            var offer = settings.EndOfRunAd.ToAd(credits);
            adButtonLabel.SetText(settings.EndOfRunAd.OfferFormat, offer.Factor);
            ShowOffer(offer);
            gameObject.SetActive(true);

            while (true)
            {
                if (await PickAsync(offer.IsOffered, cancellationToken) == MoveOnPicked)
                {
                    return;
                }

                // An ad that wasn't watched through adds nothing, and the offer stays.
                var bonus = offer.Finish(await ad.ShowAsync(cancellationToken));
                if (bonus > 0)
                {
                    save.Bank(credits);
                    LandAdBonus(bonus);
                }

                ShowOffer(offer);
            }
        }

        /// <summary>Raised when NEW BEST! stamps in.</summary>
        public event Action NewBestStamped;

        /// <summary>Raised when the Credits total has counted up to the bank and jumps as it lands.</summary>
        public event Action CreditsLanded;

        /// <summary>Raised when the end-of-Run ad has paid out and its bonus starts landing.</summary>
        public event Action AdBonusLanded;

        // Waits for the player to move on or, while the ad is on offer, to take it.
        private async UniTask<int> PickAsync(bool isAdOffered, CancellationToken cancellationToken)
        {
            using var picking = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                return await UniTask.WhenAny(
                    shopButton.OnClickAsync(picking.Token),
                    isAdOffered ? adButton.OnClickAsync(picking.Token) : UniTask.Never(picking.Token));
            }
            finally
            {
                // Stops the wait on the button that wasn't picked.
                picking.Cancel();
            }
        }

        private void ShowOffer(EndOfRunAd offer)
        {
            var isOffered = offer.IsOffered;
            adButton.gameObject.SetActive(isOffered);
            if (isOffered)
            {
                adHint.SetText(settings.EndOfRunAd.BonusHintFormat, (float)offer.Bonus);
            }
        }

        // The ad paid out: its bonus counts up on its own line once the pay-out before it is done, then the total climbs.
        private void LandAdBonus(int bonus)
        {
            adBonusRow.SetActive(true);
            AdBonusLanded?.Invoke();
            PlayPayout();
            if (step == Step.Done)
            {
                StartAdBonus();
            }
        }

        private void StartAdBonus()
        {
            step = Step.AdBonus;
            adCount.CountTo(shownCredits.AdBonus, settings.EndScreen.BonusCountUpSeconds);
        }

        private void PlayPayout()
        {
            if (payoutClip == null)
            {
                return;
            }

            var look = settings.EndScreen;
            sound.clip = payoutClip;
            sound.SetVolume(look.PayoutVolume);
            sound.time = Mathf.Clamp(look.PayoutClipStartSeconds, 0f, Mathf.Max(0f, payoutClip.length - 0.01f));
            sound.Play();
        }

        // Moves the pay-out on to its next step once the count before it has landed.
        private void Advance()
        {
            var look = settings.EndScreen;
            switch (step)
            {
                case Step.Payout when !payoutCount.IsCounting:
                    step = Step.ClearBonus;
                    bonusCount.CountTo(shownCredits.Earned, look.BonusCountUpSeconds);
                    break;
                case Step.ClearBonus when !bonusCount.IsCounting:
                    if (shownCredits.AdBonus > adCount.Value)
                    {
                        StartAdBonus();
                    }
                    else
                    {
                        step = Step.Total;
                        totalCount.CountTo(save.Credits, look.CountUpSeconds);
                    }

                    break;
                case Step.AdBonus when !adCount.IsCounting:
                    step = Step.Total;
                    totalCount.CountTo(save.Credits, look.BonusCountUpSeconds);
                    break;
                case Step.Total when !totalCount.IsCounting:
                    // An ad watched while the total counted adds its line now.
                    if (shownCredits.AdBonus > adCount.Value)
                    {
                        StartAdBonus();
                    }
                    else
                    {
                        step = Step.Done;
                    }

                    break;
            }
        }

        private void Update()
        {
            if (settings == null)
            {
                return;
            }

            // Real time, capped like game time, so a hitch (or the app coming back to the front) doesn't skip the count.
            var deltaSeconds = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            payoutCount.Tick(deltaSeconds);
            bonusCount.Tick(deltaSeconds);
            adCount.Tick(deltaSeconds);
            totalCount.Tick(deltaSeconds);
            ShowNewBest(deltaSeconds);
            Advance();

            // The total jumps as it lands.
            var look = settings.EndScreen;
            if (wasCountingTotal && !totalCount.IsCounting)
            {
                kickSecondsLeft = look.LandKickSeconds;
                CreditsLanded?.Invoke();
            }

            wasCountingTotal = totalCount.IsCounting;
            var total = creditsTotal.rectTransform;
            if (kickSecondsLeft > 0f || total.localScale.x != 1f)
            {
                kickSecondsLeft = Mathf.Max(0f, kickSecondsLeft - deltaSeconds);
                var kick = look.LandKickSeconds > 0f ? kickSecondsLeft / look.LandKickSeconds : 0f;
                var scale = Mathf.Lerp(1f, look.LandKickScale, kick);
                total.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDestroy()
        {
            if (!isWatching)
            {
                return;
            }

            payoutCount.ValueChanged -= ShowPayout;
            bonusCount.ValueChanged -= ShowBonus;
            adCount.ValueChanged -= ShowAdBonus;
            totalCount.ValueChanged -= ShowTotal;
        }

        // NEW BEST! punches in after a moment, then pulses for as long as the screen shows.
        private void ShowNewBest(float deltaSeconds)
        {
            if (newBestStamp == null)
            {
                return;
            }

            newBestStamp.Tick(deltaSeconds);
            if (newBest.enabled != newBestStamp.IsStamped)
            {
                newBest.enabled = newBestStamp.IsStamped;
                if (newBest.enabled)
                {
                    NewBestStamped?.Invoke();
                }
            }

            if (!newBestStamp.IsStamped)
            {
                return;
            }

            var size = newBestStamp.StampSize;
            if (newBestStamp.IsCounting)
            {
                pulseSeconds += deltaSeconds;
                var pulse = Mathf.Abs(Mathf.Sin(pulseSeconds * scoreLook.NewBestPulsesPerSecond * Mathf.PI));
                size = Mathf.Lerp(1f, scoreLook.NewBestPulseScale, pulse);
            }

            newBest.rectTransform.localScale = new Vector3(size, size, 1f);
        }

        // Puts a count at its first number and writes it, whether or not it changed from the last time the screen showed.
        private static void Begin(CountUp count, TMP_Text text, string format, int value)
        {
            count.Set(value);
            text.SetText(format, (float)value);
        }

        private void ShowPayout(int value) => payoutCredits.SetText(settings.EndScreen.CreditsFormat, (float)value);

        private void ShowBonus(int value) => clearBonus.SetText(GainFormat, (float)value);

        private void ShowAdBonus(int value) => adBonus.SetText(GainFormat, (float)value);

        private void ShowTotal(int value) => creditsTotal.SetText(NumberFormat, (float)value);
    }
}
