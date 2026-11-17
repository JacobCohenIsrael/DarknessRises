using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// What one Primary weapon's shots look like in the scene: its pooled Shots, Muzzle flashes and hit sparks,
    /// all built from its definition. A Shot that hits something throws a hit spark and damages it if it's an
    /// <see cref="IShotTarget"/>. Each Primary in the loadout has its own, so switching Weapons never rebuilds pools
    /// and Shots already flying finish their flight.
    /// </summary>
    public sealed class PrimaryWeaponEffects : IDisposable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly WeaponDefinition definition;
        private readonly LayerMask shotHitLayers;
        private readonly Transform effectsRoot;
        private readonly Material shotGlowMaterial;
        private readonly ObjectPool<ShotView> shots;
        private readonly ObjectPool<MuzzleFlashView> muzzleFlashes;
        private readonly ObjectPool<HitSparkView> hitSparks;
        private readonly Action<RaycastHit> onShotHit;
        private int shotsUntilLit;

        public PrimaryWeaponEffects(WeaponDefinition weaponDefinition, LayerMask hitLayers)
        {
            definition = weaponDefinition;
            shotHitLayers = hitLayers;
            onShotHit = OnShotHit;

            effectsRoot = new GameObject($"{definition.name} Effects").transform;
            shotGlowMaterial = new Material(definition.Shot.Prefab.BodyMaterial) { name = $"{definition.name} Shot Glow" };
            shotGlowMaterial.SetColor(BaseColorId, definition.Shot.GlowColor);

            shots = new ObjectPool<ShotView>(CreateShot, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: MostShotsInFlight());
            muzzleFlashes = new ObjectPool<MuzzleFlashView>(CreateMuzzleFlash, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: MostFlashesAlive());
            hitSparks = new ObjectPool<HitSparkView>(CreateHitSpark, OnGetPooled, OnReleasePooled, OnDestroyPooled, defaultCapacity: MostSparksAlive());
            Prewarm(shots, MostShotsInFlight());
            Prewarm(muzzleFlashes, MostFlashesAlive());
            Prewarm(hitSparks, MostSparksAlive());
        }

        /// <summary>Shows one shot leaving <paramref name="muzzle"/>, aimed along <paramref name="aimDirection"/> (flattened).</summary>
        public void Fire(Vector3 muzzle, Vector3 aimDirection)
        {
            aimDirection.y = 0f;
            var halfSpread = definition.SpreadDegrees * 0.5f;
            if (halfSpread > 0f)
            {
                aimDirection = Quaternion.AngleAxis(Random.Range(-halfSpread, halfSpread), Vector3.up) * aimDirection;
            }

            shots.Get().Launch(muzzle, aimDirection, definition.ShotSpeed, definition.Range, NextShotIsLit());
            muzzleFlashes.Get().Flash(muzzle);
        }

        public void Dispose()
        {
            shots.Dispose();
            muzzleFlashes.Dispose();
            hitSparks.Dispose();

            // On scene unload the effects may already be gone.
            if (effectsRoot != null)
            {
                Object.Destroy(effectsRoot.gameObject);
            }

            Object.Destroy(shotGlowMaterial);
        }

        // Lights the first Shot and then every Nth one after it.
        private bool NextShotIsLit()
        {
            var shotLight = definition.ShotLight;
            if (!shotLight.CarriesLight)
            {
                return false;
            }

            if (shotsUntilLit > 0)
            {
                shotsUntilLit--;
                return false;
            }

            shotsUntilLit = shotLight.EveryNthShot - 1;
            return true;
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
            var shot = Object.Instantiate(definition.Shot.Prefab, effectsRoot);
            shot.Prepare(shots, definition.Shot, definition.ShotLight, shotGlowMaterial, shotHitLayers, onShotHit);
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
            var spark = Object.Instantiate(definition.HitSpark.Prefab, effectsRoot);
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
                Object.Destroy(pooled.gameObject);
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
