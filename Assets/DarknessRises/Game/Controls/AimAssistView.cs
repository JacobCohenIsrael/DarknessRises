using System;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Visibility;
using UnityEngine;

namespace DarknessRises.Game.Controls
{
    /// <summary>
    /// Finds the living Enemies near the aim point and the lights shining now, and lets <see cref="AimAssist"/> bend the
    /// aim toward the nearest visible one, with touch's or the mouse's settings. Only Enemies count: Critters and Phantom
    /// eyes have no <see cref="EnemyView"/>. Reuses its buffers, so assisting every frame allocates nothing.
    /// </summary>
    public sealed class AimAssistView : MonoBehaviour
    {
        private const int MostCandidates = 16;
        private const int MostLights = 64;

        // Metres above and below the aim plane an Enemy's hitbox is looked for, so any Enemy height is found.
        private const float ReachHeight = 10f;

        [SerializeField, Tooltip("Layers Enemy hitboxes are on.")]
        private LayerMask enemyLayers = Physics.DefaultRaycastLayers;

        private readonly Collider[] overlaps = new Collider[MostCandidates];
        private readonly AimCandidate[] candidates = new AimCandidate[MostCandidates];
        private readonly LitSpot[] spots = new LitSpot[MostLights];

        private AimAssist touchAssist;
        private AimAssist mouseAssist;
        private SceneLights lights;

        /// <summary>
        /// Starts assisting with <paramref name="touch"/> on touch and <paramref name="mouse"/> with the mouse, as strong
        /// as they are in the Level at <paramref name="levelIndex"/>. Enemies count as visible while
        /// <paramref name="sceneLights"/> light them.
        /// </summary>
        public void Use(AimAssistSettings touch, AimAssistSettings mouse, int levelIndex, SceneLights sceneLights)
        {
            touchAssist = touch != null ? new AimAssist(touch, levelIndex) : null;
            mouseAssist = mouse != null ? new AimAssist(mouse, levelIndex) : null;
            lights = sceneLights;
        }

        /// <summary>
        /// <paramref name="aimPoint"/> bent toward the nearest visible Enemy close to it, seen from <paramref name="from"/>,
        /// with touch's settings when <paramref name="onTouch"/> and the mouse's otherwise. Unchanged when there's none.
        /// </summary>
        public Vector3 Assist(Vector3 from, Vector3 aimPoint, bool onTouch)
        {
            var assist = onTouch ? touchAssist : mouseAssist;
            if (assist == null || assist.Strength <= 0f)
            {
                return aimPoint;
            }

            var found = Physics.OverlapCapsuleNonAlloc(
                aimPoint + Vector3.down * ReachHeight,
                aimPoint + Vector3.up * ReachHeight,
                assist.Radius,
                overlaps,
                enemyLayers,
                QueryTriggerInteraction.Collide);
            var count = 0;
            for (var i = 0; i < found; i++)
            {
                var hit = overlaps[i];
                overlaps[i] = null;
                if (hit.TryGetComponent<EnemyView>(out var enemy) && enemy.Enemy.IsAlive)
                {
                    candidates[count++] = new AimCandidate(enemy.transform.position, hit.bounds.center);
                }
            }

            if (count == 0)
            {
                return aimPoint;
            }

            var litCount = lights.Gather(spots);
            var target = assist.PickTarget(aimPoint, candidates.AsSpan(0, count), spots.AsSpan(0, litCount));
            return target < 0 ? aimPoint : assist.Bend(from, aimPoint, candidates[target].Position);
        }
    }
}
