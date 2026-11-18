using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Shows Lightning: flashes an electric-blue key light through the Room's windows (the only real-time
    /// shadow-casting light in the game), a shadowless fill and the window sky, and fades them back to Darkness;
    /// then plays the Rumble, the thunder, at a varied pitch and start point. In a Boss fight it also plays the Boss's
    /// warning (its growl) before a flash. When does the <see cref="LightningScheduler"/>'s job.
    /// </summary>
    /// <remarks>
    /// Rumbles and warnings take turns on AudioSources, each with its own <see cref="RumbleVoice"/>, so a new sound lets
    /// a fading one roll on, and none ever stops dead: cutting Lightning short fades them out over a short authored time.
    /// </remarks>
    public sealed class LightningView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Directional light through the windows. The only light that casts real-time shadows.")]
        private Light keyLight;

        [SerializeField, Tooltip("Shadowless directional light that lets the whole Room show during the flash.")]
        private Light fillLight;

        [SerializeField, Tooltip("Rumble and warning sources. A new sound plays on a free one, so it doesn't cut off a fading one.")]
        private AudioSource[] rumbleSources = new AudioSource[3];

        private LightningFlashSettings flash;
        private RumbleSettings rumble;
        private AudioClip rumbleClip;
        private IRandom rumbleRandom;
        private AudioClip warningClip;
        private float warningClipStartSeconds;
        private Room room;
        private MaterialPropertyBlock windowProperties;
        private RumbleVoice[] rumbleVoices;

        /// <summary>The directional light that shines through the windows while Lightning flashes.</summary>
        public Light KeyLight => keyLight;

        /// <summary>When (Unity time) Lightning last flashed, or -1 before the first flash.</summary>
        public float LastFlashTime { get; private set; } = -1f;

        /// <summary>When (Unity time) the last Rumble started, or -1 before the first.</summary>
        public float LastRumbleTime { get; private set; } = -1f;

        /// <summary>The pitch and start point of the last Rumble.</summary>
        public RumbleTake LastRumbleTake { get; private set; }

        /// <summary>When (Unity time) the last Boss warning started, or -1 before the first.</summary>
        public float LastWarningTime { get; private set; } = -1f;

        private bool flashing;
        private float flashElapsedSeconds;

        /// <summary>Sets up the lights for the definition and aims them through the Room's windows. Leaves the Room dark.</summary>
        public void Show(LightningDefinition definition, Room shownRoom)
        {
            flash = definition.Flash;
            rumble = definition.Rumble;
            room = shownRoom;

            keyLight.type = LightType.Directional;
            keyLight.shadows = LightShadows.Hard;
            keyLight.shadowStrength = flash.ShadowStrength;
            keyLight.color = flash.KeyColor;
            keyLight.transform.rotation = Quaternion.Euler(flash.ElevationDegrees, room.LightningYawDegrees, 0f);

            fillLight.type = LightType.Directional;
            fillLight.shadows = LightShadows.None;
            fillLight.color = flash.FillColor;
            fillLight.transform.rotation = Quaternion.Euler(
                flash.FillElevationDegrees,
                room.LightningYawDegrees + flash.FillYawOffsetDegrees,
                0f);

            if (rumbleVoices == null)
            {
                rumbleVoices = new RumbleVoice[rumbleSources.Length];
                for (var i = 0; i < rumbleSources.Length; i++)
                {
                    rumbleVoices[i] = new RumbleVoice();
                }
            }

            foreach (var source in rumbleSources)
            {
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }

            StopFlash();
        }

        /// <summary>
        /// The Rumble clip, loaded with the Level's content, and where each Rumble's pitch and start point come from.
        /// Without a clip, Lightning comes silently.
        /// </summary>
        public void UseRumble(AudioClip clip, IRandom random)
        {
            rumbleClip = clip;
            rumbleRandom = random;
            EnsureLoaded(clip);
        }

        /// <summary>
        /// The Boss's warning before its flashes (the Colossus's growl), loaded with the Level's content, and where in the
        /// clip it starts. Null when there's no Boss.
        /// </summary>
        public void UseWarning(AudioClip clip, float clipStartSeconds)
        {
            warningClip = clip;
            warningClipStartSeconds = clipStartSeconds;
            EnsureLoaded(clip);
        }

        /// <summary>Starts the Rumble at a fresh pitch and start point. It holds a moment, then fades out.</summary>
        public void PlayRumble()
        {
            if (rumbleClip == null || rumbleRandom == null)
            {
                return;
            }

            var take = rumble.Variation.Pick(rumbleRandom);
            Play(rumbleClip, take.Pitch, take.ClipStartSeconds, rumble.HoldSeconds);
            LastRumbleTake = take;
            LastRumbleTime = Time.time;
        }

        /// <summary>Starts the Boss's warning. It fades out once the flash is due, <paramref name="secondsUntilFlash"/> from now.</summary>
        public void PlayWarning(float secondsUntilFlash)
        {
            if (warningClip == null)
            {
                return;
            }

            Play(warningClip, 1f, warningClipStartSeconds, Mathf.Max(0f, secondsUntilFlash));
            LastWarningTime = Time.time;
        }

        /// <summary>Strikes. A new flash during a fading one starts over from the strike.</summary>
        public void Flash()
        {
            flashing = true;
            flashElapsedSeconds = 0f;
            SetBrightness(flash.Brightness(0f));
            LastFlashTime = Time.time;
        }

        /// <summary>Cuts the flash short, back to Darkness, and fades any Rumble out over the authored cut time.</summary>
        public void StopFlash()
        {
            flashing = false;
            SetBrightness(0f);

            for (var i = 0; i < rumbleVoices.Length; i++)
            {
                rumbleVoices[i].FadeOut(rumble.CutFadeSeconds);
            }
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;

            if (flashing)
            {
                flashElapsedSeconds += deltaSeconds;
                flashing = flashElapsedSeconds < flash.DurationSeconds;
                SetBrightness(flash.Brightness(flashElapsedSeconds));
            }

            if (rumbleVoices == null)
            {
                return;
            }

            // Scaled time: the pause freezes Rumbles (with AudioListener.pause) and they carry on afterwards.
            for (var i = 0; i < rumbleVoices.Length; i++)
            {
                var voice = rumbleVoices[i];
                if (!voice.IsPlaying)
                {
                    continue;
                }

                voice.Tick(deltaSeconds);
                if (voice.IsPlaying)
                {
                    rumbleSources[i].volume = voice.Volume;
                }
                else
                {
                    rumbleSources[i].Stop();
                }
            }
        }

        private static void EnsureLoaded(AudioClip clip)
        {
            if (clip != null)
            {
                // The Level's content loads it already; this only makes sure, since playback seeks into the clip.
                clip.LoadAudioData();
            }
        }

        private void Play(AudioClip clip, float pitch, float clipStartSeconds, float holdSeconds)
        {
            var index = RumbleVoice.PickFor(rumbleVoices);
            var source = rumbleSources[index];
            rumbleVoices[index].Start(rumble.Volume, holdSeconds, rumble.FadeOutSeconds);
            source.clip = clip;
            source.pitch = pitch;
            source.volume = rumble.Volume;
            source.Play();

            // Measuring or seeking a clip that hasn't loaded fails (WebGL decodes audio late): it plays from the start instead.
            if (clip.loadState == AudioDataLoadState.Loaded)
            {
                source.time = Mathf.Clamp(clipStartSeconds, 0f, clip.length);
            }
        }

        private void SetBrightness(float brightness)
        {
            var lit = brightness > 0f;
            keyLight.enabled = lit;
            fillLight.enabled = lit;
            keyLight.intensity = brightness * flash.KeyIntensity;
            fillLight.intensity = brightness * flash.FillIntensity;

            windowProperties ??= new MaterialPropertyBlock();
            windowProperties.SetColor(BaseColorId, flash.WindowGlowColor * brightness);
            var windows = room.Windows;
            for (var i = 0; i < windows.Count; i++)
            {
                windows[i].SetPropertyBlock(windowProperties);
            }
        }
    }
}
