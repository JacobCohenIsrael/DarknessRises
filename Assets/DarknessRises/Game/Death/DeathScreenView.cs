using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Continues;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Death
{
    /// <summary>What the player picks on the death screen.</summary>
    public enum DeathChoice
    {
        /// <summary>End this Run and start a new one from the first Level.</summary>
        Restart,

        /// <summary>End the Run and see the end screen.</summary>
        Exit,

        /// <summary>Watch an ad to come back where the character died. Once per Run.</summary>
        Continue,
    }

    /// <summary>
    /// The death screen: shown once the character has died, it offers Continue (while the Run allows it), Restart and
    /// Exit. While Continue is on offer the screen asks "CONTINUE?" over an arcade countdown; when the countdown runs
    /// out the Run ends as if the player took Exit. <see cref="ContinueCountdown"/> counts; this shows its number, from
    /// texts made once.
    /// </summary>
    public sealed class DeathScreenView : MonoBehaviour
    {
        // The order the choices are waited on.
        private const int RestartPicked = 0;
        private const int ExitPicked = 1;
        private const int ContinuePicked = 2;

        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        [SerializeField, Tooltip("Asks whether to Continue, or says the game is over.")]
        private TMP_Text title;

        [SerializeField, Tooltip("The countdown's number, shown only while Continue is on offer.")]
        private TMP_Text countdownLabel;

        private ContinueDefinition settings;
        private ContinueCountdown countdown;
        private string[] countTexts;
        private Action<int> onCountChanged;

        /// <summary>Gets the screen ready to count down as <paramref name="continueSettings"/> say.</summary>
        public void Use(ContinueDefinition continueSettings)
        {
            if (countdown != null)
            {
                countdown.CountChanged -= onCountChanged;
            }

            settings = continueSettings ? continueSettings : throw new ArgumentNullException(nameof(continueSettings));
            countdown = new ContinueCountdown(settings.CountdownFrom, settings.CountdownSecondsPerCount);

            // Every number's text, made once.
            countTexts = new string[settings.CountdownFrom + 1];
            for (var i = 0; i < countTexts.Length; i++)
            {
                countTexts[i] = i.ToString();
            }

            onCountChanged ??= OnCountChanged;
            countdown.CountChanged += onCountChanged;
        }

        /// <summary>
        /// Shows the screen until the player picks a choice, then hides it. Continue is offered only when
        /// <paramref name="canContinue"/>, under the countdown; if the countdown runs out, the choice is Exit.
        /// </summary>
        public async UniTask<DeathChoice> ChooseAsync(bool canContinue, CancellationToken cancellationToken)
        {
            var counts = canContinue && countdown != null;
            continueButton.gameObject.SetActive(canContinue);
            countdownLabel.gameObject.SetActive(counts);
            if (settings != null)
            {
                title.text = canContinue ? settings.ContinueTitle : settings.GameOverTitle;
            }

            gameObject.SetActive(true);
            using var choosing = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                if (counts)
                {
                    countdown.Start();
                }

                var picked = await UniTask.WhenAny(
                    restartButton.OnClickAsync(choosing.Token),
                    exitButton.OnClickAsync(choosing.Token),
                    continueButton.OnClickAsync(choosing.Token),
                    counts ? CountDownAsync(choosing.Token) : UniTask.Never(choosing.Token));
                return picked switch
                {
                    RestartPicked => DeathChoice.Restart,
                    ContinuePicked => DeathChoice.Continue,

                    // Exit, or the countdown ran out: game over.
                    _ => DeathChoice.Exit,
                };
            }
            finally
            {
                // Stops the countdown and the wait on the buttons that weren't picked.
                countdown?.Stop();
                choosing.Cancel();
                if (this != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }

        // Real time, like the ad: nothing that slows game time holds the countdown up. Capped like game time, so a
        // hitch (or the app coming back to the front) doesn't skip numbers.
        private async UniTask CountDownAsync(CancellationToken cancellationToken)
        {
            while (!countdown.IsExpired)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                countdown.Tick(Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime));
            }
        }

        private void OnCountChanged(int count)
        {
            countdownLabel.text = countTexts[Mathf.Clamp(count, 0, countTexts.Length - 1)];
        }

        private void OnDestroy()
        {
            if (countdown != null)
            {
                countdown.CountChanged -= onCountChanged;
            }
        }
    }
}
