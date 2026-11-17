using System;
using System.Collections.Generic;
using DarknessRises.Game.Levels;
using DarknessRises.Game.Routes;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Levels
{
    public sealed class RoomSequenceTests
    {
        [Test]
        public void Rooms_begin_at_the_Route_points_closest_to_their_doorways()
        {
            var path = new RoutePath(new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 5f),
                new Vector3(20f, 0f, 5f),
            });

            var entries = RoomSequence.EntryDistances(path, new[] { new Vector3(10.1f, 0f, 0f), new Vector3(20f, 0f, 5f) });

            Assert.That(entries, Is.EqualTo(new[] { 0f, 10f, 25f }));
        }

        [Test]
        public void Walking_through_a_doorway_enters_the_next_Room_once()
        {
            var sequence = new RoomSequence(new[] { 0f, 10f, 25f });
            var entered = new List<int>();
            sequence.RoomEntered += entered.Add;

            sequence.Follow(5f);
            Assert.That(sequence.Current, Is.EqualTo(0));
            Assert.That(entered, Is.Empty);

            sequence.Follow(10f);
            sequence.Follow(12f);
            Assert.That(sequence.Current, Is.EqualTo(1));
            Assert.That(entered, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void A_long_step_through_two_doorways_enters_both_Rooms_in_order()
        {
            var sequence = new RoomSequence(new[] { 0f, 10f, 25f });
            var entered = new List<int>();
            sequence.RoomEntered += entered.Add;

            sequence.Follow(30f);

            Assert.That(entered, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Rooms_must_begin_at_the_start_and_follow_the_Route_in_order()
        {
            Assert.Throws<ArgumentException>(() => new RoomSequence(new[] { 2f, 10f }));
            Assert.Throws<ArgumentException>(() => new RoomSequence(new[] { 0f, 10f, 10f }));
            Assert.Throws<ArgumentException>(() => new RoomSequence(Array.Empty<float>()));
        }

        [Test]
        public void Each_Station_is_numbered_within_the_Room_it_stands_in()
        {
            var sequence = new RoomSequence(new[] { 0f, 10f, 25f });
            var stations = new[]
            {
                new Station(0, 3f),
                new Station(1, 8f),
                new Station(2, 18f),
                new Station(3, 26f),
                new Station(4, 30f),
            };

            var assigned = LevelStations.AssignToRooms(stations, sequence);

            Assert.That(assigned, Is.EqualTo(new[] { (0, 0), (0, 1), (1, 0), (2, 0), (2, 1) }));
        }
    }
}
