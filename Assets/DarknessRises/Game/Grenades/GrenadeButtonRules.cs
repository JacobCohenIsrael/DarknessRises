using UnityEngine;

namespace DarknessRises.Game.Grenades
{
    /// <summary>What the Grenade button is doing.</summary>
    public enum GrenadeButtonState
    {
        /// <summary>Waiting. A tap in the Room fires.</summary>
        Idle,

        /// <summary>Armed: the next tap in the Room throws the Grenade there instead of firing.</summary>
        Armed,

        /// <summary>Picked up by a long press: it follows the finger until it's dropped.</summary>
        Moving,
    }

    /// <summary>What a tap in the Room means.</summary>
    public enum RoomTapOutcome
    {
        Fire,
        ThrowGrenade,
    }

    /// <summary>
    /// The touch Grenade button's rules: tapping it arms and cancels the Grenade, the next tap in the Room throws it,
    /// and holding it picks it up so it can be dragged somewhere else inside the screen's safe area.
    /// Plain C#: time arrives as explicit timestamps and the screen as explicit rects (ADR 0003).
    /// </summary>
    /// <remarks>
    /// The position is the button's centre, kept normalized across the safe area so it survives resolution changes,
    /// and always clamped so the whole button stays inside it. Picking the button up cancels arming; dropping it
    /// leaves it Idle.
    /// </remarks>
    public sealed class GrenadeButtonRules
    {
        private readonly float holdSeconds;
        private float pressedAt;

        /// <param name="holdSeconds">How long a press lasts before it picks the button up.</param>
        /// <param name="normalizedPosition">The button's centre, 0 to 1 across the safe area.</param>
        public GrenadeButtonRules(float holdSeconds, Vector2 normalizedPosition)
        {
            this.holdSeconds = holdSeconds;
            NormalizedPosition = normalizedPosition;
        }

        public GrenadeButtonState State { get; private set; }
        public bool IsArmed => State == GrenadeButtonState.Armed;

        /// <summary>Whether a finger is on the button.</summary>
        public bool IsPressed { get; private set; }

        /// <summary>The button's centre, 0 to 1 across <see cref="SafeArea"/>.</summary>
        public Vector2 NormalizedPosition { get; private set; }

        /// <summary>The part of the screen the button must stay inside, in screen pixels.</summary>
        public Rect SafeArea { get; private set; }

        /// <summary>The button's size in screen pixels.</summary>
        public Vector2 ButtonSize { get; private set; }

        /// <summary>The button's centre in screen pixels.</summary>
        public Vector2 ScreenPosition => SafeArea.min + Vector2.Scale(NormalizedPosition, SafeArea.size);

        /// <summary>
        /// Fits the button to the screen: the safe area it must stay inside and its size, both in screen pixels.
        /// Keeps the normalized position, moved in just enough to keep the whole button inside.
        /// </summary>
        public void Fit(Rect safeArea, Vector2 buttonSize)
        {
            SafeArea = safeArea;
            ButtonSize = buttonSize;
            NormalizedPosition = Normalize(Clamp(ScreenPosition));
        }

        /// <summary>A finger went down on the button at <paramref name="time"/>.</summary>
        public void Press(float time)
        {
            IsPressed = true;
            pressedAt = time;
        }

        /// <summary>Picks the button up once the finger has stayed on it for the hold time.</summary>
        public void Tick(float time)
        {
            if (IsPressed && State != GrenadeButtonState.Moving && time - pressedAt >= holdSeconds)
            {
                State = GrenadeButtonState.Moving;
            }
        }

        /// <summary>While the button is picked up, moves its centre under <paramref name="screenPoint"/>, inside the safe area.</summary>
        public void Drag(Vector2 screenPoint)
        {
            if (State == GrenadeButtonState.Moving)
            {
                NormalizedPosition = Normalize(Clamp(screenPoint));
            }
        }

        /// <summary>
        /// The finger lifted off the button. A short press is a tap: it arms the Grenade when there's a charge
        /// to throw, or cancels it when already armed. Lifting a picked-up button drops it where it is.
        /// </summary>
        /// <returns>Whether the button was dropped somewhere, so its position should be saved.</returns>
        public bool Release(float time, int charges)
        {
            if (!IsPressed)
            {
                return false;
            }

            Tick(time);
            IsPressed = false;
            switch (State)
            {
                case GrenadeButtonState.Moving:
                    State = GrenadeButtonState.Idle;
                    return true;
                case GrenadeButtonState.Armed:
                    State = GrenadeButtonState.Idle;
                    return false;
                default:
                    State = charges > 0 ? GrenadeButtonState.Armed : GrenadeButtonState.Idle;
                    return false;
            }
        }

        /// <summary>
        /// A tap in the Room. While armed it throws the Grenade at the tapped point instead of firing, and the
        /// button goes back to Idle.
        /// </summary>
        public RoomTapOutcome TapRoom()
        {
            if (State != GrenadeButtonState.Armed)
            {
                return RoomTapOutcome.Fire;
            }

            State = GrenadeButtonState.Idle;
            return RoomTapOutcome.ThrowGrenade;
        }

        /// <summary>The character's Grenade charges now. Running out cancels arming.</summary>
        public void UpdateCharges(int charges)
        {
            if (charges <= 0 && State == GrenadeButtonState.Armed)
            {
                State = GrenadeButtonState.Idle;
            }
        }

        /// <summary>Drops whatever the button was doing (it was hidden): not armed, not pressed, not picked up.</summary>
        public void Cancel()
        {
            State = GrenadeButtonState.Idle;
            IsPressed = false;
        }

        private Vector2 Clamp(Vector2 centre)
        {
            var half = ButtonSize * 0.5f;
            return new Vector2(
                ClampAxis(centre.x, SafeArea.xMin + half.x, SafeArea.xMax - half.x),
                ClampAxis(centre.y, SafeArea.yMin + half.y, SafeArea.yMax - half.y));
        }

        // A button bigger than the safe area sits in its middle.
        private static float ClampAxis(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }

        private Vector2 Normalize(Vector2 screenPoint)
        {
            var size = SafeArea.size;
            return new Vector2(
                size.x > 0f ? (screenPoint.x - SafeArea.xMin) / size.x : 0.5f,
                size.y > 0f ? (screenPoint.y - SafeArea.yMin) / size.y : 0.5f);
        }
    }
}
