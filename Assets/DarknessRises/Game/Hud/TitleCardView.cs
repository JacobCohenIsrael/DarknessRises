using TMPro;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>A line of large text across the middle of the screen, such as a Level's title. Whoever shows it fades it.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TitleCardView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;

        /// <summary>How much the card shows, from 0 (hidden) to 1.</summary>
        public float Alpha
        {
            set
            {
                group.alpha = value;
                var shown = value > 0f;
                if (label.enabled != shown)
                {
                    label.enabled = shown;
                }
            }
        }

        /// <summary>Puts <paramref name="text"/> on the card, hidden until <see cref="Alpha"/> raises it.</summary>
        public void Show(string text)
        {
            label.text = text;
            Alpha = 0f;
        }

        private void Reset()
        {
            group = GetComponent<CanvasGroup>();
            label = GetComponentInChildren<TMP_Text>();
        }
    }
}
