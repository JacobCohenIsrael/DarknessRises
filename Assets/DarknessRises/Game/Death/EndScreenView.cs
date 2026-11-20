using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Ads;
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
    /// The end screen after Exit: the Run's Score, the Level reached and the Enemies killed, then the Credits the Run
    /// earned and the new total in the bank, counted up arcade-style. It offers the end-of-Run ad, which multiplies the
    /// Run's Credits (<see cref="EndOfRunAd"/> decides when and how much), and never blocks moving on. It leads to a
    /// new Run for now, and to the Shop once the Shop exists.
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

        [SerializeField] private TMP_Text levelReached;
        [SerializeField] private TMP_Text enemiesKilled;
        [SerializeField] private TMP_Text score;

        [Header("Credits")]
        [SerializeField, Tooltip("The Credits the Run earned.")]
        private TMP_Text creditsEarned;

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

        [SerializeField, Tooltip("Plays the bonus landing.")]
        private AudioSource sound;

        [SerializeField] private Button newRunButton;

        private readonly CountUp earnedCount = new();
        private readonly CountUp bonusCount = new();
        private readonly CountUp totalCount = new();

        private CreditsDefinition settings;
        private IRewardedAd ad;
        private PlayerSave save;
        private AudioClip bonusClip;
        private float kickSecondsLeft;
        private bool isWatching;

        /// <summary>
        /// Gets the screen ready to show Credits as <paramref name="credits"/> says, bank the ad's bonus in
        /// <paramref name="playerSave"/> and show <paramref name="rewardedAd"/> for it.
        /// </summary>
        public void Use(CreditsDefinition credits, IRewardedAd rewardedAd, PlayerSave playerSave, AudioClip bonusSound)
        {
            settings = credits ? credits : throw new ArgumentNullException(nameof(credits));
            ad = rewardedAd ?? throw new ArgumentNullException(nameof(rewardedAd));
            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
            bonusClip = bonusSound;

            // Exit from the pause menu leaves the Level frozen and the listener paused; the screen's sound still plays.
            sound.ignoreListenerPause = true;

            var color = settings.EndScreen.Color;
            for (var i = 0; i < creditsTexts.Length; i++)
            {
                creditsTexts[i].color = color;
            }

            if (!isWatching)
            {
                isWatching = true;
                earnedCount.ValueChanged += ShowEarned;
                bonusCount.ValueChanged += ShowBonus;
                totalCount.ValueChanged += ShowTotal;
            }
        }

        /// <summary>Shows <paramref name="record"/> and the Credits it earned until the player moves on.</summary>
        public async UniTask ShowAsync(RunRecord record, CancellationToken cancellationToken)
        {
            // TMP's numeric SetText formats without allocating; {0} prints no decimals.
            levelReached.SetText(NumberFormat, (float)record.LevelReached);
            enemiesKilled.SetText(NumberFormat, (float)record.EnemiesKilled);
            score.SetText(NumberFormat, (float)record.Score);

            // The Run's Credits were banked when it ended; this only catches a Run nobody banks.
            var credits = record.Credits;
            save.Bank(credits);

            var look = settings.EndScreen;
            Begin(earnedCount, creditsEarned, GainFormat, 0);
            Begin(bonusCount, adBonus, GainFormat, credits.AdBonus);
            Begin(totalCount, creditsTotal, NumberFormat, save.Credits - credits.Total);
            earnedCount.CountTo(credits.Earned, look.CountUpSeconds);
            totalCount.CountTo(save.Credits, look.CountUpSeconds);
            adBonusRow.SetActive(credits.AdBonus > 0);

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
                    LandBonus(bonus);
                }

                ShowOffer(offer);
            }
        }

        // Waits for the player to move on or, while the ad is on offer, to take it.
        private async UniTask<int> PickAsync(bool isAdOffered, CancellationToken cancellationToken)
        {
            using var picking = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                return await UniTask.WhenAny(
                    newRunButton.OnClickAsync(picking.Token),
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

        // The ad paid out: its bonus counts up on its own line, and the total with it.
        private void LandBonus(int bonus)
        {
            var look = settings.EndScreen;
            adBonusRow.SetActive(true);
            bonusCount.CountTo(bonus, look.BonusCountUpSeconds);
            totalCount.CountTo(save.Credits, look.BonusCountUpSeconds);
            kickSecondsLeft = look.BonusKickSeconds;
            if (bonusClip != null)
            {
                sound.clip = bonusClip;
                sound.volume = look.BonusVolume;
                sound.time = Mathf.Clamp(look.BonusClipStartSeconds, 0f, Mathf.Max(0f, bonusClip.length - 0.01f));
                sound.Play();
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
            earnedCount.Tick(deltaSeconds);
            bonusCount.Tick(deltaSeconds);
            totalCount.Tick(deltaSeconds);

            var total = creditsTotal.rectTransform;
            if (kickSecondsLeft > 0f || total.localScale.x != 1f)
            {
                var look = settings.EndScreen;
                kickSecondsLeft = Mathf.Max(0f, kickSecondsLeft - deltaSeconds);
                var kick = look.BonusKickSeconds > 0f ? kickSecondsLeft / look.BonusKickSeconds : 0f;
                var scale = Mathf.Lerp(1f, look.BonusKickScale, kick);
                total.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void OnDestroy()
        {
            if (!isWatching)
            {
                return;
            }

            earnedCount.ValueChanged -= ShowEarned;
            bonusCount.ValueChanged -= ShowBonus;
            totalCount.ValueChanged -= ShowTotal;
        }

        // Puts a count at its first number and writes it, whether or not it changed from the last time the screen showed.
        private static void Begin(CountUp count, TMP_Text text, string format, int value)
        {
            count.Set(value);
            text.SetText(format, (float)value);
        }

        private void ShowEarned(int value) => creditsEarned.SetText(GainFormat, (float)value);

        private void ShowBonus(int value) => adBonus.SetText(GainFormat, (float)value);

        private void ShowTotal(int value) => creditsTotal.SetText(NumberFormat, (float)value);
    }
}
