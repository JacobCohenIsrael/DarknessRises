using System;
using System.Runtime.CompilerServices;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Randomness;
using UnityEngine;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>
    /// Shows a living Enemy's Tells: its eyes glint (emissive, so they show in full Darkness) while it is within
    /// range of the character, once they've opened after it spawned, and it makes positional sounds now and then. Sits on the Enemy prefab and is driven
    /// by its <see cref="EnemyView"/>. Tells only reveal; nothing here feeds back into what the Enemy does.
    /// </summary>
    /// <remarks>
    /// During a Wind-up the Enemy view swaps the eyes to its brighter Wind-up material, which outshines the glint
    /// at any distance, opened or not: a real attack is never hidden. The glint material comes back when the Wind-up
    /// ends. The eyes' opening and Tell sounds run on Enemy time, so they follow Slow-mo and stop while paused.
    /// </remarks>
    public sealed class EnemyTellView : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Glint changes smaller than this aren't worth a material update.
        private const float GlintStep = 1f / 64f;

        [SerializeField, Tooltip("The eye renderers. They share one emissive material instance per Enemy.")]
        private Renderer[] eyes = new Renderer[0];

        [SerializeField, Tooltip("Positional source for Tell sounds, kept apart from the Enemy's other sounds so its pitch can vary.")]
        private AudioSource tellVoice;

        private EnemyTellSettings settings;
        private AudioClip[] soundClips;
        private IRandom random;
        private EnemyTime time;
        private TellSoundSchedule soundSchedule;
        private EyeOpening opening;
        private Material eyeInstance;
        private Transform watcher;
        private float glint = -1f;

        // The current Tell sound's own random pitch, before Enemy time slows it.
        private float tellPitch = 1f;
        private bool awake;

        /// <summary>
        /// Called once, when the pool creates the Enemy, before the Enemy remembers its renderers' materials,
        /// so the eyes' own material instance is what it restores on every spawn. The Tell sounds come from
        /// <paramref name="content"/>, already loaded.
        /// </summary>
        public void Prepare(EnemyTellSettings tellSettings, EnemyAudioSettings audio, EnemyTime enemyTime, ILoadedContent content)
        {
            settings = tellSettings;
            time = enemyTime;

            // Seeded per Enemy, so Enemies made in the same frame don't share a sequence and call out together.
            opening = settings.EyeGlint.Opening.Create();
            random = new SystemRandom(unchecked((Environment.TickCount * 397) ^ RuntimeHelpers.GetHashCode(this)));
            var sounds = settings.Sounds;
            soundClips = content.Clips(sounds.Clips);
            soundSchedule = new TellSoundSchedule(sounds.MinIntervalSeconds, sounds.MaxIntervalSeconds, random);

            tellVoice.playOnAwake = false;
            tellVoice.loop = false;
            tellVoice.spatialBlend = 1f;
            tellVoice.rolloffMode = AudioRolloffMode.Linear;
            tellVoice.maxDistance = audio.HearingRange;
            tellVoice.SetVolume(sounds.Volume);

            // Slow-mo slows Tell sounds with the Enemy's other sounds, on top of each sound's random pitch.
            time.ScaleChanged += OnEnemyTimeScaled;
            OnEnemyTimeScaled(time.Scale);

            if (eyes.Length == 0)
            {
                return;
            }

            eyeInstance = new Material(eyes[0].sharedMaterial) { name = $"{name} Eyes" };
            eyeInstance.EnableKeyword("_EMISSION");
            for (var i = 0; i < eyes.Length; i++)
            {
                eyes[i].sharedMaterial = eyeInstance;
            }

            SetGlint(0f);
        }

        /// <summary>The Enemy has spawned: start the Tells, measured from <paramref name="character"/>.</summary>
        public void Wake(Transform character)
        {
            watcher = character;
            awake = true;
            opening.Restart();
            glint = -1f;
            UpdateGlint();

            if (soundClips.Length > 0)
            {
                soundSchedule.Start();
            }
        }

        /// <summary>The Enemy has died: its eyes go dark and it makes no more Tell sounds.</summary>
        public void Silence()
        {
            awake = false;
            soundSchedule.Stop();
            tellVoice.Stop();
            SetGlint(0f);
        }

        private void Update()
        {
            if (!awake)
            {
                return;
            }

            var deltaSeconds = time.Scaled(Time.deltaTime);
            opening.Tick(deltaSeconds);
            UpdateGlint();
            if (soundSchedule.Tick(deltaSeconds))
            {
                PlayTellSound();
            }
        }

        private void OnDestroy()
        {
            if (time != null)
            {
                time.ScaleChanged -= OnEnemyTimeScaled;
            }

            Destroy(eyeInstance);
        }

        private void UpdateGlint()
        {
            var offset = transform.position - watcher.position;
            offset.y = 0f;
            var eyeGlint = settings.EyeGlint;
            var strength = EyeGlint.Strength(offset.magnitude, eyeGlint.Range, eyeGlint.FadeDistance, opening.Strength);
            if (Mathf.Abs(strength - glint) >= GlintStep || (strength != glint && (strength == 0f || strength == 1f)))
            {
                SetGlint(strength);
            }
        }

        private void SetGlint(float strength)
        {
            glint = strength;
            if (eyeInstance != null)
            {
                eyeInstance.SetColor(EmissionColorId, settings.EyeGlint.Color * strength);
            }
        }

        private void PlayTellSound()
        {
            var sounds = settings.Sounds;
            var clip = soundClips[soundSchedule.PickIndex(soundClips.Length)];
            if (clip == null)
            {
                return;
            }

            tellPitch = random.Range(sounds.MinPitch, sounds.MaxPitch);
            OnEnemyTimeScaled(time.Scale);
            tellVoice.clip = clip;
            tellVoice.Play();
        }

        private void OnEnemyTimeScaled(float scale)
        {
            tellVoice.pitch = EnemyVoicePitch.For(tellPitch, scale);
        }

        private void Reset()
        {
            tellVoice = GetComponent<AudioSource>();
        }
    }
}
