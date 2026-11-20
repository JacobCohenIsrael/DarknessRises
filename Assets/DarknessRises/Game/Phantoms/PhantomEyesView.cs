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
    /// (as soon as a Lightning flash fades, <see cref="PhantomAfterFlash"/>) far from the character at a spot on the floor that is dark, in view and open to the character, or a lone voice
    /// comes from a dark spot. Up to an authored number of pairs show at once, each at its own spot; they blink while
    /// they hold, and a pair that held its time sometimes opens again nearby (<see cref="PhantomRelocation"/>). Voices
    /// never repeat the last clip (<see cref="PhantomVoicePicker"/>). Each frame each pair's
    /// <see cref="PhantomEyesSighting"/> decides whether it stays: light on its spot, a real Enemy near the character or
    /// near its spot, or a fight starting sends it away at once, and the player aiming at it (<see cref="PhantomAim"/>)
    /// makes it blink out. During a Wave fight, rarely, one silent pair opens at the far edge of the dark, away from every
    /// real Enemy (<see cref="PhantomFightSchedule"/>); never in the Boss fight.
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
        private PhantomAfterFlash afterFlash;
        private PhantomFightSchedule fightSchedule;
        private PhantomEyesSpot fightSpotShape;
        private PhantomAfterFlash fightAfterFlash;
        private PhantomVoiceGap voiceGap;
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

        /// <summary>How many pairs that opened in a Wave fight are out.</summary>
        public int FightEyesOut
        {
            get
            {
                var count = 0;
                for (var i = 0; i < pairs.Length; i++)
                {
                    count += pairs[i].Sighting.IsShowing && pairs[i].Sighting.IsInFight ? 1 : 0;
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
            view.afterFlash = phantomDefinition.ToAfterFlash();
            view.fightSchedule = phantomDefinition.ToFightSchedule(view.random);
            view.fightSpotShape = phantomDefinition.ToFightSpot();
            view.fightAfterFlash = phantomDefinition.ToAfterFlash();
            view.voiceGap = phantomDefinition.ToVoiceGap();
            view.voiceClips = loadedContent.Clips(phantomDefinition.WhisperClips);

            // The most pairs of eyes, and one more so a lone voice can come while they're all out. Safe-time pairs are
            // gone once a fight starts, so fight pairs reuse them.
            var prefab = loadedContent.Prefab<PhantomEyesPair>(phantomDefinition.Prefab);
            view.pairs = new PhantomEyesPair[Mathf.Max(phantomDefinition.MaxPairs, phantomDefinition.FightMaxPairs) + 1];
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
            return MayShowNow() && EyesOut < definition.MaxPairs ? Appear(spotShape, false) : null;
        }

        /// <summary>
        /// Makes a pair open now at the far edge of the dark, as in a Wave fight, if it is one, fewer than the most fight
        /// pairs are out, and a spot away from every real Enemy can be found. The rest of the rules hold.
        /// </summary>
        /// <returns>The pair that opened, or null.</returns>
        public PhantomEyesPair FightAppearNow()
        {
            return PhantomFightSchedule.MayShow(Moment) && FightEyesOut < definition.FightMaxPairs
                ? Appear(fightSpotShape, true)
                : null;
        }

        private PhantomEyesPair Appear(PhantomEyesSpot shape, bool inFight)
        {
            var pair = FreePair();
            if (pair == null)
            {
                return null;
            }

            spotCount = lights.Gather(spots);
            for (var attempt = 0; attempt < definition.PlacementAttempts; attempt++)
            {
                if (TryPlace(pair, shape.Pick(character.position, character.forward, random), shape))
                {
                    pair.Hops = 0;
                    Open(pair, inFight);
                    return pair;
                }
            }

            return null;
        }

        /// <summary>Makes a lone voice come from a dark spot now if a pair is free, a spot can be found and the last voice began long enough ago.</summary>
        /// <returns>The pair whose voice plays, or null.</returns>
        public PhantomEyesPair VoiceNow()
        {
            if (!MayShowNow() || voiceClips.Length == 0 || !voiceGap.IsOpen)
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
                if (TryPlaceVoice(pair, voiceSpotShape.Pick(character.position, character.forward, random)))
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
            voiceGap.Tick(deltaSeconds);
            var moment = Moment;
            var nearest = enemies.NearestMetres(character.position);
            var mayShow = PhantomEyesSchedule.MayShow(moment, nearest, definition.EnemyClearanceMetres);
            spotCount = lights.Gather(spots);
            for (var i = 0; i < pairs.Length; i++)
            {
                TickPair(pairs[i], deltaSeconds, moment, nearest, mayShow);
            }

            // Due eyes wait for a flash to fade, then open into the dark it leaves.
            if (!mayShow)
            {
                afterFlash.Cancel();
            }

            if (afterFlash.Tick(deltaSeconds, IsRoomLit()) && AppearNow() == null)
            {
                schedule.Missed(PhantomEvent.Eyes);
            }

            // In a Wave fight: rarely, one pair at most, at the far edge, also as a flash fades.
            if (!PhantomFightSchedule.MayShow(moment))
            {
                fightAfterFlash.Cancel();
            }

            if (fightAfterFlash.Tick(deltaSeconds, IsRoomLit()) && FightAppearNow() == null)
            {
                fightSchedule.Missed();
            }

            if (fightSchedule.Tick(deltaSeconds, moment, FightEyesOut + fightAfterFlash.Waiting))
            {
                fightAfterFlash.Wait();
            }

            // Pairs waiting for a flash to fade count as out, so no more are due than may show.
            var due = schedule.Tick(deltaSeconds, moment, nearest, EyesOut + afterFlash.Waiting);
            if (due == PhantomEvent.Eyes)
            {
                afterFlash.Wait();
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
                // A fight or an Enemy near cancels the hop: nothing opens again. The dark counts once the light is gone.
                if (!IsLit(pair))
                {
                    pair.HopGapLeft -= deltaSeconds;
                }

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
            var spotEnemy = enemies.NearestMetres(pair.EyesPosition);
            var conditions = new PhantomConditions(moment, nearest, IsLit(pair), IsAimedAt(pair), away.magnitude, spotEnemy);
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
            // Fight eyes never open again nearby: one rare pair, then the fight's own.
            if (!sighting.IsInFight && relocation.Hops(vanish, pair.Hops, random))
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
                if (TryPlace(pair, relocation.PickNear(from, random), spotShape))
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
        // character, within shape's band, staying away along the walk, clear of every real Enemy, apart from the other
        // pairs, dark, and in view.
        private bool TryPlace(PhantomEyesPair pair, Vector3 wanted, PhantomEyesSpot shape)
        {
            var search = definition.FloorSearchMetres;
            var position = character.position;
            if (!NavMesh.SamplePosition(wanted, out var floor, search, NavMesh.AllAreas)
                || !NavMesh.SamplePosition(position, out var from, search, NavMesh.AllAreas)
                || NavMesh.Raycast(from.position, floor.position, out _, NavMesh.AllAreas))
            {
                return false;
            }

            if (!shape.IsFarOut(floor.position, position)
                || enemies.NearestMetres(floor.position) <= definition.SpotClearanceMetres
                || !PhantomEyesSpot.StaysAway(floor.position, position, character.forward, definition.WalkMarginMetres, definition.KeepAwayMetres)
                || !PhantomEyesSpot.IsApart(floor.position, otherSpots, OtherSpots(pair), definition.PairSeparationMetres))
            {
                return false;
            }

            pair.PlaceAt(floor.position, position);
            return !IsLit(pair) && IsInView(pair);
        }

        // Puts the pair at the floor nearest to wanted for a lone voice: far out, apart from the other pairs, and dark. A
        // sound needs no line of sight, so it may come from behind a wall or from the way the character came.
        private bool TryPlaceVoice(PhantomEyesPair pair, Vector3 wanted)
        {
            var position = character.position;
            if (!NavMesh.SamplePosition(wanted, out var floor, definition.FloorSearchMetres, NavMesh.AllAreas)
                || !spotShape.IsFarOut(floor.position, position)
                || enemies.NearestMetres(floor.position) <= definition.SpotClearanceMetres
                || !PhantomEyesSpot.IsApart(floor.position, otherSpots, OtherSpots(pair), definition.PairSeparationMetres))
            {
                return false;
            }

            pair.PlaceAt(floor.position, position);
            return !IsLit(pair);
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

        private void Open(PhantomEyesPair pair, bool inFight = false)
        {
            pair.Sighting.Show(random.Range(definition.MinHoldSeconds, definition.MaxHoldSeconds), inFight);
            pair.ShowStrength(pair.Sighting.Strength);
            Appeared?.Invoke(pair);

            // Fight eyes are silent: in a fight the player listens for real Enemies. Too soon after another voice, the
            // eyes open silent too, so voices don't pile up.
            if (!inFight && random.Range(0f, 1f) < definition.VoiceWithEyesChance && voiceGap.IsOpen)
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
            voiceGap.Started();
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

        private bool IsRoomLit()
        {
            for (var i = 0; i < spotCount; i++)
            {
                if (spots[i].IsWholeRoom)
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
