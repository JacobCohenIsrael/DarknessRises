using DarknessRises.Game.Content;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR || DEBUG
using DarknessRises.Game.Debugging;
#endif

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// The character's health in the scene. Enemies' landed attacks reach its <see cref="CharacterHealth"/> rule
    /// through here; a landed hit shakes the camera with a Cinemachine impulse and plays a hurt sound, and the character
    /// blinks while it's invulnerable. Sits on the character's root, the object Enemies hunt.
    /// </summary>
    public sealed class CharacterHealthView : MonoBehaviour, IEnemyAttackTarget
    {
        [SerializeField, Tooltip("Sends the hit's camera shake. The camera's Cinemachine Impulse Listener picks it up.")]
        private CinemachineImpulseSource impulseSource;

        [SerializeField, Tooltip("Plays the character's hurt and death sounds.")]
        private AudioSource voice;

        [SerializeField, Tooltip("Renderers hidden and shown in turn while the character is invulnerable.")]
        private Renderer[] blinkRenderers = new Renderer[0];

        [SerializeField, Tooltip("Debug/GodMode. Only read in the Editor and debug builds.")]
        private InputActionReference godMode;

        private HitReactionSettings settings;
        private AudioClip hurtClip;
        private AudioClip deathClip;
        private bool isShown = true;
#if UNITY_EDITOR || DEBUG
        private GodModeToggle godModeToggle;
#endif

        public CharacterHealth Health { get; private set; }

#if UNITY_EDITOR || DEBUG
        /// <summary>Debug only: the god mode switch of the current <see cref="Health"/>, if shown.</summary>
        public GodModeToggle GodMode => godModeToggle;
#endif

        public bool CanBeAttacked => Health != null && Health.IsAlive;

        /// <summary>
        /// Gives the character full, fresh hit points from <paramref name="player"/>. Its hurt and death sounds come from
        /// <paramref name="content"/>, already loaded.
        /// </summary>
        public CharacterHealth Show(PlayerDefinition player, ILoadedContent content)
        {
            Unbind();

            settings = player.HitReaction;
            hurtClip = content.Clip(settings.HurtClip);
            deathClip = content.Clip(settings.DeathClip);
            Health = new CharacterHealth(player.HitPoints, player.InvulnerabilitySeconds);
            Health.Hurt += OnHurt;
            Health.Died += OnDied;

            var impulse = impulseSource.ImpulseDefinition;
            impulse.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDuration = settings.ShakeSeconds;

            voice.playOnAwake = false;
            voice.volume = settings.Volume;
            SetShown(true);

#if UNITY_EDITOR || DEBUG
            godModeToggle = new GodModeToggle(godMode, Health);
#endif
            return Health;
        }

        public void TakeAttack(int damage)
        {
            Health?.TakeHit(damage);
        }

        private void Update()
        {
            if (Health == null)
            {
                return;
            }

            Health.Tick(Time.deltaTime);

            // Shown, hidden, shown... counting down to the end of the invulnerability, so it always ends shown.
            var shown = !Health.IsInvulnerable || ((int)(Health.InvulnerableSecondsLeft / settings.BlinkSeconds) & 1) == 0;
            SetShown(shown);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
#if UNITY_EDITOR || DEBUG
            godModeToggle?.Dispose();
            godModeToggle = null;
#endif
            if (Health == null)
            {
                return;
            }

            Health.Hurt -= OnHurt;
            Health.Died -= OnDied;
            Health = null;
        }

        private void OnHurt()
        {
            impulseSource.GenerateImpulseWithForce(settings.ShakeForce);
            Play(hurtClip);
        }

        private void OnDied()
        {
            impulseSource.GenerateImpulseWithForce(settings.ShakeForce);
            Play(deathClip);
            SetShown(true);
        }

        private void SetShown(bool shown)
        {
            if (shown == isShown)
            {
                return;
            }

            isShown = shown;
            for (var i = 0; i < blinkRenderers.Length; i++)
            {
                blinkRenderers[i].enabled = shown;
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                voice.PlayOneShot(clip);
            }
        }
    }
}
