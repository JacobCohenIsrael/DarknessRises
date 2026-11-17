using System;
using System.Collections.Generic;
using DarknessRises.Game.Waves;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarknessRises.Game.EditModeTests.Waves
{
    public sealed class SpawnPointPickerTests
    {
        private const int Seed = 1234;
        private const int Picks = 200;

        private sealed class FakeLighting : ISpawnPointLighting
        {
            private readonly HashSet<int> lit;

            public FakeLighting(params int[] litSpawnPoints)
            {
                lit = new HashSet<int>(litSpawnPoints);
            }

            public bool IsLit(int spawnPoint) => lit.Contains(spawnPoint);
        }

        [Test]
        public void Lit_Spawn_points_are_never_picked()
        {
            var picker = new SpawnPointPicker(5, new Random(Seed));
            var lighting = new FakeLighting(0, 2, 3);

            for (var i = 0; i < Picks; i++)
            {
                Assert.That(picker.TryPick(lighting, out var spawnPoint), NUnit.Framework.Is.True);
                Assert.That(spawnPoint, NUnit.Framework.Is.EqualTo(1).Or.EqualTo(4));
            }
        }

        [Test]
        public void Nothing_is_picked_when_every_Spawn_point_is_lit()
        {
            var picker = new SpawnPointPicker(3, new Random(Seed));

            Assert.That(picker.TryPick(new FakeLighting(0, 1, 2), out _), NUnit.Framework.Is.False);
        }

        [Test]
        public void A_Room_without_Spawn_points_has_nowhere_to_spawn()
        {
            var picker = new SpawnPointPicker(0, new Random(Seed));

            Assert.That(picker.TryPick(new FakeLighting(), out _), NUnit.Framework.Is.False);
        }

        [Test]
        public void The_only_dark_Spawn_point_is_picked_even_if_it_was_just_used()
        {
            var picker = new SpawnPointPicker(3, new Random(Seed));
            var lighting = new FakeLighting(0, 2);

            picker.TryPick(lighting, out var first);
            picker.TryPick(lighting, out var second);

            Assert.That(first, NUnit.Framework.Is.EqualTo(1));
            Assert.That(second, NUnit.Framework.Is.EqualTo(1));
        }

        [Test]
        public void The_same_Spawn_point_is_not_picked_twice_in_a_row_while_another_is_dark()
        {
            var picker = new SpawnPointPicker(4, new Random(Seed));
            var lighting = new FakeLighting();

            picker.TryPick(lighting, out var previous);
            for (var i = 0; i < Picks; i++)
            {
                picker.TryPick(lighting, out var spawnPoint);
                Assert.That(spawnPoint, NUnit.Framework.Is.Not.EqualTo(previous));
                previous = spawnPoint;
            }
        }

        [Test]
        public void Every_dark_Spawn_point_gets_used()
        {
            var picker = new SpawnPointPicker(4, new Random(Seed));
            var lighting = new FakeLighting(1);
            var used = new HashSet<int>();

            for (var i = 0; i < Picks; i++)
            {
                picker.TryPick(lighting, out var spawnPoint);
                used.Add(spawnPoint);
            }

            Assert.That(used, NUnit.Framework.Is.EquivalentTo(new[] { 0, 2, 3 }));
        }

        [Test]
        public void Picking_does_not_allocate()
        {
            var picker = new SpawnPointPicker(6, new Random(Seed));
            var lighting = new FakeLighting(2, 5);
            picker.TryPick(lighting, out _);

            Assert.That(() => { picker.TryPick(lighting, out _); }, Is.Not.AllocatingGCMemory());
        }
    }
}
