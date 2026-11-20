namespace DarknessRises.Game.Upgrades
{
    /// <summary>
    /// The number an Upgrade sets. Saved nowhere (an Upgrade is saved by its id), so entries can be added freely; an
    /// authored Upgrade stores the entry's number, so never reorder or renumber them.
    /// </summary>
    public enum UpgradeStat
    {
        /// <summary>Damage one Shot of a Primary weapon deals.</summary>
        Damage = 0,

        /// <summary>Shots in a Primary weapon's full magazine.</summary>
        MagazineSize = 1,

        /// <summary>Seconds a Primary weapon's reload takes.</summary>
        ReloadSeconds = 2,

        /// <summary>Shots per second a Primary weapon fires while the trigger is held.</summary>
        ShotsPerSecond = 3,

        /// <summary>Rounds a Primary weapon's full ammo reserve holds. Only for a Weapon whose ammo isn't unlimited.</summary>
        AmmoReserve = 4,

        /// <summary>Grenades the character carries.</summary>
        GrenadeCharges = 10,

        /// <summary>Seconds for one missing Grenade charge to refill.</summary>
        GrenadeRefillSeconds = 11,

        /// <summary>Metres from a Grenade's explosion within which Enemies are damaged.</summary>
        GrenadeBlastRadius = 12,

        /// <summary>Seconds the Flare burns.</summary>
        FlareDurationSeconds = 20,

        /// <summary>Seconds from the end of the Flare until it can be used again.</summary>
        FlareCooldownSeconds = 21,

        /// <summary>Seconds Enemies stay slowed by Slow-mo.</summary>
        SlowMoDurationSeconds = 22,

        /// <summary>Seconds from the end of Slow-mo until it can be used again.</summary>
        SlowMoCooldownSeconds = 23,

        /// <summary>Hit points the character starts a Run with.</summary>
        MaxHitPoints = 30,

        /// <summary>Metres the Aura's light reaches.</summary>
        AuraRange = 31,
    }
}
