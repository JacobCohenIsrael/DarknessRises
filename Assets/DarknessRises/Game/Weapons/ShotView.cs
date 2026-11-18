using System;
using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A pooled Shot: a glowing, unlit body with a short trail that flies straight until it hits something
    /// or uses up its range, then goes back to its pool. What a hit does is up to whoever fired it, told where it hit
    /// and which way the Shot was flying.
    /// </summary>
    /// <remarks>
    /// A Shot can also grow as it flies, carry a shadowless point light that dims toward the end of its range,
    /// and throw off particles (the Flamethrower's flames). The light and particles are optional parts of the prefab.
    /// </remarks>
    public sealed class ShotView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer body;
        [SerializeField] private TrailRenderer trail;

        [SerializeField, Tooltip("Optional. Lights the surroundings when the Weapon's Shots carry light.")]
        private Light shotLight;

        [SerializeField, Tooltip("Optional. Particles the Shot throws off as it flies, such as flames.")]
        private ParticleSystem particles;

        private IObjectPool<ShotView> pool;
        private ShotSettings settings;
        private ShotLightSettings lightSettings;
        private ShotFlight flight;
        private LayerMask hitLayers;
        private Action<RaycastHit, Vector3> onHit;
        private bool isLit;

        public Material BodyMaterial => body.sharedMaterial;

        /// <summary>Called once, when the pool creates this Shot.</summary>
        public void Prepare(
            IObjectPool<ShotView> owner,
            ShotSettings shotSettings,
            ShotLightSettings shotLightSettings,
            Material glowMaterial,
            LayerMask layers,
            Action<RaycastHit, Vector3> hit)
        {
            pool = owner;
            settings = shotSettings;
            lightSettings = shotLightSettings;
            hitLayers = layers;
            onHit = hit;
            body.sharedMaterial = glowMaterial;
            transform.localScale = Vector3.one * settings.Size;

            trail.enabled = settings.TrailSeconds > 0f;
            trail.time = settings.TrailSeconds;
            trail.widthMultiplier = settings.Size;
            trail.startColor = settings.GlowColor;
            var end = settings.GlowColor;
            end.a = 0f;
            trail.endColor = end;

            if (shotLight != null)
            {
                shotLight.type = LightType.Point;
                shotLight.shadows = LightShadows.None;
                shotLight.color = lightSettings.Color;
                shotLight.range = lightSettings.Range;
                shotLight.enabled = false;
            }
        }

        /// <param name="lit">Whether this Shot carries a light. Ignored when the prefab has no light.</param>
        public void Launch(Vector3 origin, Vector3 direction, float speed, float range, bool lit)
        {
            flight = new ShotFlight(origin, direction, speed, range);
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(flight.Direction, Vector3.up));
            transform.localScale = Vector3.one * settings.Size;
            trail.Clear();

            isLit = lit && shotLight != null;
            if (shotLight != null)
            {
                shotLight.enabled = isLit;
                shotLight.intensity = lightSettings.Intensity;
            }

            if (particles != null)
            {
                particles.Clear();
                particles.Play();
            }
        }

        private void Update()
        {
            var step = flight.StepLength(Time.deltaTime);
            if (Physics.Raycast(flight.Position, flight.Direction, out var hit, step, hitLayers, QueryTriggerInteraction.Ignore))
            {
                flight.Stop(hit.point);
                onHit(hit, flight.Direction);
            }
            else
            {
                flight.Advance(step);
            }

            var rangeUsed = flight.RangeUsed;
            transform.position = flight.Position;
            transform.localScale = Vector3.one * (settings.Size * Mathf.Lerp(1f, settings.SizeAtRange, rangeUsed));
            if (isLit)
            {
                shotLight.intensity = lightSettings.Intensity * (1f - rangeUsed);
            }

            if (flight.IsSpent)
            {
                pool.Release(this);
            }
        }

        private void Reset()
        {
            body = GetComponentInChildren<MeshRenderer>();
            trail = GetComponentInChildren<TrailRenderer>();
            shotLight = GetComponentInChildren<Light>();
            particles = GetComponentInChildren<ParticleSystem>();
        }
    }
}
