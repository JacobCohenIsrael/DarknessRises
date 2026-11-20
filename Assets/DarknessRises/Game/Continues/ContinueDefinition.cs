using UnityEngine;

namespace DarknessRises.Game.Continues
{
    /// <summary>
    /// Authored tuning for Continue: how often a Run allows it, the ad the player watches for it, and the burst of light
    /// that pushes nearby Enemies back as the character comes back. Never changed at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkness Rises/Continue Definition", fileName = "ContinueDefinition", order = 90)]
    public sealed class ContinueDefinition : ScriptableObject
    {
        [SerializeField, Min(0), Tooltip("How many times a Run lets the character Continue.")]
        private int usesPerRun = 1;

        [SerializeField, Min(0.1f), Tooltip("Seconds the fake ad's placeholder screen shows before it reports the ad as watched.")]
        private float fakeAdSeconds = 5f;

        [Header("Death screen")]
        [SerializeField, Tooltip("The death screen's title while a Continue is on offer, arcade style.")]
        private string continueTitle = "CONTINUE?";

        [SerializeField, Tooltip("The death screen's title once the Run has no Continue left.")]
        private string gameOverTitle = "GAME OVER";

        [SerializeField, Min(0), Tooltip(
            "The number the Continue countdown starts at. It counts down to 0, and when 0 has had its turn the Run ends as if the player took Exit.")]
        private int countdownFrom = 10;

        [SerializeField, Min(0.1f), Tooltip("Seconds each number of the countdown stays up.")]
        private float countdownSecondsPerCount = 1f;

        [SerializeField, Min(0f), Tooltip("Seconds the character can't be hit after it comes back.")]
        private float invulnerabilitySeconds = 2f;

        [SerializeField, Range(0f, 1f), Tooltip(
            "The share of a full ammo reserve every Primary weapon gets back on a Continue. 1 refills them all: the ad buys a fresh start.")]
        private float ammoRefillShare = 1f;

        [Header("Push-back")]
        [SerializeField, Min(0.1f), Tooltip("Metres around the character the burst clears: every Enemy inside is pushed out to this distance.")]
        private float pushBackRadius = 5f;

        [SerializeField, Min(0.1f), Tooltip("How hard Enemies are pushed: the metres per second they slide back at until they reach the radius.")]
        private float pushBackForce = 14f;

        [Header("Burst of light")]
        [SerializeField] private Color lightColor = new(0.85f, 0.92f, 1f);

        [SerializeField, Min(0f), Tooltip("URP point light intensity at the peak of the burst.")]
        private float lightIntensity = 120f;

        [SerializeField, Min(0f), Tooltip("Metres the light reaches. Wider than the push-back, so the player sees the Enemies it pushed.")]
        private float lightRange = 12f;

        [SerializeField, Min(0.01f), Tooltip("Seconds the light lasts, fading out from its peak.")]
        private float lightSeconds = 1.4f;

        [SerializeField, Min(0f), Tooltip("Metres above the floor the light sits.")]
        private float lightHeight = 1.5f;

        public int UsesPerRun => usesPerRun;
        public float FakeAdSeconds => fakeAdSeconds;
        public string ContinueTitle => continueTitle;
        public string GameOverTitle => gameOverTitle;
        public int CountdownFrom => countdownFrom;
        public float CountdownSecondsPerCount => countdownSecondsPerCount;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public float AmmoRefillShare => ammoRefillShare;
        public float PushBackRadius => pushBackRadius;
        public float PushBackForce => pushBackForce;
        public Color LightColor => lightColor;
        public float LightIntensity => lightIntensity;
        public float LightRange => lightRange;
        public float LightSeconds => lightSeconds;
        public float LightHeight => lightHeight;
    }
}
