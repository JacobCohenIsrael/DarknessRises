using System;
using System.Collections.Generic;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// What a scene's sources were doing as it unloaded: each one still playing above a threshold stops dead there, which
    /// is heard as a cut (issue #121). The boot runs it on every scene it unloads in the Editor and development builds,
    /// and logs a warning for each one, so a sound that skips its scene's fade can't come back unnoticed.
    /// Plain C# (ADR 0003).
    /// </summary>
    public static class SceneUnloadAudit
    {
        /// <summary>One source as its scene unloads.</summary>
        public readonly struct Reading
        {
            public Reading(string name, bool isPlaying, float volume)
            {
                Name = name;
                IsPlaying = isPlaying;
                Volume = volume;
            }

            /// <summary>Where the source is, for the log.</summary>
            public string Name { get; }

            /// <summary>Whether it is playing.</summary>
            public bool IsPlaying { get; }

            /// <summary>Its effective volume: its own, times its category's gain and its scene's fade.</summary>
            public float Volume { get; }
        }

        /// <summary>
        /// Adds to <paramref name="cut"/> every reading still playing louder than <paramref name="threshold"/>.
        /// </summary>
        /// <returns>How many were added.</returns>
        public static int FindCuts(IReadOnlyList<Reading> readings, float threshold, ICollection<Reading> cut)
        {
            if (readings == null)
            {
                throw new ArgumentNullException(nameof(readings));
            }

            if (cut == null)
            {
                throw new ArgumentNullException(nameof(cut));
            }

            var found = 0;
            for (var i = 0; i < readings.Count; i++)
            {
                var reading = readings[i];
                if (reading.IsPlaying && reading.Volume > threshold)
                {
                    cut.Add(reading);
                    found++;
                }
            }

            return found;
        }
    }
}
