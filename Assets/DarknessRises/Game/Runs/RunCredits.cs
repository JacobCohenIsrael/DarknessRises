using System;

namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// The Credits one Run earned: what its kills and cleared Levels paid, the bonus of its end-of-Run ad, and how many of
    /// them have been banked so far. Credits are flat income, never multiplied by the Combo: they aren't the Score.
    /// Plain C# (ADR 0003). The <see cref="RunRecord"/> holds one per Run.
    /// </summary>
    /// <remarks>
    /// Banking can happen more than once (when the app goes to the background mid-Run, when the Run ends, after the ad):
    /// each time only what hasn't been banked yet is taken, so no Credit is banked twice and none is left behind.
    /// </remarks>
    public sealed class RunCredits
    {
        /// <summary>Raised whenever <see cref="Total"/> changes, with the new total.</summary>
        public event Action<int> Changed;

        /// <summary>What the Run's kills and cleared Levels earned. The end-of-Run ad multiplies this.</summary>
        public int Earned { get; private set; }

        /// <summary>What the end-of-Run ad added on top. 0 until it's watched.</summary>
        public int AdBonus { get; private set; }

        /// <summary>Whether the Run's end-of-Run ad has paid out. It pays once per Run.</summary>
        public bool HasAdBonus { get; private set; }

        /// <summary>Everything the Run earned, ad bonus included.</summary>
        public int Total => Earned + AdBonus;

        /// <summary>How many of the Run's Credits are in the bank already.</summary>
        public int Banked { get; private set; }

        /// <summary>How many of the Run's Credits still wait to be banked.</summary>
        public int Unbanked => Total - Banked;

        /// <summary>A kill or a cleared Level paid <paramref name="credits"/>.</summary>
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
