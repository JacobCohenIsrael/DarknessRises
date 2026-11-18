using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Death;
using DarknessRises.Game.Runs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DarknessRises.Game.Pausing
{
    /// <summary>
    /// Shows a Level's <see cref="GamePause"/>: the Pause action and the on-screen pause button, the pause menu, the end
    /// screen after Exit, pausing when the app loses focus, and the freeze itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// How the freeze works, one approach for everything: <c>Time.timeScale = 0</c> and <c>AudioListener.pause</c>.
    /// Every gameplay clock advances by the scaled <c>Time.deltaTime</c>: the Route walk, Enemy time (Slow-mo scales
    /// it further), Skill cooldowns, Grenade refill, Weapons and Shots, Lightning, Waves and Stragglers, invulnerability
    /// and the Level's beats. NavMesh agents, Animators, physics and Cinemachine (impulse included) run on scaled time
    /// too, so they all stop on the same frame and carry on from there on Resume. Every sound pauses and resumes with
    /// the listener; the player's commands are held so nothing fires while the menu is up.
    /// </para>
    /// <para>
    /// UniTask flows wait for frames (<c>NextFrame</c>, <c>Yield</c>, <c>WaitUntil</c>) and count scaled seconds, so
    /// they freeze too. Any <c>UniTask.Delay</c> in gameplay must keep its default <c>DelayType.DeltaTime</c> (scaled),
    /// never <c>Realtime</c> or <c>UnscaledDeltaTime</c>. The UI (EventSystem, button transitions) runs on unscaled
    /// time, so the menus work while frozen. Nothing else in the game touches <c>Time.timeScale</c> (Slow-mo scales
    /// only Enemy time); the scale from before the freeze is put back.
    /// </para>
    /// </remarks>
    public sealed class PauseFlow : MonoBehaviour
    {
        [SerializeField, Tooltip("Menu/Pause: pauses, or resumes while paused.")]
        private InputActionReference pauseAction;

        [SerializeField, Tooltip("The on-screen pause button, shown while the game can pause.")]
        private Button pauseButton;

        [SerializeField] private PauseMenuView pauseMenu;
        [SerializeField] private EndScreenView endScreen;

        [SerializeField, Tooltip("The player's commands, held while frozen.")]
        private PlayerControls controls;

        // Owns the pause flow's lifetime: the scene's.
        private CancellationTokenSource lifetime;
        private GamePause pause;
        private RunFlow run;
        private bool isFrozen;
        private float timeScaleBeforeFreeze;

        /// <summary>The pause being shown, once there is one.</summary>
        public GamePause Pause => pause;

        /// <summary>Shows <paramref name="gamePause"/> of a Level of <paramref name="runFlow"/>.</summary>
        public void Watch(GamePause gamePause, RunFlow runFlow)
        {
            if (pause != null)
            {
                throw new InvalidOperationException("This pause flow already shows a pause.");
            }

            pause = gamePause ?? throw new ArgumentNullException(nameof(gamePause));
            run = runFlow ?? throw new ArgumentNullException(nameof(runFlow));
            lifetime = new CancellationTokenSource();

            pause.StateChanged += OnStateChanged;
            run.PhaseChanged += OnPhaseChanged;
            pauseAction.action.performed += OnPauseAction;
            pauseAction.action.Enable();
            pauseButton.onClick.AddListener(OnPauseButton);
            pauseMenu.ResumePicked += OnResumePicked;
            pauseMenu.RestartPicked += OnRestartPicked;
            pauseMenu.ExitPicked += OnExitPicked;
            Show();
        }

        private void OnDestroy()
        {
            if (pause == null)
            {
                return;
            }

            pause.StateChanged -= OnStateChanged;
            run.PhaseChanged -= OnPhaseChanged;
            pauseAction.action.performed -= OnPauseAction;
            pauseAction.action.Disable();
            pauseButton.onClick.RemoveListener(OnPauseButton);
            pauseMenu.ResumePicked -= OnResumePicked;
            pauseMenu.RestartPicked -= OnRestartPicked;
            pauseMenu.ExitPicked -= OnExitPicked;
            lifetime.Cancel();
            lifetime.Dispose();

            // The Level goes frozen after Restart or Exit: the next one starts with time and sound running.
            Thaw();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && ListensToFocus)
            {
                pause?.LoseFocus();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && ListensToFocus)
            {
                pause?.LoseFocus();
            }
        }

        // A batch-mode Editor (headless tests and captures) hands the foreground straight back to the developer's
        // window, so it never has focus: there, only the player pauses.
        private static bool ListensToFocus
        {
            get
            {
#if UNITY_EDITOR
                return !Application.isBatchMode;
#else
                return true;
#endif
            }
        }

        private void OnPauseAction(InputAction.CallbackContext context) => pause.Toggle();

        private void OnPauseButton() => pause.Pause();

        private void OnResumePicked() => pause.Resume();

        private void OnRestartPicked() => pause.Restart();

        private void OnExitPicked()
        {
            if (pause.Exit())
            {
                ShowEndScreenAsync(lifetime.Token).Forget();
            }
        }

        // The end screen shows over the frozen Level, then a new Run starts, which the boot loads.
        private async UniTaskVoid ShowEndScreenAsync(CancellationToken cancellationToken)
        {
            await endScreen.ShowAsync(run.Record, cancellationToken);
            run.StartNewRun();
        }

        private void OnStateChanged(PauseState _) => Show();

        private void OnPhaseChanged(RunPhase _) => Show();

        private void Show()
        {
            if (pause.IsFrozen)
            {
                Freeze();
            }
            else
            {
                Thaw();
            }

            pauseMenu.IsShown = pause.IsPaused;
            pauseButton.gameObject.SetActive(pause.CanPause);
        }

        private void Freeze()
        {
            if (isFrozen)
            {
                return;
            }

            isFrozen = true;
            timeScaleBeforeFreeze = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            controls.Hold(true);
        }

        private void Thaw()
        {
            if (!isFrozen)
            {
                return;
            }

            isFrozen = false;
            Time.timeScale = timeScaleBeforeFreeze;
            AudioListener.pause = false;

            // On scene unload the character may already be destroyed.
            if (controls != null)
            {
                controls.Hold(false);
            }
        }
    }
}
