using UnityEngine;

namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// The device settings in <see cref="PlayerPrefs"/>: where they live on a phone, a desktop and in the Editor, under
    /// the names they have always had. A thin adapter.
    /// </summary>
    public sealed class PlayerPrefsDeviceSettings : IDeviceSettings
    {
        public bool TryGetInt(string name, out int value)
        {
            if (!PlayerPrefs.HasKey(name))
            {
                value = 0;
                return false;
            }

            value = PlayerPrefs.GetInt(name);
            return true;
        }

        public bool TryGetFloat(string name, out float value)
        {
            if (!PlayerPrefs.HasKey(name))
            {
                value = 0f;
                return false;
            }

            value = PlayerPrefs.GetFloat(name);
            return true;
        }

        public bool SetInt(string name, int value)
        {
            PlayerPrefs.SetInt(name, value);
            return true;
        }

        public bool SetFloat(string name, float value)
        {
            PlayerPrefs.SetFloat(name, value);
            return true;
        }

        public void Remove(string name) => PlayerPrefs.DeleteKey(name);

        // On the Web player this also syncs the player's files to the browser: PlayerPrefs is one of them.
        public void Flush() => PlayerPrefs.Save();
    }
}
