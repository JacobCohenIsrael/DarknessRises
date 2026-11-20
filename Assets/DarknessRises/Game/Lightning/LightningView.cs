using DarknessRises.Game.Audio;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using UnityEngine;
using UnityEngine.Serialization;

namespace DarknessRises.Game.Lightning
{
    /// <summary>
    /// Shows Lightning: flashes an electric-blue key light through the Room's windows (the only real-time
    /// shadow-casting light in the game) from a slightly different angle each strike, a shadowless fill and the window
    /// sky, and fades them back to Darkness. Drives the storm: a thunder loop that runs all the time Lightning reaches
    /// the character and swells after each flash (the Rumble), played by a <see cref="StormPlayer"/> (the boot's, which
    /// carries the storm from Level to Level, or the scene's own when it's played alone). In a Boss fight it also plays
    /// the Boss's warning (its growl) before a flash. When does the <see cref="LightningScheduler"/>'s job; how the
    /// storm's volume and pitch move does the <see cref="StormBed"/>'s.
    /// </summary>
    /// <remarks>
    /// Audio is plain AudioSource volume and pitch, with no AudioMixer, so it works on WebGL. The storm's pitch follows
    /// Slow-mo the way Enemy sounds do (<see cref="EnemyVoicePitch"/> on <see cref="EnemyTime"/>). Everything runs on
    /// scaled time, so the pause (<c>Time.timeScale = 0</c> and <c>AudioListener.pause</c>) freezes it, and it carries on
    /// afterwards. Warnings take turns on AudioSources, each with its own <see cref="WarningVoice"/>, so a new one lets a
    /// fading one roll on.
    /// </remarks>
    public sealed class LightningView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Tooltip("Directional light through the windows. The only light that casts real-time shadows.")]
        private Light keyLight;

        [SerializeField, Tooltip("Shadowless directional light that lets the whole Room show during the flash.")]
        private Light fillLight;

        [SerializeField, Tooltip("The scene's own storm player, used when the scene is played without the boot, whose player carries the storm across Levels.")]
        private StormPlayer ownStorm;

        [SerializeField, FormerlySerializedAs("rumbleSources"),
         Tooltip("Boss warning sources. A new warning plays on a free one, so it doesn't cut off a fading one.")]
        private AudioSource[] warningSources = new AudioSource[2];

        private LightningFlashSettings flash;
        private BossWarningSettings warning;
        private LightningAngles angles;
        private StormPlayer storm;
        private IRandom random;
        private EnemyTime enemyTime;
        private AudioClip warningClip;
        private float warningClipStartSeconds;
        private Room room;
        private MaterialPropertyBlock windowProperties;
        private WarningVoice[] warningVoices;

        private bool flashing;
        private float flashElapsedSeconds;

        /// <summary>The directional light that shines through the windows while Lightning flashes.</summary>
        public Light KeyLight => keyLight;

        /// <summary>The scene's own storm player.</summary>
        public StormPlayer OwnStorm => ownStorm;

        /// <summary>The storm player this Level drives, once it has one.</summary>
        public StormPlayer Storm => storm;

        /// <summary>When (Unity time) Lightning last flashed, or -1 before the first flash.</summary>
        public float LastFlashTime { get; private set; } = -1f;

        /// <summary>The angle of the last strike.</summary>
        public LightningAngle LastFlashAngle { get; private set; }

        /// <summary>When (Unity time) the last Rumble started, or -1 before the first.</summary>
        public float LastRumbleTime { get; private set; } = -1f;

        /// <summary>When (Unity time) the last Boss warning started, or -1 before the first.</summary>
        public float LastWarningTime { get; private set; } = -1f;

        /// <summary>Sets up the lights for the definition and aims them through the Room's windows. Leaves the Room dark.</summary>
        public void Show(LightningDefinition definition, Room shownRoom)
        {
            flash = definition.Flash;
            warning = definition.BossWarning;
            angles = flash.Angles;
            room = shownRoom;

            keyLight.type = LightType.Directional;
            keyLight.shadows = LightShadows.Hard;
            keyLight.shadowStrength = flash.ShadowStrength;
            keyLight.color = flash.KeyColor;

            fillLight.type = LightType.Directional;
            fillLight.shadows = LightShadows.None;
            fillLight.color = flash.FillColor;
            Aim(new LightningAngle(angles.MiddleElevationDegrees, room.LightningYawDegrees));

            if (warningVoices == null)
            {
                warningVoices = new WarningVoice[warningSources.Length];
                for (var i = 0; i < warningSources.Length; i++)
                {
                    warningVoices[i] = new WarningVoice();
                }
            }

            foreach (var source in warningSources)
            {
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }

            StopFlash();
        }

        /// <summary>
        /// Drives the storm on <paramref name="player"/> (the boot's, or null for the scene's own) with the storm of
        /// <paramref name="settings"/>; where each strike's angle and Rumble's pitch nudge come from; and the Enemy time
        /// whose Slow-mo the storm's pitch follows. Without a clip, Lightning comes silently.
        /// </summary>
        public void UseStorm(StormPlayer player, StormSettings settings, IRandom strikeRandom, EnemyTime time)
        {
            storm = player != null ? player : ownStorm;
            random = strikeRandom;
            enemyTime = time;
            storm.Play(settings.Clip, settings.Envelope);
        }

        /// <summary>
        /// The Boss's warning before its flashes (the Colossus's growl), loaded with the Level's content, and where in the
        /// clip it starts. Null when there's no Boss.
        /// </summary>
        public void UseWarning(AudioClip clip, float clipStartSeconds)
        {
            warningClip = clip;
            warningClipStartSeconds = clipStartSeconds;
            if (clip != null)
            {
                clip.LoadAudioData();
            }
        }

        /// <summary>
        /// Whether the storm reaches the character (<see cref="LightningScheduler.IsStormRunning"/>). The bed fades in or
        /// out over its authored time; it never starts or stops dead.
        /// </summary>
        public void SetStormAudible(bool audible)
        {
            if (storm != null)
            {
                storm.SetAudible(audible);
            }
        }

        /// <summary>The Rumble: the storm bed swells, with a small pitch nudge, then settles back.</summary>
        public void PlayRumble()
        {
            if (storm == null || random == null)
            {
                return;
            }

            storm.Rumble(random);
            LastRumbleTime = Time.time;
        }

        /// <summary>Starts the Boss's warning. It fades out once the flash is due, <paramref name="secondsUntilFlash"/> from now.</summary>
        public void PlayWarning(float secondsUntilFlash)
        {
            if (warningClip == null)
            {
                return;
            }

            var index = WarningVoice.PickFor(warningVoices);
            var source = warningSources[index];
            warningVoices[index].Start(warning.Volume, Mathf.Max(0f, secondsUntilFlash), warning.FadeOutSeconds);
            source.clip = warningClip;
            source.pitch = 1f;
            source.SetVolume(warning.Volume);
            source.Play();

            // Seeking a clip that hasn't loaded fails (WebGL decodes audio late): it plays from the start instead.
            if (warningClip.loadState == AudioDataLoadState.Loaded)
            {
                source.time = Mathf.Clamp(warningClipStartSeconds, 0f, warningClip.length);
            }

            LastWarningTime = Time.time;
        }

        /// <summary>Strikes, from a fresh angle. A new flash during a fading one starts over from the strike.</summary>
        public void Flash()
        {
            if (random != null)
            {
                LastFlashAngle = angles.Pick(random, room.LightningYawDegrees);
                Aim(LastFlashAngle);
            }

            flashing = true;
            flashElapsedSeconds = 0f;
            SetBrightness(flash.Brightness(0f));
            LastFlashTime = Time.time;
        }

        /// <summary>
        /// Cuts the flash short, back to Darkness, and fades any Boss warning out over the authored cut time. The storm
        /// bed is left alone: it follows <see cref="SetStormAudible"/>.
        /// </summary>
        public void StopFlash()
        {
            flashing = false;
            SetBrightness(0f);

            for (var i = 0; i < warningVoices.Length; i++)
            {
                warningVoices[i].FadeOut(warning.CutFadeSeconds);
            }
        }

        private void Update()
        {
            // Scaled time: the pause freezes the flash, the storm and warnings (with AudioListener.pause), and they carry on.
            var deltaSeconds = Time.deltaTime;

            if (flashing)
            {
                flashElapsedSeconds += deltaSeconds;
                flashing = flashElapsedSeconds < flash.DurationSeconds;
                SetBrightness(flash.Brightness(flashElapsedSeconds));
            }

            if (storm != null)
            {
                storm.PitchScale = enemyTime?.Scale ?? EnemyTime.NormalScale;
            }

            UpdateWarnings(deltaSeconds);
        }

        // The boot's storm outlives this Level: the next one picks it up as this one leaves it, at normal pitch.
        private void OnDestroy()
        {
            if (storm != null && storm != ownStorm)
            {
                storm.PitchScale = EnemyTime.NormalScale;
            }
        }

        private void UpdateWarnings(float deltaSeconds)
        {
            if (warningVoices == null)
            {
                return;
            }

            for (var i = 0; i < warningVoices.Length; i++)
            {
                var voice = warningVoices[i];
                if (!voice.IsPlaying)
                {
                    continue;
                }

                voice.Tick(deltaSeconds);
                if (voice.IsPlaying)
                {
                    warningSources[i].SetVolume(voice.Volume);
                }
                else
                {
                    warningSources[i].Stop();
                }
            }
        }

        private void Aim(LightningAngle angle)
        {
            keyLight.transform.rotation = Quaternion.Euler(angle.ElevationDegrees, angle.YawDegrees, 0f);
            fillLight.transform.rotation = Quaternion.Euler(
                flash.FillElevationDegrees,
                angle.YawDegrees + flash.FillYawOffsetDegrees,
                0f);
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
