using System;
using System.Collections.Generic;
using DarknessRises.Game.Grenades;
using DarknessRises.Game.Skills;
using DarknessRises.Game.Weapons;

namespace DarknessRises.Game.Upgrades
{
    /// <summary>
    /// A Run's effective stats: every number an Upgrade can change, as it stands for this Run. Built once when the Run's
    /// Level starts, from the authored definitions and the levels the player owns, and read by every system that used
    /// to read those numbers from its definition: the Primary weapons, the Grenade, the Skills, the character's hit
    /// points and the Aura. The definitions themselves never change (ADR 0003). Plain C#.
    /// </summary>
    public sealed class RunStats
    {
        private readonly WeaponRunStats[] primaries;

        /// <param name="primaries">One entry per Primary the character carries, in switching order.</param>
        public RunStats(
            IReadOnlyList<WeaponRunStats> primaries,
            GrenadeRunStats grenade,
            SkillTiming flare,
            SkillTiming slowMo,
            int maxHitPoints,
            float auraRange)
        {
            if (primaries == null || primaries.Count == 0)
            {
                throw new ArgumentException("The character carries at least one Primary weapon.", nameof(primaries));
            }

            if (maxHitPoints < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHitPoints), maxHitPoints, "The character starts a Run alive.");
            }

            if (auraRange < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(auraRange), auraRange, "A light can't reach a negative distance.");
            }

            this.primaries = new WeaponRunStats[primaries.Count];
            for (var i = 0; i < primaries.Count; i++)
            {
                this.primaries[i] = primaries[i];
            }

            Grenade = grenade;
            Flare = flare;
            SlowMo = slowMo;
            MaxHitPoints = maxHitPoints;
            AuraRange = auraRange;
        }

        /// <summary>The carried Primaries' numbers, in switching order.</summary>
        public IReadOnlyList<WeaponRunStats> Primaries => primaries;

        public GrenadeRunStats Grenade { get; }

        /// <summary>How long the Flare burns and its cooldown.</summary>
        public SkillTiming Flare { get; }

        /// <summary>How long Slow-mo lasts and its cooldown.</summary>
        public SkillTiming SlowMo { get; }

        /// <summary>Hit points the character starts the Run with.</summary>
        public int MaxHitPoints { get; }

        /// <summary>Metres the Aura's light reaches.</summary>
        public float AuraRange { get; }
    }
}
