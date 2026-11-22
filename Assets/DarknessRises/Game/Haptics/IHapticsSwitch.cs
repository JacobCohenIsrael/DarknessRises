namespace DarknessRises.Game.Haptics
{
    /// <summary>The VIBRATION switch in the Volume panel: on or off, kept in the device settings.</summary>
    public interface IHapticsSwitch
    {
        /// <summary>Whether this device can vibrate at all. Where it can't, the switch isn't shown.</summary>
        bool IsAvailable { get; }

        /// <summary>Whether Haptics are on.</summary>
        bool IsOn { get; }

        /// <summary>Turns Haptics on or off and keeps the choice on the device. Off stops what is playing.</summary>
        void SetOn(bool on);
    }
}
