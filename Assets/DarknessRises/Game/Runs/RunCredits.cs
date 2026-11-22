using System;
using DarknessRises.Game.Credits;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// The Credits one Run earned and how many of them have been banked so far. They come from the Run's Score, converted
    /// at an authored rate (<see cref="ScoreToCredits"/>), the flat bonus of every Level it cleared, and, once the Run has
    /// ended, the bonus of its end-of-Run ad, which multiplies those two.
    /// Plain C# (ADR 0003). The <see cref="RunRecord"/> holds one per Run and hands it every point of Score.
    /// </summary>
    /// <remarks>
    /// Banking can happen more than once (when the app goes to the background mid-Run, when the Run ends, after the ad): each time
    /// only what hasn't been banked yet is taken, so no Credit is banked twice and none is left behind. The Score is
    /// converted as a whole each time, so the points short of a Credit at one banking count towards the next.
    /// </remarks>
    public sealed class RunCredits
    {
        private ScoreToCredits conversion;

        /// <summary>A Run whose every point of Score is one Credit. Tests and tools use it to earn exact amounts.</summary>
        public RunCredits()
            : this(ScoreToCredits.OnePerPoint)
        {
        }

        /// <param name="scoreToCredits">How the Run's Score pays Credits.</param>
        public RunCredits(ScoreToCredits scoreToCredits)
        {
            conversion = scoreToCredits;
        }

        /// <summary>Raised whenever <see cref="Total"/> changes, with the new total.</summary>
        public event Action<int> Changed;

        /// <summary>The Score that pays one Credit in this Run.</summary>
        public int ScorePerCredit => conversion.ScorePerCredit;

        /// <summary>The Run's Score so far: what <see cref="FromScore"/> is converted from.</summary>
        public int Score { get; private set; }

        /// <summary>The Credits the Run's Score pays: its whole Score so far, converted and rounded down.</summary>
        public int FromScore => conversion.CreditsFor(Score);

        /// <summary>The flat Credits the Run earned on top of its Score: the bonus of every Level it cleared.</summary>
        public int Earned { get; private set; }

        /// <summary>What the Run's play earned: its Score's Credits and its flat Credits. The end-of-Run ad multiplies this.</summary>
        public int Earnings => FromScore + Earned;

        /// <summary>What the end-of-Run ad added on top. 0 until it's watched.</summary>
        public int AdBonus { get; private set; }

        /// <summary>Whether the Run's end-of-Run ad has paid out. It pays once per Run.</summary>
        public bool HasAdBonus { get; private set; }

        /// <summary>Everything the Run earned: its Score's Credits, its flat Credits and the ad's bonus.</summary>
        public int Total => Earnings + AdBonus;

        /// <summary>How many of the Run's Credits are in the bank already.</summary>
        public int Banked { get; private set; }

        /// <summary>How many of the Run's Credits still wait to be banked.</summary>
        public int Unbanked => Total - Banked;

        /// <summary>
        /// Converts the Run's Score at <paramref name="rule"/> from now on. Only while the Score is 0 (or the rate is the
        /// same), so Credits already counted, or banked, never change under a new rate.
        /// </summary>
        /// <returns>Whether the Run converts at <paramref name="rule"/>: false when it has scored at another rate.</returns>
        public bool ConvertAt(ScoreToCredits rule)
        {
            if (rule.ScorePerCredit == conversion.ScorePerCredit)
            {
                return true;
            }

            if (Score > 0)
            {
                return false;
            }

            conversion = rule;
            return true;
        }

        /// <summary>The Run scored <paramref name="points"/> more: they count towards its Credits.</summary>
        public void AddScore(int points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), points, "Score is only ever earned.");
            }

            if (points == 0)
            {
                return;
            }

            var before = FromScore;
            Score = (int)Math.Min(int.MaxValue, (long)Score + points);
            if (FromScore != before)
            {
                Changed?.Invoke(Total);
            }
        }

        /// <summary>A cleared Level paid <paramref name="credits"/> flat Credits, whatever the Score.</summary>
        public void Earn(int credits)
        {
            if (credits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(credits), credits, "Credits are only ever earned.");
            }

            if (credits == 0)
            {
                return;
            }

            Earned += credits;
            Changed?.Invoke(Total);
        }

        /// <summary>The end-of-Run ad was watched: it adds <paramref name="credits"/> on top. Once per Run.</summary>
        /// <returns>Whether the bonus was added: false when this Run's ad has already paid out.</returns>
        public bool AddAdBonus(int credits)
        {
            if (credits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(credits), credits, "An ad never takes Credits away.");
            }

            if (HasAdBonus)
            {
                return false;
            }

            HasAdBonus = true;
            AdBonus = credits;
            if (credits > 0)
            {
                Changed?.Invoke(Total);
            }

            return true;
        }

        /// <summary>Hands over the Credits that haven't been banked yet and counts them as banked.</summary>
        public int TakeUnbanked()
        {
            var taken = Unbanked;
            Banked += taken;
            return taken;
        }
    }
}
