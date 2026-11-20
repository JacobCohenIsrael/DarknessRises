using System;

namespace DarknessRises.Game.Enemies.Tells
{
    /// <summary>One Enemy, as the footstep budget sees it: how far it is from the character and whether it walks.</summary>
    public readonly struct FootstepCandidate
    {
        public FootstepCandidate(float sqrDistance, bool isWalking, float hearingRange)
        {
            SqrDistance = sqrDistance;
            IsWalking = isWalking;
            SqrHearingRange = hearingRange * hearingRange;
        }

        public float SqrDistance { get; }
        public bool IsWalking { get; }
        public float SqrHearingRange { get; }

        public bool CanBeHeard => IsWalking && SqrDistance <= SqrHearingRange;
    }

    /// <summary>
    /// Which Enemies' footsteps are heard: only the nearest few that walk within their hearing range, so a crowd never
    /// takes more than a small, fixed budget of voices on a phone. Nearer wins; at equal distance the earlier one does.
    /// Plain C# (ADR 0003), allocation free once made.
    /// </summary>
    public sealed class FootstepVoicePicker
    {
        private readonly int[] nearest;
        private readonly float[] nearestSqrDistance;

        /// <param name="maxHeard">How many Enemies' footsteps can be heard at once. 0 silences them all.</param>
        public FootstepVoicePicker(int maxHeard)
        {
            if (maxHeard < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHeard), maxHeard, "The budget can't be negative.");
            }

            nearest = new int[maxHeard];
            nearestSqrDistance = new float[maxHeard];
        }

        public int MaxHeard => nearest.Length;

        /// <summary>
        /// Sets <paramref name="heard"/>[i] for the nearest <see cref="MaxHeard"/> candidates that can be heard, and
        /// clears it for the rest. Returns how many are heard.
        /// </summary>
        public int Pick(ReadOnlySpan<FootstepCandidate> candidates, Span<bool> heard)
        {
            if (heard.Length < candidates.Length)
            {
                throw new ArgumentException("There must be a heard flag for every candidate.", nameof(heard));
            }

            var count = 0;
            for (var i = 0; i < candidates.Length; i++)
            {
                heard[i] = false;
                var candidate = candidates[i];
                if (!candidate.CanBeHeard)
                {
                    continue;
                }

                // Insert into the nearest list, kept sorted by distance; a strictly nearer one pushes the farthest out.
                var place = count;
                while (place > 0 && candidate.SqrDistance < nearestSqrDistance[place - 1])
                {
                    place--;
                }

                if (place >= nearest.Length)
                {
                    continue;
                }

                var last = Math.Min(count, nearest.Length - 1);
                for (var j = last; j > place; j--)
                {
                    nearest[j] = nearest[j - 1];
                    nearestSqrDistance[j] = nearestSqrDistance[j - 1];
                }

                nearest[place] = i;
                nearestSqrDistance[place] = candidate.SqrDistance;
                count = Math.Min(count + 1, nearest.Length);
            }

            for (var i = 0; i < count; i++)
            {
                heard[nearest[i]] = true;
            }

            return count;
        }
    }
}
