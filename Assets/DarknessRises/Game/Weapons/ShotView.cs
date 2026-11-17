using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A pooled Shot: a glowing, unlit body with a short trail that flies straight until it hits something
    /// or uses up its range, then goes back to its pool.
    /// </summary>
    public sealed class ShotView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer body;
        [SerializeField] private TrailRenderer trail;

        private IObjectPool<ShotView> pool;
        private ShotFlight flight;
        private LayerMask hitLayers;

        public Material BodyMaterial => body.sharedMaterial;

        /// <summary>Called once, when the pool creates this Shot.</summary>
        public void Prepare(IObjectPool<ShotView> owner, ShotSettings settings, Material glowMaterial, LayerMask layers)
        {
            pool = owner;
            hitLayers = layers;
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

        public void Launch(Vector3 origin, Vector3 direction, float speed, float range)
        {
            flight = new ShotFlight(origin, direction, speed, range);
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(flight.Direction, Vector3.up));
            trail.Clear();
        }

        private void Update()
        {
            var step = flight.StepLength(Time.deltaTime);
            if (Physics.Raycast(flight.Position, flight.Direction, out var hit, step, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // Enemies and damage arrive with their own ticket; for now a Shot stops at whatever it hits.
                flight.Stop(hit.point);
            }
            else
            {
                flight.Advance(step);
            }

            transform.position = flight.Position;

            if (flight.IsSpent)
            {
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
