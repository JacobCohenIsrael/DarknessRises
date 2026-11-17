using System;

namespace DarknessRises.Game.Health
{
    /// <summary>
    /// The character's hit points. A landed hit takes some away and makes the character invulnerable for a moment;
    /// hit points never regenerate, and at zero the character dies. Plain C# (ADR 0003); time comes in as an explicit delta.
    /// </summary>
    public sealed class CharacterHealth
    {
        private readonly float invulnerabilitySeconds;

        public CharacterHealth(int hitPoints, float invulnerabilitySeconds)
        {
            if (hitPoints < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(hitPoints), hitPoints, "The character needs at least one hit point.");
            }

            if (invulnerabilitySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(invulnerabilitySeconds), invulnerabilitySeconds, "Invulnerability can't be negative.");
            }

            MaxHitPoints = hitPoints;
            HitPoints = hitPoints;
            this.invulnerabilitySeconds = invulnerabilitySeconds;
        }

        /// <summary>Raised when a hit lands and the character survives it.</summary>
        public event Action Hurt;

        /// <summary>Raised once, when the last hit point is lost.</summary>
        public event Action Died;

        public int MaxHitPoints { get; }

        public int HitPoints { get; private set; }

        public bool IsAlive => HitPoints > 0;

        /// <summary>Seconds of invulnerability left from the last hit.</summary>
        public float InvulnerableSecondsLeft { get; private set; }

        public bool IsInvulnerable => InvulnerableSecondsLeft > 0f;

        /// <summary>Debug only: while on, no hit lands. The debug key that sets it is compiled out of release builds.</summary>
        public bool IsGodMode { get; set; }

        /// <summary>
        /// Takes <paramref name="damage"/> hit points away, unless the character is dead, invulnerable or in god mode.
        /// </summary>
        /// <returns>Whether the hit landed.</returns>
        public bool TakeHit(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), damage, "Damage can't be negative.");
            }

            if (damage == 0 || !IsAlive || IsInvulnerable || IsGodMode)
            {
                return false;
            }

            HitPoints = Math.Max(0, HitPoints - damage);
            if (HitPoints == 0)
            {
                InvulnerableSecondsLeft = 0f;
                Died?.Invoke();
                return true;
            }

            InvulnerableSecondsLeft = invulnerabilitySeconds;
            Hurt?.Invoke();
            return true;
        }

        /// <summary>Runs down the invulnerability from the last hit.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            InvulnerableSecondsLeft = Math.Max(0f, InvulnerableSecondsLeft - deltaSeconds);
        }
    }
}
