using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Enemies;
using UnityEngine;

namespace DarknessRises.Game.Continues
{
    /// <summary>
    /// Continue's burst of light around the character: a bright, shadowless point light that flares and fades out, and
    /// a shove that pushes every living Enemy nearby back out to the push-back radius (<see cref="ContinuePushBack"/>).
    /// Only Lightning casts real-time shadows.
    /// </summary>
    public sealed class ContinueBurstView : MonoBehaviour
    {
        // Colliders one burst can find. Far more than fit around the character.
        private const int MaxColliders = 64;

        [SerializeField] private Light burstLight;

        [SerializeField, Tooltip("Where Enemies' hitboxes are.")]
        private LayerMask enemyLayers = Physics.DefaultRaycastLayers;

        private readonly Collider[] colliders = new Collider[MaxColliders];
        private ContinueDefinition definition;
        private ContinuePushBack pushBack;

        /// <summary>Gets the burst ready from <paramref name="continueDefinition"/>. It stays dark until it goes off.</summary>
        public void Prepare(ContinueDefinition continueDefinition)
        {
            definition = continueDefinition;
            pushBack = new ContinuePushBack(definition.PushBackRadius, definition.PushBackForce);

            burstLight.type = LightType.Point;
            burstLight.shadows = LightShadows.None;
            burstLight.color = definition.LightColor;
            burstLight.range = definition.LightRange;
            burstLight.transform.localPosition = Vector3.up * definition.LightHeight;
            burstLight.enabled = false;
        }

        /// <summary>
        /// Sets the burst off at the character's feet, <paramref name="centre"/>: pushes the Enemies around it back at
        /// once, then fades the light out. An Enemy right on top of the character goes along <paramref name="facing"/>.
        /// </summary>
        public async UniTask BurstAsync(Vector3 centre, Vector3 facing, CancellationToken cancellationToken)
        {
            PushEnemiesBack(centre, facing);

            burstLight.intensity = definition.LightIntensity;
            burstLight.enabled = true;
            try
            {
                var left = definition.LightSeconds;
                while (left > 0f)
                {
                    // Eases out: a bright flash that falls off quickly, then a lingering glow.
                    var share = left / definition.LightSeconds;
                    burstLight.intensity = definition.LightIntensity * share * share;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                    left -= Time.deltaTime;
                }
            }
            finally
            {
                if (burstLight != null)
                {
                    burstLight.enabled = false;
                }
            }
        }

        private void PushEnemiesBack(Vector3 centre, Vector3 facing)
        {
            var found = Physics.OverlapSphereNonAlloc(
                centre, definition.PushBackRadius, colliders, enemyLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < found; i++)
            {
                if (colliders[i].TryGetComponent<EnemyView>(out var enemy) && enemy.Enemy.IsAlive)
                {
                    var push = pushBack.For(centre, enemy.transform.position, facing);
                    if (push.Moves)
                    {
                        enemy.PushBack(push.Velocity, push.Seconds);
                    }
                }

                colliders[i] = null;
            }
        }
    }
}
