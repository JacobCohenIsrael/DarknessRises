using DarknessRises.Game.Rooms;
using UnityEngine;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Shows Lightning: plays the Rumble, then flashes a cold key light through the Room's windows
    /// (the only real-time shadow-casting light in the game), a shadowless fill and the window sky,
    /// and fades them back to Darkness. When does the <see cref="LightningScheduler"/>'s job.
    /// </summary>
    public sealed class LightningView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Directional light through the windows. The only light that casts real-time shadows.")]
        private Light keyLight;

        [SerializeField, Tooltip("Shadowless directional light that lets the whole Room show during the flash.")]
        private Light fillLight;

        [SerializeField] private AudioSource rumbleSource;

        private LightningFlashSettings flash;
        private RumbleSettings rumble;
        private AudioClip rumbleClip;
        private Room room;
        private MaterialPropertyBlock windowProperties;

        private bool flashing;
        private float flashElapsedSeconds;

        private bool rumbling;
        private float rumbleElapsedSeconds;
        private float rumbleFadeStartSeconds;

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

            rumbleSource.playOnAwake = false;
            rumbleSource.loop = false;
            rumbleSource.spatialBlend = 0f;

            StopFlash();
        }

        /// <summary>The Rumble clip, loaded with the Level's content. Without one, Lightning comes silently.</summary>
        public void UseRumble(AudioClip clip)
        {
            rumbleClip = clip;
            rumbleSource.clip = clip;
        }

        /// <summary>Starts the Rumble. It fades out once the flash is due, <paramref name="secondsUntilFlash"/> from now.</summary>
        public void PlayRumble(float secondsUntilFlash)
        {
            var clip = rumbleClip;
            if (clip == null)
            {
                return;
            }

            rumbleSource.volume = rumble.Volume;
            rumbleSource.Play();
            rumbleSource.time = Mathf.Clamp(rumble.ClipStartSeconds, 0f, clip.length);
            rumbling = true;
            rumbleElapsedSeconds = 0f;
            rumbleFadeStartSeconds = secondsUntilFlash;
        }

        /// <summary>Strikes. A new flash during a fading one starts over from the strike.</summary>
        public void Flash()
        {
            flashing = true;
            flashElapsedSeconds = 0f;
            SetBrightness(flash.Brightness(0f));
        }

        /// <summary>Cuts the flash and the Rumble short, back to Darkness.</summary>
        public void StopFlash()
        {
            flashing = false;
            SetBrightness(0f);

            if (rumbling)
            {
                rumbling = false;
                rumbleSource.Stop();
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

            if (rumbling)
            {
                rumbleElapsedSeconds += deltaSeconds;
                var fadeSeconds = rumbleElapsedSeconds - rumbleFadeStartSeconds;
                if (fadeSeconds <= 0f)
                {
                    return;
                }

                var remaining = rumble.FadeOutSeconds > 0f ? 1f - fadeSeconds / rumble.FadeOutSeconds : 0f;
                if (remaining <= 0f)
                {
                    rumbling = false;
                    rumbleSource.Stop();
                }
                else
                {
                    rumbleSource.volume = rumble.Volume * remaining;
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
