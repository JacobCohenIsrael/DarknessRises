using System;
using UnityEngine;

namespace DarknessRises.Game.Hud
{
    /// <summary>How the Weapon button reads and looks: the equipped Primary weapon's icon and name over its ammo reserve.</summary>
    [Serializable]
    public sealed class WeaponButtonSettings
    {
        [SerializeField, Tooltip("The reserve line. {0} is the rounds left in the equipped Weapon's ammo reserve.")]
        private string reserveFormat = "RESERVE {0}";

        [SerializeField, Tooltip("The reserve line of a Weapon whose ammo never runs out, the Pistol. Empty shows nothing.")]
        private string unlimitedText = "NO LIMIT";

        [SerializeField] private Color textColor = new(0.9f, 0.9f, 0.9f, 0.95f);

        [SerializeField, Tooltip("Tint of the equipped Weapon's icon, a white silhouette.")]
        private Color iconColor = new(0.9f, 0.9f, 0.9f, 0.95f);

        [SerializeField, Tooltip("The reserve line once the reserve is empty: what's in the magazine is all that's left.")]
        private Color emptyReserveColor = new(1f, 0.35f, 0.25f, 1f);

        [SerializeField, Tooltip("The button's face.")]
        private Color faceColor = new(0.1f, 0.1f, 0.11f, 0.6f);

        [SerializeField, Tooltip("The face's colour at the moment a Weapon runs dry and the Pistol takes over. It fades back to the face colour.")]
        private Color runDryFlashColor = new(1f, 0.35f, 0.25f, 0.9f);

        [SerializeField, Min(0f), Tooltip("Seconds the run-dry flash takes to fade out. 0 turns it off.")]
        private float runDryFlashSeconds = 0.8f;

        public string ReserveFormat => reserveFormat;
        public string UnlimitedText => unlimitedText;
        public Color TextColor => textColor;
        public Color IconColor => iconColor;
        public Color EmptyReserveColor => emptyReserveColor;
        public Color FaceColor => faceColor;
        public Color RunDryFlashColor => runDryFlashColor;
        public float RunDryFlashSeconds => runDryFlashSeconds;
    }
}
