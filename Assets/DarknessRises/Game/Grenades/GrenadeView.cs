using System;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// A pooled Grenade in flight: a small glowing, tumbling body with a short trail that follows its arc
    /// until it lands or hits something, then reports where it went off and goes back to its pool.
    /// </summary>
    public sealed class GrenadeView : MonoBehaviour
    {
        // Metres the Grenade is kept off a surface it hits, so the explosion's light isn't buried in it.
        private const float SurfaceOffset = 0.05f;

        // Colliders one flight step can cross. A step is a few centimetres; this is plenty.
        private const int MaxStepHits = 8;

        private readonly RaycastHit[] hits = new RaycastHit[MaxStepHits];

        [SerializeField] private MeshRenderer body;
        [SerializeField] private TrailRenderer trail;

        private IObjectPool<GrenadeView> pool;
        private LayerMask hitLayers;
        private Action<Vector3> onLanded;
        private float tumbleSpeed;
        private GrenadeFlight flight;

        public Material BodyMaterial => body.sharedMaterial;

        /// <summary>Called once, when the pool creates this Grenade.</summary>
        public void Prepare(
            IObjectPool<GrenadeView> owner,
            GrenadeBodySettings settings,
            Material glowMaterial,
            LayerMask layers,
            Action<Vector3> landed)
        {
            pool = owner;
            hitLayers = layers;
            onLanded = landed;
            tumbleSpeed = settings.TumbleSpeed;
            body.sharedMaterial = glowMaterial;
            transform.localScale = Vector3.one * settings.Size;

            trail.enabled = settings.TrailSeconds > 0f;
            trail.time = settings.TrailSeconds;
            trail.widthMultiplier = settings.Size;
            trail.startColor = settings.GlowColor;
            var end = settings.GlowColor;
            end.a = 0f;
            trail.endColor = end;
        }

        public void Throw(GrenadeFlight grenadeFlight)
        {
            flight = grenadeFlight;
            transform.position = flight.Position;
            trail.Clear();
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            var from = flight.Position;
            flight.Advance(deltaSeconds);
            var step = flight.Position - from;
            var stepLength = step.magnitude;

            if (stepLength > 0f && TryHitSolid(from, step / stepLength, stepLength, out var hit))
            {
                flight.Stop(hit.point + hit.normal * SurfaceOffset);
            }

            transform.position = flight.Position;
            transform.Rotate(Vector3.right, tumbleSpeed * deltaSeconds, Space.Self);

            if (flight.HasLanded)
            {
                onLanded(flight.Position);
                pool.Release(this);
            }
        }

        // Walls and Obstacles stop the Grenade where it hits them. It flies over and past Enemies (anything a Shot
        // can damage), so it can be lobbed into the middle of a group instead of going off on the first one in the way.
        private bool TryHitSolid(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
        {
            var count = Physics.RaycastNonAlloc(origin, direction, hits, distance, hitLayers, QueryTriggerInteraction.Ignore);
            nearest = default;
            var found = false;
            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if ((!found || hit.distance < nearest.distance) && !hit.collider.TryGetComponent<IShotTarget>(out _))
                {
                    nearest = hit;
                    found = true;
                }
            }

            return found;
        }

        private void Reset()
        {
            body = GetComponentInChildren<MeshRenderer>();
            trail = GetComponentInChildren<TrailRenderer>();
        }
    }
}
