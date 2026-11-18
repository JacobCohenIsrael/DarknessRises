using System;
using System.Linq;
using DarknessRises.Game.Runs;
using DarknessRises.Game.Waves;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Runs
{
    public sealed class RampTests
    {
        private const string Shambler = "Shambler";
        private const string Brute = "Brute";
        private const string Runner = "Runner";

        private static WavePlan<string> Wave(params string[] order) => new(1.5f, 0.8f, order);

        private static string[] Order(WavePlan<string> wave) =>
            Enumerable.Range(0, wave.Count).Select(wave.EnemyTypeAt).ToArray();

        [Test]
        public void The_first_Level_is_as_authored()
        {
            var ramp = new Ramp<string>(
                new RampTuning(
                    waveSizeGrowthPerLevel: 0.5f,
                    levelsPerExtraWave: 1,
                    maxExtraWaves: 3,
                    enemyHitPointsGrowthPerLevel: 0.5f,
                    enemySpeedGrowthPerLevel: 0.5f,
                    runnersFromLevel: 2,
                    firstRunnerShare: 0.5f,
                    bossHitPointsGrowthPerLevel: 0.5f),
                Runner);

            var step = ramp.StepAt(1);
            var waves = ramp.ScaleStation(new[] { Wave(Shambler, Shambler, Shambler) }, step);

            Assert.That(waves, Has.Length.EqualTo(1));
            Assert.That(Order(waves[0]), Is.EqualTo(new[] { Shambler, Shambler, Shambler }));
            Assert.That(step.EnemyHitPoints(4), Is.EqualTo(4));
            Assert.That(step.EnemySpeed(1.4f), Is.EqualTo(1.4f));
            Assert.That(step.BossHitPoints(100), Is.EqualTo(100));
            Assert.That(step.HasRunners, Is.False);
        }

        [Test]
        public void Waves_grow_each_Level_by_repeating_their_spawn_order()
        {
            var ramp = new Ramp<string>(new RampTuning(waveSizeGrowthPerLevel: 0.5f), Runner);

            var wave = ramp.ScaleWave(Wave(Shambler, Brute), ramp.StepAt(3));

            Assert.That(Order(wave), Is.EqualTo(new[] { Shambler, Brute, Shambler, Brute }));
            Assert.That(wave.DelaySeconds, Is.EqualTo(1.5f), "Timing stays as authored.");
            Assert.That(wave.SpawnIntervalSeconds, Is.EqualTo(0.8f));
        }

        [Test]
        public void Wave_growth_stops_at_its_cap()
        {
            var ramp = new Ramp<string>(new RampTuning(waveSizeGrowthPerLevel: 1f, maxWaveSizeMultiplier: 2f), Runner);

            Assert.That(ramp.ScaleWave(Wave(Shambler, Shambler), ramp.StepAt(10)).Count, Is.EqualTo(4));
        }

        [Test]
        public void Stations_get_extra_Waves_every_few_Levels_up_to_a_cap()
        {
            var ramp = new Ramp<string>(new RampTuning(levelsPerExtraWave: 2, maxExtraWaves: 2), Runner);
            var authored = new[] { Wave(Shambler), Wave(Brute, Brute) };

            Assert.That(ramp.ScaleStation(authored, ramp.StepAt(2)), Has.Length.EqualTo(2));
            Assert.That(ramp.ScaleStation(authored, ramp.StepAt(3)), Has.Length.EqualTo(3));
            Assert.That(ramp.ScaleStation(authored, ramp.StepAt(5)), Has.Length.EqualTo(4));
            Assert.That(ramp.ScaleStation(authored, ramp.StepAt(99)), Has.Length.EqualTo(4));

            var withExtra = ramp.ScaleStation(authored, ramp.StepAt(3));
            Assert.That(Order(withExtra[2]), Is.EqualTo(new[] { Brute, Brute }), "An extra Wave repeats the last one.");
        }

        [Test]
        public void A_Station_without_Waves_stays_without()
        {
            var ramp = new Ramp<string>(new RampTuning(levelsPerExtraWave: 1, maxExtraWaves: 5), Runner);

            Assert.That(ramp.ScaleStation(Array.Empty<WavePlan<string>>(), ramp.StepAt(4)), Is.Empty);
        }

        [Test]
        public void Enemies_get_tougher_and_faster_each_Level_up_to_their_caps()
        {
            var ramp = new Ramp<string>(
                new RampTuning(
                    enemyHitPointsGrowthPerLevel: 0.25f,
                    maxEnemyHitPointsMultiplier: 2f,
                    enemySpeedGrowthPerLevel: 0.1f,
                    maxEnemySpeedMultiplier: 1.3f),
                Runner);

            var third = ramp.StepAt(3);
            Assert.That(third.EnemyHitPoints(4), Is.EqualTo(6));
            Assert.That(third.EnemySpeed(2f), Is.EqualTo(2.4f).Within(1e-5f));

            var far = ramp.StepAt(20);
            Assert.That(far.EnemyHitPoints(4), Is.EqualTo(8));
            Assert.That(far.EnemySpeed(2f), Is.EqualTo(2.6f).Within(1e-5f));
        }

        [Test]
        public void Scaled_hit_points_round_and_never_drop_below_one()
        {
            var ramp = new Ramp<string>(new RampTuning(enemyHitPointsGrowthPerLevel: 0.25f), Runner);

            Assert.That(ramp.StepAt(2).EnemyHitPoints(2), Is.EqualTo(3), "2.5 rounds up.");
            Assert.That(ramp.StepAt(1).EnemyHitPoints(0), Is.EqualTo(1));
        }

        [Test]
        public void The_Boss_gets_tougher_each_Level_without_a_cap()
        {
            var ramp = new Ramp<string>(new RampTuning(bossHitPointsGrowthPerLevel: 0.5f), Runner);

            Assert.That(ramp.StepAt(2).BossHitPoints(100), Is.EqualTo(150));
            Assert.That(ramp.StepAt(21).BossHitPoints(100), Is.EqualTo(1100));
        }

        [Test]
        public void Runners_appear_from_the_configured_Level()
        {
            var ramp = new Ramp<string>(new RampTuning(runnersFromLevel: 3, firstRunnerShare: 0.25f), Runner);
            var authored = Wave(Shambler, Shambler, Shambler, Shambler);

            Assert.That(ramp.StepAt(2).HasRunners, Is.False);
            Assert.That(Order(ramp.ScaleWave(authored, ramp.StepAt(2))), Has.No.Member(Runner));

            Assert.That(ramp.StepAt(3).HasRunners, Is.True);
            Assert.That(Order(ramp.ScaleWave(authored, ramp.StepAt(3))), Is.EqualTo(new[] { Shambler, Shambler, Shambler, Runner }));
        }

        [Test]
        public void Runners_take_a_growing_share_of_each_Wave_up_to_a_cap()
        {
            var ramp = new Ramp<string>(
                new RampTuning(runnersFromLevel: 2, firstRunnerShare: 0.2f, runnerShareGrowthPerLevel: 0.2f, maxRunnerShare: 0.5f),
                Runner);
            var authored = Wave(Enumerable.Repeat(Shambler, 10).ToArray());

            int RunnersAt(int level) => Order(ramp.ScaleWave(authored, ramp.StepAt(level))).Count(type => type == Runner);

            Assert.That(RunnersAt(2), Is.EqualTo(2));
            Assert.That(RunnersAt(3), Is.EqualTo(4));
            Assert.That(RunnersAt(4), Is.EqualTo(5));
            Assert.That(RunnersAt(9), Is.EqualTo(5));
        }

        [Test]
        public void Runners_are_spread_through_the_Wave_and_the_Wave_keeps_its_size()
        {
            var ramp = new Ramp<string>(new RampTuning(runnersFromLevel: 1, firstRunnerShare: 0.4f), Runner);

            var wave = ramp.ScaleWave(Wave(Shambler, Shambler, Shambler, Shambler, Shambler), ramp.StepAt(1));

            Assert.That(Order(wave), Is.EqualTo(new[] { Shambler, Shambler, Runner, Shambler, Runner }));
        }

        [Test]
        public void Even_a_small_Wave_gets_a_Runner_once_they_come()
        {
            var ramp = new Ramp<string>(new RampTuning(runnersFromLevel: 1, firstRunnerShare: 0.1f), Runner);

            Assert.That(Order(ramp.ScaleWave(Wave(Shambler, Shambler), ramp.StepAt(1))), Is.EqualTo(new[] { Shambler, Runner }));
        }

        [Test]
        public void Without_a_Runner_no_Runners_come()
        {
            var ramp = new Ramp<string>(new RampTuning(runnersFromLevel: 1, firstRunnerShare: 0.5f), null);

            Assert.That(ramp.StepAt(4).HasRunners, Is.False);
            Assert.That(Order(ramp.ScaleWave(Wave(Shambler, Shambler), ramp.StepAt(4))), Is.EqualTo(new[] { Shambler, Shambler }));
        }

        [Test]
        public void Runners_and_growth_combine()
        {
            var ramp = new Ramp<string>(
                new RampTuning(waveSizeGrowthPerLevel: 0.5f, runnersFromLevel: 3, firstRunnerShare: 0.5f),
                Runner);

            var wave = ramp.ScaleWave(Wave(Shambler, Shambler), ramp.StepAt(3));

            Assert.That(Order(wave), Is.EqualTo(new[] { Shambler, Runner, Shambler, Runner }));
        }

        [Test]
        public void Levels_count_from_1()
        {
            var ramp = new Ramp<string>(new RampTuning(), Runner);

            Assert.That(() => ramp.StepAt(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void The_Ramp_never_weakens_anything()
        {
            Assert.That(() => new RampTuning(maxEnemySpeedMultiplier: 0.5f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RampTuning(waveSizeGrowthPerLevel: -0.1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RampTuning(firstRunnerShare: 1.5f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
