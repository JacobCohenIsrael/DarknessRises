using System;
using DarknessRises.Game.Audio;
using DarknessRises.Game.Content;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Critters;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Visibility;
using UnityEngine;
using UnityEngine.AI;

namespace DarknessRises.Game.Phantoms
{
    /// <summary>
    /// Shows Phantom eyes: when their <see cref="PhantomEyesSchedule"/> allows, a pair looks out of the dark far ahead of
    /// the character, at a spot on the floor that is dark, in view and open to the character; a faint whisper may come
    /// from it. Each frame the <see cref="PhantomEyesSighting"/> decides whether they stay: light on the spot, a real
    /// Enemy near, or a fight starting sends them away at once, and the player aiming at them (<see cref="PhantomAim"/>)
    /// makes them blink out. The eyes are emissive renderers, not lights, and have no collider: a Shot passes through.
    /// One pair, made once from its prefab and reused. Made by the scene's composition root once the content has loaded.
    /// </summary>
    [DefaultExecutionOrder(AfterEveryLight)]
    public sealed class PhantomEyesView : MonoBehaviour
    {
        // After everything that switches a light on in a frame (Lightning, Gunfire, sparks, Skills, Grenades), so light
        // reaching the eyes sends them away before that frame is drawn: they're never seen lit, not even for one frame.
        private const int AfterEveryLight = 1000;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const int MostLights = 64;

        [SerializeField, Tooltip("The two eye renderers. They share one emissive material instance.")]
        private Renderer[] eyes = new Renderer[0];

        [SerializeField, Tooltip("Positional source for the whisper.")]
        private AudioSource voice;

        private readonly LitSpot[] spots = new LitSpot[MostLights];
        private readonly SoundFade whisper = new();
        private PhantomEyesDefinition definition;
        private PhantomEyesSchedule schedule;
        private PhantomEyesSighting sighting;
        private PhantomEyesSpot spotShape;
        private Transform character;
        private PlayerControls controls;
        private Camera viewCamera;
        private RunFlow run;
        private StationRhythm rhythm;
        private SceneLights lights;
        private LivingEnemies enemies;
        private AudioClip[] whisperClips;
        private IRandom random;
        private Material eyeInstance;
        private float shownStrength = -1f;

        /// <summary>Raised when a pair appears, with where its eyes are.</summary>
        public event Action<Vector3> Appeared;

        /// <summary>Raised when a pair goes away, with why.</summary>
        public event Action<PhantomVanish> Vanished;

        /// <summary>The current or last sighting.</summary>
        public PhantomEyesSighting Sighting => sighting;

        /// <summary>Where the eyes are (between the two), whether they show or not.</summary>
        public Vector3 EyesPosition { get; private set; }

        /// <summary>The whisper's voice, for checking its state.</summary>
        public AudioSource Voice => voice;

        /// <summary>
        /// Makes the Phantom eyes under <paramref name="parent"/> from <paramref name="phantomDefinition"/>'s prefab in
        /// <paramref name="loadedContent"/>, looking out at <paramref name="characterTransform"/>.
        /// </summary>
        public static PhantomEyesView Create(
            Transform parent,
            PhantomEyesDefinition phantomDefinition,
            Transform characterTransform,
            PlayerControls playerControls,
            Camera camera,
            RunFlow runFlow,
            StationRhythm stationRhythm,
            SceneLights sceneLights,
            LivingEnemies livingEnemies,
            ILoadedContent loadedContent)
        {
            var view = Instantiate(loadedContent.Prefab<PhantomEyesView>(phantomDefinition.Prefab), parent);
            view.name = phantomDefinition.name;
            view.definition = phantomDefinition;
            view.character = characterTransform;
            view.controls = playerControls;
            view.viewCamera = camera;
            view.run = runFlow;
            view.rhythm = stationRhythm;
            view.lights = sceneLights;
            view.enemies = livingEnemies;
            view.random = new SystemRandom();
            view.schedule = phantomDefinition.ToSchedule(view.random);
            view.sighting = phantomDefinition.ToSighting();
            view.spotShape = phantomDefinition.ToSpot();
            view.whisperClips = loadedContent.Clips(phantomDefinition.WhisperClips);
            view.Prepare();
            return view;
        }

        /// <summary>Makes a pair appear now if a spot can be found, whatever the schedule says. The rest of the rules hold.</summary>
        /// <returns>Whether they appeared.</returns>
        public bool AppearNow()
        {
            var nearest = enemies.NearestMetres(character.position);
            if (sighting.IsShowing || !PhantomEyesSchedule.MayShow(Moment, nearest, definition.EnemyClearanceMetres))
            {
                return false;
            }

            for (var attempt = 0; attempt < definition.PlacementAttempts; attempt++)
            {
                if (TryPlace())
                {
                    Show();
                    return true;
                }
            }

            return false;
        }

        private CritterMoment Moment => CritterMoments.Of(run.Phase, rhythm.Beat);

        // The one place the whisper's voice is set up: route it to the ambient mixer group here.
        private void Prepare()
        {
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.maxDistance = definition.HearingRange;
            voice.dopplerLevel = 0f;
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

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            whisper.Tick(deltaSeconds);
            voice.volume = whisper.Volume;
            if (!whisper.IsAudible && voice.isPlaying && deltaSeconds > 0f)
            {
                voice.Stop();
            }

            var nearest = enemies.NearestMetres(character.position);
            if (!sighting.IsShowing)
            {
                if (schedule.Tick(deltaSeconds, Moment, nearest, false))
                {
                    AppearNow();
                }

                return;
            }

            var away = EyesPosition - character.position;
            away.y = 0f;
            var conditions = new PhantomConditions(Moment, nearest, IsLit(), IsAimedAt(), away.magnitude);
            var wasBlinking = sighting.IsBlinkingOut;
            var vanish = sighting.Tick(deltaSeconds, conditions);
            ShowStrength(sighting.Strength);
            if (!wasBlinking && (sighting.IsBlinkingOut || vanish != PhantomVanish.None))
            {
                whisper.FadeOut(definition.WhisperFadeSeconds);
            }

            if (vanish != PhantomVanish.None)
            {
                Vanished?.Invoke(vanish);
            }
        }

        private void OnDestroy()
        {
            Destroy(eyeInstance);
        }

        private bool TryPlace()
        {
            var wanted = spotShape.Pick(character.position, character.forward, random);
            var search = definition.FloorSearchMetres;
            if (!NavMesh.SamplePosition(wanted, out var floor, search, NavMesh.AllAreas)
                || !NavMesh.SamplePosition(character.position, out var from, search, NavMesh.AllAreas)
                || NavMesh.Raycast(from.position, floor.position, out _, NavMesh.AllAreas))
            {
                return false;
            }

            if (!PhantomEyesSpot.StaysAway(floor.position, character.position, character.forward, definition.WalkMarginMetres, definition.KeepAwayMetres))
            {
                return false;
            }

            var toCharacter = character.position - floor.position;
            toCharacter.y = 0f;
            transform.SetPositionAndRotation(
                floor.position,
                toCharacter.sqrMagnitude > 0f ? Quaternion.LookRotation(toCharacter) : Quaternion.identity);
            EyesPosition = EyesCentre();
            if (IsLit() || !IsInView())
            {
                return false;
            }

            return true;
        }

        private void Show()
        {
            sighting.Show(random.Range(definition.MinHoldSeconds, definition.MaxHoldSeconds));
            shownStrength = -1f;
            ShowStrength(sighting.Strength);
            StartWhisper();
            Appeared?.Invoke(EyesPosition);
        }

        private void StartWhisper()
        {
            if (whisperClips.Length == 0)
            {
                return;
            }

            var clip = whisperClips[Mathf.Min((int)random.Range(0f, whisperClips.Length), whisperClips.Length - 1)];
            if (clip == null)
            {
                return;
            }

            voice.clip = clip;
            voice.pitch = random.Range(definition.WhisperMinPitch, definition.WhisperMaxPitch);
            // Somewhere in its first seconds, so it isn't the same breath every time, with enough left to last the hold.
            var latestStart = Mathf.Min(definition.WhisperLatestStartSeconds, clip.length - definition.MaxHoldSeconds * voice.pitch);
            voice.time = latestStart > 0f ? random.Range(0f, latestStart) : 0f;
            whisper.Start(definition.WhisperVolume);
            voice.volume = whisper.Volume;
            voice.Play();
        }

        private bool IsLit()
        {
            var floor = transform.position;
            var count = lights.Gather(spots);
            for (var i = 0; i < count; i++)
            {
                if (spots[i].Lights(EyesPosition) || spots[i].Lights(floor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInView()
        {
            var viewport = viewCamera.WorldToViewportPoint(EyesPosition);
            var margin = definition.ScreenMarginShare;
            return viewport.z > 0f
                   && viewport.x >= margin && viewport.x <= 1f - margin
                   && viewport.y >= margin && viewport.y <= 1f - margin;
        }

        private bool IsAimedAt()
        {
            var isAiming = !controls.IsHeld && (!controls.IsTouchActive || controls.IsFireHeld);
            return PhantomAim.IsAimedAt(
                controls.PointerPosition,
                isAiming,
                viewCamera.WorldToScreenPoint(EyesPosition),
                Screen.height,
                definition.AimRadiusShare);
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

        private void ShowStrength(float strength)
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
            }

            if (eyeInstance != null)
            {
                eyeInstance.SetColor(EmissionColorId, definition.Color * strength);
            }
        }

        private void Reset()
        {
            voice = GetComponent<AudioSource>();
        }
    }
}
