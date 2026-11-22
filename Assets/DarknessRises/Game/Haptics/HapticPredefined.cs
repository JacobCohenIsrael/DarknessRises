namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// The phone's own tuned click a pattern may use instead of its pulses, where the phone has one (Android 10 and
    /// later: <c>VibrationEffect.createPredefined</c>). These feel crisper than a short pulse on good motors.
    /// </summary>
    public enum HapticPredefined
    {
        /// <summary>The pattern's own pulses, everywhere.</summary>
        None = -1,

        /// <summary><c>EFFECT_CLICK</c>.</summary>
        Click = 0,

        /// <summary><c>EFFECT_DOUBLE_CLICK</c>.</summary>
        DoubleClick = 1,

        /// <summary><c>EFFECT_TICK</c>: the lightest.</summary>
        Tick = 2,

        /// <summary><c>EFFECT_HEAVY_CLICK</c>.</summary>
        HeavyClick = 5,
    }
}
