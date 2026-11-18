using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Bosses
{
    /// <summary>The glowing weak spot on the Colossus's foot: the one part a Shot can hurt. Passes hits to the fight.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ColossusWeakSpot : MonoBehaviour, IShotTarget
    {
        [SerializeField] private ColossusView colossus;

        // Bosses take no Knockback.
        public bool TakeShot(in ShotHit hit)
        {
            return colossus.HitWeakSpot(hit.Damage);
        }
    }
}
