using DarknessRises.Game.Hud;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Hud
{
    public sealed class GoCueTests
    {
        private const float Tolerance = 1e-4f;

        // A camera looking north (+z), tilted 60 degrees down.
        private static readonly Quaternion View = Quaternion.Euler(60f, 0f, 0f);

        private static Vector2 OnScreen(Vector3 routeDirection, Quaternion view)
        {
            return GoCue.ScreenDirection(routeDirection, view * Vector3.right, view * Vector3.up);
        }

        [Test]
        public void A_Route_heading_east_points_right()
        {
            var direction = OnScreen(Vector3.right, View);

            Assert.That(direction.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(direction.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(GoCue.AngleDegrees(direction), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void A_Route_heading_away_from_the_camera_points_up()
        {
            var direction = OnScreen(Vector3.forward, View);

            Assert.That(direction.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(direction.y, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(GoCue.AngleDegrees(direction), Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void A_Route_heading_west_points_left()
        {
            var direction = OnScreen(Vector3.left, View);

            Assert.That(GoCue.AngleDegrees(direction), Is.EqualTo(180f).Within(Tolerance));
        }

        [Test]
        public void A_Route_heading_towards_the_camera_points_down()
        {
            var direction = OnScreen(Vector3.back, View);

            Assert.That(GoCue.AngleDegrees(direction), Is.EqualTo(-90f).Within(Tolerance));
        }

        [Test]
        public void The_arrow_follows_a_camera_that_has_turned()
        {
            // The camera now looks east, so a Route heading east runs up the screen.
            var turned = Quaternion.Euler(60f, 90f, 0f);

            var direction = OnScreen(Vector3.right, turned);

            Assert.That(GoCue.AngleDegrees(direction), Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void The_direction_is_always_a_unit_however_steep_the_camera()
        {
            var direction = OnScreen(new Vector3(1f, 0f, 1f).normalized, Quaternion.Euler(25f, 0f, 0f));

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(direction.x, Is.GreaterThan(direction.y), "A shallow camera flattens what runs away from it.");
        }

        [Test]
        public void A_Route_heading_straight_into_the_view_points_up()
        {
            var direction = GoCue.ScreenDirection(View * Vector3.forward, View * Vector3.right, View * Vector3.up);

            Assert.That(direction, Is.EqualTo(Vector2.up));
        }
    }
}
