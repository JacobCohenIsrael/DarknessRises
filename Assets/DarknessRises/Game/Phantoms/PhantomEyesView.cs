using System;
using System.Collections.Generic;
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
    /// Makes the dark feel watched. When the <see cref="PhantomEyesSchedule"/> says so, a pair of Phantom eyes looks out
    /// far from the character at a spot on the floor that is dark, in view and open to the character, or a lone voice
    /// comes from a dark spot. Up to an authored number of pairs show at once, each at its own spot; they blink while
    /// they hold, and a pair that held its time sometimes opens again nearby (<see cref="PhantomRelocation"/>). Voices
    /// never repeat the last clip (<see cref="PhantomVoicePicker"/>). Each frame each pair's
    /// <see cref="PhantomEyesSighting"/> decides whether it stays: light on its spot, a real Enemy near, or a fight
    /// starting sends it away at once, and the player aiming at it (<see cref="PhantomAim"/>) makes it blink out.
    /// The pairs are made once from the prefab (the most pairs, and one more for a lone voice) and reused. Made by the
    /// scene's composition root once the content has loaded.
    /// </summary>
    [DefaultExecutionOrder(AfterEveryLight)]
    public sealed class PhantomEyesView : MonoBehaviour
    {
        // After everything that switches a light on in a frame (Lightning, Gunfire, sparks, Skills, Grenades), so light
        // reaching the eyes sends them away before that frame is drawn: they're never seen lit, not even for one frame.
        private const int AfterEveryLight = 1000;

        private const int MostLights = 64;

        private readonly LitSpot[] spots = new LitSpot[MostLights];
        private int spotCount;
        private PhantomEyesDefinition definition;
        private PhantomEyesSchedule schedule;
        private PhantomEyesSpot spotShape;
        private PhantomEyesSpot voiceSpotShape;
        private PhantomRelocation relocation;
        private PhantomVoicePicker voicePicker;
        private PhantomEyesPair[] pairs;
        private Vector3[] otherSpots;
        private Transform character;
        private PlayerControls controls;
        private Camera viewCamera;
        private RunFlow run;
        private StationRhythm rhythm;
        private SceneLights lights;
        private LivingEnemies enemies;
        private AudioClip[] voiceClips;
        private IRandom random;

        /// <summary>Raised when a pair opens, with where its eyes are; also when one opens again nearby.</summary>
        public event Action<PhantomEyesPair> Appeared;

        /// <summary>Raised when a pair goes away, with why.</summary>
        public event Action<PhantomEyesPair, PhantomVanish> Vanished;

        /// <summary>Raised when a holding pair starts a blink.</summary>
        public event Action<PhantomEyesPair> Blinked;

        /// <summary>Raised when a voice starts, from a pair's spot: with its clip, and whether eyes came with it.</summary>
        public event Action<PhantomEyesPair, AudioClip, bool> Voiced;

        /// <summary>The pooled pairs.</summary>
        public IReadOnlyList<PhantomEyesPair> Pairs => pairs;

        /// <summary>The schedule, for checking when the next event is due.</summary>
        public PhantomEyesSchedule Schedule => schedule;

        /// <summary>How many pairs of eyes are out (showing, or about to open again nearby).</summary>
        public int EyesOut
        {
            get
            {
                var count = 0;
                for (var i = 0; i < pairs.Length; i++)
                {
                    count += pairs[i].IsEyesOut ? 1 : 0;
                }

                return count;
            }
        }

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
            var holder = new GameObject(phantomDefinition.name);
            holder.transform.SetParent(parent, false);
            var view = holder.AddComponent<PhantomEyesView>();
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
            view.spotShape = phantomDefinition.ToSpot();
            view.voiceSpotShape = phantomDefinition.ToVoiceSpot();
            view.relocation = phantomDefinition.ToRelocation();
            view.voicePicker = new PhantomVoicePicker(view.random);
            view.voiceClips = loadedContent.Clips(phantomDefinition.WhisperClips);

            // The most pairs of eyes, and one more so a lone voice can come while they're all out.
            var prefab = loadedContent.Prefab<PhantomEyesPair>(phantomDefinition.Prefab);
            view.pairs = new PhantomEyesPair[phantomDefinition.MaxPairs + 1];
            view.otherSpots = new Vector3[view.pairs.Length];
            for (var i = 0; i < view.pairs.Length; i++)
            {
                var pair = Instantiate(prefab, holder.transform);
                pair.name = $"{phantomDefinition.name} {i + 1}";
                pair.Prepare(phantomDefinition.ToSighting(view.random), phantomDefinition.Color, phantomDefinition.HearingRange);
                view.pairs[i] = pair;
            }

            return view;
        }

        /// <summary>Makes a pair open now if one is free and a spot can be found, whatever the schedule says. The rest of the rules hold.</summary>
        /// <returns>The pair that opened, or null.</returns>
        public PhantomEyesPair AppearNow()
        {
            if (!MayShowNow() || EyesOut >= definition.MaxPairs)
            {
                return null;
            }

            var pair = FreePair();
            if (pair == null)
            {
                return null;
            }

            spotCount = lights.Gather(spots);
            for (var attempt = 0; attempt < definition.PlacementAttempts; attempt++)
            {
                if (TryPlace(pair, spotShape.Pick(character.position, character.forward, random), true))
                {
                    pair.Hops = 0;
                    Open(pair);
                    return pair;
                }
            }

            return null;
        }

        /// <summary>Makes a lone voice come from a dark spot now if a pair is free and a spot can be found.</summary>
        /// <returns>The pair whose voice plays, or null.</returns>
        public PhantomEyesPair VoiceNow()
        {
            if (!MayShowNow() || voiceClips.Length == 0)
            {
                return null;
            }

            var pair = FreePair();
            if (pair == null)
            {
                return null;
            }

            spotCount = lights.Gather(spots);
            for (var attempt = 0; attempt < definition.PlacementAttempts; attempt++)
            {
                // A voice needs no screen: from any side, but still far out, on open floor, in the dark.
                if (TryPlace(pair, voiceSpotShape.Pick(character.position, character.forward, random), false))
                {
                    pair.IsVoiceOnly = true;
                    pair.VoiceOnlyLeft = random.Range(definition.MinVoiceOnlySeconds, definition.MaxVoiceOnlySeconds);
                    if (StartVoice(pair, false))
                    {
                        return pair;
                    }

                    pair.IsVoiceOnly = false;
                    return null;
                }
            }

            return null;
        }

        private CritterMoment Moment => CritterMoments.Of(run.Phase, rhythm.Beat);

        private bool MayShowNow()
        {
            return PhantomEyesSchedule.MayShow(Moment, enemies.NearestMetres(character.position), definition.EnemyClearanceMetres);
        }

        private void Update()
        {
            var deltaSeconds = Time.deltaTime;
            var moment = Moment;
            var nearest = enemies.NearestMetres(character.position);
            var mayShow = PhantomEyesSchedule.MayShow(moment, nearest, definition.EnemyClearanceMetres);
            spotCount = lights.Gather(spots);
            for (var i = 0; i < pairs.Length; i++)
            {
                TickPair(pairs[i], deltaSeconds, moment, nearest, mayShow);
            }

            var due = schedule.Tick(deltaSeconds, moment, nearest, EyesOut);
            if (due == PhantomEvent.Eyes && AppearNow() == null)
            {
                schedule.Missed(due);
            }
            else if (due == PhantomEvent.Voice && voiceClips.Length > 0 && VoiceNow() == null)
            {
                schedule.Missed(due);
            }
        }

        private void TickPair(PhantomEyesPair pair, float deltaSeconds, CritterMoment moment, float nearest, bool mayShow)
        {
            pair.TickVoice(deltaSeconds);
            if (pair.IsVoiceOnly)
            {
                pair.VoiceOnlyLeft -= deltaSeconds;
                if (pair.VoiceOnlyLeft <= 0f || !mayShow)
                {
                    pair.IsVoiceOnly = false;
                    pair.VoiceFade.FadeOut(mayShow ? definition.WhisperFadeSeconds : 0f);
                }

                return;
            }

            if (pair.IsHopping)
            {
                // A fight or an Enemy near cancels the hop: nothing opens again.
                pair.HopGapLeft -= deltaSeconds;
                if (!mayShow)
                {
                    pair.IsHopping = false;
                }
                else if (pair.HopGapLeft <= 0f)
                {
                    pair.IsHopping = false;
                    Hop(pair);
                }

                return;
            }

            var sighting = pair.Sighting;
            if (!sighting.IsShowing)
            {
                return;
            }

            var away = pair.EyesPosition - character.position;
            away.y = 0f;
            var conditions = new PhantomConditions(moment, nearest, IsLit(pair), IsAimedAt(pair), away.magnitude);
            var wasBlinkingOut = sighting.IsBlinkingOut;
            var wasBlinking = sighting.IsBlinking;
            var vanish = sighting.Tick(deltaSeconds, conditions);
            pair.ShowStrength(sighting.Strength);
            if (!wasBlinking && sighting.IsBlinking)
            {
                Blinked?.Invoke(pair);
            }

            if (!wasBlinkingOut && (sighting.IsBlinkingOut || vanish != PhantomVanish.None))
            {
                // Light, an Enemy or a fight silence it at once; otherwise it trails off.
                var sudden = vanish == PhantomVanish.Lit || vanish == PhantomVanish.EnemyNear || vanish == PhantomVanish.MomentEnded;
                pair.VoiceFade.FadeOut(sudden ? 0f : definition.WhisperFadeSeconds);
            }

            if (vanish == PhantomVanish.None)
            {
                return;
            }

            Vanished?.Invoke(pair, vanish);
            if (relocation.Hops(vanish, pair.Hops, random))
            {
                pair.IsHopping = true;
                pair.HopGapLeft = relocation.Gap(random);
            }
        }

        private void Hop(PhantomEyesPair pair)
        {
            var from = pair.transform.position;
            for (var attempt = 0; attempt < definition.PlacementAttempts; attempt++)
            {
                if (TryPlace(pair, relocation.PickNear(from, random), true))
                {
                    pair.Hops++;
                    Open(pair);
                    return;
                }
            }

            // No dark spot nearby: it just stays gone.
            pair.transform.position = from;
        }

        private PhantomEyesPair FreePair()
        {
            for (var i = 0; i < pairs.Length; i++)
            {
                if (!pairs[i].IsBusy)
                {
                    return pairs[i];
                }
            }

            return null;
        }

        // Puts the pair at the floor nearest to wanted, if that spot keeps every rule: on the floor and open to the
        // character, far out, staying away along the walk, apart from the other pairs, dark, and (for eyes) in view.
        private bool TryPlace(PhantomEyesPair pair, Vector3 wanted, bool mustBeSeen)
        {
            var search = definition.FloorSearchMetres;
            var position = character.position;
            if (!NavMesh.SamplePosition(wanted, out var floor, search, NavMesh.AllAreas)
                || !NavMesh.SamplePosition(position, out var from, search, NavMesh.AllAreas)
                || NavMesh.Raycast(from.position, floor.position, out _, NavMesh.AllAreas))
            {
                return false;
            }

            if (!spotShape.IsFarOut(floor.position, position)
                || !PhantomEyesSpot.StaysAway(floor.position, position, character.forward, definition.WalkMarginMetres, definition.KeepAwayMetres)
                || !PhantomEyesSpot.IsApart(floor.position, otherSpots, OtherSpots(pair), definition.PairSeparationMetres))
            {
                return false;
            }

            pair.PlaceAt(floor.position, position);
            return !IsLit(pair) && (!mustBeSeen || IsInView(pair));
        }

        // Gathers where the other busy pairs are into otherSpots; returns how many.
        private int OtherSpots(PhantomEyesPair pair)
        {
            var count = 0;
            for (var i = 0; i < pairs.Length; i++)
            {
                if (pairs[i] != pair && pairs[i].IsBusy)
                {
                    otherSpots[count++] = pairs[i].transform.position;
                }
            }

            return count;
        }

        private void Open(PhantomEyesPair pair)
        {
            pair.Sighting.Show(random.Range(definition.MinHoldSeconds, definition.MaxHoldSeconds));
            pair.ShowStrength(pair.Sighting.Strength);
            Appeared?.Invoke(pair);
            if (random.Range(0f, 1f) < definition.VoiceWithEyesChance)
            {
                StartVoice(pair, true);
            }
        }

        private bool StartVoice(PhantomEyesPair pair, bool withEyes)
        {
            var index = voicePicker.Pick(voiceClips.Length);
            var clip = index >= 0 ? voiceClips[index] : null;
            if (clip == null)
            {
                return false;
            }

            var pitch = random.Range(definition.WhisperMinPitch, definition.WhisperMaxPitch);

            // Somewhere in its first seconds, so it isn't the same breath every time, with enough left to last the hold.
            var latestStart = Mathf.Min(definition.WhisperLatestStartSeconds, clip.length - definition.MaxHoldSeconds * pitch);
            pair.PlayVoice(clip, definition.WhisperVolume, pitch, latestStart > 0f ? random.Range(0f, latestStart) : 0f);
            Voiced?.Invoke(pair, clip, withEyes);
            return true;
        }

        private bool IsLit(PhantomEyesPair pair)
        {
            var floor = pair.transform.position;
            var eyes = pair.EyesPosition;
            for (var i = 0; i < spotCount; i++)
            {
                if (spots[i].Lights(eyes) || spots[i].Lights(floor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInView(PhantomEyesPair pair)
        {
            var viewport = viewCamera.WorldToViewportPoint(pair.EyesPosition);
            var margin = definition.ScreenMarginShare;
            return viewport.z > 0f
                   && viewport.x >= margin && viewport.x <= 1f - margin
                   && viewport.y >= margin && viewport.y <= 1f - margin;
        }

        private bool IsAimedAt(PhantomEyesPair pair)
        {
            var isAiming = !controls.IsHeld && (!controls.IsTouchActive || controls.IsFireHeld);
            return PhantomAim.IsAimedAt(
                controls.PointerPosition,
                isAiming,
                viewCamera.WorldToScreenPoint(pair.EyesPosition),
                Screen.height,
                definition.AimRadiusShare);
        }
    }
}
