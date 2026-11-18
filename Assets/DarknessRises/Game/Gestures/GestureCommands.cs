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

        /// <summary>Relocate in the one-finger Flick's direction. Only a Boss fight that allows Relocate acts on it.</summary>
        Relocate,
    }

    /// <summary>
    /// Which command each Gesture means: Tap or Hold fires, Double-tap reloads, a Two-finger flick up Flares and down
    /// triggers Slow-mo, and a one-finger Flick in any direction Relocates that way.
    /// </summary>
    /// <remarks>
    /// One-finger Flicks are all Relocate so that flicking toward a Position never triggers a Skill. The mapping never
    /// depends on the fight: outside a Boss fight a Flick still means Relocate, which nothing acts on there.
    /// A Double-tap's first Tap still fires a shot, because a Tap fires without waiting to see if a second follows.
    /// A Two-finger flick sideways and a Two-finger tap mean nothing; the Two-finger tap recognizer stays registered
    /// for future use.
    /// </remarks>
    public static class GestureCommands
    {
        public static GestureCommand For(in Gesture gesture, float twoFingerFlickAxisHalfAngleDegrees)
        {
            switch (gesture.Kind)
            {
                case GestureKind.Tap:
                    return GestureCommand.FireOnce;
                case GestureKind.HoldStarted:
                    return GestureCommand.FireHold;
                case GestureKind.HoldEnded:
                    return GestureCommand.FireRelease;
                case GestureKind.DoubleTap:
                    return GestureCommand.Reload;
                case GestureKind.Flick:
                    return gesture.Direction == Vector2.zero ? GestureCommand.None : GestureCommand.Relocate;
                case GestureKind.TwoFingerFlick:
                    return ForTwoFingerFlick(gesture.Direction, twoFingerFlickAxisHalfAngleDegrees);
                default:
                    return GestureCommand.None;
            }
        }

        private static GestureCommand ForTwoFingerFlick(Vector2 direction, float halfAngleDegrees)
        {
            if (direction == Vector2.zero)
            {
                return GestureCommand.None;
            }

            if (Vector2.Angle(direction, Vector2.up) <= halfAngleDegrees)
            {
                return GestureCommand.Flare;
            }

            if (Vector2.Angle(direction, Vector2.down) <= halfAngleDegrees)
            {
                return GestureCommand.SlowMo;
            }

            return GestureCommand.None;
        }
    }
}
