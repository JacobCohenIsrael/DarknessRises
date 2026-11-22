using DarknessRises.Game.Grenades;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows the Grenade charges: one small grenade icon per charge, lit while the charge is carried and dimmed once
    /// it's spent, with the charge that is refilling lighting up from the bottom as it refills
    /// (<see cref="GrenadeChargeIcons"/>).
    /// </summary>
    public sealed class GrenadeHudView : MonoBehaviour
    {
        [SerializeField, Tooltip("One charge's icon, its dimmed outline. Cloned for each charge the character carries.")]
        private RectTransform iconTemplate;

        [SerializeField, Tooltip("The template icon's lit fill, a vertically filled Image over the dimmed one.")]
        private Image iconFillTemplate;

        private GrenadeCharges charges;
        private Image[] fills = System.Array.Empty<Image>();

        public void Show(GrenadeCharges grenadeCharges)
        {
            charges = grenadeCharges;
            BuildIcons(charges.MaxCharges);
            Refresh();
        }

        // The refilling icon fills up continuously, so this runs every frame. Setting an unchanged fill amount is free.
        private void Update()
        {
            if (charges != null)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < fills.Length; i++)
            {
                fills[i].fillAmount = GrenadeChargeIcons.Fill(i, charges);
            }
        }

        private void BuildIcons(int count)
        {
            var parent = iconTemplate.parent;
            var fillPath = iconFillTemplate.transform.GetSiblingIndex();

            // Drop icons left from an earlier Show; the template is always the first.
            for (var i = parent.childCount - 1; i >= 1; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }

            fills = new Image[count];
            fills[0] = iconFillTemplate;
            for (var i = 1; i < count; i++)
            {
                var icon = Instantiate(iconTemplate, parent);
                icon.name = $"{iconTemplate.name} {i + 1}";
                fills[i] = icon.GetChild(fillPath).GetComponent<Image>();
            }
        }
    }
}
