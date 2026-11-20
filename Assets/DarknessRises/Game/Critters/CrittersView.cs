using System;
using System.Collections.Generic;
using DarknessRises.Game.Content;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Visibility;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace DarknessRises.Game.Critters
{
    /// <summary>
    /// Sends Critters darting through the dark when their <see cref="CritterSchedule"/> says so, in the moment the Run and
    /// the Station rhythm make (<see cref="CritterMoments"/>). Each dash is planned across the character's view
    /// (<see cref="CritterDash.Plan"/>) and kept only if both its ends are on the NavMesh and in the dark, with no
    /// Obstacle between them (a NavMesh raycast), so a Critter never runs through a wall or appears in the light.
    /// Critters are pooled. Made by the scene's composition root once the Level's content has loaded.
    /// </summary>
    public sealed class CrittersView : MonoBehaviour
    {
        private const int MostLights = 64;

        private readonly List<CritterView> critters = new();
        private readonly LitSpot[] spots = new LitSpot[MostLights];
        private CritterDefinition definition;
        private CritterSchedule schedule;
        private CritterDashShape shape;
        private ObjectPool<CritterView> pool;
        private Transform hatchery;
        private Transform character;
        private RunFlow run;
        private StationRhythm rhythm;
        private SceneLights lights;
        private ILoadedContent content;
        private IRandom random;

        /// <summary>Raised when a Critter sets off on a dash.</summary>
        public event Action<CritterView> Darted;

        /// <summary>Every Critter this view has made, in or out of its pool.</summary>
        public IReadOnlyList<CritterView> Critters => critters;

        /// <summary>The moment Critters see now.</summary>
        public CritterMoment Moment => CritterMoments.Of(run.Phase, rhythm.Beat);

        /// <summary>
        /// Makes the Critters under <paramref name="parent"/>: a pool of <paramref name="critterDefinition"/>'s prefab from
        /// <paramref name="loadedContent"/>, darting around <paramref name="characterTransform"/> to the moments of
        /// <paramref name="runFlow"/> and <paramref name="stationRhythm"/>, never where <paramref name="sceneLights"/> shine.
        /// </summary>
        public static CrittersView Create(
            Transform parent,
            CritterDefinition critterDefinition,
            Transform characterTransform,
            RunFlow runFlow,
            StationRhythm stationRhythm,
            SceneLights sceneLights,
            ILoadedContent loadedContent)
        {
            var view = new GameObject("Critters").AddComponent<CrittersView>();
            view.transform.SetParent(parent, false);
            view.definition = critterDefinition;
            view.character = characterTransform;
            view.run = runFlow;
            view.rhythm = stationRhythm;
            view.lights = sceneLights;
            view.content = loadedContent;
            view.random = new SystemRandom();
            view.schedule = critterDefinition.Schedule.ToSchedule(view.random);
            view.shape = critterDefinition.Dash.ToShape();

            // Critters are instantiated under this inactive object, so they never wake up before they dash.
            view.hatchery = new GameObject("Hatchery").transform;
            view.hatchery.SetParent(view.transform, false);
            view.hatchery.gameObject.SetActive(false);
            view.pool = new ObjectPool<CritterView>(view.Create, actionOnRelease: HideCritter, actionOnDestroy: DestroyCritter);
            view.Prewarm(critterDefinition.Schedule.MaxAlive);
            return view;
        }

        /// <summary>Sends a Critter darting now if a dash can be placed, whatever the schedule says.</summary>
        /// <returns>The Critter, or null if no dash could be placed.</returns>
        public CritterView DartNow()
        {
            for (var attempt = 0; attempt < definition.Dash.PlacementAttempts; attempt++)
            {
                if (TryPlace(out var dash))
                {
                    var critter = pool.Get();
                    critter.Dash(dash, character);
                    Darted?.Invoke(critter);
                    return critter;
                }
            }

            return null;
        }

        private void Update()
        {
            if (schedule.Tick(Time.deltaTime, Moment, OutCount()))
            {
                DartNow();
            }
        }

        private void OnDestroy()
        {
            pool?.Dispose();
        }

        private int OutCount()
        {
            var count = 0;
            for (var i = 0; i < critters.Count; i++)
            {
                if (critters[i].IsOut)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryPlace(out CritterDash dash)
        {
            var plan = CritterDash.Plan(character.position, character.forward, shape, random);
            dash = default;
            var search = definition.Dash.FloorSearchMetres;
            if (!NavMesh.SamplePosition(plan.From, out var from, search, NavMesh.AllAreas)
                || !NavMesh.SamplePosition(plan.To, out var to, search, NavMesh.AllAreas)
                || NavMesh.Raycast(from.position, to.position, out _, NavMesh.AllAreas))
            {
                return false;
            }

            var litCount = lights.Gather(spots);
            for (var i = 0; i < litCount; i++)
            {
                if (spots[i].Lights(from.position) || spots[i].Lights(to.position))
                {
                    return false;
                }
            }

            dash = new CritterDash(from.position, to.position, plan.Speed);
            return true;
        }

        private void Prewarm(int count)
        {
            var warmed = new CritterView[count];
            for (var i = 0; i < count; i++)
            {
                warmed[i] = pool.Get();
            }

            for (var i = 0; i < count; i++)
            {
                pool.Release(warmed[i]);
            }
        }

        private CritterView Create()
        {
            var critter = Instantiate(content.Prefab<CritterView>(definition.Prefab), hatchery);
            critter.name = definition.name;
            critter.gameObject.SetActive(false);
            critter.transform.SetParent(transform, false);
            critter.Prepare(pool, definition, content, random);
            critters.Add(critter);
            return critter;
        }

        private static void HideCritter(CritterView critter)
        {
            critter.gameObject.SetActive(false);
        }

        private static void DestroyCritter(CritterView critter)
        {
            if (critter != null)
            {
                Object.Destroy(critter.gameObject);
            }
        }
    }
}
