namespace DarknessRises.Game.Haptics
{
    /// <summary>Picks the vibration motor of the platform the game runs on: the one platform check for Haptics.</summary>
    public static class HapticDevices
    {
        /// <summary>
        /// Android: the system vibrator. The Web player: <c>navigator.vibrate</c>, where the browser has it and the screen
        /// is touched. The Editor: a silent motor that claims it can vibrate, so the dev log shows what would play.
        /// Everywhere else (the desktop player, iOS for now): a silent one that can't.
        /// </summary>
        public static IHapticDevice OfThisPlayer()
        {
#if UNITY_EDITOR
            return new SilentHapticDevice(true);
#elif UNITY_ANDROID
            return AndroidHapticDevice.TryCreate() ?? (IHapticDevice)new HandheldHapticDevice();
#elif UNITY_WEBGL
            return WebHapticDevice.TryCreate() ?? (IHapticDevice)new SilentHapticDevice(false);
#else
            return new SilentHapticDevice(false);
#endif
        }
    }
}
