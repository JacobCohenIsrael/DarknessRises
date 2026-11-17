using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>A black cover over the whole screen, for fading out. Whoever shows it fades it.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenFadeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image cover;

        /// <summary>How black the screen is, from 0 (clear) to 1.</summary>
        public float Alpha
        {
            set
            {
                group.alpha = value;
                var shown = value > 0f;
                if (cover.enabled != shown)
                {
                    cover.enabled = shown;
                }
            }
        }

        private void Reset()
        {
            group = GetComponent<CanvasGroup>();
            cover = GetComponentInChildren<Image>();
        }
    }
}
