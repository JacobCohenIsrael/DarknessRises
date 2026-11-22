namespace DarknessRises.Game.Enemies
{
    /// <summary>
    /// What one Enemy is going for: the character, or a Lure that drew it. Plain C# (ADR 0003). Pooled with its Enemy,
    /// so <see cref="Reset"/> starts it over on the character.
    /// </summary>
    /// <remarks>
    /// Fairness: an Enemy only changes what it goes for while it's chasing. One already winding up at the character
    /// finishes that attack on the character (it may still land) and turns to the Lure once it's chasing again; one that
    /// was going for a Lure that has ended finishes its swing at nothing, then comes back. So the Lure never makes a
    /// lunge in progress miss, and never makes one land.
    /// </remarks>
    public sealed class EnemyQuarry
    {
        /// <summary>Whether it's going for a Lure right now, not the character.</summary>
        public bool IsLured { get; private set; }

        /// <summary>Whether a Lure is drawing it: it goes for the Lure as soon as it's chasing.</summary>
        public bool IsDrawn { get; private set; }

        /// <summary>A Lure has drawn it.</summary>
        public void Draw()
        {
            IsDrawn = true;
        }

        /// <summary>The Lure that drew it ended: it goes back to the character as soon as it's chasing.</summary>
        public void Release()
        {
            IsDrawn = false;
        }

        /// <summary>
        /// Takes up what it's drawn to, if <paramref name="state"/> lets it: only while chasing. Returns whether what it goes
        /// for changed, so the view can steer it there.
        /// </summary>
        public bool Settle(EnemyBehaviourState state)
        {
            if (state != EnemyBehaviourState.Chasing || IsLured == IsDrawn)
            {
                return false;
            }

            IsLured = IsDrawn;
            return true;
        }

        /// <summary>A freshly spawned Enemy goes for the character.</summary>
        public void Reset()
        {
            IsLured = false;
            IsDrawn = false;
        }
    }
}
