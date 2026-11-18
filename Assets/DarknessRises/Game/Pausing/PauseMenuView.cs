using System;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Pausing
{
    /// <summary>The pause menu: Resume, Restart and Exit. It only reports the pick; <see cref="PauseFlow"/> acts on it.</summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        public event Action ResumePicked;
        public event Action RestartPicked;
        public event Action ExitPicked;

        public bool IsShown
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        private void Awake()
        {
            resumeButton.onClick.AddListener(OnResume);
            restartButton.onClick.AddListener(OnRestart);
            exitButton.onClick.AddListener(OnExit);
        }

        private void OnDestroy()
        {
            resumeButton.onClick.RemoveListener(OnResume);
            restartButton.onClick.RemoveListener(OnRestart);
            exitButton.onClick.RemoveListener(OnExit);
        }

        private void OnResume() => ResumePicked?.Invoke();

        private void OnRestart() => RestartPicked?.Invoke();

        private void OnExit() => ExitPicked?.Invoke();
    }
}
