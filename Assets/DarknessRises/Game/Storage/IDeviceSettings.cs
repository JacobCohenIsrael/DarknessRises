namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// The one place for what belongs to the device rather than to the player's progress: the volume sliders, where
    /// the Grenade button was dropped, whether the Shoot hint was shown. Small numbers under a name, kept apart from
    /// the save (ADR 0009). On a phone or a desktop they live in <c>PlayerPrefs</c>; on the Web player, in the
    /// browser's <c>localStorage</c>, so a new upload of the game still has them.
    /// </summary>
    public interface IDeviceSettings
    {
        /// <summary>The whole number kept under <paramref name="name"/>, if one was ever kept.</summary>
        bool TryGetInt(string name, out int value);

        /// <summary>The number kept under <paramref name="name"/>, if one was ever kept.</summary>
        bool TryGetFloat(string name, out float value);

        /// <summary>Keeps <paramref name="value"/> under <paramref name="name"/>. False when it couldn't be kept.</summary>
        bool SetInt(string name, int value);

        /// <summary>Keeps <paramref name="value"/> under <paramref name="name"/>. False when it couldn't be kept.</summary>
        bool SetFloat(string name, float value);

        /// <summary>Forgets what is kept under <paramref name="name"/>, if anything.</summary>
        void Remove(string name);

        /// <summary>Puts on disk what was set, where setting alone leaves it in memory.</summary>
        void Flush();
    }
}
