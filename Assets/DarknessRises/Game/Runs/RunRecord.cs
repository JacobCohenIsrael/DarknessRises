namespace DarknessRises.Game.Runs
{
    /// <summary>
    /// What the end screen shows about a Run: the Level reached and the Enemies killed. There is no score.
    /// Plain C# (ADR 0003). The Run flow ticket moves the Level on; for now every Run stays on the first Level.
    /// </summary>
    public sealed class RunRecord
    {
        public const int FirstLevel = 1;

        public int LevelReached { get; private set; } = FirstLevel;

        public int EnemiesKilled { get; private set; }

        public void CountKill()
        {
            EnemiesKilled++;
        }
    }
}
