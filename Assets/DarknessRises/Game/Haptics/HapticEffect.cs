namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// Every vibration the game can play. Each has an authored <see cref="HapticPattern"/> on the
    /// <see cref="HapticsDefinition"/>; whoever owns the moment asks for the effect by name, never for a duration.
    /// </summary>
    public enum HapticEffect
    {
        // In a Level.
        GrenadeExplosion,
        EnemyHit,
        EnemyKilled,
        BossWindUp,
        BossStomp,
        BossDefeated,
        Hurt,
        HeartbeatSlow,
        HeartbeatFast,
        Death,
        ComboUp,
        ReloadDone,
        WeaponSwitch,
        RanDry,
        Flare,
        Adrenaline,
        LureThrow,
        LureLand,
        LightningNear,
        StationClear,
        StationClearS,
        ScoreBoost,
        NewBest,
        CreditsLanded,
        AdBonus,

        // Menus and the Shop.
        Bought,
        WeaponUnlocked,
        CannotAfford,
        ButtonTap,
    }
}
