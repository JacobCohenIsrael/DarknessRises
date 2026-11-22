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
    /// It gives off a small, dim, shadowless light and its can glows (<see cref="LureGlow"/>), from the throw until it
    /// ends, then both fade out and the body goes. The light is one of the scene's lights, so Enemies striking the Lure
    /// stand in it and count as visible, for Aim assist too: a place to shoot at, not a second Flare. One Lure exists,
    /// reused for every throw, so throwing allocates nothing. Bosses aren't living Enemies of the Waves, so they never
    /// see it.
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

        [SerializeField, Tooltip("The small shadowless point light on the can. Only Lightning casts real-time shadows.")]
        private Light glowLight;

        [SerializeField, Tooltip("The can, whose material's emission glows in the light's colour.")]
        private Renderer can;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private LureDefinition definition;
        private Lure lure;
        private LivingEnemies enemies;
        private GrenadeFlight flight;
        private LureGlow glow;
        private Material canGlow;
        private float shownBrightness = -1f;

        /// <summary>The Lure rule this view throws, once <see cref="Use"/> has been called.</summary>
        public Lure Lure => lure;

        /// <summary>The small light on the Lure, one of the scene's lights while it shines.</summary>
        public Light Light => glowLight;

        /// <summary>How bright the Lure's light and glow are now, once <see cref="Use"/> has been called.</summary>
        public LureGlow Glow => glow;

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

            glow = new LureGlow(definition.FadeOutSeconds, definition.PulseSeconds, definition.PulseDepth);
            glowLight.type = LightType.Point;
            glowLight.shadows = LightShadows.None;
            glowLight.color = definition.LightColor;
            glowLight.range = definition.LightRange;
            glowLight.transform.localPosition = Vector3.up * definition.LightHeight;
            if (can != null && canGlow == null)
            {
                // The can's own copy, with emission on, made once for every throw and Run in this scene.
                canGlow = new Material(can.sharedMaterial) { name = $"{can.sharedMaterial.name} Glow" };
                canGlow.EnableKeyword("_EMISSION");
                can.sharedMaterial = canGlow;
            }

            shownBrightness = -1f;
            Render(0f);

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

            if (glow.IsShining)
            {
                glow.Tick(Time.deltaTime);
                Render(glow.Brightness);
                if (!glow.IsShining && !lure.Skill.IsActive)
                {
                    body.gameObject.SetActive(false);
                }
            }
        }

        // Sets the light and the can's glow to brightness, from 0 (dark) to 1 (full strength).
        private void Render(float brightness)
        {
            if (brightness == shownBrightness)
            {
                return;
            }

            shownBrightness = brightness;
            glowLight.intensity = definition.LightIntensity * brightness;
            glowLight.enabled = brightness > 0f;
            if (canGlow != null)
            {
                canGlow.SetColor(EmissionColorId, definition.LightColor * (definition.GlowEmission * brightness));
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
            glow.Light();
            Render(glow.Brightness);
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

            // The can stays where it lies while its light fades out, then goes (in Update).
            glow.FadeOut();
            if (!glow.IsShining)
            {
                Render(0f);
                body.gameObject.SetActive(false);
            }

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
            if (canGlow != null)
            {
                Destroy(canGlow);
            }
        }
    }
}
