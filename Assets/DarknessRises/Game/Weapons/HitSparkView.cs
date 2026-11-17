using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A pooled hit spark: throws a few glowing sparks off the hit surface and flashes a shadowless point light
    /// that fades out, then goes back to its pool once the sparks are gone. Only Lightning casts real-time shadows.
    /// </summary>
    public sealed class HitSparkView : MonoBehaviour
    {
        [SerializeField] private Light sparkLight;
        [SerializeField] private ParticleSystem sparks;

        private IObjectPool<HitSparkView> pool;
        private HitSparkSettings settings;
        private float secondsLeft;
        private float lifeSecondsLeft;

        /// <summary>Seconds the longest-lived spark lasts.</summary>
        public float LifeSeconds => sparks.main.startLifetime.constantMax;

        /// <summary>Called once, when the pool creates this hit spark.</summary>
        public void Prepare(IObjectPool<HitSparkView> owner, HitSparkSettings sparkSettings)
        {
            pool = owner;
            settings = sparkSettings;
            sparkLight.type = LightType.Point;
            sparkLight.shadows = LightShadows.None;
            sparkLight.color = settings.Color;
            sparkLight.range = settings.Range;

            var main = sparks.main;
            main.startColor = settings.Color;
            main.playOnAwake = false;
            main.loop = false;
        }

        /// <summary>Shows a hit at <paramref name="point"/> on a surface facing <paramref name="normal"/>.</summary>
        public void Spark(Vector3 point, Vector3 normal)
        {
            transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal, Vector3.up));
            sparkLight.transform.position = point + normal * settings.SurfaceOffset;
            sparkLight.intensity = settings.Intensity;
            sparkLight.enabled = true;
            secondsLeft = settings.Seconds;

            sparks.Clear();
            sparks.Emit(settings.SparkCount);
            lifeSecondsLeft = Mathf.Max(settings.Seconds, LifeSeconds);
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            secondsLeft -= deltaSeconds;
            lifeSecondsLeft -= deltaSeconds;

            if (secondsLeft > 0f)
            {
                sparkLight.intensity = settings.Intensity * (secondsLeft / settings.Seconds);
            }
            else if (sparkLight.enabled)
            {
                sparkLight.enabled = false;
            }

            if (lifeSecondsLeft <= 0f)
            {
                pool.Release(this);
            }
        }

        private void Reset()
        {
            sparkLight = GetComponentInChildren<Light>();
            sparks = GetComponentInChildren<ParticleSystem>();
        }
    }
}
