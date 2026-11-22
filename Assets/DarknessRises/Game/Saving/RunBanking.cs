using System;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// Banks every Run's Credits and its Score in the <see cref="PlayerSave"/>, so none are ever lost: when a Run ends,
    /// whichever way (Exit, the Continue countdown running out, Restart from the pause menu), and when the app goes to
    /// the background or quits with a Run still on. A Run that ends is compared with the best Score of the Runs before
    /// it, so the end screen can tell a new best. Plain C# (ADR 0003). Whoever owns the Run and the save owns this.
    /// </summary>
    public sealed class RunBanking : IDisposable
    {
        private readonly RunFlow run;
        private readonly PlayerSave save;

        // The best Score before the current Run: what it's compared with when it ends. BankNow may already have kept the
        // Run's Score as the best by then.
        private int bestBeforeRun;

        public RunBanking(RunFlow runFlow, PlayerSave playerSave)
        {
            run = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
            bestBeforeRun = save.BestScore;
            run.RunEnded += OnRunEnded;
        }

        /// <summary>
        /// The app is going to the background or quitting: banks what the current Run has earned so far and writes the
        /// save. If the Run carries on afterwards, only what it earns from here is banked later.
        /// </summary>
        public void BankNow()
        {
            if (run.IsInRun)
            {
                save.KeepScore(run.Record.Score);
            }

            save.Bank(run.Record.Credits);
        }

        public void Dispose()
        {
            run.RunEnded -= OnRunEnded;
        }

        private void OnRunEnded(RunRecord record)
        {
            record.CompareWithBest(Math.Min(bestBeforeRun, save.BestScore));
            save.KeepScore(record.Score);
            save.FinishRun(record.Credits);
            bestBeforeRun = save.BestScore;
        }
    }
}
