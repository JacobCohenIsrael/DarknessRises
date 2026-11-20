using System;
using UnityEngine;

namespace DarknessRises.Game.Weapons
{
    /// <summary>
    /// How the Primary weapons' ammo reserves come back during a Run. Every reserve starts a Run full; how much one
    /// holds is authored on each Weapon. The Pistol's is unlimited.
    /// </summary>
    [Serializable]
    public sealed class AmmoSettings
    {
        [SerializeField, Range(0f, 1f), Tooltip(
            "The share of a full reserve every Primary gets back when a new Level of the Run starts. 0.5 is half; never above full.")]
        private float levelRefillShare = 0.5f;

        public float LevelRefillShare => levelRefillShare;
    }
}
