using UnityEngine;

namespace DarknessRises.Game.Gestures
{
    /// <summary>The player command a Gesture stands for.</summary>
    public enum GestureCommand
    {
        None,

        /// <summary>Fire once (a Tap).</summary>
        FireOnce,

        /// <summary>Start holding Fire (a Hold started).</summary>
        FireHold,

        /// <summary>Let go of Fire (the Hold ended).</summary>
        FireRelease,

        /// <summary>Throw a Grenade. No Gesture means it any more; <see cref="GestureInput"/> turns an armed Grenade button's Room tap into it.</summary>
        Grenade,
        Reload,
        Flare,
        SlowMo,

        /// <summary>Relocate in the Flick's direction. Only a Boss fight that allows Relocate acts on it.</summary>
        Relocate,
    }

    /// <summary>
    /// Which command each Gesture means: Tap or Hold fires, Two-finger tap triggers Slow-mo,
    /// and a Flick reloads when it's down, Flares when it's up and Relocates in any other direction.
    /// </summary>
    /// <remarks>
    /// Double-tap means nothing: on touch the Grenade is thrown with the Grenade button, not a Gesture, so a tap fires
    /// at once with no stray shot. The Double-tap recognizer stays registered for future use.
    /// The mapping never depends on the fight: a Flick down reloads and a Flick up Flares in a Boss fight too.
    /// A sideways Flick outside a Boss fight still means Relocate, which nothing acts on there.
    /// </remarks>
    public static class GestureCommands
    {
        public static GestureCommand For(in Gesture gesture, float flickAxisHalfAngleDegrees)
        {
            switch (gesture.Kind)
            {
                case GestureKind.Tap:
                    return GestureCommand.FireOnce;
                case GestureKind.HoldStarted:
                    return GestureCommand.FireHold;
                case GestureKind.HoldEnded:
                    return GestureCommand.FireRelease;
                case GestureKind.TwoFingerTap:
                    return GestureCommand.SlowMo;
                case GestureKind.Flick:
                    return ForFlick(gesture.Direction, flickAxisHalfAngleDegrees);
                default:
                    return GestureCommand.None;
            }
        }

        private static GestureCommand ForFlick(Vector2 direction, float halfAngleDegrees)
        {
            if (direction == Vector2.zero)
            {
                return GestureCommand.None;
            }

            if (Vector2.Angle(direction, Vector2.down) <= halfAngleDegrees)
            {
                return GestureCommand.Reload;
            }

            if (Vector2.Angle(direction, Vector2.up) <= halfAngleDegrees)
            {
                return GestureCommand.Flare;
            }

            return GestureCommand.Relocate;
        }
    }
}
