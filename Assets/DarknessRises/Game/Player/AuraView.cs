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

        public void Show(AuraSettings settings)
        {
            auraLight.type = LightType.Point;
            auraLight.shadows = LightShadows.None;
            auraLight.color = settings.Color;
            auraLight.intensity = settings.Intensity;
            auraLight.range = settings.Range;
            transform.localPosition = Vector3.up * settings.Height;
        }

        private void Reset()
        {
            auraLight = GetComponent<Light>();
        }
    }
}
