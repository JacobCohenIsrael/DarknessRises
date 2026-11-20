using UnityEngine;

namespace DarknessRises.Game.Credits
{
    /// <summary>
    /// Authored tuning for Credits: what a cleared Level pays on top of its kills, the end-of-Run ad that multiplies a
    /// Run's Credits, and how the end screen shows them. What a kill pays is authored on each Enemy type and Boss.
    /// Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Credits Definition", fileName = "CreditsDefinition", order = 95)]
    public sealed class CreditsDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("The Credits a cleared Level (its Boss beaten) pays on top of its kills, growing with the Level number.")]
        private LevelClearBonusSettings levelClearBonus = new();

        [SerializeField, Tooltip("The one rewarded ad the end screen offers, which multiplies the Credits the Run earned.")]
        private EndOfRunAdSettings endOfRunAd = new();

        [SerializeField, Tooltip("How the end screen shows the Credits a Run earned and the new total.")]
        private CreditsEndScreenSettings endScreen = new();

        public LevelClearBonusSettings LevelClearBonus => levelClearBonus;
        public EndOfRunAdSettings EndOfRunAd => endOfRunAd;
        public CreditsEndScreenSettings EndScreen => endScreen;
    }
}
