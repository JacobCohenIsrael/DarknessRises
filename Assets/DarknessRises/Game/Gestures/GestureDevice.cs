using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace DarknessRises.Game.Gestures
{
    /// <summary>The state of the <see cref="GestureDevice"/>: one button per command, and the Relocate direction.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct GestureDeviceState : IInputStateTypeInfo
    {
        public const int FireBit = 0;
        public const int ReloadBit = 1;
        public const int GrenadeBit = 2;
        public const int FlareBit = 3;
        public const int AdrenalineBit = 4;
        public const int NextWeaponBit = 5;
        public const int LureBit = 6;

        public static FourCC Format => new('G', 'S', 'T', 'R');

        [InputControl(name = "fire", layout = "Button", bit = FireBit, displayName = "Fire")]
        [InputControl(name = "reload", layout = "Button", bit = ReloadBit, displayName = "Reload")]
        [InputControl(name = "grenade", layout = "Button", bit = GrenadeBit, displayName = "Grenade")]
        [InputControl(name = "flare", layout = "Button", bit = FlareBit, displayName = "Flare")]
        [InputControl(name = "adrenaline", layout = "Button", bit = AdrenalineBit, displayName = "Adrenaline")]
        [InputControl(name = "nextWeapon", layout = "Button", bit = NextWeaponBit, displayName = "Next Weapon")]
        [InputControl(name = "lure", layout = "Button", bit = LureBit, displayName = "Lure")]
        [FieldOffset(0)]
        public uint buttons;

        [InputControl(name = "relocate", layout = "Vector2", displayName = "Relocate")]
        [FieldOffset(4)]
        public Vector2 relocate;

        public FourCC format => Format;

        public bool Equals(GestureDeviceState other)
        {
            return buttons == other.buttons && relocate == other.relocate;
        }
    }

    /// <summary>
    /// A virtual input device that touch Gestures press. Its controls are bound to the Gameplay actions under the
    /// Touch control scheme, so touch drives the same named actions as keyboard and mouse.
    /// </summary>
    [InputControlLayout(stateType = typeof(GestureDeviceState), displayName = "Gestures")]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public sealed class GestureDevice : InputDevice
    {
        static GestureDevice()
        {
            InputSystem.RegisterLayout<GestureDevice>();
        }

        public ButtonControl Fire { get; private set; }
        public ButtonControl Reload { get; private set; }
        public ButtonControl Grenade { get; private set; }
        public ButtonControl Flare { get; private set; }
        public ButtonControl Adrenaline { get; private set; }
        public ButtonControl NextWeapon { get; private set; }
        public ButtonControl Lure { get; private set; }
        public Vector2Control Relocate { get; private set; }

        // Touching the static constructor registers the layout before any scene or actions asset needs it.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
        }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            Fire = GetChildControl<ButtonControl>("fire");
            Reload = GetChildControl<ButtonControl>("reload");
            Grenade = GetChildControl<ButtonControl>("grenade");
            Flare = GetChildControl<ButtonControl>("flare");
            Adrenaline = GetChildControl<ButtonControl>("adrenaline");
            NextWeapon = GetChildControl<ButtonControl>("nextWeapon");
            Lure = GetChildControl<ButtonControl>("lure");
            Relocate = GetChildControl<Vector2Control>("relocate");
        }
    }
}
