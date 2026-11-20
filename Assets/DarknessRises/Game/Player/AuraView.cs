using UnityEngine;

namespace DarknessRises.Game.Player
{
    /// <summary>
    /// Shows the Aura with a shadowless point light. Only Lightning casts real-time shadows.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class AuraView : MonoBehaviour
    {
        [SerializeField] private Light auraLight;

        public Light Light => auraLight;

        public void Show(AuraSettings settings)
        {
            auraLight.type = LightType.Point;
            auraLight.shadows = LightShadows.None;
            auraLight.color = settings.Color;
            auraLight.intensity = settings.Intensity;
            auraLight.range = settings.Range;
            transform.localPosition = Vector3.up * settings.Height;
        }

        /// <summary>Sets how far the Aura's light reaches: the Run's number, Upgrades applied.</summary>
        public void Reach(float metres)
        {
            auraLight.range = metres;
        }

        private void Reset()
        {
            auraLight = GetComponent<Light>();
        }
    }
}
