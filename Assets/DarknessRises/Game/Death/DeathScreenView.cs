using System.Threading;
using Cysharp.Threading.Tasks;
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

    /// <summary>The death screen: shown once the character has died, it offers Continue (while the Run allows it), Restart and Exit.</summary>
    public sealed class DeathScreenView : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        /// <summary>
        /// Shows the screen until the player picks a choice, then hides it. Continue is offered only when
        /// <paramref name="canContinue"/>.
        /// </summary>
        public async UniTask<DeathChoice> ChooseAsync(bool canContinue, CancellationToken cancellationToken)
        {
            continueButton.gameObject.SetActive(canContinue);
            gameObject.SetActive(true);
            using var choosing = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var picked = await UniTask.WhenAny(
                    restartButton.OnClickAsync(choosing.Token),
                    exitButton.OnClickAsync(choosing.Token),
                    continueButton.OnClickAsync(choosing.Token));
                return picked switch
                {
                    0 => DeathChoice.Restart,
                    1 => DeathChoice.Exit,
                    _ => DeathChoice.Continue,
                };
            }
            finally
            {
                // Stops waiting on the buttons that weren't picked.
                choosing.Cancel();
                if (this != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
