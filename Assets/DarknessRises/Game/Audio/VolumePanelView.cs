using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The volume panel: a slider each for Music, Sound and Ambient over a shade that takes every touch, so nothing
    /// behind it is pressed. The same panel opens from the pause menu and from the Shop. Moving a slider changes its
    /// category at once, sounds already playing included; letting go of the Sound slider plays a sample at the new volume.
    /// While it's open the Ambient sounds play on even if the listener is paused, so the Ambient slider is heard from the
    /// pause menu too. It changes only when a slider moves: there is no per-frame work.
    /// </summary>
    public sealed class VolumePanelView : MonoBehaviour
    {
        [SerializeField, Tooltip("The scene's volumes, which the sliders set.")]
        private SceneAudioVolumes volumes;

        [SerializeField, Tooltip("The button elsewhere on screen that opens the panel.")]
        private Button openButton;

        [SerializeField] private TextMeshProUGUI openLabel;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI closeLabel;

        [SerializeField, Tooltip("The sliders, by category: Sound, Ambient, Music.")]
        private Slider[] sliders = Array.Empty<Slider>();

        [SerializeField, Tooltip("Each slider's name, by category: Sound, Ambient, Music.")]
        private TextMeshProUGUI[] names = Array.Empty<TextMeshProUGUI>();

        [SerializeField, Tooltip("Each slider's percent, by category: Sound, Ambient, Music.")]
        private TextMeshProUGUI[] values = Array.Empty<TextMeshProUGUI>();

        [SerializeField, Tooltip("Report letting go of each slider, by category: Sound, Ambient, Music.")]
        private SliderRelease[] releases = Array.Empty<SliderRelease>();

        [SerializeField, Tooltip("Plays the sample, in the Sound category.")]
        private AudioSource sample;

        private AudioVolumeSettings settings;
        private AudioClip sampleClip;

        // Every percent's label, made once, so moving a slider allocates nothing.
        private string[] percentLabels = Array.Empty<string>();
        private UnityEngine.Events.UnityAction<float>[] sliderListeners = Array.Empty<UnityEngine.Events.UnityAction<float>>();
        private Action[] releaseListeners = Array.Empty<Action>();
        private bool isListening;

        /// <summary>Raised when the panel opens.</summary>
        public event Action Opened;

        /// <summary>Raised when the panel closes.</summary>
        public event Action Closed;

        /// <summary>Whether the panel is showing.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>The slider of <paramref name="category"/>, for checks and headless probes.</summary>
        public Slider SliderOf(AudioCategory category) => sliders[AudioVolumes.IndexOf(category)];

        /// <summary>The sample's source.</summary>
        public AudioSource Sample => sample;

        /// <summary>
        /// Readies the panel with its wording and <paramref name="sound"/>, the sample for the Sound slider. The open
        /// button works from here on.
        /// </summary>
        public void Use(AudioVolumeSettings volumeSettings, AudioClip sound)
        {
            settings = volumeSettings ?? throw new ArgumentNullException(nameof(volumeSettings));
            sampleClip = sound;
            percentLabels = new string[VolumeCurve.MaxPercent + 1];
            for (var percent = VolumeCurve.MinPercent; percent <= VolumeCurve.MaxPercent; percent++)
            {
                percentLabels[percent] = string.Format(settings.PercentFormat, percent);
            }

            title.text = settings.Title;
            openLabel.text = settings.OpenLabel;
            closeLabel.text = settings.CloseLabel;
            for (var i = 0; i < AudioVolumes.CategoryCount; i++)
            {
                names[i].text = settings.LabelOf((AudioCategory)i);
            }

            Listen();
            openButton.interactable = true;
        }

        /// <summary>Shows the panel with the sliders where the volumes are now.</summary>
        public void Open()
        {
            if (settings == null || IsOpen)
            {
                return;
            }

            for (var i = 0; i < AudioVolumes.CategoryCount; i++)
            {
                ShowPercent((AudioCategory)i);
            }

            gameObject.SetActive(true);
            volumes.HearWhilePaused(AudioCategory.Ambient, true);
            Opened?.Invoke();
        }

        /// <summary>Hides the panel and puts the volumes on disk.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            gameObject.SetActive(false);
            StopHearingAmbientWhilePaused();
            volumes.Volumes.Save();
            Closed?.Invoke();
        }

        // Here rather than in Awake: the panel starts hidden, and its Awake waits until it first shows.
        private void Listen()
        {
            if (isListening)
            {
                return;
            }

            isListening = true;

            // Not serialized: the sample is heard over a paused listener (the pause menu).
            sample.ignoreListenerPause = true;
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            sliderListeners = new UnityEngine.Events.UnityAction<float>[sliders.Length];
            releaseListeners = new Action[releases.Length];
            for (var i = 0; i < sliders.Length; i++)
            {
                var category = (AudioCategory)i;
                sliderListeners[i] = value => OnSliderMoved(category, value);
                sliders[i].onValueChanged.AddListener(sliderListeners[i]);
                releaseListeners[i] = () => OnSliderReleased(category);
                releases[i].Released += releaseListeners[i];
            }
        }

        private void OnDisable()
        {
            // Hidden with what holds it (the pause menu on Resume) as well as by Close.
            StopHearingAmbientWhilePaused();
        }

        private void OnDestroy()
        {
            if (!isListening)
            {
                return;
            }

            // On scene unload the open button may already be destroyed.
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
            }

            closeButton.onClick.RemoveListener(Close);
            for (var i = 0; i < sliderListeners.Length; i++)
            {
                sliders[i].onValueChanged.RemoveListener(sliderListeners[i]);
                releases[i].Released -= releaseListeners[i];
            }
        }

        private void StopHearingAmbientWhilePaused()
        {
            // On scene unload the volumes may already be destroyed.
            if (volumes != null)
            {
                volumes.HearWhilePaused(AudioCategory.Ambient, false);
            }
        }

        private void OnSliderMoved(AudioCategory category, float value)
        {
            volumes.Volumes.Set(category, Mathf.RoundToInt(value));
            ShowPercent(category);
        }

        private void ShowPercent(AudioCategory category)
        {
            var index = AudioVolumes.IndexOf(category);
            var percent = volumes.Volumes.Percent(category);
            sliders[index].SetValueWithoutNotify(percent);
            values[index].text = percentLabels[percent];
        }

        // Letting go is when a setting is final: it goes on disk, and the Sound slider lets the player hear it.
        private void OnSliderReleased(AudioCategory category)
        {
            volumes.Volumes.Save();
            if (category == AudioCategory.Sound)
            {
                PlaySample();
            }
        }

        private void PlaySample()
        {
            if (sampleClip == null)
            {
                return;
            }

            sample.Stop();
            sample.clip = sampleClip;
            sample.SetVolume(settings.SampleVolume);
            sample.Play();

            // Seeking a clip that hasn't loaded fails (WebGL decodes audio late): it plays from the start instead.
            if (sampleClip.loadState == AudioDataLoadState.Loaded)
            {
                sample.time = Mathf.Clamp(settings.SampleClipStartSeconds, 0f, sampleClip.length);
            }
        }
    }
}
