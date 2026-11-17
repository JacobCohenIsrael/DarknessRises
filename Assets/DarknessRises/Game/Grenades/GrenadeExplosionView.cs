using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// A pooled Grenade explosion: throws out a burst of fire and sparks, flashes a shadowless point light that
    /// lights the area up and fades out, plays the bang and shakes the camera through a Cinemachine impulse,
    /// then goes back to its pool once the burst is gone. Only Lightning casts real-time shadows.
    /// </summary>
    public sealed class GrenadeExplosionView : MonoBehaviour
    {
        [SerializeField] private Light flashLight;
        [SerializeField] private ParticleSystem fireballs;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private AudioSource bang;
        [SerializeField] private CinemachineImpulseSource impulse;

        private IObjectPool<GrenadeExplosionView> pool;
        private GrenadeExplosionSettings settings;
        private AudioClip bangClip;
        private float lightSecondsLeft;
        private float lifeSecondsLeft;

        /// <summary>Seconds until the longest-lived part of the burst is gone.</summary>
        public float LifeSeconds => Mathf.Max(
            settings != null ? settings.LightSeconds : 0f,
            fireballs.main.startLifetime.constantMax,
            sparks.main.startLifetime.constantMax);

        /// <summary>Called once, when the pool creates this explosion. <paramref name="bangSound"/> is its bang, already loaded.</summary>
        public void Prepare(IObjectPool<GrenadeExplosionView> owner, GrenadeExplosionSettings explosionSettings, AudioClip bangSound)
        {
            pool = owner;
            settings = explosionSettings;
            bangClip = bangSound;

            flashLight.type = LightType.Point;
            flashLight.shadows = LightShadows.None;
            flashLight.color = settings.LightColor;
            flashLight.range = settings.LightRange;
            flashLight.transform.localPosition = Vector3.up * settings.LightHeight;
            flashLight.enabled = false;

            Quiet(fireballs);
            Quiet(sparks);

            bang.playOnAwake = false;
            bang.spatialBlend = 1f;
            bang.rolloffMode = AudioRolloffMode.Linear;
            bang.maxDistance = settings.HearingRange;
            bang.volume = settings.Volume;

            var shake = impulse.ImpulseDefinition;
            shake.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
            shake.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            shake.ImpulseDuration = settings.ShakeSeconds;
        }

        /// <summary>Sets the explosion off at <paramref name="position"/>.</summary>
        public void Explode(Vector3 position)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);

            flashLight.intensity = settings.LightIntensity;
            flashLight.enabled = true;
            lightSecondsLeft = settings.LightSeconds;

            fireballs.Clear();
            fireballs.Emit(settings.FireballCount);
            sparks.Clear();
            sparks.Emit(settings.SparkCount);
            lifeSecondsLeft = LifeSeconds;

            if (bangClip != null)
            {
                bang.PlayOneShot(bangClip);
            }

            if (settings.ShakeForce > 0f)
            {
                impulse.GenerateImpulseAtPositionWithVelocity(position, Vector3.down * settings.ShakeForce);
            }
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            lightSecondsLeft -= deltaSeconds;
            lifeSecondsLeft -= deltaSeconds;

            if (lightSecondsLeft > 0f)
            {
                // Eases out: a bright flash that falls off quickly, then a lingering glow.
                var left = lightSecondsLeft / settings.LightSeconds;
                flashLight.intensity = settings.LightIntensity * left * left;
            }
            else if (flashLight.enabled)
            {
                flashLight.enabled = false;
            }

            if (lifeSecondsLeft <= 0f)
            {
                pool.Release(this);
            }
        }

        private static void Quiet(ParticleSystem particles)
        {
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
        }
    }
}
