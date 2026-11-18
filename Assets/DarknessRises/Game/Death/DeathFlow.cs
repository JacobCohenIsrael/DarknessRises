using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    /// offers Restart or Exit. Both go through the <see cref="RunFlow"/>: Restart starts a new Run at once; Exit ends
    /// the Run and shows the end screen, which then starts a new Run. Whoever runs the Levels (the boot) loads it.
    /// Continue (a later ticket) is a third choice here, back into the same Run.
    /// </summary>
    public sealed class DeathFlow : MonoBehaviour
    {
        [SerializeField] private DeathScreenView deathScreen;
        [SerializeField] private EndScreenView endScreen;

        // Owns the death flow's lifetime: the scene's.
        private CancellationTokenSource lifetime;
        private CharacterHealth health;
        private RunFlow run;
        private CharacterAnimationView characterAnimation;
        private RouteWalker routeWalker;
        private PrimaryWeaponView primaryWeapon;
        private bool isDeathFinished;
        private Action onDeathFinished;

        /// <summary>Waits for the character's death, then runs the death and end screens.</summary>
        public void Watch(
            CharacterHealth characterHealth,
            RunFlow runFlow,
            CharacterAnimationView animationView,
            RouteWalker walker,
            PrimaryWeaponView weapon)
        {
            Unbind();

            health = characterHealth;
            run = runFlow;
            characterAnimation = animationView;
            routeWalker = walker;
            primaryWeapon = weapon;
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

        private async UniTaskVoid PlayAsync(CancellationToken cancellationToken)
        {
            routeWalker.enabled = false;
            primaryWeapon.enabled = false;

            isDeathFinished = false;
            characterAnimation.DeathFinished += onDeathFinished;
            characterAnimation.Die();
            await UniTask.WaitUntil(this, flow => flow.isDeathFinished, cancellationToken: cancellationToken);
            characterAnimation.DeathFinished -= onDeathFinished;

            var choice = await deathScreen.ChooseAsync(cancellationToken);
            if (choice == DeathChoice.Restart)
            {
                run.Restart();
                return;
            }

            run.Exit();
            await endScreen.ShowAsync(run.Record, cancellationToken);
            run.StartNewRun();
        }

        private void OnDeathFinished()
        {
            isDeathFinished = true;
        }
    }
}
