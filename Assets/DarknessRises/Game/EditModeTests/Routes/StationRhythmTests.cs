using System.Collections.Generic;
using DarknessRises.Game.Routes;
using NUnit.Framework;

namespace DarknessRises.Game.EditModeTests.Routes
{
    public sealed class StationRhythmTests
    {
        // Ready 2 s, Clear 1 s, Quiet 0.5 s, Go 1 s.
        private static readonly StationRhythmTiming Timing = new(2f, 1f, 0.5f, 1f);

        private StationRhythm rhythm;
        private Station station;
        private List<StationBeat> beats;

        [SetUp]
        public void SetUp()
        {
            rhythm = new StationRhythm(Timing);
            station = new Station(0, 5f);
            beats = new List<StationBeat>();
            rhythm.BeatChanged += beats.Add;
        }

        [Test]
        public void Character_walks_until_it_arrives_at_a_Station()
        {
            rhythm.Tick(10f);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Walking));
            Assert.That(rhythm.Station, Is.Null);
            Assert.That(beats, Is.Empty);
        }

        [Test]
        public void Arriving_starts_the_Ready_beat()
        {
            Assert.That(rhythm.Arrive(station), Is.True);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Ready));
            Assert.That(rhythm.Station, Is.SameAs(station));
            Assert.That(rhythm.BeatSeconds, Is.EqualTo(2f));
        }

        [Test]
        public void The_fight_starts_once_the_Ready_beat_is_over()
        {
            rhythm.Arrive(station);

            rhythm.Tick(1.9f);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Ready));

            rhythm.Tick(0.2f);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Fight));
        }

        [Test]
        public void The_fight_lasts_until_it_is_won()
        {
            rhythm.Arrive(station);
            rhythm.Tick(2f);

            rhythm.Tick(600f);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Fight));
            Assert.That(station.IsCleared, Is.False);

            Assert.That(rhythm.WinFight(), Is.True);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Clear));
        }

        [Test]
        public void A_full_stop_plays_every_beat_in_order_then_clears_the_Station()
        {
            rhythm.Arrive(station);
            rhythm.Tick(2f);
            rhythm.WinFight();

            rhythm.Tick(1f);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Quiet));
            rhythm.Tick(0.5f);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Go));
            Assert.That(station.IsCleared, Is.False, "The character holds until the Go cue has played.");
            rhythm.Tick(1f);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Walking));
            Assert.That(station.IsCleared, Is.True);
            Assert.That(rhythm.Station, Is.Null);
            Assert.That(beats, Is.EqualTo(new[]
            {
                StationBeat.Ready, StationBeat.Fight, StationBeat.Clear, StationBeat.Quiet, StationBeat.Go, StationBeat.Walking,
            }));
        }

        [Test]
        public void The_Station_is_already_cleared_when_the_walk_is_announced()
        {
            var clearedWhenWalking = false;
            rhythm.BeatChanged += beat =>
            {
                if (beat == StationBeat.Walking)
                {
                    clearedWhenWalking = station.IsCleared;
                }
            };

            rhythm.Arrive(station);
            rhythm.Tick(2f);
            rhythm.WinFight();
            rhythm.Tick(2.5f);

            Assert.That(clearedWhenWalking, Is.True);
        }

        [Test]
        public void A_long_step_carries_over_into_the_next_beats()
        {
            rhythm.Arrive(station);
            rhythm.Tick(2f);
            rhythm.WinFight();

            // Clear (1 s) and Quiet (0.5 s) pass, leaving 0.25 s of the Go beat played.
            rhythm.Tick(1.75f);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Go));
            Assert.That(rhythm.SecondsInBeat, Is.EqualTo(0.25f).Within(1e-4f));
        }

        [Test]
        public void Beats_of_no_length_pass_in_one_tick()
        {
            rhythm = new StationRhythm(default);
            rhythm.BeatChanged += beats.Add;

            rhythm.Arrive(station);
            rhythm.Tick(0f);
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Fight));

            rhythm.WinFight();
            rhythm.Tick(0f);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Walking));
            Assert.That(station.IsCleared, Is.True);
        }

        [Test]
        public void Winning_only_counts_during_the_fight()
        {
            Assert.That(rhythm.WinFight(), Is.False, "walking");

            rhythm.Arrive(station);
            Assert.That(rhythm.WinFight(), Is.False, "still getting ready");
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Ready));

            rhythm.Tick(2f);
            rhythm.WinFight();
            Assert.That(rhythm.WinFight(), Is.False, "already won");
            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Clear));
        }

        [Test]
        public void A_second_Station_cant_arrive_while_the_beats_play()
        {
            rhythm.Arrive(station);

            Assert.That(rhythm.Arrive(new Station(1, 9f)), Is.False);
            Assert.That(rhythm.Station, Is.SameAs(station));
        }

        [Test]
        public void The_rhythm_plays_again_at_the_next_Station()
        {
            rhythm.Arrive(station);
            rhythm.Tick(2f);
            rhythm.WinFight();
            rhythm.Tick(2.5f);
            var next = new Station(1, 9f);

            Assert.That(rhythm.Arrive(next), Is.True);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Ready));
            Assert.That(rhythm.SecondsInBeat, Is.Zero);
            Assert.That(rhythm.Station, Is.SameAs(next));
        }

        [Test]
        public void Abandoning_goes_back_to_walking_without_clearing_the_Station()
        {
            rhythm.Arrive(station);
            rhythm.Tick(2f);

            Assert.That(rhythm.Abandon(), Is.True);

            Assert.That(rhythm.Beat, Is.EqualTo(StationBeat.Walking));
            Assert.That(rhythm.Station, Is.Null);
            Assert.That(station.IsCleared, Is.False);
            Assert.That(rhythm.Abandon(), Is.False, "nothing left to abandon");
        }

        [Test]
        public void Ticking_does_not_allocate()
        {
            rhythm.Arrive(station);

            AllocationAssert.DoesNotAllocate(() => rhythm.Tick(0.001f));
        }

        [Test]
        public void A_beat_cant_last_less_than_no_time()
        {
            Assert.That(() => new StationRhythmTiming(-1f, 0f, 0f, 0f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => new StationRhythmTiming(0f, 0f, 0f, -1f), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
