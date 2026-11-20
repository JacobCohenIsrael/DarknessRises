using System;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// A number counting up arcade-style: from where it stands to a target over a set time, fast at first and slowing as
    /// it lands. It reports each whole number once, so whoever shows it writes text only when the number changes.
    /// Plain C#: time arrives as an explicit delta (ADR 0003).
    /// </summary>
    public sealed class CountUp
    {
        private int from;
        private float seconds;
        private float elapsedSeconds;

        /// <summary>Raised when the number showing changes, with the new one.</summary>
        public event Action<int> ValueChanged;

        /// <summary>The number showing now.</summary>
        public int Value { get; private set; }

        /// <summary>The number it counts to.</summary>
        public int Target { get; private set; }

        public bool IsCounting => Value != Target;

        /// <summary>Shows <paramref name="value"/> at once, with no counting.</summary>
        public void Set(int value)
        {
            from = Target = value;
            seconds = elapsedSeconds = 0f;
            Show(value);
        }

        /// <summary>Counts from the number showing to <paramref name="value"/> over <paramref name="overSeconds"/>.</summary>
        public void CountTo(int value, float overSeconds)
        {
            if (overSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(overSeconds), overSeconds, "Counting can't take negative time.");
            }

            from = Value;
            Target = value;
            seconds = overSeconds;
            elapsedSeconds = 0f;
            if (overSeconds <= 0f)
            {
                Show(value);
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time can't run backwards.");
            }

            if (!IsCounting)
            {
                return;
            }

            elapsedSeconds += deltaSeconds;
            if (elapsedSeconds >= seconds)
            {
                Show(Target);
                return;
            }

            // Ease out: most of the way in the first half, then settling onto the target.
            var left = 1f - elapsedSeconds / seconds;
            var progress = 1f - left * left;
            Show(from + (int)((Target - from) * (double)progress));
        }

        private void Show(int value)
        {
            if (value == Value)
            {
                return;
            }

            Value = value;
            ValueChanged?.Invoke(value);
        }
    }
}
