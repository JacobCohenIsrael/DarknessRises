using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Bosses;
using DarknessRises.Game.Content;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Randomness;
using DarknessRises.Game.Rooms;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Waves;
using UnityEngine;

namespace DarknessRises.Game.Levels
{
    /// <summary>
    /// Runs a Level in the scene: the start beat as the character walks in, the fights in each Room it walks through,
    /// the Boss fight in the Boss Room, and the end beat before it walks out the far door. The rules live in
    /// <see cref="RoomSequence"/>, <see cref="LevelFights{TEnemyType}"/> and the beat settings; this feeds them frame time
    /// and shows the beats.
    /// The beats are UniTask flows on a cancellation token the Level owns, cancelled when the Level goes away.
    /// </summary>
    public sealed class LevelView : MonoBehaviour
    {
        [SerializeField, Tooltip("The Level's Route, through every Room. Its last Station is where the end beat plays.")]
        private RouteLayout route;

        [SerializeField, Tooltip("The Level's Rooms, in the order the Route walks through them. Matched by order to the Level definition's Rooms.")]
        private Room[] rooms = Array.Empty<Room>();

        [SerializeField, Tooltip("The doorway into each Room after the first, in order. Each sits on a point of the Route.")]
        private Transform[] doorways = Array.Empty<Transform>();

        [Header("Boss")]
        [SerializeField, Tooltip("The Boss Room, after the last of the Rooms above. The Route's second-last Station stands in it.")]
        private Room bossRoom;

        [SerializeField, Tooltip("The doorway into the Boss Room. It sits on a point of the Route.")]
        private Transform bossDoorway;

        [SerializeField, Tooltip("The Boss fight in the Boss Room. Without it (or a Boss in the Level definition) the Level has no Boss.")]
        private ColossusView boss;

        [Header("Beats")]
        [SerializeField, Tooltip("Shadowless light behind the entrance door. It fades to Darkness during the start beat.")]
        private Light entranceLight;

        [SerializeField, Tooltip("Shadowless light by the far door. It rises during the calm of the end beat.")]
        private Light exitLight;

        [SerializeField] private TitleCardView titleCard;
        [SerializeField] private ScreenFadeView screenFade;

        private LevelDefinition definition;
        private int levelIndex;
        private RouteProgress progress;
        private RoomSequence sequence;
        private EnemyPools pools;
        private LevelSpawner spawner;
        private StragglerSchedule<EnemyDefinition> stragglers;
        private LevelFights<EnemyDefinition> fights;
        private StragglerPlan<EnemyDefinition>[] roomStragglers;
        private Station exitStation;
        private Station bossStation;
        private bool hasBoss;
        private bool isWalking;
        private RampStep rampStep;
        private Transform fightingCharacter;
        private EnemyTime enemies;
        private ILoadedContent loadedContent;
        private CancellationTokenSource lifetime;

        /// <summary>Raised when the character walks through a doorway into the next Room.</summary>
        public event Action<Room> RoomEntered;

        /// <summary>Raised once the start beat is over and the character walks the Route.</summary>
        public event Action StartBeatFinished;

        /// <summary>Raised when the character reaches a Station with Waves to fight.</summary>
        public event Action FightStarted;

        /// <summary>Raised when the Station's last Wave is dead and the character walks on.</summary>
        public event Action FightCleared;

        /// <summary>Raised when the character reaches the Boss and its fight starts.</summary>
        public event Action BossFightStarted;

        /// <summary>Raised once the Boss is beaten and the character is back on the Route.</summary>
        public event Action BossFightEnded;

        /// <summary>Raised when the last fight is over and the end beat starts.</summary>
        public event Action EndBeatStarted;

        /// <summary>Raised once the character has walked out the far door and the Level is over.</summary>
        public event Action Finished;

        /// <summary>Raised when any of the Level's Enemies dies.</summary>
        public event Action<Enemy> EnemyDied;

        /// <summary>The Room the Level starts in.</summary>
        public Room FirstRoom => rooms.Length > 0 ? rooms[0] : null;

        public bool IsFinished { get; private set; }

        /// <summary>The Boss of this Level, if it has one.</summary>
        public ColossusView Boss => hasBoss ? boss : null;

        /// <summary>
        /// Starts the Level as Level <paramref name="index"/> of the Run: the start beat plays as the character walks in
        /// along <paramref name="routeProgress"/>, then each Room's fights run as the character reaches them, with their
        /// Waves and Enemies scaled by the <paramref name="ramp"/> at that index. Its Enemies run on
        /// <paramref name="enemyTime"/>, built from the prefabs and sounds already loaded in <paramref name="content"/>.
        /// </summary>
        public void Run(
            LevelDefinition levelDefinition,
            int index,
            Ramp<EnemyDefinition> ramp,
            RouteProgress routeProgress,
            Transform character,
            EnemyTime enemyTime,
            ILoadedContent content)
        {
            Stop();

            definition = levelDefinition ? levelDefinition : throw new ArgumentNullException(nameof(levelDefinition));
            progress = routeProgress ?? throw new ArgumentNullException(nameof(routeProgress));
            if (ramp == null)
            {
                throw new ArgumentNullException(nameof(ramp));
            }

            levelIndex = index;
            rampStep = ramp.StepAt(index);
            fightingCharacter = character;
            enemies = enemyTime;
            loadedContent = content;
            hasBoss = boss != null && definition.Boss != null && bossRoom != null && bossDoorway != null;

            if (rooms.Length != definition.Rooms.Count)
            {
                throw new InvalidOperationException(
                    $"The Level scene has {rooms.Length} Rooms but {definition.name} defines {definition.Rooms.Count}.");
            }

            var stations = route.CreateStations(progress.Path);
            if (stations.Length < (hasBoss ? 2 : 1))
            {
                throw new InvalidOperationException(
                    hasBoss
                        ? "A Level with a Boss needs a Station for the Boss and a last Station for its end beat."
                        : "A Level's Route needs a last Station for its end beat.");
            }

            // The Boss Room comes after the Level's Rooms, through its own doorway.
            var doorwayPositions = new Vector3[doorways.Length + (hasBoss ? 1 : 0)];
            for (var i = 0; i < doorways.Length; i++)
            {
                doorwayPositions[i] = doorways[i].position;
            }

            if (hasBoss)
            {
                doorwayPositions[doorways.Length] = bossDoorway.position;
            }

            sequence = new RoomSequence(RoomSequence.EntryDistances(progress.Path, doorwayPositions));
            var stationWaves = WavesByStation(stations, ramp, rampStep);

            roomStragglers = new StragglerPlan<EnemyDefinition>[rooms.Length];
            var roomSpawnPoints = new RoomSpawnPoints[rooms.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                roomStragglers[i] = definition.Rooms[i].Stragglers.ToPlan();
                roomSpawnPoints[i] = rooms[i].SpawnPoints;
            }

            pools = new EnemyPools(transform, enemyTime, content, rampStep);
            pools.EnemyDied += OnEnemyDied;
            Prewarm(stationWaves, roomStragglers);
            spawner = new LevelSpawner(
                pools,
                roomSpawnPoints,
                progress,
                character,
                definition.SpawnPointLitIntensity,
                definition.StragglerMinMetresAhead);
            stragglers = new StragglerSchedule<EnemyDefinition>(spawner.Stragglers, new SystemRandom());

            // Subscribed before the fights are, so lights created since the last Station count from the first spawn.
            progress.StationReached += OnStationReached;
            fights = new LevelFights<EnemyDefinition>(progress, stationWaves, spawner.Waves, stragglers, hasBoss);
            fights.ExitReached += OnExitReached;
            fights.BossReached += OnBossReached;
            fights.FightStarted += OnFightStarted;
            fights.FightCleared += OnFightCleared;
            sequence.RoomEntered += OnRoomEntered;
            Enter(0);

            exitStation = null;
            bossStation = null;
            isWalking = false;
            IsFinished = false;
            lifetime = new CancellationTokenSource();
            PlayAsync(lifetime.Token).Forget();
        }

        private void Update()
        {
            if (fights == null)
            {
                return;
            }

            sequence.Follow(progress.Distance);
            fights.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            Stop();
        }

        private async UniTask PlayAsync(CancellationToken cancellation)
        {
            await PlayStartBeatAsync(cancellation);
            StartBeatFinished?.Invoke();

            fights.StragglersAllowed = true;
            isWalking = true;
            if (hasBoss)
            {
                await FightBossAsync(cancellation);
            }

            await UniTask.WaitUntil(() => exitStation != null, cancellationToken: cancellation);
            fights.StragglersAllowed = false;

            // The calm only comes once nothing is left hunting the character.
            await UniTask.WaitUntil(() => fights.AliveCount == 0, cancellationToken: cancellation);
            EndBeatStarted?.Invoke();
            await PlayEndBeatAsync(cancellation);

            IsFinished = true;
            Finished?.Invoke();
        }

        // The Boss Room ends the Level: the character stops at the Boss's Station until the Boss is beaten.
        private async UniTask FightBossAsync(CancellationToken cancellation)
        {
            await UniTask.WaitUntil(() => bossStation != null, cancellationToken: cancellation);
            fights.StragglersAllowed = false;
            BossFightStarted?.Invoke();

            await boss.FightAsync(definition.Boss, rampStep, progress.Position, fightingCharacter, enemies, loadedContent, cancellation);

            BossFightEnded?.Invoke();
            bossStation.Clear();
        }

        /// <summary>
        /// Debug: drops the current Station's fight and puts the character straight on the Boss's Station.
        /// Only once the start beat is over and before the Boss fight. The key that calls it is compiled out of release builds.
        /// </summary>
        /// <returns>Whether the character skipped ahead.</returns>
        public bool SkipToBoss()
        {
            if (!hasBoss || fights == null || !isWalking || bossStation != null || exitStation != null)
            {
                return false;
            }

            // The character's own Stations, not the ones made here to plan the fights.
            var stations = progress.Stations;
            fights.AbandonCurrent();
            return progress.SkipTo(stations[stations.Count - 2]);
        }

        private async UniTask PlayStartBeatAsync(CancellationToken cancellation)
        {
            var start = definition.Start;
            titleCard.Show(definition.TitleFor(levelIndex));
            entranceLight.color = start.DoorLightColor;
            entranceLight.range = start.DoorLightRange;
            entranceLight.enabled = true;

            for (var elapsed = 0f; elapsed < start.DurationSeconds; elapsed += Time.deltaTime)
            {
                titleCard.Alpha = start.TitleAlpha(elapsed);
                entranceLight.intensity = start.DoorLightIntensityAt(elapsed);
                await UniTask.NextFrame(cancellation);
            }

            titleCard.Alpha = 0f;
            entranceLight.intensity = 0f;
            entranceLight.enabled = false;
        }

        private async UniTask PlayEndBeatAsync(CancellationToken cancellation)
        {
            var end = definition.End;
            exitLight.color = end.CalmLightColor;
            exitLight.range = end.CalmLightRange;
            exitLight.enabled = true;

            for (var elapsed = 0f; elapsed < end.CalmSeconds; elapsed += Time.deltaTime)
            {
                exitLight.intensity = end.CalmLightIntensityAt(elapsed);
                await UniTask.NextFrame(cancellation);
            }

            // The character walks out the far door as the screen fades to black.
            exitStation.Clear();
            for (var elapsed = 0f; elapsed < end.FadeOutSeconds || !progress.IsFinished; elapsed += Time.deltaTime)
            {
                screenFade.Alpha = end.ScreenFadeAt(elapsed);
                await UniTask.NextFrame(cancellation);
            }

            screenFade.Alpha = 1f;
            titleCard.Show(definition.ClearedTitleFor(levelIndex));
            for (var elapsed = 0f; elapsed <= 0f || end.CardAlphaAt(elapsed) < 1f; elapsed += Time.deltaTime)
            {
                titleCard.Alpha = end.CardAlphaAt(elapsed);
                await UniTask.NextFrame(cancellation);
            }

            titleCard.Alpha = 1f;

            // A moment to read it before the next Level starts.
            for (var elapsed = 0f; elapsed < end.CardHoldSeconds; elapsed += Time.deltaTime)
            {
                await UniTask.NextFrame(cancellation);
            }
        }

        // Each Station's Waves, from the definition of the Room it stands in, scaled by the Ramp.
        // The last Station is the end beat's.
        private IReadOnlyList<WavePlan<EnemyDefinition>>[] WavesByStation(
            IReadOnlyList<Station> stations,
            Ramp<EnemyDefinition> ramp,
            RampStep rampStep)
        {
            var assigned = LevelStations.AssignToRooms(stations, sequence);
            // Every Station but the end beat's, and but the Boss's in a Level with a Boss.
            var stationWaves = new IReadOnlyList<WavePlan<EnemyDefinition>>[stations.Count - (hasBoss ? 2 : 1)];
            for (var i = 0; i < stationWaves.Length; i++)
            {
                var (room, indexInRoom) = assigned[i];
                var authored = definition.Rooms[room].Stations;
                stationWaves[i] = indexInRoom < authored.Count
                    ? ramp.ScaleStation(authored[indexInRoom].ToPlans(), rampStep)
                    : Array.Empty<WavePlan<EnemyDefinition>>();
            }

            return stationWaves;
        }

        private void OnRoomEntered(int room)
        {
            if (room >= rooms.Length)
            {
                // The Boss Room: no Waves or Stragglers start there.
                stragglers.Follow(null);
                RoomEntered?.Invoke(bossRoom);
                return;
            }

            Enter(room);
            RoomEntered?.Invoke(rooms[room]);
        }

        private void Enter(int room)
        {
            spawner.CurrentRoom = room;
            spawner.RefreshLights();
            stragglers.Follow(roomStragglers[room]);
        }

        private void OnStationReached(Station _)
        {
            spawner.RefreshLights();
        }

        private void OnExitReached(Station station)
        {
            exitStation = station;
        }

        private void OnBossReached(Station station)
        {
            bossStation = station;
        }

        private void OnFightStarted(Station _)
        {
            FightStarted?.Invoke();
        }

        private void OnFightCleared(Station _)
        {
            FightCleared?.Invoke();
        }

        private void Stop()
        {
            if (lifetime != null)
            {
                lifetime.Cancel();
                lifetime.Dispose();
                lifetime = null;
            }

            if (fights == null)
            {
                return;
            }

            progress.StationReached -= OnStationReached;
            fights.ExitReached -= OnExitReached;
            fights.BossReached -= OnBossReached;
            fights.FightStarted -= OnFightStarted;
            fights.FightCleared -= OnFightCleared;
            sequence.RoomEntered -= OnRoomEntered;
            fights.Dispose();
            pools.EnemyDied -= OnEnemyDied;
            pools.Dispose();
            fights = null;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            EnemyDied?.Invoke(enemy);
        }

        // Enough of each Enemy type for the Station that needs the most of it, plus every Room's Stragglers,
        // so fights don't instantiate.
        private void Prewarm(
            IReadOnlyList<IReadOnlyList<WavePlan<EnemyDefinition>>> stationWaves,
            IReadOnlyList<StragglerPlan<EnemyDefinition>> stragglerPlans)
        {
            var most = new Dictionary<EnemyDefinition, int>();
            var atStation = new Dictionary<EnemyDefinition, int>();
            foreach (var plans in stationWaves)
            {
                atStation.Clear();
                foreach (var plan in plans)
                {
                    for (var i = 0; i < plan.Count; i++)
                    {
                        var enemyType = plan.EnemyTypeAt(i);
                        atStation.TryGetValue(enemyType, out var count);
                        atStation[enemyType] = count + 1;
                    }
                }

                foreach (var pair in atStation)
                {
                    most.TryGetValue(pair.Key, out var count);
                    most[pair.Key] = Math.Max(count, pair.Value);
                }
            }

            var stragglersMost = new Dictionary<EnemyDefinition, int>();
            foreach (var plan in stragglerPlans)
            {
                if (plan == null)
                {
                    continue;
                }

                stragglersMost.TryGetValue(plan.EnemyType, out var count);
                stragglersMost[plan.EnemyType] = Math.Max(count, plan.MaxAlive);
            }

            foreach (var pair in stragglersMost)
            {
                most.TryGetValue(pair.Key, out var count);
                most[pair.Key] = count + pair.Value;
            }

            foreach (var pair in most)
            {
                pools.Prewarm(pair.Key, pair.Value);
            }
        }
    }
}
