using System;
using DarknessRises.Game.Controls;
using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// Holds the character's Primary weapon in the scene: forwards the player's Fire and Reload commands to the
    /// <see cref="PrimaryWeapon"/> rule, turns the weapon mount toward the aim point, and shows each shot
    /// with a pooled Shot and a pooled Muzzle flash. A Shot that hits something throws a pooled hit spark
    /// and damages it if it's an <see cref="IShotTarget"/>.
    /// </summary>
    /// <remarks>
    /// Runs after the default execution order so the character has already walked this frame,
    /// and Shots leave from where the character is now.
    /// </remarks>
    [DefaultExecutionOrder(AfterCharacterMovement)]
    public sealed class PrimaryWeaponView : MonoBehaviour
    {
        private const int AfterCharacterMovement = 100;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private PlayerControls controls;

        [SerializeField, Tooltip("Turns (yaw only) to face the aim point.")]
        private Transform mount;

        [SerializeField, Tooltip("Where Shots and the Muzzle flash start. A child of the mount.")]
        private Transform muzzle;

        [SerializeField, Tooltip("What a Shot stops at.")]
        private LayerMask shotHitLayers = Physics.DefaultRaycastLayers;

        private WeaponDefinition definition;
        private ObjectPool<ShotView> shots;
        private ObjectPool<MuzzleFlashView> muzzleFlashes;
        private ObjectPool<HitSparkView> hitSparks;
        private Action<RaycastHit> onShotHit;
        private Material shotGlowMaterial;
        private Transform effectsRoot;

        public PrimaryWeapon Weapon { get; private set; }

        /// <summary>Arms the character with a fresh, fully loaded Weapon built from <paramref name="weaponDefinition"/>.</summary>
        public PrimaryWeapon Equip(WeaponDefinition weaponDefinition)
        {
            Unequip();

            definition = weaponDefinition;
            Weapon = new PrimaryWeapon(definition.PrimaryStats);
            Weapon.Fired += OnFired;

            effectsRoot = new GameObject($"{definition.name} Effects").transform;
            shotGlowMaterial = new Material(definition.Shot.Prefab.BodyMaterial) { name = $"{definition.name} Shot Glow" };
            shotGlowMaterial.SetColor(BaseColorId, definition.Shot.GlowColor);

            shots = new ObjectPool<ShotView>(CreateShot, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: MostShotsInFlight());
            muzzleFlashes = new ObjectPool<MuzzleFlashView>(CreateMuzzleFlash, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: MostFlashesAlive());
            hitSparks = new ObjectPool<HitSparkView>(CreateHitSpark, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: MostSparksAlive());
            Prewarm(shots, MostShotsInFlight());
            Prewarm(muzzleFlashes, MostFlashesAlive());
            Prewarm(hitSparks, MostSparksAlive());

            return Weapon;
        }

        public void Unequip()
        {
            if (Weapon == null)
            {
                return;
            }

            Weapon.Fired -= OnFired;
            Weapon = null;
            shots.Dispose();
            muzzleFlashes.Dispose();
            hitSparks.Dispose();

            // On scene unload the effects may already be gone.
            if (effectsRoot != null)
            {
                Destroy(effectsRoot.gameObject);
            }

            Destroy(shotGlowMaterial);
        }

        private void Update()
        {
            if (Weapon == null)
            {
                return;
            }

            if (controls.TryGetAimPoint(muzzle.position.y, out var aimPoint)
                && AimPlane.TryGetFlatDirection(mount.position, aimPoint, out var aimDirection))
            {
                mount.rotation = Quaternion.LookRotation(aimDirection, Vector3.up);
            }

            if (controls.WasReloadPressedThisFrame)
            {
                Weapon.Reload();
            }

            if (controls.IsFireHeld)
            {
                Weapon.PullTrigger();
            }
            else
            {
                Weapon.ReleaseTrigger();
            }

            Weapon.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Unequip();
        }

        private void OnFired()
        {
            var direction = mount.forward;
            direction.y = 0f;
            shots.Get().Launch(muzzle.position, direction, definition.ShotSpeed, definition.Range);
            muzzleFlashes.Get().Flash(muzzle.position);
        }

        private void OnShotHit(RaycastHit hit)
        {
            hitSparks.Get().Spark(hit.point, hit.normal);
            if (hit.collider.TryGetComponent<IShotTarget>(out var target))
            {
                target.TakeShot(definition.Damage);
            }
        }

        // Enough pooled Shots for a held trigger with every Shot flying its full range.
        private int MostShotsInFlight()
        {
            return Mathf.CeilToInt(definition.ShotsPerSecond * definition.Range / definition.ShotSpeed) + 1;
        }

        // Enough pooled Muzzle flashes for a held trigger, however short the gap between shots.
        private int MostFlashesAlive()
        {
            return Mathf.CeilToInt(definition.ShotsPerSecond * definition.MuzzleFlash.Seconds) + 1;
        }

        // Enough pooled hit sparks for a held trigger with every Shot hitting something.
        private int MostSparksAlive()
        {
            var sparkSeconds = Mathf.Max(definition.HitSpark.Seconds, definition.HitSpark.Prefab.LifeSeconds);
            return Mathf.CeilToInt(definition.ShotsPerSecond * sparkSeconds) + 1;
        }

        private ShotView CreateShot()
        {
            onShotHit ??= OnShotHit;
            var shot = Instantiate(definition.Shot.Prefab, effectsRoot);
            shot.Prepare(shots, definition.Shot, shotGlowMaterial, shotHitLayers, onShotHit);
            shot.gameObject.SetActive(false);
            return shot;
        }

        private MuzzleFlashView CreateMuzzleFlash()
        {
            var flashObject = new GameObject("Muzzle Flash", typeof(Light), typeof(MuzzleFlashView));
            flashObject.transform.SetParent(effectsRoot, false);
            flashObject.SetActive(false);
            var flash = flashObject.GetComponent<MuzzleFlashView>();
            flash.Prepare(muzzleFlashes, definition.MuzzleFlash);
            return flash;
        }

        private HitSparkView CreateHitSpark()
        {
            var spark = Instantiate(definition.HitSpark.Prefab, effectsRoot);
            spark.gameObject.SetActive(false);
            spark.Prepare(hitSparks, definition.HitSpark);
            return spark;
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
