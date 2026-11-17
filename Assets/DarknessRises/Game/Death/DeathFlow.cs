using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Health;
using DarknessRises.Game.Player;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Death
{
    /// <summary>
    /// What happens when the character dies: it stops walking and shooting, plays its death, and the death screen
    /// offers Restart or Exit. Exit shows the end screen first. Both start a new Run, for now by reloading the scene;
    /// the Run flow ticket replaces that.
    /// </summary>
    public sealed class DeathFlow : MonoBehaviour
    {
        [SerializeField] private DeathScreenView deathScreen;
        [SerializeField] private EndScreenView endScreen;

        // Owns the death flow's lifetime: the scene's.
        private CancellationTokenSource lifetime;
        private CharacterHealth health;
        private RunRecord record;
        private CharacterAnimationView characterAnimation;
        private RouteWalker routeWalker;
        private PrimaryWeaponView primaryWeapon;
        private bool isDeathFinished;
        private Action onDeathFinished;

        /// <summary>Waits for the character's death, then runs the death and end screens.</summary>
        public void Watch(
            CharacterHealth characterHealth,
            RunRecord runRecord,
            CharacterAnimationView animationView,
            RouteWalker walker,
            PrimaryWeaponView weapon)
        {
            Unbind();

            health = characterHealth;
            record = runRecord;
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
            if (choice == DeathChoice.Exit)
            {
                await endScreen.ShowAsync(record, cancellationToken);
            }

            StartNewRun();
        }

        private void OnDeathFinished()
        {
            isDeathFinished = true;
        }

        private void StartNewRun()
        {
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
