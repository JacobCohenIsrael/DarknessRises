using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Runs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Death
{
    /// <summary>
    /// The end screen after Exit: the Level reached and the Enemies killed, with no score. It leads to a new Run
    /// for now, and to the Shop once the Shop exists.
    /// </summary>
    public sealed class EndScreenView : MonoBehaviour
    {
        private const string NumberFormat = "{0}";

        [SerializeField] private TMP_Text levelReached;
        [SerializeField] private TMP_Text enemiesKilled;
        [SerializeField] private Button newRunButton;

        /// <summary>Shows <paramref name="record"/> until the player moves on.</summary>
        public async UniTask ShowAsync(RunRecord record, CancellationToken cancellationToken)
        {
            // TMP's numeric SetText formats without allocating; {0} prints no decimals.
            levelReached.SetText(NumberFormat, (float)record.LevelReached);
            enemiesKilled.SetText(NumberFormat, (float)record.EnemiesKilled);
            gameObject.SetActive(true);
            await newRunButton.OnClickAsync(cancellationToken);
        }
    }
}
