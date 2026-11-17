using DarknessRises.Game.Weapons;
using TMPro;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows the Primary weapon's magazine count and whether it's reloading, at the bottom right of the HUD.
    /// Updates only when the Weapon changes, never every frame.
    /// </summary>
    public sealed class AmmoHudView : MonoBehaviour
    {
        private const string MagazineFormat = "{0} / {1}";

        [SerializeField] private TMP_Text magazineCount;
        [SerializeField] private GameObject reloadingIndicator;

        private PrimaryWeapon weapon;

        public void Show(PrimaryWeapon primaryWeapon)
        {
            Unbind();

            weapon = primaryWeapon;
            weapon.Fired += Refresh;
            weapon.ReloadStarted += Refresh;
            weapon.ReloadFinished += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (weapon == null)
            {
                return;
            }

            weapon.Fired -= Refresh;
            weapon.ReloadStarted -= Refresh;
            weapon.ReloadFinished -= Refresh;
            weapon = null;
        }

        private void Refresh()
        {
            // TMP's numeric SetText formats without allocating; {0} prints no decimals.
            magazineCount.SetText(MagazineFormat, (float)weapon.RoundsInMagazine, (float)weapon.MagazineSize);
            reloadingIndicator.SetActive(weapon.IsReloading);
        }
    }
}
