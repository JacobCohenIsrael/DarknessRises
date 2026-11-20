using System;
using DarknessRises.Game.Randomness;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// Picks which of the Phantom eyes' voices (a whisper, a breath, a hush) plays next: at random, but never the same
    /// one twice in a row, so the dark doesn't sound like a loop. Plain C# (ADR 0003).
    /// </summary>
    public sealed class PhantomVoicePicker
    {
        private readonly IRandom random;

        public PhantomVoicePicker(IRandom random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>The index picked last; -1 before the first.</summary>
        public int Last { get; private set; } = -1;

        /// <summary>The index of the next of <paramref name="count"/> voices; -1 when there are none.</summary>
        public int Pick(int count)
        {
            if (count <= 0)
            {
                return -1;
            }

            if (count == 1)
            {
                return Last = 0;
            }

            if (Last < 0 || Last >= count)
            {
                return Last = Index(count);
            }

            // One of the others: pick among count - 1, then step over the last.
            var index = Index(count - 1);
            return Last = index >= Last ? index + 1 : index;
        }

        private int Index(int count)
        {
            return Math.Min((int)random.Range(0f, count), count - 1);
        }
    }
}
