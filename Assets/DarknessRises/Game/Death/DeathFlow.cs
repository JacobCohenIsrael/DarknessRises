using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Ads;
using DarknessRises.Game.Continues;
using DarknessRises.Game.Health;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Weapons;
using UnityEngine;

namespace DarknessRises.Game.Death
{
    /// <summary>
    /// What happens when the character dies: it stops walking and shooting, plays its death, and the death screen
    /// offers Continue (while the Run allows it, under an arcade countdown that ends the Run when it runs out),
    /// Restart or Exit. All three go through the <see cref="RunFlow"/>.
    /// Continue shows a rewarded ad; once it's watched, the character comes back where it died with full hit points,
    /// its ammo reserves refilled and a burst of light that pushes nearby Enemies back, and the Level carries on. Restart starts a new Run at once;
    /// Exit ends the Run and shows the end screen, which the player leaves for the Shop. Whoever runs the Levels (the boot)
    /// loads what comes next.
    /// </summary>
    public sealed class DeathFlow : MonoBehaviour
    {
        [SerializeField] private DeathScreenView deathScreen;
        [SerializeField] private EndScreenView endScreen;

        // Owns the death flow's lifetime: the scene's. Each death gets a scope of its own inside it.
        private CancellationTokenSource lifetime;
        private CharacterHealth health;
        private RunFlow run;
        private CharacterAnimationView characterAnimation;
        private RouteWalker routeWalker;
        private PrimaryWeaponView primaryWeapon;
        private ContinueDefinition continueDefinition;
        private IRewardedAd continueAd;
        private ContinueBurstView continueBurst;
        private bool isDeathFinished;
        private Action onDeathFinished;

        /// <summary>
        /// Waits for the character's death, then runs the death and end screens. A Continue costs
        /// <paramref name="ad"/>, and comes back with <paramref name="burst"/>, as <paramref name="continueSettings"/> says.
        /// </summary>
        public void Watch(
            CharacterHealth characterHealth,
            RunFlow runFlow,
            CharacterAnimationView animationView,
            RouteWalker walker,
            PrimaryWeaponView weapon,
            ContinueDefinition continueSettings,
            IRewardedAd ad,
            ContinueBurstView burst)
        {
            Unbind();

            health = characterHealth;
            run = runFlow;
            characterAnimation = animationView;
            routeWalker = walker;
            primaryWeapon = weapon;
            continueDefinition = continueSettings;
            continueAd = ad;
            continueBurst = burst;
            deathScreen.Use(continueSettings);
            lifetime ??= new CancellationTokenSource();
            onDeathFinished ??= OnDeathFinished;
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            Unbind();
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
        }

        private void Unbind()
        {
            if (health != null)
            {
                health.Died -= OnDied;
                health = null;
            }

            if (characterAnimation != null)
            {
                characterAnimation.DeathFinished -= onDeathFinished;
            }
        }

        private void OnDied()
        {
            run.Die();
            PlayAsync(lifetime.Token).Forget();
        }

        private async UniTaskVoid PlayAsync(CancellationToken sceneToken)
        {
            // The death screen's scope: the screen, the ad and the Continue that follows all end with it, or with the scene.
            using var deathScope = CancellationTokenSource.CreateLinkedTokenSource(sceneToken);
            var cancellationToken = deathScope.Token;

            routeWalker.enabled = false;
            primaryWeapon.enabled = false;

            isDeathFinished = false;
            characterAnimation.DeathFinished += onDeathFinished;
            characterAnimation.Die();
            await UniTask.WaitUntil(this, flow => flow.isDeathFinished, cancellationToken: cancellationToken);
            characterAnimation.DeathFinished -= onDeathFinished;

            while (true)
            {
                var choice = await deathScreen.ChooseAsync(run.CanContinue(continueDefinition.UsesPerRun), cancellationToken);
                switch (choice)
                {
                    case DeathChoice.Continue:
                        // An ad that wasn't watched earns nothing: back to the death screen, Continue still on offer.
                        if (await continueAd.ShowAsync(cancellationToken) != AdOutcome.Watched)
                        {
                            continue;
                        }

                        await ContinueAsync(cancellationToken);
                        return;

                    case DeathChoice.Restart:
                        run.Restart();
                        return;

                    default:
                        run.Exit();
                        await endScreen.ShowAsync(run.Record, cancellationToken);
                        run.Leave();
                        return;
                }
            }
        }

        // Back where the character died, with full hit points, while the burst pushes the Enemies around it back.
        private async UniTask ContinueAsync(CancellationToken cancellationToken)
        {
            if (!run.Continue(continueDefinition.UsesPerRun))
            {
                return;
            }

            health.Restore(continueDefinition.InvulnerabilitySeconds);

            // The ad buys a fresh start: ammo comes back with the hit points.
            primaryWeapon.Loadout?.Refill(continueDefinition.AmmoRefillShare);
            characterAnimation.Revive();
            routeWalker.enabled = true;
            primaryWeapon.enabled = true;
            await continueBurst.BurstAsync(
                continueBurst.transform.position, routeWalker.Progress.Direction, cancellationToken);
        }

        private void OnDeathFinished()
        {
            isDeathFinished = true;
        }
    }
}
