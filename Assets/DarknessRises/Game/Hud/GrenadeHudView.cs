using DarknessRises.Game.Grenades;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows the Grenade charges next to the ammo: one pip per charge, filled while the charge is carried,
    /// with the charge that is refilling filling up around its pip.
    /// </summary>
    public sealed class GrenadeHudView : MonoBehaviour
    {
        [SerializeField, Tooltip("One charge's pip. Cloned for each charge the character carries.")]
        private RectTransform pipTemplate;

        [SerializeField, Tooltip("The template pip's fill, a radial filled Image.")]
        private Image pipFillTemplate;

        private GrenadeCharges charges;
        private Image[] fills = System.Array.Empty<Image>();

        public void Show(GrenadeCharges grenadeCharges)
        {
            charges = grenadeCharges;
            BuildPips(charges.MaxCharges);
            Refresh();
        }

        // The refilling pip fills up continuously, so this runs every frame. Setting an unchanged fill amount is free.
        private void Update()
        {
            if (charges != null)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var carried = charges.Charges;
            for (var i = 0; i < fills.Length; i++)
            {
                fills[i].fillAmount = i < carried ? 1f : i == carried ? charges.RefillProgress : 0f;
            }
        }

        private void BuildPips(int count)
        {
            var parent = pipTemplate.parent;
            var fillPath = pipFillTemplate.transform.GetSiblingIndex();

            // Drop pips left from an earlier Show; the template is always the first.
            for (var i = parent.childCount - 1; i >= 1; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }

            fills = new Image[count];
            fills[0] = pipFillTemplate;
            for (var i = 1; i < count; i++)
            {
                var pip = Instantiate(pipTemplate, parent);
                pip.name = $"{pipTemplate.name} {i + 1}";
                fills[i] = pip.GetChild(fillPath).GetComponent<Image>();
            }
        }
    }
}
