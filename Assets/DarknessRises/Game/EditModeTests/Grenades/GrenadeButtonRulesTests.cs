using DarknessRises.Game.Grenades;
using NUnit.Framework;
using UnityEngine;

namespace DarknessRises.Game.EditModeTests.Grenades
{
    public sealed class GrenadeButtonRulesTests
    {
        private const float HoldSeconds = 0.5f;
        private const int SomeCharges = 2;

        private static readonly Rect SafeArea = new(100f, 0f, 1000f, 500f);
        private static readonly Vector2 ButtonSize = new(100f, 100f);

        private GrenadeButtonRules rules;

        [SetUp]
        public void SetUp()
        {
            rules = new GrenadeButtonRules(HoldSeconds, new Vector2(0.5f, 0.5f));
            rules.Fit(SafeArea, ButtonSize);
        }

        private void Tap(int charges = SomeCharges)
        {
            rules.Press(0f);
            rules.Release(0.1f, charges);
        }

        [Test]
        public void StartsIdle()
        {
            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
        }

        [Test]
        public void TapArmsWithACharge()
        {
            Tap();

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Armed));
        }

        [Test]
        public void TapWithNoChargesDoesNotArm()
        {
            Tap(charges: 0);

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
        }

        [Test]
        public void SecondTapCancels()
        {
            Tap();
            Tap();

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
            Assert.That(rules.TapRoom(), Is.EqualTo(RoomTapOutcome.Fire));
        }

        [Test]
        public void RoomTapFiresWhenIdle()
        {
            Assert.That(rules.TapRoom(), Is.EqualTo(RoomTapOutcome.Fire));
        }

        [Test]
        public void RoomTapWhileArmedThrowsInsteadOfFiringAndDisarms()
        {
            Tap();

            Assert.That(rules.TapRoom(), Is.EqualTo(RoomTapOutcome.ThrowGrenade));
            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
            Assert.That(rules.TapRoom(), Is.EqualTo(RoomTapOutcome.Fire));
        }

        [Test]
        public void RunningOutOfChargesWhileArmedDisarms()
        {
            Tap();

            rules.UpdateCharges(0);

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
        }

        [Test]
        public void ChargesLeftKeepItArmed()
        {
            Tap();

            rules.UpdateCharges(1);

            Assert.That(rules.IsArmed, Is.True);
        }

        [Test]
        public void PressShorterThanTheHoldTimeIsNotAPickUp()
        {
            rules.Press(0f);
            rules.Tick(HoldSeconds - 0.01f);

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
        }

        [Test]
        public void PressHeldForTheHoldTimePicksTheButtonUp()
        {
            rules.Press(1f);
            rules.Tick(1f + HoldSeconds);

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Moving));
        }

        [Test]
        public void ReleasingALongPressNeverArms()
        {
            rules.Press(0f);

            var moved = rules.Release(HoldSeconds + 0.1f, SomeCharges);

            Assert.That(moved, Is.True);
            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
        }

        [Test]
        public void PickingUpAnArmedButtonCancelsArming()
        {
            Tap();
            rules.Press(1f);
            rules.Tick(1f + HoldSeconds);
            rules.Release(2f, SomeCharges);

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
        }

        [Test]
        public void DroppingKeepsTheDraggedPosition()
        {
            rules.Press(0f);
            rules.Tick(HoldSeconds);
            rules.Drag(new Vector2(300f, 200f));
            rules.Release(1f, SomeCharges);

            Assert.That(rules.ScreenPosition.x, Is.EqualTo(300f).Within(0.001f));
            Assert.That(rules.ScreenPosition.y, Is.EqualTo(200f).Within(0.001f));
        }

        [Test]
        public void DragBeforePickUpDoesNotMove()
        {
            var before = rules.ScreenPosition;
            rules.Press(0f);
            rules.Drag(new Vector2(300f, 200f));

            Assert.That(rules.ScreenPosition, Is.EqualTo(before));
        }

        [Test]
        public void DragStaysInsideTheSafeArea()
        {
            rules.Press(0f);
            rules.Tick(HoldSeconds);
            rules.Drag(new Vector2(-500f, 9000f));

            Assert.That(rules.ScreenPosition.x, Is.EqualTo(SafeArea.xMin + ButtonSize.x * 0.5f).Within(0.001f));
            Assert.That(rules.ScreenPosition.y, Is.EqualTo(SafeArea.yMax - ButtonSize.y * 0.5f).Within(0.001f));
        }

        [Test]
        public void FitClampsASavedPositionIntoTheSafeArea()
        {
            var saved = new GrenadeButtonRules(HoldSeconds, new Vector2(1f, 0f));

            saved.Fit(SafeArea, ButtonSize);

            Assert.That(saved.ScreenPosition.x, Is.EqualTo(SafeArea.xMax - ButtonSize.x * 0.5f).Within(0.001f));
            Assert.That(saved.ScreenPosition.y, Is.EqualTo(SafeArea.yMin + ButtonSize.y * 0.5f).Within(0.001f));
        }

        [Test]
        public void FitKeepsTheNormalizedPositionAcrossResolutions()
        {
            rules.Fit(new Rect(0f, 0f, 2000f, 1000f), ButtonSize);

            Assert.That(rules.NormalizedPosition.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(rules.ScreenPosition.x, Is.EqualTo(1000f).Within(0.001f));
        }

        [Test]
        public void CancelDisarms()
        {
            Tap();

            rules.Cancel();

            Assert.That(rules.State, Is.EqualTo(GrenadeButtonState.Idle));
            Assert.That(rules.IsPressed, Is.False);
        }
    }
}
