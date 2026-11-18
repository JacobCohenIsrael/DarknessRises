using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Player;
using DarknessRises.Game.Visibility;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Grenades
{
    /// <summary>
    /// Holds the character's Grenade, the Secondary weapon. On the player's Grenade command it spends a charge from
    /// the <see cref="GrenadeCharges"/> rule and starts the throw animation; when the Grenade leaves the hand it
    /// flies a pooled Grenade to the aim point. The explosion damages every Enemy in the blast radius and shows
    /// a pooled explosion.
    /// </summary>
    /// <remarks>
    /// The blast hits each Enemy through <see cref="IShotTarget"/>, the same way a Shot does. It ignores cover:
    /// an Enemy behind a pillar inside the radius is hit too.
    /// </remarks>
    [DefaultExecutionOrder(AfterCharacterMovement)]
    public sealed class GrenadeThrowerView : MonoBehaviour
    {
        private const int AfterCharacterMovement = 100;

        // Colliders one blast can find. Far more than a Wave packs into a blast radius.
        private const int MaxBlastColliders = 64;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private PlayerControls controls;

        [SerializeField, Tooltip("Where the Grenade leaves the hand. Child of the Weapon mount, so it faces the aim.")]
        private Transform hand;

        [SerializeField, Tooltip("The floor the aim point is found on. The character's root.")]
        private Transform floor;

        [SerializeField, Tooltip("What a Grenade in flight stops at. Enemies on these layers are flown past.")]
        private LayerMask flightHitLayers = Physics.DefaultRaycastLayers;

        [SerializeField, Tooltip("What the blast can damage.")]
        private LayerMask blastLayers = Physics.DefaultRaycastLayers;

        private readonly Collider[] blastColliders = new Collider[MaxBlastColliders];
        private readonly IShotTarget[] blastTargets = new IShotTarget[MaxBlastColliders];

        private GrenadeDefinition definition;
        private CharacterAnimationView characterAnimation;
        private ObjectPool<GrenadeView> grenades;
        private ObjectPool<GrenadeExplosionView> explosions;
        private System.Action<Vector3> onLanded;
        private Material glowMaterial;
        private Transform effectsRoot;
        private GrenadeView grenadePrefab;
        private GrenadeExplosionView explosionPrefab;
        private AudioClip bangClip;

        private SceneLights sceneLights;

        public GrenadeCharges Charges { get; private set; }

        /// <summary>
        /// Gives the character a full set of Grenades built from <paramref name="grenadeDefinition"/>,
        /// thrown with <paramref name="animation"/>'s throw. Its prefabs and bang come from
        /// <paramref name="content"/>, already loaded. Explosion lights are added to <paramref name="lights"/>.
        /// </summary>
        public GrenadeCharges Equip(
            GrenadeDefinition grenadeDefinition, CharacterAnimationView animation, ILoadedContent content, SceneLights lights)
        {
            Unequip();

            sceneLights = lights;
            definition = grenadeDefinition;
            characterAnimation = animation;
            grenadePrefab = content.Prefab<GrenadeView>(grenadeDefinition.Body.Prefab);
            explosionPrefab = content.Prefab<GrenadeExplosionView>(grenadeDefinition.Explosion.Prefab);
            bangClip = content.Clip(grenadeDefinition.Explosion.Clip);
            Charges = new GrenadeCharges(definition.ChargeStats);
            characterAnimation.GrenadeReleased += OnGrenadeReleased;

            effectsRoot = new GameObject($"{definition.name} Effects").transform;
            glowMaterial = new Material(grenadePrefab.BodyMaterial) { name = $"{definition.name} Glow" };
            glowMaterial.SetColor(BaseColorId, definition.Body.GlowColor);

            onLanded ??= Explode;
            var mostAlive = MostAlive();
            grenades = new ObjectPool<GrenadeView>(CreateGrenade, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: mostAlive);
            explosions = new ObjectPool<GrenadeExplosionView>(CreateExplosion, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: mostAlive);
            Prewarm(grenades, mostAlive);
            Prewarm(explosions, mostAlive);

            return Charges;
        }

        public void Unequip()
        {
            if (Charges == null)
            {
                return;
            }

            characterAnimation.GrenadeReleased -= OnGrenadeReleased;
            Charges = null;
            grenades.Dispose();
            explosions.Dispose();

            // On scene unload the effects may already be gone.
            if (effectsRoot != null)
            {
                Destroy(effectsRoot.gameObject);
            }

            Destroy(glowMaterial);
        }

        private void Update()
        {
            if (Charges == null)
            {
                return;
            }

            Charges.Tick(Time.deltaTime);

            // The charge is spent when the throw starts, so a throw already under way can't be doubled up.
            if (!controls.WasGrenadePressedThisFrame)
            {
                return;
            }

            if (Charges.CanThrow && characterAnimation.Throw())
            {
                Charges.TrySpend();
            }
            else
            {
                controls.ForgetGrenadeAim();
            }
        }

        private void OnDestroy()
        {
            Unequip();
        }

        private void OnGrenadeReleased()
        {
            if (characterAnimation.IsDead)
            {
                return;
            }

            var origin = hand.position;
            if (!controls.TryGetGrenadeAimPoint(floor.position.y, out var aimPoint))
            {
                // No aim point (pointer above the horizon): throw as far as it goes, straight ahead.
                aimPoint = origin + hand.forward * definition.MaxThrowRange;
                aimPoint.y = floor.position.y;
            }

            var target = GrenadeFlight.ClampToRange(origin, aimPoint, definition.MaxThrowRange);
            var flight = new GrenadeFlight(origin, target, definition.ThrowSpeed, definition.Gravity, definition.MinFlightSeconds);
            grenades.Get().Throw(flight);
        }

        private void Explode(Vector3 position)
        {
            explosions.Get().Explode(position);

            var found = Physics.OverlapSphereNonAlloc(
                position, definition.BlastRadius, blastColliders, blastLayers, QueryTriggerInteraction.Ignore);
            var hit = 0;
            for (var i = 0; i < found; i++)
            {
                if (blastColliders[i].TryGetComponent<IShotTarget>(out var target) && !WasHit(target, hit))
                {
                    blastTargets[hit++] = target;
                    target.TakeShot(definition.Damage);
                }

                blastColliders[i] = null;
            }

            for (var i = 0; i < hit; i++)
            {
                blastTargets[i] = null;
            }
        }

        private bool WasHit(IShotTarget target, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (ReferenceEquals(blastTargets[i], target))
                {
                    return true;
                }
            }

            return false;
        }

        // Every charge in the air or going off at once.
        private int MostAlive()
        {
            return definition.ChargeStats.MaxCharges + 1;
        }

        private GrenadeView CreateGrenade()
        {
            var grenade = Instantiate(grenadePrefab, effectsRoot);
            grenade.Prepare(grenades, definition.Body, glowMaterial, flightHitLayers, onLanded);
            grenade.gameObject.SetActive(false);
            return grenade;
        }

        private GrenadeExplosionView CreateExplosion()
        {
            var explosion = Instantiate(explosionPrefab, effectsRoot);
            explosion.gameObject.SetActive(false);
            explosion.Prepare(explosions, definition.Explosion, bangClip);
            sceneLights.Add(explosion.GetComponentInChildren<Light>(true));
            return explosion;
        }

        private static void OnGetPooled(Component pooled)
        {
            pooled.gameObject.SetActive(true);
        }

        private static void OnReleasePooled(Component pooled)
        {
            pooled.gameObject.SetActive(false);
        }

        private static void OnDestroyPooled(Component pooled)
        {
            if (pooled != null)
            {
                Destroy(pooled.gameObject);
            }
        }

        private static void Prewarm<T>(ObjectPool<T> pool, int count) where T : class
        {
            var warmed = new T[count];
            for (var i = 0; i < count; i++)
            {
                warmed[i] = pool.Get();
            }

            for (var i = 0; i < count; i++)
            {
                pool.Release(warmed[i]);
            }
        }
    }
}
