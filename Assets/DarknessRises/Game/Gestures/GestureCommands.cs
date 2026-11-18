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
    /// Which command each Gesture means: Tap or Hold fires, Two-finger double-tap reloads, a Two-finger flick up Flares
    /// and down triggers Slow-mo, and a one-finger Flick in any direction Relocates that way.
    /// </summary>
    /// <remarks>
    /// One-finger Flicks are all Relocate so that flicking toward a Position never triggers a Skill. The mapping never
    /// depends on the fight: outside a Boss fight a Flick still means Relocate, which nothing acts on there.
    /// Reload is two-fingered so that quickly tapping two targets with one finger is two Shots, never a Reload.
    /// Neither Two-finger tap of a Two-finger double-tap fires: two fingers are never a Tap.
    /// A Two-finger flick sideways and a single Two-finger tap mean nothing.
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
                case GestureKind.TwoFingerDoubleTap:
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
