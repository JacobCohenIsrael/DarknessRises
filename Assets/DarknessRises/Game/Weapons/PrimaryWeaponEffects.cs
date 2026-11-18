using System;
using DarknessRises.Game.Content;
using DarknessRises.Game.Visibility;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// What one Primary weapon's shots look like in the scene: its pooled Shots, Muzzle flashes and hit sparks,
    /// all built from its definition. A Shot that hits something throws a hit spark and damages and pushes it (Knockback) if it's an
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
        private readonly ShotView shotPrefab;
        private readonly HitSparkView hitSparkPrefab;
        private readonly ObjectPool<ShotView> shots;
        private readonly ObjectPool<MuzzleFlashView> muzzleFlashes;
        private readonly ObjectPool<HitSparkView> hitSparks;
        private readonly Action<RaycastHit, Vector3> onShotHit;
        private readonly SceneLights lights;
        private int shotsUntilLit;

        /// <param name="content">Where the Shot and hit spark prefabs are, already loaded. The effects never release them.</param>
        /// <param name="sceneLights">Where every Muzzle flash, lit Shot and hit spark light is registered as it's created.</param>
        public PrimaryWeaponEffects(WeaponDefinition weaponDefinition, LayerMask hitLayers, ILoadedContent content, SceneLights sceneLights)
        {
            lights = sceneLights;
            definition = weaponDefinition;
            shotHitLayers = hitLayers;
            onShotHit = OnShotHit;
            shotPrefab = content.Prefab<ShotView>(definition.Shot.Prefab);
            hitSparkPrefab = content.Prefab<HitSparkView>(definition.HitSpark.Prefab);

            effectsRoot = new GameObject($"{definition.name} Effects").transform;
            shotGlowMaterial = new Material(shotPrefab.BodyMaterial) { name = $"{definition.name} Shot Glow" };
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

        // Something that can't be hurt by this Shot (the Colossus's foot off its weak spot or outside the damage
        // window) gets a dull spark, so the player can tell what does and doesn't hurt. Walls keep the normal spark.
        // The hit carries the Weapon's Knockback along the Shot's flight; only regular Enemies are pushed by it.
        private void OnShotHit(RaycastHit hit, Vector3 direction)
        {
            var hurt = true;
            if (hit.collider.TryGetComponent<IShotTarget>(out var target))
            {
                var knockback = definition.Knockback;
                hurt = target.TakeShot(new ShotHit(definition.Damage, direction, knockback.Metres, knockback.Seconds));
            }

            hitSparks.Get().Spark(hit.point, hit.normal, dull: !hurt);
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
            var sparkSeconds = Mathf.Max(definition.HitSpark.Seconds, hitSparkPrefab.LifeSeconds);
            return Mathf.CeilToInt(definition.ShotsPerSecond * sparkSeconds) + 1;
        }

        private ShotView CreateShot()
        {
            var shot = Object.Instantiate(shotPrefab, effectsRoot);
            shot.Prepare(shots, definition.Shot, definition.ShotLight, shotGlowMaterial, shotHitLayers, onShotHit);
            shot.gameObject.SetActive(false);
            lights.Add(shot.GetComponentInChildren<Light>(true));
            return shot;
        }

        private MuzzleFlashView CreateMuzzleFlash()
        {
            var flashObject = new GameObject("Muzzle Flash", typeof(Light), typeof(MuzzleFlashView));
            flashObject.transform.SetParent(effectsRoot, false);
            flashObject.SetActive(false);
            var flash = flashObject.GetComponent<MuzzleFlashView>();
            flash.Prepare(muzzleFlashes, definition.MuzzleFlash);
            lights.Add(flashObject.GetComponent<Light>());
            return flash;
        }

        private HitSparkView CreateHitSpark()
        {
            var spark = Object.Instantiate(hitSparkPrefab, effectsRoot);
            spark.gameObject.SetActive(false);
            spark.Prepare(hitSparks, definition.HitSpark);
            lights.Add(spark.GetComponentInChildren<Light>(true));
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
