using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>
    /// The planted foot around its weak spot. It stops Shots but never takes damage, so a Shot here shows a dull hit spark
    /// and the player can tell it apart from a hit on the weak spot.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ColossusFoot : MonoBehaviour, IShotTarget
    {
        public bool TakeShot(int damage)
        {
            return false;
        }
    }
}
