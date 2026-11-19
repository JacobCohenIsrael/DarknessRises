using System.Collections.Generic;
using DarknessRises.Game.Enemies;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Waves
{
    public sealed class StationFightTests
    {
        private const string Shambler = "Shambler";

        private sealed class FakeSpawner : IEnemySpawner<string>
        {
            public readonly List<Enemy> Alive = new();

            public Enemy TrySpawn(string enemyType)
            {
                var enemy = new Enemy();
                enemy.Spawn(1);
                Alive.Add(enemy);
                return enemy;
            }

            public void KillAll()
            {
                foreach (var enemy in Alive.ToArray())
                {
                    enemy.TakeHit(1);
                }

                Alive.Clear();
            }
        }

        // Ready 2 s, Clear 1 s, Quiet 0.5 s, Go 1 s.
        private static readonly StationRhythmTiming Timing = new(2f, 1f, 0.5f, 1f);

        private FakeSpawner spawner;
        private StationRhythm rhythm;
        private StationFight<string> fight;
        private Station station;
        private List<StationBeat> beats;

        [SetUp]
        public void SetUp()
        {
            spawner = new FakeSpawner();
            rhythm = new StationRhythm(Timing);
            fight = new StationFight<string>(rhythm);
            station = new Station(0, 5f);
            beats = new List<StationBeat>();
            rhythm.BeatChanged += beats.Add;
        }

        private WaveSchedule<string> Waves(params WavePlan<string>[] waves)
        {
            return new WaveSchedule<string>(waves, spawner);
        }

        private static WavePlan<string> OneShambler(float delaySeconds = 0f)
        {
            return new WavePlan<string>(delaySeconds, 0f, new[] { Shambler });
        }

        [Test]
        public void No_Enemy_comes_during_the_Ready_beat()
        {
            Assert.That(fight.Begin(station, Waves(OneShambler())), Is.True);

            fight.Tick(1.9f);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Ready));
            Assert.That(spawner.Alive, Is.Empty);
            Assert.That(fight.Station, Is.SameAs(station));
        }

        [Test]
        public void The_Waves_start_when_the_Ready_beat_ends()
        {
            fight.Begin(station, Waves(OneShambler()));

            fight.Tick(1.9f);
            fight.Tick(0.2f);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Fight));
            Assert.That(spawner.Alive, Has.Count.EqualTo(1));
        }

        [Test]
        public void A_Waves_own_delay_counts_from_the_end_of_the_Ready_beat()
        {
            fight.Begin(station, Waves(OneShambler(delaySeconds: 1f)));

            fight.Tick(2f);
            fight.Tick(0.5f);
            Assert.That(spawner.Alive, Is.Empty);

            fight.Tick(0.6f);
            Assert.That(spawner.Alive, Has.Count.EqualTo(1));
        }

        [Test]
        public void The_clear_is_called_when_the_last_Wave_is_dead_and_the_character_still_holds()
        {
            fight.Begin(station, Waves(OneShambler()));
            fight.Tick(2f);

            spawner.KillAll();
            Assert.That(fight.Tick(0.1f), Is.False);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Clear));
            Assert.That(station.IsCleared, Is.False);
            Assert.That(fight.Waves, Is.Not.Null, "The stop isn't over until the character sets off.");
        }

        [Test]
        public void The_character_sets_off_after_the_clear_the_quiet_and_the_go()
        {
            fight.Begin(station, Waves(OneShambler()));
            fight.Tick(2f);
            spawner.KillAll();
            fight.Tick(0f);

            Assert.That(fight.Tick(1f), Is.False);
            Assert.That(fight.Tick(0.5f), Is.False);
            Assert.That(station.IsCleared, Is.False);
            Assert.That(fight.Tick(1f), Is.True, "The stop ends on the tick the Go beat does.");

            Assert.That(station.IsCleared, Is.True);
            Assert.That(fight.Waves, Is.Null);
            Assert.That(fight.Station, Is.Null);
            Assert.That(beats, Is.EqualTo(new[]
            {
                StationBeat.Ready, StationBeat.Fight, StationBeat.Clear, StationBeat.Quiet, StationBeat.Go, StationBeat.Walking,
            }));
            Assert.That(fight.Tick(1f), Is.False, "Nothing more to end.");
        }

        [Test]
        public void A_Station_without_Waves_is_cleared_at_once_with_no_beats()
        {
            Assert.That(fight.Begin(station, Waves()), Is.False);

            Assert.That(station.IsCleared, Is.True);
            Assert.That(beats, Is.Empty);
            Assert.That(fight.Waves, Is.Null);
        }

        [Test]
        public void An_abandoned_stop_leaves_the_Station_uncleared_and_frees_the_rhythm()
        {
            fight.Begin(station, Waves(OneShambler()));
            fight.Tick(2.5f);

            Assert.That(fight.Abandon(), Is.SameAs(station));

            Assert.That(station.IsCleared, Is.False);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Walking));
            Assert.That(fight.Abandon(), Is.Null);
            Assert.That(fight.Begin(new Station(1, 9f), Waves(OneShambler())), Is.True);
        }

        [Test]
        public void Only_one_Station_is_fought_at_a_time()
        {
            fight.Begin(station, Waves(OneShambler()));

            Assert.That(
                () => fight.Begin(new Station(1, 9f), Waves(OneShambler())),
                Throws.TypeOf<System.InvalidOperationException>());
        }

        [Test]
        public void Ticking_through_a_fight_does_not_allocate()
        {
            fight.Begin(station, Waves(OneShambler(delaySeconds: 1000f)));
            fight.Tick(2f);

            AllocationAssert.DoesNotAllocate(() => fight.Tick(0.001f));
        }
    }
}
