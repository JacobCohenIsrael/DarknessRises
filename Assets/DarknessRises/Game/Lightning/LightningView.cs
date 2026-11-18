using DarknessRises.Game.Rooms;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Shows Lightning: plays the Rumble, then flashes an electric-blue key light through the Room's windows
    /// (the only real-time shadow-casting light in the game), a shadowless fill and the window sky,
    /// and fades them back to Darkness. When does the <see cref="LightningScheduler"/>'s job.
    /// </summary>
    /// <remarks>
    /// Rumbles alternate between AudioSources, each with its own <see cref="RumbleVoice"/>, so a new Rumble lets a
    /// fading one roll on, and no Rumble ever stops dead: cutting Lightning short fades them out over a short authored time.
    /// </remarks>
    public sealed class LightningView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Directional light through the windows. The only light that casts real-time shadows.")]
        private Light keyLight;

        [SerializeField, Tooltip("Shadowless directional light that lets the whole Room show during the flash.")]
        private Light fillLight;

        [SerializeField, Tooltip("Rumble sources. A new Rumble plays on a free one, so it doesn't cut off a fading one.")]
        private AudioSource[] rumbleSources = new AudioSource[2];

        private LightningFlashSettings flash;
        private RumbleSettings rumble;
        private AudioClip rumbleClip;
        private Room room;
        private MaterialPropertyBlock windowProperties;
        private RumbleVoice[] rumbleVoices;

        /// <summary>The directional light that shines through the windows while Lightning flashes.</summary>
        public Light KeyLight => keyLight;

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

        /// <summary>The Rumble clip, loaded with the Level's content. Without one, Lightning comes silently.</summary>
        public void UseRumble(AudioClip clip)
        {
            rumbleClip = clip;
            if (clip != null)
            {
                // The Level's content loads it already; this only makes sure, since the Rumble seeks into the clip.
                clip.LoadAudioData();
            }
        }

        /// <summary>Starts the Rumble. It fades out once the flash is due, <paramref name="secondsUntilFlash"/> from now.</summary>
        public void PlayRumble(float secondsUntilFlash)
        {
            var clip = rumbleClip;
            if (clip == null)
            {
                return;
            }

            var index = RumbleVoice.PickFor(rumbleVoices);
            var source = rumbleSources[index];
            rumbleVoices[index].Start(rumble.Volume, Mathf.Max(0f, secondsUntilFlash), rumble.FadeOutSeconds);
            source.clip = clip;
            source.volume = rumble.Volume;
            source.Play();

            // Measuring or seeking a clip that hasn't loaded fails (WebGL decodes audio late): it plays from the start instead.
            if (clip.loadState == AudioDataLoadState.Loaded)
            {
                source.time = Mathf.Clamp(rumble.ClipStartSeconds, 0f, clip.length);
            }
        }

        /// <summary>Strikes. A new flash during a fading one starts over from the strike.</summary>
        public void Flash()
        {
            flashing = true;
            flashElapsedSeconds = 0f;
            SetBrightness(flash.Brightness(0f));
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
