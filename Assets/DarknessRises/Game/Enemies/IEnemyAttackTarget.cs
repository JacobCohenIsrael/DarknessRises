namespace DarknessRises.Game.Enemies
{
    /// <summary>Something Enemies hunt and attack: the character. Sits on the Transform the Enemies are spawned hunting.</summary>
    public interface IEnemyAttackTarget
    {
        /// <summary>False once it's dead, so Enemies stop winding up at it.</summary>
        bool CanBeAttacked { get; }

        /// <summary>A landed attack, taking <paramref name="damage"/> hit points.</summary>
        void TakeAttack(int damage);
    }
}
