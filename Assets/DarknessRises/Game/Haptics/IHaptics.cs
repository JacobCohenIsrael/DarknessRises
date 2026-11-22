namespace DarknessRises.Game.Haptics
{
    /// <summary>
    /// What the owners of the game's moments call to make the phone vibrate: one call per authored effect. Whether it
    /// actually plays (the VIBRATION switch, the pause, the rate limits, a stronger effect playing) isn't theirs to know.
    /// </summary>
    public interface IHaptics
    {
        /// <summary>Plays <paramref name="effect"/> at full strength, if the rules let it.</summary>
        void Play(HapticEffect effect);

        /// <summary>Plays <paramref name="effect"/> at <paramref name="strength"/> (0 to 1), if the rules let it.</summary>
        void Play(HapticEffect effect, float strength);

        /// <summary>
        /// Holds every effect for <paramref name="hold"/> while <paramref name="held"/> (the pause, an ad, the loading
        /// screen), and stops the one that is playing. Each reason is held and let go on its own.
        /// </summary>
        void SetHeld(HapticsHold hold, bool held);
    }
}
