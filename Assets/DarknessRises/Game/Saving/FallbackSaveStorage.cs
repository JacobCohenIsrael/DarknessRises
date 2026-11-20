using System;
using UnityEngine;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// The save kept in a first place, with a second to fall back on: on the Web player, the browser's storage first
    /// and the player's own file second. Plain C# (ADR 0003).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The save is read from the first place. If nothing is there but the fallback has a save (a build from before the
    /// save moved, played at this same address, or a time the first place couldn't be written), that save is read and
    /// carried over to the first place at once. The fallback keeps its copy.
    /// </para>
    /// <para>
    /// The save is written to the first place. If that fails (storage blocked, or full) it's written to the fallback
    /// instead, and the first place's older save is removed, so the newer save is the one read next time. If the
    /// fallback fails too, nothing is removed: an older save is better than none.
    /// </para>
    /// </remarks>
    public sealed class FallbackSaveStorage : ISaveStorage
    {
        private readonly ISaveStorage first;
        private readonly ISaveStorage fallback;

        public FallbackSaveStorage(ISaveStorage first, ISaveStorage fallback)
        {
            this.first = first ?? throw new ArgumentNullException(nameof(first));
            this.fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        /// <summary>Whether the last save written went to the fallback because the first place couldn't take it.</summary>
        public bool IsOnFallback { get; private set; }

        public bool TryRead(out string text)
        {
            if (first.TryRead(out text))
            {
                return true;
            }

            if (!fallback.TryRead(out text))
            {
                return false;
            }

            // Carried over. If the first place can't take it, the fallback still has it for next time.
            NoteWhereTheSaveIs(!first.TryWrite(text));
            return true;
        }

        public bool TryWrite(string text)
        {
            if (first.TryWrite(text))
            {
                NoteWhereTheSaveIs(false);
                return true;
            }

            if (!fallback.TryWrite(text))
            {
                return false;
            }

            first.Delete();
            NoteWhereTheSaveIs(true);
            return true;
        }

        public void Delete()
        {
            first.Delete();
            fallback.Delete();
            IsOnFallback = false;
        }

        // Says so once each time the save ends up on the fallback, for a bug report's log.
        private void NoteWhereTheSaveIs(bool isOnFallback)
        {
            if (isOnFallback && !IsOnFallback)
            {
                Debug.LogWarning("[Save] The first storage wouldn't take the save, so it's kept in the fallback storage.");
            }

            IsOnFallback = isOnFallback;
        }
    }
}
