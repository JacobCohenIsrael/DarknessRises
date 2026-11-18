using DarknessRises.Game.Bosses;
using DarknessRises.Game.EditModeTests.Levels;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Bosses
{
    public sealed class ColossusAddDropsTests
    {
        private static readonly Vector3[] Points =
        {
            new(0f, 0f, 0f),
            new(10f, 0f, 0f),
            new(20f, 0f, 0f),
        };

        [Test]
        public void Adds_drop_only_where_the_character_is_far_enough_away()
        {
            var random = new ScriptedRandom();
            random.Next(0f, 1.5f);

            Assert.That(ColossusAddDrops.Pick(Points, new Vector3(1f, 0f, 0f), 6f, random), Is.EqualTo(1));
            Assert.That(ColossusAddDrops.Pick(Points, new Vector3(1f, 0f, 0f), 6f, random), Is.EqualTo(2));
            Assert.That(random.LastRange, Is.EqualTo((0f, 2f)), "Two points are far enough.");
        }

        [Test]
        public void With_none_far_enough_the_farthest_one_is_used()
        {
            Assert.That(ColossusAddDrops.Pick(Points, new Vector3(9f, 0f, 0f), 50f, new ScriptedRandom()), Is.EqualTo(2));
        }

        [Test]
        public void Height_doesnt_count()
        {
            var random = new ScriptedRandom();
            random.Next(0f);

            Assert.That(ColossusAddDrops.Pick(Points, new Vector3(0f, 30f, 0f), 6f, random), Is.EqualTo(1));
        }
    }
}
