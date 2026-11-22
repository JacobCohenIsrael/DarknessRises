using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A pooled Muzzle flash: a shadowless point light and an unlit glow that fade (and maybe flicker) over the flash's
    /// duration, and maybe a few sparks thrown forward, then it goes back to its pool. Only Lightning casts real-time shadows.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class MuzzleFlashView : MonoBehaviour
    {
        [SerializeField] private Light flashLight;

        [SerializeField, Tooltip("The unlit glow at the muzzle, lying flat. It shrinks as the flash fades. Optional.")]
        private Transform glow;

        [SerializeField, Tooltip("Sparks thrown forward with each flash, in world space. Optional.")]
        private ParticleSystem sparks;

        [SerializeField, Min(0f), Tooltip("How small the glow gets as the flash fades out, as a share of its full size.")]
        private float glowEndScale = 0.4f;

        [SerializeField, Range(0f, 180f), Tooltip("Degrees the glow turns at random about the aim each shot, so no two flashes look alike.")]
        private float glowRollDegrees = 20f;

        private IObjectPool<MuzzleFlashView> pool;
        private MuzzleFlashSettings settings;
        private Vector3 glowScale = Vector3.one;
        private float secondsLeft;

        /// <summary>Called once, when the pool creates this flash.</summary>
        public void Prepare(IObjectPool<MuzzleFlashView> owner, MuzzleFlashSettings flashSettings)
        {
            if (flashLight == null)
            {
                flashLight = GetComponent<Light>();
            }

            pool = owner;
            settings = flashSettings;
            flashLight.type = LightType.Point;
            flashLight.shadows = LightShadows.None;
            flashLight.color = settings.Color;
            flashLight.range = settings.Range;
            if (glow != null)
            {
                glowScale = glow.localScale;
            }
        }

        /// <summary>Flashes at <paramref name="position"/>, facing along <paramref name="direction"/> (flat).</summary>
        public void Flash(Vector3 position, Vector3 direction)
        {
            direction.y = 0f;
            var facing = direction.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity;
            transform.SetPositionAndRotation(position, facing);
            if (glow != null)
            {
                glow.localRotation = Quaternion.Euler(90f, Random.Range(-glowRollDegrees, glowRollDegrees), 0f);
            }

            secondsLeft = settings.Seconds;
            Show(1f);
            if (sparks != null && settings.SparkCount > 0)
            {
                sparks.Emit(settings.SparkCount);
            }
        }

        private void Update()
        {
            secondsLeft -= Time.deltaTime;
            if (secondsLeft <= 0f)
            {
                pool.Release(this);
                return;
            }

            Show(MuzzleFlashSettings.Strength(secondsLeft, settings.Seconds, settings.Flicker, Random.value));
        }

        private void Show(float strength)
        {
            flashLight.intensity = settings.Intensity * strength;
            if (glow != null)
            {
                glow.localScale = glowScale * Mathf.Lerp(glowEndScale, 1f, strength);
            }
        }

        private void Reset()
        {
            flashLight = GetComponent<Light>();
        }
    }
}
