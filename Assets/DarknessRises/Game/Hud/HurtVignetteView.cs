using System;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Health;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// The red at the screen's edges when the character is hurt, over the whole screen and under the rest of the HUD:
    /// a flash on every landed hit, and on the last few hit points a looping heartbeat, slow and then fast, with the
    /// edge pulsing in time with it (<see cref="HurtFeedback"/>). The loops play in the Sound category and stop at once
    /// on death, a Continue or <see cref="Stop"/>. Runs on scaled time, so the pause freezes the edge, and
    /// <c>AudioListener.pause</c> the loops, which the pulse reads its beat from.
    /// </summary>
    public sealed class HurtVignetteView : MonoBehaviour
    {
        [SerializeField, Tooltip("Stretched over the whole screen: clear in the middle, coloured at the edges. Never a raycast target.")]
        private Image edge;

        [SerializeField, Tooltip("A looping 2D source for the slow heartbeat, routed to the Sound category.")]
        private AudioSource slowHeartbeat;

        [SerializeField, Tooltip("A looping 2D source for the fast heartbeat, routed to the Sound category.")]
        private AudioSource fastHeartbeat;

        private HurtFeedbackSettings settings;
        private HurtFeedback feedback;
        private CharacterHealth health;
        private int lastHitPoints;
        private float shownStrength = -1f;
        private float shownSlowWeight = -1f;
        private float shownFastWeight = -1f;
        private Action onHurt;
        private Action onDied;
        private Action onRestored;
        private HeartbeatBeats slowBeats;
        private HeartbeatBeats fastBeats;

        /// <summary>
        /// Raised on each strong beat of the heartbeat heard (the slow loop or the fast one, whichever the hit points
        /// call for), read from the loop's own playback so whatever follows it stays on the beat.
        /// </summary>
        public event Action<HeartbeatTier> Beat;

        /// <summary>The rule behind the edge, for checks.</summary>
        public HurtFeedback Feedback => feedback;

        /// <summary>The slow loop's source, for checks: read its state, never listen.</summary>
        public AudioSource SlowHeartbeat => slowHeartbeat;

        /// <summary>The fast loop's source, for checks: read its state, never listen.</summary>
        public AudioSource FastHeartbeat => fastHeartbeat;

        /// <summary>
        /// Shows how hurt <paramref name="characterHealth"/> is from now on, with the slow and fast heartbeat loops
        /// (already loaded) played as <paramref name="hurtFeedback"/> says.
        /// </summary>
        public void Show(CharacterHealth characterHealth, HurtFeedbackSettings hurtFeedback, AudioClip slowClip, AudioClip fastClip)
        {
            Unbind();

            settings = hurtFeedback ?? throw new ArgumentNullException(nameof(hurtFeedback));
            onHurt ??= OnHealthChanged;
            onDied ??= OnHealthChanged;
            onRestored ??= OnRestored;

            feedback = new HurtFeedback(settings.ToTiming());
            slowBeats = new HeartbeatBeats(settings.Slow.ToRhythm());
            fastBeats = new HeartbeatBeats(settings.Fast.ToRhythm());
            health = characterHealth;
            health.Hurt += onHurt;
            health.Died += onDied;
            health.Restored += onRestored;
            lastHitPoints = health.HitPoints;

            Prepare(slowHeartbeat, slowClip);
            Prepare(fastHeartbeat, fastClip);
            shownStrength = shownSlowWeight = shownFastWeight = -1f;
            UpdateLow();
        }

        /// <summary>Clears the edge and silences the heartbeat at once: the Level is over.</summary>
        public void Stop()
        {
            feedback?.Stop();
            Silence();
            Apply();
        }

        private void Update()
        {
            if (feedback == null)
            {
                return;
            }

            var deltaSeconds = Time.deltaTime;
            feedback.Tick(deltaSeconds);
            Apply();
            FindBeat(deltaSeconds);
        }

        private void FindBeat(float deltaSeconds)
        {
            if (!feedback.IsLow)
            {
                return;
            }

            var slowBeat = slowHeartbeat.isPlaying && slowBeats.Advance(slowHeartbeat.time, deltaSeconds);
            var fastBeat = fastHeartbeat.isPlaying && fastBeats.Advance(fastHeartbeat.time, deltaSeconds);
            if (feedback.Tier == HeartbeatTier.Slow ? slowBeat : fastBeat)
            {
                Beat?.Invoke(feedback.Tier);
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (health == null)
            {
                return;
            }

            health.Hurt -= onHurt;
            health.Died -= onDied;
            health.Restored -= onRestored;
            health = null;
        }

        private static void Prepare(AudioSource source, AudioClip clip)
        {
            source.Stop();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.SetVolume(0f);
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
            var wasLow = feedback.IsLow;
            lastHitPoints = health.HitPoints;
            feedback.SetHitPoints(health.HitPoints, health.MaxHitPoints);
            if (!feedback.IsLow)
            {
                Silence();
            }
            else if (!wasLow)
            {
                // Both loops start together from their first beat, so the crossfade between them stays in step.
                slowBeats.Reset();
                fastBeats.Reset();
                Play(slowHeartbeat);
                Play(fastHeartbeat);
            }

            Apply();
        }

        private static void Play(AudioSource source)
        {
            if (source.clip == null)
            {
                return;
            }

            source.time = 0f;
            source.Play();
        }

        private void Silence()
        {
            slowBeats?.Reset();
            fastBeats?.Reset();
            slowHeartbeat.Stop();
            fastHeartbeat.Stop();
        }

        private void Apply()
        {
            if (feedback == null)
            {
                return;
            }

            var slowWeight = feedback.SlowWeight;
            if (!Mathf.Approximately(slowWeight, shownSlowWeight))
            {
                shownSlowWeight = slowWeight;
                slowHeartbeat.SetVolume(slowWeight * settings.Slow.Volume);
            }

            var fastWeight = feedback.FastWeight;
            if (!Mathf.Approximately(fastWeight, shownFastWeight))
            {
                shownFastWeight = fastWeight;
                fastHeartbeat.SetVolume(fastWeight * settings.Fast.Volume);
            }

            // The pulse follows the loops' own playback, so it stays on their beat through pauses and hitches.
            var strength = feedback.Strength(slowHeartbeat.time, fastHeartbeat.time);
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

            var tint = settings.EdgeColor;
            tint.a *= strength;
            edge.color = tint;
        }
    }
}
