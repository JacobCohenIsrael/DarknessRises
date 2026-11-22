using System;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Health;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The red at the screen's edges when the character is hurt, over the whole screen and under the rest of the HUD:
    /// a flash on every landed hit, and a slow pulse with a heartbeat sound on the last few hit points
    /// (<see cref="HurtFeedback"/>). The heartbeat plays in the Sound category and stops at once on death, a Continue
    /// or <see cref="Stop"/>. Runs on scaled time, so the pause freezes it (and <c>AudioListener.pause</c> its sound).
    /// </summary>
    public sealed class HurtVignetteView : MonoBehaviour
    {
        [SerializeField, Tooltip("Stretched over the whole screen: clear in the middle, coloured at the edges. Never a raycast target.")]
        private Image edge;

        [SerializeField, Tooltip("A 2D source for the heartbeat, routed to the Sound category.")]
        private AudioSource heartbeat;

        private HurtFeedbackSettings settings;
        private HurtFeedback feedback;
        private CharacterHealth health;
        private AudioClip heartbeatClip;
        private int lastHitPoints;
        private float shownStrength = -1f;
        private Action onHurt;
        private Action onDied;
        private Action onRestored;
        private Action<bool> onBeat;

        /// <summary>The rule behind the edge, for checks.</summary>
        public HurtFeedback Feedback => feedback;

        /// <summary>The heartbeat's source, for checks: read its state, never listen.</summary>
        public AudioSource Heartbeat => heartbeat;

        /// <summary>Shows how hurt <paramref name="characterHealth"/> is from now on, with <paramref name="clip"/> (already loaded) for the heartbeat.</summary>
        public void Show(CharacterHealth characterHealth, HurtFeedbackSettings hurtFeedback, AudioClip clip)
        {
            Unbind();

            settings = hurtFeedback ?? throw new ArgumentNullException(nameof(hurtFeedback));
            heartbeatClip = clip;
            onHurt ??= OnHealthChanged;
            onDied ??= OnHealthChanged;
            onRestored ??= OnRestored;
            onBeat ??= OnBeat;

            feedback = new HurtFeedback(settings.ToTiming());
            feedback.Beat += onBeat;
            health = characterHealth;
            health.Hurt += onHurt;
            health.Died += onDied;
            health.Restored += onRestored;
            lastHitPoints = health.HitPoints;
            feedback.SetHitPoints(health.HitPoints, health.MaxHitPoints);

            heartbeat.playOnAwake = false;
            heartbeat.loop = false;
            heartbeat.pitch = settings.HeartbeatPitch;
            heartbeat.SetVolume(settings.HeartbeatVolume);
            heartbeat.Stop();
            shownStrength = -1f;
            Apply();
        }

        /// <summary>Clears the edge and silences the heartbeat at once: the Level is over.</summary>
        public void Stop()
        {
            feedback?.Stop();
            heartbeat.Stop();
            Apply();
        }

        private void Update()
        {
            if (feedback == null)
            {
                return;
            }

            feedback.Tick(Time.deltaTime);
            Apply();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (feedback != null)
            {
                feedback.Beat -= onBeat;
            }

            if (health == null)
            {
                return;
            }

            health.Hurt -= onHurt;
            health.Died -= onDied;
            health.Restored -= onRestored;
            health = null;
        }

        // A hit landed, survived or not: the flash is as strong as the hit points it took.
        private void OnHealthChanged()
        {
            feedback.Hit(lastHitPoints - health.HitPoints);
            UpdateLow();
        }

        // A Continue: back to full, so the heartbeat stops and nothing is left of the last flash.
        private void OnRestored()
        {
            feedback.Stop();
            UpdateLow();
        }

        private void UpdateLow()
        {
            lastHitPoints = health.HitPoints;
            feedback.SetHitPoints(health.HitPoints, health.MaxHitPoints);
            if (!feedback.IsLow)
            {
                heartbeat.Stop();
            }

            Apply();
        }

        private void OnBeat(bool isSoft)
        {
            if (heartbeatClip != null)
            {
                heartbeat.PlayOneShot(heartbeatClip, isSoft ? settings.DubStrength : 1f);
            }
        }

        private void Apply()
        {
            var strength = feedback?.Strength ?? 0f;
            if (Mathf.Approximately(strength, shownStrength))
            {
                return;
            }

            shownStrength = strength;
            var visible = strength > 0f;
            if (edge.enabled != visible)
            {
                edge.enabled = visible;
            }

            var tint = settings != null ? settings.EdgeColor : edge.color;
            tint.a *= strength;
            edge.color = tint;
        }
    }
}
