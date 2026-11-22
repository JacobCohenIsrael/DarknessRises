using DarknessRises.Game.Audio;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Grenades;
using UnityEngine;
using UnityEngine.AI;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Throws the character's Lure in the scene: on the player's Lure command it throws the noisemaker at the pointer (F)
    /// or the way a sideways Two-finger flick went, flies it along an arc, and once it lands loops its rattle there and
    /// draws every living Enemy that comes within its radius (see the <see cref="Lure"/> rule). Enemies that reach it
    /// strike it as they would the character, and the first landed strike breaks it.
    /// </summary>
    /// <remarks>
    /// It lights nothing: the body is an ordinary lit object, seen only in Lightning, Gunfire, the Aura or a Skill's light.
    /// One Lure exists, reused for every throw, so throwing allocates nothing. Bosses aren't living Enemies of the Waves,
    /// so they never see it.
    /// </remarks>
    public sealed class LureView : MonoBehaviour, IEnemyAttackTarget
    {
        [SerializeField] private PlayerControls controls;

        [SerializeField, Tooltip("Where the Lure is thrown from: the character's root, on the floor.")]
        private Transform thrower;

        [SerializeField, Tooltip("The noisemaker itself, shown from the throw until the Lure ends. What Enemies walk to.")]
        private Transform body;

        [SerializeField, Tooltip("The rattle: a looping 3D source on the body, in the Sound category.")]
        private AudioSource rattle;

        private LureDefinition definition;
        private Lure lure;
        private LivingEnemies enemies;
        private GrenadeFlight flight;

        /// <summary>The Lure rule this view throws, once <see cref="Use"/> has been called.</summary>
        public Lure Lure => lure;

        bool IEnemyAttackTarget.CanBeAttacked => lure != null && lure.IsOut;

        // An Enemy reached it and struck it.
        void IEnemyAttackTarget.TakeAttack(int damage)
        {
            lure?.Break();
        }

        /// <summary>
        /// Throws <paramref name="lureRule"/> as <paramref name="lureDefinition"/> authors it, drawing Enemies from
        /// <paramref name="livingEnemies"/>, with the rattle <paramref name="rattleClip"/> (already loaded).
        /// </summary>
        public void Use(LureDefinition lureDefinition, Lure lureRule, LivingEnemies livingEnemies, AudioClip rattleClip)
        {
            Unsubscribe();
            definition = lureDefinition;
            lure = lureRule;
            enemies = livingEnemies;

            rattle.clip = rattleClip;
            rattle.loop = true;
            rattle.playOnAwake = false;
            rattle.spatialBlend = 1f;
            rattle.rolloffMode = AudioRolloffMode.Linear;
            rattle.maxDistance = definition.RattleHearingRange;
            rattle.SetVolume(definition.RattleVolume);
            rattle.Stop();

            body.gameObject.SetActive(false);
            lure.Landed += OnLanded;
            lure.Ended += OnEnded;
        }

        private void Update()
        {
            if (lure == null)
            {
                return;
            }

            if (controls.WasLurePressedThisFrame && lure.Skill.IsReady)
            {
                Throw();
            }

            if (lure.IsFlying)
            {
                flight.Advance(Time.deltaTime);
                body.position = flight.Position;
            }
            else if (lure.IsOut)
            {
                DrawEnemies();
            }
        }

        private void Throw()
        {
            var from = thrower.position;
            if (!controls.TryGetLureAimPoint(from, definition.FlickThrowMetres, definition.MaxThrowMetres, out var aimPoint))
            {
                return;
            }

            // On the walkable floor, so the Enemies it draws can reach it.
            if (!NavMesh.SamplePosition(aimPoint, out var onFloor, definition.FloorSnapMetres, NavMesh.AllAreas))
            {
                return;
            }

            var point = onFloor.position;
            if (!lure.TryThrow(point))
            {
                return;
            }

            var release = from + Vector3.up * definition.ReleaseHeight;
            flight = new GrenadeFlight(release, point, float.PositiveInfinity, definition.Gravity, Mathf.Max(lure.FlightSeconds, 0.01f));
            body.position = release;
            body.gameObject.SetActive(true);
        }

        private void OnLanded(Lure landed)
        {
            body.position = landed.Point;
            body.gameObject.SetActive(true);
            rattle.Play();
        }

        private void OnEnded(Lure ended, LureEnd why)
        {
            rattle.Stop();
            body.gameObject.SetActive(false);
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.Quarry.IsDrawn)
                {
                    enemy.Release();
                }
            }
        }

        // Every living Enemy that has come within the radius goes for it. Once drawn, an Enemy stays drawn while it lasts.
        private void DrawEnemies()
        {
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.Enemy.IsAlive || enemy.Quarry.IsDrawn)
                {
                    continue;
                }

                if (lure.Draws(enemy.transform.position, alreadyLured: false))
                {
                    enemy.DrawTo(body, this);
                }
            }
        }

        private void Unsubscribe()
        {
            if (lure == null)
            {
                return;
            }

            lure.Landed -= OnLanded;
            lure.Ended -= OnEnded;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}
