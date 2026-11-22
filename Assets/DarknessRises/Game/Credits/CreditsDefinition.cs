using UnityEngine;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// Authored tuning for Credits: the rate a Run's Score pays them at, the flat bonus a cleared Level pays on top, and
    /// how the end screen pays them out. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Credits Definition", fileName = "CreditsDefinition", order = 95)]
    public sealed class CreditsDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Credits have their own colour everywhere they show (the end screen, the Shop), so they never read as the Score.")]
        private Color color = new(0.35f, 1f, 0.78f, 1f);

        [SerializeField, Tooltip("How a Run's Score pays Credits. Doubling the Score (the x2 Score boost) doubles them too.")]
        private ScoreToCreditsSettings scoreToCredits = new();

        [SerializeField, Tooltip("The flat Credits a cleared Level (its Boss beaten) pays on top of the Score's, growing with the Level number, so surviving pays even with poor aim.")]
        private LevelClearBonusSettings levelClearBonus = new();

        [SerializeField, Tooltip("How the end screen pays out the Run's Score as Credits and shows the new total.")]
        private CreditsEndScreenSettings endScreen = new();

        public Color Color => color;
        public ScoreToCreditsSettings ScoreToCredits => scoreToCredits;
        public LevelClearBonusSettings LevelClearBonus => levelClearBonus;
        public CreditsEndScreenSettings EndScreen => endScreen;
    }
}
