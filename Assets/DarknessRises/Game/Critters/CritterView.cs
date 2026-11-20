using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Enemies.Tells;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Critters
{
    /// <summary>
    /// A pooled Critter in the scene: a small dark animal that scurries along one <see cref="CritterDash"/>, its eyes
    /// glinting near the character (emissive, like an Enemy's, by the same distance rule), then goes back to its pool.
    /// Lights show its body like anything else. A Shot kills it with a squeak, for fun: it counts as neither a hit nor a
    /// miss (<see cref="IScoreNeutralTarget"/>), and it never hurts the character. It is not an Enemy, so Aim assist,
    /// the Score, Credits and kill counts never see it.
    /// </summary>
    public sealed class CritterView : MonoBehaviour, IShotTarget, IScoreNeutralTarget
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Glint changes smaller than this aren't worth a material update.
        private const float GlintStep = 1f / 64f;

        [SerializeField, Tooltip("What Shots hit. Turned off once the Critter is killed.")]
        private Collider body;

        [SerializeField, Tooltip("Positional source for the scurry and the squeak.")]
        private AudioSource voice;

        [SerializeField, Tooltip("Every renderer of the body, hidden when the Critter is killed.")]
        private Renderer[] renderers = new Renderer[0];

        [SerializeField, Tooltip("The eye renderers. They share one emissive material instance per Critter.")]
        private Renderer[] eyes = new Renderer[0];

        private readonly SoundFade scurry = new();
        private readonly SoundTail squeakTail = new();
        private IObjectPool<CritterView> pool;
        private CritterDefinition definition;
        private AudioClip[] scurryClips;
        private AudioClip[] squeakClips;
        private IRandom random;
        private Material eyeInstance;
        private Transform watcher;
        private CritterDash dash;
        private float secondsIn;
        private float glint = -1f;

        /// <summary>Whether it is dashing, not yet killed.</summary>
        public bool IsAlive { get; private set; }

        /// <summary>Whether it's out of its pool: dashing, or squeaking its last.</summary>
        public bool IsOut { get; private set; }

        /// <summary>How strongly its eyes glint now, from 0 to 1.</summary>
        public float Glint => Mathf.Max(0f, glint);

        /// <summary>Called once, when the pool creates the Critter. Its sounds come from <paramref name="content"/>, already loaded.</summary>
        public void Prepare(IObjectPool<CritterView> owner, CritterDefinition critterDefinition, ILoadedContent content, IRandom randomness)
        {
            pool = owner;
            definition = critterDefinition;
            random = randomness;
            var sounds = definition.Sounds;
            scurryClips = content.Clips(sounds.ScurryClips);
            squeakClips = content.Clips(sounds.SqueakClips);
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.maxDistance = sounds.HearingRange;
            voice.dopplerLevel = 0f;

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

        /// <summary>Sends the Critter along <paramref name="path"/>, its eyes glinting by their distance from <paramref name="character"/>.</summary>
        public void Dash(CritterDash path, Transform character)
        {
            dash = path;
            watcher = character;
            secondsIn = 0f;
            IsAlive = true;
            IsOut = true;
            var heading = dash.To - dash.From;
            heading.y = 0f;
            transform.SetPositionAndRotation(dash.From, heading.sqrMagnitude > 0f ? Quaternion.LookRotation(heading) : Quaternion.identity);
            SetBodyShown(true);
            gameObject.SetActive(true);
            glint = -1f;
            UpdateGlint();
            squeakTail.Clear();

            var sounds = definition.Sounds;
            var clip = Pick(scurryClips);
            if (clip != null)
            {
                voice.clip = clip;
                voice.pitch = random.Range(sounds.ScurryMinPitch, sounds.ScurryMaxPitch);
                voice.time = sounds.ScurryStartSeconds < clip.length ? sounds.ScurryStartSeconds : 0f;
                scurry.Start(sounds.ScurryVolume);
                voice.volume = scurry.Volume;
                voice.Play();
            }
        }

        public bool TakeShot(in ShotHit hit)
        {
            if (!IsAlive)
            {
                return false;
            }

            IsAlive = false;
            SetBodyShown(false);
            SetGlint(0f);
            scurry.Stop();
            voice.Stop();

            var sounds = definition.Sounds;
            var clip = Pick(squeakClips);
            if (clip != null)
            {
                var pitch = random.Range(sounds.SqueakMinPitch, sounds.SqueakMaxPitch);
                voice.pitch = pitch;
                voice.volume = 1f;
                voice.PlayOneShot(clip, sounds.SqueakVolume);

                // A clip's length is only known once its audio data has loaded (WebGL decodes audio late).
                if (clip.loadState == AudioDataLoadState.Loaded)
                {
                    squeakTail.Play(clip.length);
                }
            }

            return true;
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            squeakTail.Tick(deltaSeconds, voice.pitch);
            if (!IsAlive)
            {
                if (!squeakTail.IsPlaying)
                {
                    GoHome();
                }

                return;
            }

            secondsIn += deltaSeconds;
            transform.position = dash.At(secondsIn);
            UpdateGlint();
            if (dash.IsOver(secondsIn) && !scurry.IsFading)
            {
                // Into the dark gap: out of sight at once, the scurry trailing off behind it.
                SetBodyShown(false);
                SetGlint(0f);
                scurry.FadeOut(definition.Sounds.ScurryFadeSeconds);
            }

            scurry.Tick(deltaSeconds);
            voice.volume = scurry.Volume;
            if (dash.IsOver(secondsIn) && !scurry.IsAudible)
            {
                voice.Stop();
                IsAlive = false;
                GoHome();
            }
        }

        private void OnDestroy()
        {
            Destroy(eyeInstance);
        }

        private void GoHome()
        {
            IsOut = false;
            pool.Release(this);
        }

        private void UpdateGlint()
        {
            var offset = transform.position - watcher.position;
            offset.y = 0f;
            var eyeSettings = definition.Eyes;
            var strength = EyeGlint.Strength(offset.magnitude, eyeSettings.Range, eyeSettings.FadeDistance);
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
                eyeInstance.SetColor(EmissionColorId, definition.Eyes.Color * strength);
            }
        }

        private void SetBodyShown(bool shown)
        {
            body.enabled = shown;
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = shown;
            }
        }

        private AudioClip Pick(AudioClip[] clips)
        {
            return clips.Length == 0 ? null : clips[Mathf.Min((int)random.Range(0f, clips.Length), clips.Length - 1)];
        }
    }
}
