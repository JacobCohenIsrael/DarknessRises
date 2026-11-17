using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Death
{
    /// <summary>What the player picks on the death screen. Continue comes in a later ticket.</summary>
    public enum DeathChoice
    {
        /// <summary>End this Run and start a new one from the first Level.</summary>
        Restart,

        /// <summary>End the Run and see the end screen.</summary>
        Exit,
    }

    /// <summary>The death screen: shown once the character has died, it offers Restart and Exit.</summary>
    public sealed class DeathScreenView : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        /// <summary>Shows the screen until the player picks Restart or Exit, then hides it.</summary>
        public async UniTask<DeathChoice> ChooseAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            using var choosing = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                var picked = await UniTask.WhenAny(
                    restartButton.OnClickAsync(choosing.Token),
                    exitButton.OnClickAsync(choosing.Token));
                return picked == 0 ? DeathChoice.Restart : DeathChoice.Exit;
            }
            finally
            {
                // Stops waiting on the button that wasn't picked.
                choosing.Cancel();
                if (this != null)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
