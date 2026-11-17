using System;
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

            // Walls and Obstacles stop the Grenade where it hits them; Enemies too.
            if (stepLength > 0f
                && Physics.Raycast(from, step / stepLength, out var hit, stepLength, hitLayers, QueryTriggerInteraction.Ignore))
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

        private void Reset()
        {
            body = GetComponentInChildren<MeshRenderer>();
            trail = GetComponentInChildren<TrailRenderer>();
        }
    }
}
