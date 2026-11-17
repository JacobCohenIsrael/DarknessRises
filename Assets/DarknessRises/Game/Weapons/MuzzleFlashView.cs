using UnityEngine;
using UnityEngine.Pool;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// A pooled Muzzle flash: a shadowless point light that fades out over the flash's duration,
    /// then goes back to its pool. Only Lightning casts real-time shadows.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class MuzzleFlashView : MonoBehaviour
    {
        [SerializeField] private Light flashLight;

        private IObjectPool<MuzzleFlashView> pool;
        private MuzzleFlashSettings settings;
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
        }

        public void Flash(Vector3 position)
        {
            transform.position = position;
            secondsLeft = settings.Seconds;
            flashLight.intensity = settings.Intensity;
        }

        private void Update()
        {
            secondsLeft -= Time.deltaTime;
            if (secondsLeft <= 0f)
            {
                pool.Release(this);
                return;
            }

            flashLight.intensity = settings.Intensity * (secondsLeft / settings.Seconds);
        }

        private void Reset()
        {
            flashLight = GetComponent<Light>();
        }
    }
}
