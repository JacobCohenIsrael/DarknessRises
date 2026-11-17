using UnityEngine;

namespace DarknessRises.Game.Skills
{
    /// <summary>
    /// Shows the Flare with a shadowless point light on the character, switched off while the Flare isn't burning.
    /// Only Lightning casts real-time shadows.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class FlareView : MonoBehaviour
    {
        [SerializeField] private Light flareLight;

        private FlareDefinition definition;
        private float shownBrightness = -1f;

        public void Show(FlareDefinition flare)
        {
            definition = flare;
            flareLight.type = LightType.Point;
            flareLight.shadows = LightShadows.None;
            flareLight.color = definition.Color;
            flareLight.range = definition.Radius;
            transform.localPosition = Vector3.up * definition.Height;
            shownBrightness = -1f;
            Render(0f);
        }

        /// <summary>Shows the light at <paramref name="brightness"/>, from 0 (off) to 1 (full strength).</summary>
        public void Render(float brightness)
        {
            if (brightness == shownBrightness)
            {
                return;
            }

            shownBrightness = brightness;
            flareLight.intensity = definition.Intensity * brightness;
            flareLight.enabled = brightness > 0f;
        }

        private void Reset()
        {
            flareLight = GetComponent<Light>();
        }
    }
}
