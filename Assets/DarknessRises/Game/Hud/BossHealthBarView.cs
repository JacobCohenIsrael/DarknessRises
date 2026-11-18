using DarknessRises.Game.Bosses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The Boss health bar across the top of the HUD, shown only during a Boss fight: the Boss's name, its hit points as
    /// a fill, a lighter trail for the hit points just lost, and ticks where its phases start. It slides in when the fight
    /// starts and out when it ends, inside the safe area and clear of the HP pips and the pause button.
    /// </summary>
    /// <remarks>
    /// The fill and the ticks are read from the fight's <see cref="ColossusPattern"/>; <see cref="BossHealthBar"/> only
    /// moves the trail and the slide. Both run on scaled time, so the bar freezes while the game is paused.
    /// </remarks>
    public sealed class BossHealthBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("The bar itself, name included. Anchored to the top centre of the HUD.")]
        private RectTransform panel;

        [SerializeField] private TextMeshProUGUI bossName;
        [SerializeField] private RectTransform bar;
        [SerializeField] private Image background;
        [SerializeField] private Image trail;
        [SerializeField] private Image fill;

        [SerializeField, Tooltip("Where phase 2 starts, then where phase 3 starts.")]
        private Image[] phaseTicks = System.Array.Empty<Image>();

        private ColossusView boss;
        private BossHealthBarSettings settings;
        private BossHealthBar rules;
        private ColossusPattern pattern;
        private Canvas canvas;
        private System.Action<int, int> onDamaged;

        private Rect fittedSafeArea;
        private float fittedScale;
        private float fittedSlide = -1f;
        private float shownFill = -1f;
        private float shownTrail = -1f;

        /// <summary>The bar's rules while a Boss is watched; null before.</summary>
        public BossHealthBar Rules => rules;

        /// <summary>Shows <paramref name="watched"/>'s hit points whenever it fights.</summary>
        public void Watch(ColossusView watched, BossHealthBarSettings barSettings)
        {
            Unwatch();
            boss = watched;
            settings = barSettings;
            rules = new BossHealthBar(settings.TrailHoldSeconds, settings.TrailDrainPerSecond, settings.SlideSeconds);
            canvas = GetComponentInParent<Canvas>();
            onDamaged ??= OnDamaged;
            Style();
            panel.gameObject.SetActive(false);

            boss.FightBegan += OnFightBegan;
            boss.FightEnded += OnFightEnded;
        }

        private void OnDestroy()
        {
            Unwatch();
        }

        private void Unwatch()
        {
            if (boss != null)
            {
                boss.FightBegan -= OnFightBegan;
                boss.FightEnded -= OnFightEnded;
            }

            StopReading();
            boss = null;
        }

        private void Style()
        {
            bossName.fontSize = settings.NameFontSize;
            bossName.color = settings.NameColor;
            bossName.rectTransform.sizeDelta = new Vector2(0f, settings.NameFontSize);
            bar.sizeDelta = new Vector2(0f, settings.BarHeight);
            background.color = settings.BackgroundColor;
            trail.color = settings.TrailColor;
            fill.color = settings.FillColor;
            for (var i = 0; i < phaseTicks.Length; i++)
            {
                phaseTicks[i].color = settings.TickColor;
                phaseTicks[i].rectTransform.sizeDelta = new Vector2(settings.TickWidth, 0f);
            }
        }

        private void OnFightBegan(ColossusPattern fight)
        {
            StopReading();
            pattern = fight;
            pattern.Damaged += onDamaged;

            bossName.text = boss.Definition.DisplayName;
            PlaceTick(0, pattern.Phases.Phase2Below);
            PlaceTick(1, pattern.Phases.Phase3Below);

            rules.Show(Fill());
            fittedSlide = -1f;
            shownFill = shownTrail = -1f;
            panel.gameObject.SetActive(true);
            Show();
        }

        private void OnFightEnded()
        {
            rules.Hide();
        }

        private void StopReading()
        {
            if (pattern != null)
            {
                pattern.Damaged -= onDamaged;
            }
        }

        private void OnDamaged(int dealt, int left)
        {
            rules.Hit();
        }

        private void PlaceTick(int index, float share)
        {
            if (index >= phaseTicks.Length)
            {
                return;
            }

            var tick = phaseTicks[index].rectTransform;
            tick.anchorMin = new Vector2(share, 0f);
            tick.anchorMax = new Vector2(share, 1f);
            tick.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            if (rules == null || pattern == null || !panel.gameObject.activeSelf)
            {
                return;
            }

            rules.Tick(Time.deltaTime, Fill());
            Show();
            if (!rules.IsOnScreen)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private float Fill()
        {
            return pattern.MaxHitPoints > 0 ? (float)pattern.HitPoints / pattern.MaxHitPoints : 0f;
        }

        private void Show()
        {
            Fit();

            var fillShare = Fill();
            if (!Mathf.Approximately(fillShare, shownFill))
            {
                shownFill = fillShare;
                fill.rectTransform.anchorMax = new Vector2(fillShare, 1f);
            }

            if (!Mathf.Approximately(rules.Trail, shownTrail))
            {
                shownTrail = rules.Trail;
                trail.rectTransform.anchorMax = new Vector2(shownTrail, 1f);
            }
        }

        // Inside the safe area, clear of the HP pips and the pause button; above the screen's top edge when slid out.
        // Only redone when the safe area, the HUD scale or the slide change.
        private void Fit()
        {
            var safeArea = Screen.safeArea;
            var scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            var slide = rules.SlideIn;
            if (safeArea == fittedSafeArea && Mathf.Approximately(scale, fittedScale) && Mathf.Approximately(slide, fittedSlide))
            {
                return;
            }

            fittedSafeArea = safeArea;
            fittedScale = scale;
            fittedSlide = slide;

            var topInset = (Screen.height - safeArea.yMax) / scale;
            var safeWidth = safeArea.width / scale;
            var width = Mathf.Max(1f, Mathf.Min(settings.MaxWidth, safeWidth - 2f * settings.SideClearance));
            var shownTop = -(topInset + settings.TopMargin);
            var hiddenTop = settings.TotalHeight;
            var x = (safeArea.center.x - Screen.width * 0.5f) / scale;

            panel.sizeDelta = new Vector2(width, settings.TotalHeight);
            panel.anchoredPosition = new Vector2(x, Mathf.Lerp(hiddenTop, shownTop, Mathf.SmoothStep(0f, 1f, slide)));
        }
    }
}
