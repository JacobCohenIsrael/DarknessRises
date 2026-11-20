using System;

namespace DarknessRises.Game.Storage
{
    /// <summary>
    /// Device settings kept in a first place, with a second to fall back on: the browser's storage first and
    /// <c>PlayerPrefs</c> second, on the Web player. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A setting is read from the first place. If it isn't there but the fallback has it (a build from before the
    /// settings moved, or a time the first place was full), it's read from the fallback and carried over to the first.
    /// </para>
    /// <para>
    /// A setting is set in the first place. If that fails (storage blocked or full) it's set in the fallback instead,
    /// and the first place's older value is removed, so the newer one is the one read next time.
    /// </para>
    /// <para>
    /// The fallback is flushed only when something was changed in it since the last flush: on the Web player that
    /// flush syncs the file system, which there is no reason to do while every setting goes to the first place.
    /// </para>
    /// </remarks>
    public sealed class FallbackDeviceSettings : IDeviceSettings
    {
        private readonly IDeviceSettings first;
        private readonly IDeviceSettings fallback;

        // Whether the fallback holds a change it hasn't flushed.
        private bool isFallbackChanged;

        public FallbackDeviceSettings(IDeviceSettings first, IDeviceSettings fallback)
        {
            this.first = first ?? throw new ArgumentNullException(nameof(first));
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public bool TryGetInt(string name, out int value)
        {
            if (first.TryGetInt(name, out value))
            {
                return true;
            }

            if (!fallback.TryGetInt(name, out value))
            {
                return false;
            }

            first.SetInt(name, value);
            return true;
        }

        public bool TryGetFloat(string name, out float value)
        {
            if (first.TryGetFloat(name, out value))
            {
                return true;
            }

            if (!fallback.TryGetFloat(name, out value))
            {
                return false;
            }

            first.SetFloat(name, value);
            return true;
        }

        public bool SetInt(string name, int value)
        {
            if (first.SetInt(name, value))
            {
                return true;
            }

            if (!fallback.SetInt(name, value))
            {
                return false;
            }

            isFallbackChanged = true;
            first.Remove(name);
            return true;
        }

        public bool SetFloat(string name, float value)
        {
            if (first.SetFloat(name, value))
            {
                return true;
            }

            if (!fallback.SetFloat(name, value))
            {
                return false;
            }

            isFallbackChanged = true;
            first.Remove(name);
            return true;
        }

        public void Remove(string name)
        {
            first.Remove(name);
            fallback.Remove(name);
            isFallbackChanged = true;
        }

        public void Flush()
        {
            first.Flush();
            if (isFallbackChanged)
            {
                fallback.Flush();
                isFallbackChanged = false;
            }
        }
    }
}
