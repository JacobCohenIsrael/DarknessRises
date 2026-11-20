using DarknessRises.Game.Audio;
using UnityEngine;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// One pair of Phantom eyes: two emissive eye renderers (not lights, no collider: a Shot passes through) and a
    /// positional voice for its whisper, breath or hush. It holds its own sighting and voice fade; the
    /// <see cref="PhantomEyesView"/> decides where it goes and when. Made from the prefab once per pooled pair and reused.
    /// </summary>
    public sealed class PhantomEyesPair : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField, Tooltip("The two eye renderers. They share one emissive material instance.")]
        private Renderer[] eyes = new Renderer[0];

        [SerializeField, Tooltip("Positional source for the whisper, breath or hush.")]
        private AudioSource voice;

        private readonly SoundFade voiceFade = new();
        private Vector3[] openScales = new Vector3[0];
        private Material eyeInstance;
        private Color color;
        private float shownStrength = -1f;

        /// <summary>The eyes' sighting: opening, holding (and blinking), blinking out.</summary>
        public PhantomEyesSighting Sighting { get; private set; }

        /// <summary>Where the eyes are (between the two), whether they show or not.</summary>
        public Vector3 EyesPosition { get; private set; }

        /// <summary>The voice, for checking its state.</summary>
        public AudioSource Voice => voice;

        /// <summary>The voice's fade.</summary>
        public SoundFade VoiceFade => voiceFade;

        /// <summary>Times this pair has opened again nearby since it first opened.</summary>
        public int Hops { get; set; }

        /// <summary>Whether it waits in the dark to open again nearby.</summary>
        public bool IsHopping { get; set; }

        /// <summary>Seconds of dark left before it opens again nearby.</summary>
        public float HopGapLeft { get; set; }

        /// <summary>Whether its voice plays alone, with no eyes.</summary>
        public bool IsVoiceOnly { get; set; }

        /// <summary>Seconds a lone voice has left before it fades.</summary>
        public float VoiceOnlyLeft { get; set; }

        /// <summary>Whether it's out in any way: eyes, about to open again, or a voice still heard.</summary>
        public bool IsBusy => Sighting.IsShowing || IsHopping || IsVoiceOnly || voiceFade.IsAudible;

        /// <summary>Whether it counts as a pair of eyes out: showing, or about to open again.</summary>
        public bool IsEyesOut => Sighting.IsShowing || IsHopping;

        /// <summary>Sets the pair up once, after it's made: its sighting, its look and its voice (Ambient, ADR 0010).</summary>
        public void Prepare(PhantomEyesSighting sighting, Color eyeColor, float hearingRange)
        {
            Sighting = sighting;
            color = eyeColor;
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.maxDistance = hearingRange;
            voice.dopplerLevel = 0f;
            openScales = new Vector3[eyes.Length];
            for (var i = 0; i < eyes.Length; i++)
            {
                openScales[i] = eyes[i].transform.localScale;
            }

            if (eyes.Length > 0)
            {
                eyeInstance = new Material(eyes[0].sharedMaterial) { name = $"{name} Eyes" };
                eyeInstance.EnableKeyword("_EMISSION");
                for (var i = 0; i < eyes.Length; i++)
                {
                    eyes[i].sharedMaterial = eyeInstance;
                }
            }

            ShowStrength(0f);
        }

        /// <summary>Puts the pair on <paramref name="floor"/>, facing <paramref name="character"/>.</summary>
        public void PlaceAt(Vector3 floor, Vector3 character)
        {
            var toCharacter = character - floor;
            toCharacter.y = 0f;
            transform.SetPositionAndRotation(
                floor,
                toCharacter.sqrMagnitude > 0f ? Quaternion.LookRotation(toCharacter) : Quaternion.identity);
            EyesPosition = EyesCentre();
        }

        /// <summary>Moves the voice's fade on and applies it; stops the source once it's silent.</summary>
        public void TickVoice(float deltaSeconds)
        {
            voiceFade.Tick(deltaSeconds);
            voice.SetVolume(voiceFade.Volume);
            if (!voiceFade.IsAudible && voice.isPlaying && deltaSeconds > 0f)
            {
                voice.Stop();
            }
        }

        /// <summary>Plays <paramref name="clip"/> from the pair's spot at <paramref name="volume"/> and <paramref name="pitch"/>, from <paramref name="startSeconds"/> in.</summary>
        public void PlayVoice(AudioClip clip, float volume, float pitch, float startSeconds)
        {
            voice.clip = clip;
            voice.pitch = pitch;
            voice.time = startSeconds;
            voiceFade.Start(volume);
            voice.SetVolume(voiceFade.Volume);
            voice.Play();
        }

        /// <summary>
        /// Shows the eyes at <paramref name="strength"/>, from 0 (hidden) to 1: their glow, and how open they are (each eye
        /// squashed upright like a lid closing), so opening, blinking and blinking out read as eyes, not a fading light.
        /// </summary>
        public void ShowStrength(float strength)
        {
            if (strength == shownStrength)
            {
                return;
            }

            shownStrength = strength;
            var shown = strength > 0f;
            for (var i = 0; i < eyes.Length; i++)
            {
                eyes[i].enabled = shown;
                var open = openScales[i];
                eyes[i].transform.localScale = new Vector3(open.x, open.y * strength, open.z);
            }

            if (eyeInstance != null)
            {
                eyeInstance.SetColor(EmissionColorId, color * strength);
            }
        }

        private Vector3 EyesCentre()
        {
            if (eyes.Length == 0)
            {
                return transform.position;
            }

            var sum = Vector3.zero;
            for (var i = 0; i < eyes.Length; i++)
            {
                sum += eyes[i].transform.position;
            }

            return sum / eyes.Length;
        }

        private void OnDestroy()
        {
            Destroy(eyeInstance);
        }

        private void Reset()
        {
            voice = GetComponent<AudioSource>();
        }
    }
}
