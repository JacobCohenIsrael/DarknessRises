using TMPro;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// A line of large text across the middle of the screen, such as a Level's title, with an optional smaller line under
    /// it. Whoever shows it fades it. It's never a raycast target, so taps go through it and fire.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TitleCardView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;

        [SerializeField, Tooltip("The smaller line under the title. Empty when the card has only a title.")]
        private TMP_Text subtitle;

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

                if (subtitle != null && subtitle.enabled != shown)
                {
                    subtitle.enabled = shown;
                }
            }
        }

        /// <summary>
        /// Puts <paramref name="text"/> on the card, with <paramref name="line"/> under it if there is one, hidden until
        /// <see cref="Alpha"/> raises it.
        /// </summary>
        public void Show(string text, string line = null)
        {
            label.text = text;
            if (subtitle != null)
            {
                subtitle.text = line ?? string.Empty;
            }

            Alpha = 0f;
        }

        private void Reset()
        {
            group = GetComponent<CanvasGroup>();
            label = GetComponentInChildren<TMP_Text>();
        }
    }
}
