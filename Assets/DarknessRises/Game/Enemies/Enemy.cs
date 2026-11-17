using System;

namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// One Enemy's changing state: its hit points, and whether it is alive. Plain C# (ADR 0003).
    /// Pooled with its view, so <see cref="Spawn"/> starts it over.
    /// </summary>
    public sealed class Enemy
    {
        /// <summary>Raised when a hit takes hit points away and the Enemy survives it.</summary>
        public event Action<Enemy> Hurt;

        /// <summary>Raised once per life, when the Enemy's hit points run out.</summary>
        public event Action<Enemy> Died;

        public int HitPoints { get; private set; }

        public bool IsAlive { get; private set; }

        /// <summary>Brings the Enemy to life with full hit points.</summary>
        public void Spawn(int hitPoints)
        {
            if (hitPoints < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(hitPoints), hitPoints, "An Enemy needs at least one hit point.");
            }

            HitPoints = hitPoints;
            IsAlive = true;
        }

        /// <summary>Takes <paramref name="damage"/> hit points away. A dead Enemy ignores hits.</summary>
        public void TakeHit(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage can't be negative.");
            }

            if (!IsAlive || damage == 0)
            {
                return;
            }

            HitPoints = Math.Max(0, HitPoints - damage);
            if (HitPoints > 0)
            {
                Hurt?.Invoke(this);
                return;
            }

            IsAlive = false;
            Died?.Invoke(this);
        }
    }
}
