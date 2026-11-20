using System;
using DarknessRises.Game.Runs;

namespace DarknessRises.Game.Saving
{
    /// <summary>
    /// Banks every Run's Credits in the <see cref="PlayerSave"/>, so none are ever lost: when a Run ends, whichever way
    /// (Exit, the Continue countdown running out, Restart from the pause menu), and when the app goes to the background
    /// or quits with a Run still on. Plain C# (ADR 0003). Whoever owns the Run and the save owns this.
    /// </summary>
    public sealed class RunBanking : IDisposable
    {
        private readonly RunFlow run;
        private readonly PlayerSave save;

        public RunBanking(RunFlow runFlow, PlayerSave playerSave)
        {
            run = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            save = playerSave ?? throw new ArgumentNullException(nameof(playerSave));
            run.RunEnded += OnRunEnded;
        }

        /// <summary>
        /// The app is going to the background or quitting: banks what the current Run has earned so far and writes the
        /// save. If the Run carries on afterwards, only what it earns from here is banked later.
        /// </summary>
        public void BankNow()
        {
            save.Bank(run.Record.Credits);
        }

        public void Dispose()
        {
            run.RunEnded -= OnRunEnded;
        }

        private void OnRunEnded(RunRecord record)
        {
            save.FinishRun(record.Credits);
        }
    }
}
