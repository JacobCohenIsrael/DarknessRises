using System.Collections.Generic;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Routes;
using DarknessRises.Game.Waves;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Levels
{
    public sealed class LevelFightsTests
    {
        private const string Shambler = "Shambler";
        private const string Straggler = "Straggler";

        private FakeSpawner waveSpawner;
        private FakeSpawner stragglerSpawner;
        private ScriptedRandom random;
        private Station fight;
        private Station exit;
        private RouteProgress progress;
        private LevelFights<string> fights;

        // A 20 m Route walked at 1 m/s, with a fight at 10 m and the Level's end at 18 m.
        [SetUp]
        public void SetUp()
        {
            waveSpawner = new FakeSpawner();
            stragglerSpawner = new FakeSpawner();
            random = new ScriptedRandom();
            fight = new Station(0, 10f);
            exit = new Station(1, 18f);
            progress = new RouteProgress(
                new RoutePath(new[] { Vector3.zero, new Vector3(20f, 0f, 0f) }),
                new[] { fight, exit },
                1f);

            var stationWaves = new IReadOnlyList<WavePlan<string>>[]
            {
                new[] { new WavePlan<string>(0f, 0f, new[] { Shambler, Shambler }) },
            };

            var stragglers = new StragglerSchedule<string>(stragglerSpawner, random);
            random.Next(3f);
            stragglers.Follow(new StragglerPlan<string>(Straggler, 3f, 3f, 2));
            fights = new LevelFights<string>(progress, stationWaves, waveSpawner, stragglers);
        }

        [TearDown]
        public void TearDown()
        {
            fights.Dispose();
        }

        private void Run(float seconds, float step = 0.5f)
        {
            for (var t = 0f; t < seconds; t += step)
            {
                progress.Tick(step);
                fights.Tick(step);
            }
        }

        [Test]
        public void Stragglers_come_while_the_character_keeps_walking()
        {
            fights.StragglersAllowed = true;

            Run(4f);

            Assert.That(stragglerSpawner.Spawned, Is.EqualTo(new[] { Straggler }));
            Assert.That(progress.IsAtStation, Is.False);
            Assert.That(progress.Distance, Is.EqualTo(4f), "A Straggler never stops the character.");
        }

        [Test]
        public void No_Stragglers_come_while_they_arent_allowed()
        {
            fights.StragglersAllowed = false;

            Run(9f);

            Assert.That(stragglerSpawner.Spawned, Is.Empty);
        }

        [Test]
        public void No_Stragglers_come_while_the_character_fights_at_a_Station()
        {
            fights.StragglersAllowed = true;
            Run(10f);
            stragglerSpawner.KillAll();
            var stragglersBefore = stragglerSpawner.Spawned.Count;

            Assert.That(progress.CurrentStation, Is.SameAs(fight));
            Run(30f);

            Assert.That(stragglerSpawner.Spawned, Has.Count.EqualTo(stragglersBefore));
            Assert.That(waveSpawner.Spawned, Has.Count.EqualTo(2));
            Assert.That(fights.AliveCount, Is.EqualTo(2));
        }

        [Test]
        public void Tells_when_a_Station_fight_starts_and_when_it_is_cleared()
        {
            Station started = null;
            Station cleared = null;
            fights.FightStarted += station => started = station;
            fights.FightCleared += station => cleared = station;

            Run(10f);
            Assert.That(started, Is.SameAs(fight));
            Assert.That(cleared, Is.Null);

            waveSpawner.KillAll();
            Run(1f);
            Assert.That(cleared, Is.SameAs(fight));
        }

        [Test]
        public void The_end_of_the_Level_is_no_Station_fight()
        {
            var fightsStarted = 0;
            fights.FightStarted += _ => fightsStarted++;

            Run(10f);
            waveSpawner.KillAll();
            Run(20f);

            Assert.That(fightsStarted, Is.EqualTo(1));
        }

        [Test]
        public void The_last_Station_is_left_to_the_owner_instead_of_being_cleared()
        {
            Station reached = null;
            fights.ExitReached += station => reached = station;

            Run(10f);
            waveSpawner.KillAll();
            Run(20f);

            Assert.That(fight.IsCleared, Is.True);
            Assert.That(reached, Is.SameAs(exit));
            Assert.That(exit.IsCleared, Is.False);
            Assert.That(progress.CurrentStation, Is.SameAs(exit));

            exit.Clear();
            Run(3f);
            Assert.That(progress.IsFinished, Is.True);
        }
    

        [Test]
        public void In_a_Level_with_a_Boss_the_Station_before_the_end_is_the_Boss_and_left_to_the_owner()
        {
            fights.Dispose();
            var boss = new Station(1, 14f);
            var end = new Station(2, 18f);
            progress = new RouteProgress(
                new RoutePath(new[] { Vector3.zero, new Vector3(20f, 0f, 0f) }),
                new[] { fight, boss, end },
                1f);
            var stationWaves = new IReadOnlyList<WavePlan<string>>[]
            {
                new[] { new WavePlan<string>(0f, 0f, new[] { Shambler }) },
            };
            fights = new LevelFights<string>(progress, stationWaves, waveSpawner, new StragglerSchedule<string>(stragglerSpawner, random), hasBoss: true);
            Station bossReached = null;
            Station exitReached = null;
            fights.BossReached += station => bossReached = station;
            fights.ExitReached += station => exitReached = station;

            Run(10f);
            waveSpawner.KillAll();
            Run(10f);

            Assert.That(bossReached, Is.SameAs(boss));
            Assert.That(progress.CurrentStation, Is.SameAs(boss), "The Boss fight holds the character until it's beaten.");
            Assert.That(exitReached, Is.Null);

            boss.Clear();
            Run(5f);
            Assert.That(exitReached, Is.SameAs(end));
        }

        [Test]
        public void An_abandoned_fight_counts_as_cleared()
        {
            Station cleared = null;
            fights.FightCleared += station => cleared = station;
            Run(10f);

            fights.AbandonCurrent();

            Assert.That(cleared, Is.SameAs(fight));
            Assert.That(fights.Current, Is.Null);
        }
    }
}
