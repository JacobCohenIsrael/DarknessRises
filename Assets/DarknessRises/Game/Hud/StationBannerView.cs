using System;
using DarknessRises.Game.Content;
using DarknessRises.Game.Routes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows the Station rhythm on the HUD: the Ready banner as the character stops at a Station, the Clear banner once
    /// its last Wave is dead, and the Go cue with an arrow pointing along the Route before it sets off, each with its
    /// sound. The rhythm decides the beats (<see cref="StationRhythm"/>) and <see cref="BannerPop"/> how a banner
    /// shows; this only reacts to the beat changing. One banner object serves all three, its text is set only when a
    /// beat starts, and nothing in it is a raycast target, so it never takes a tap.
    /// </summary>
    public sealed class StationBannerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;

        [SerializeField, Tooltip("The dark strip across the screen behind the text.")]
        private Image band;

        [SerializeField, Tooltip("Holds the text and the arrow. Scaled by the pop.")]
        private RectTransform banner;

        [SerializeField] private TMP_Text label;

        [SerializeField, Tooltip("The Go cue's arrow. Its sprite points right.")]
        private Image arrow;

        [SerializeField, Tooltip("Play the banners' sounds in 2D, taking turns, so a new sound never cuts the last one's tail.")]
        private AudioSource[] voices = Array.Empty<AudioSource>();

        [SerializeField, Tooltip("The camera the player sees the Room through, to tell which way the Route heads on screen.")]
        private Camera worldCamera;

        private StationRhythm rhythm;
        private StationBannerSettings settings;
        private RouteProgress route;
        private BannerPop pop;
        private AudioClip readyClip;
        private AudioClip clearClip;
        private AudioClip goClip;
        private Action<StationBeat> onBeatChanged;
        private int nextVoice;
        private bool pointsAlongRoute;
        private float secondsPointing;
        private float shownAlpha = -1f;
        private float shownScale = -1f;

        /// <summary>
        /// Starts showing the beats of <paramref name="stationRhythm"/> as <paramref name="bannerSettings"/> say, with
        /// the Go cue pointing the way <paramref name="routeProgress"/> heads. The sounds are already loaded in
        /// <paramref name="content"/>.
        /// </summary>
        public void Watch(
            StationRhythm stationRhythm,
            StationBannerSettings bannerSettings,
            RouteProgress routeProgress,
            ILoadedContent content)
        {
            Unwatch();

            rhythm = stationRhythm ?? throw new ArgumentNullException(nameof(stationRhythm));
            settings = bannerSettings ?? throw new ArgumentNullException(nameof(bannerSettings));
            route = routeProgress ?? throw new ArgumentNullException(nameof(routeProgress));
            pop = new BannerPop(settings.PopSeconds, settings.PopScale, settings.FadeOutSeconds);
            readyClip = content.Clip(settings.Ready.Clip);
            clearClip = content.Clip(settings.Clear.Clip);
            goClip = content.Clip(settings.Go.Clip);

            banner.anchoredPosition = settings.Position;
            band.rectTransform.anchoredPosition = new Vector2(0f, settings.Position.y);
            band.rectTransform.sizeDelta = new Vector2(0f, settings.BandHeight);
            band.color = settings.BandColor;
            label.fontSize = settings.FontSize;
            label.characterSpacing = settings.CharacterSpacing;
            arrow.rectTransform.sizeDelta = new Vector2(settings.ArrowSize, settings.ArrowSize);

            onBeatChanged ??= OnBeatChanged;
            rhythm.BeatChanged += onBeatChanged;
            Apply();
        }

        private void OnDestroy()
        {
            Unwatch();
        }

        private void Unwatch()
        {
            if (rhythm != null)
            {
                rhythm.BeatChanged -= onBeatChanged;
                rhythm = null;
            }
        }

        private void OnBeatChanged(StationBeat beat)
        {
            switch (beat)
            {
                case StationBeat.Ready:
                    Show(settings.Ready, readyClip, false);
                    break;
                case StationBeat.Clear:
                    Show(settings.Clear, clearClip, false);
                    break;
                case StationBeat.Go:
                    Show(settings.Go, goClip, true);
                    break;
                case StationBeat.Walking:
                    // The Go cue sees the character off.
                    pop.Hide(settings.GoLingerSeconds);
                    break;
                default:
                    pop.Hide();
                    break;
            }
        }

        private void Show(StationBannerCue cue, AudioClip clip, bool withArrow)
        {
            label.text = cue.Text;
            label.color = cue.Color;
            arrow.color = cue.Color;
            pointsAlongRoute = withArrow;
            secondsPointing = 0f;
            pop.Show();

            // Shown afresh, even when the last banner hasn't faded yet.
            shownAlpha = -1f;
            Apply();
            if (withArrow)
            {
                PointArrow();
            }

            Play(clip, cue);
        }

        private void Play(AudioClip clip, StationBannerCue cue)
        {
            if (clip == null || voices.Length == 0)
            {
                return;
            }

            var voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.clip = clip;
            voice.volume = cue.Volume;
            voice.time = Mathf.Clamp(cue.ClipStartSeconds, 0f, Mathf.Max(0f, clip.length - 0.01f));
            voice.Play();
        }

        private void Update()
        {
            if (pop == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            pop.Tick(deltaSeconds);
            Apply();

            if (pointsAlongRoute && pop.IsVisible)
            {
                secondsPointing += deltaSeconds;
                PointArrow();
            }
        }

        // Touches the canvas only when what it shows changes, so a hidden banner costs nothing.
        private void Apply()
        {
            var alpha = pop.Alpha;
            if (!Mathf.Approximately(alpha, shownAlpha))
            {
                shownAlpha = alpha;
                group.alpha = alpha;
                var visible = alpha > 0f;
                if (label.enabled != visible)
                {
                    label.enabled = visible;
                }

                if (band.enabled != visible)
                {
                    band.enabled = visible;
                }

                var arrowVisible = visible && pointsAlongRoute;
                if (arrow.enabled != arrowVisible)
                {
                    arrow.enabled = arrowVisible;
                }
            }

            var scale = pop.Scale;
            if (!Mathf.Approximately(scale, shownScale))
            {
                shownScale = scale;
                banner.localScale = new Vector3(scale, scale, 1f);
            }
        }

        // The arrow sits beside the text on the side the Route heads to, pointing that way and nudging along it.
        private void PointArrow()
        {
            var view = worldCamera.transform;
            var direction = GoCue.ScreenDirection(route.Direction, view.right, view.up);
            var nudge = settings.ArrowPulseDistance * Mathf.PingPong(secondsPointing * settings.ArrowPulsesPerSecond * 2f, 1f);
            var arrowRect = arrow.rectTransform;
            arrowRect.anchoredPosition = direction * (settings.ArrowDistance + nudge);
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, GoCue.AngleDegrees(direction));
        }
    }
}
