#if UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Headless
{
    /// <summary>
    /// Keeps a batch-mode Editor invisible on the developer's desktop (AGENTS.md rule 13).
    /// Even with <c>-batchmode</c>, entering Play Mode or capturing the Game view makes Unity
    /// open a floating Game window and bring it to the front. This guard hides every window
    /// the batch-mode Editor shows and hands the foreground straight back to whatever window
    /// had it before. It does nothing in an interactive Editor.
    /// Capture with <c>unity command capture game_view -- --source camera</c>, which renders the
    /// camera directly and doesn't need a visible Game view.
    /// </summary>
    [InitializeOnLoad]
    internal static class HeadlessWindowGuard
    {
        private const uint EventSystemForeground = 0x0003;
        private const uint EventObjectShow = 0x8002;
        private const uint WinEventOutOfContext = 0x0000;
        private const int ObjectIdWindow = 0;
        private const int ShowWindowHide = 0;

        private delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr window, int objectId,
            int childId, uint eventThread, uint eventTime);

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        // Held in static fields so the GC can't collect delegates that native code still calls.
        private static readonly WinEventProc OnWinEvent = HandleWinEvent;
        private static readonly EnumWindowsProc OnEnumWindow = HideIfOwnAndVisible;

        private static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;
        private static IntPtr _showHook;
        private static IntPtr _foregroundHook;
        private static IntPtr _lastForeignForeground;

        static HeadlessWindowGuard()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            RememberForeignForeground(GetForegroundWindow());
            _showHook = SetWinEventHook(EventObjectShow, EventObjectShow, IntPtr.Zero, OnWinEvent,
                OwnProcessId, 0, WinEventOutOfContext);
            _foregroundHook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero,
                OnWinEvent, 0, 0, WinEventOutOfContext);

            AssemblyReloadEvents.beforeAssemblyReload += Unhook;
            EditorApplication.quitting += Unhook;
            EditorApplication.update += HideOwnWindows;
            HideOwnWindows();
        }

        private static void HandleWinEvent(IntPtr hook, uint eventType, IntPtr window, int objectId,
            int childId, uint eventThread, uint eventTime)
        {
            if (objectId != ObjectIdWindow || window == IntPtr.Zero)
            {
                return;
            }

            if (eventType == EventSystemForeground && !IsOwn(window))
            {
                RememberForeignForeground(window);
                return;
            }

            HideOwnWindows();
        }

        // Also polled each Editor tick, in case a window appears between hook callbacks.
        private static void HideOwnWindows()
        {
            EnumWindows(OnEnumWindow, IntPtr.Zero);
        }

        private static bool HideIfOwnAndVisible(IntPtr window, IntPtr parameter)
        {
            if (!IsOwn(window) || !IsWindowVisible(window))
            {
                return true;
            }

            var ownedForeground = IsOwn(GetForegroundWindow());
            ShowWindow(window, ShowWindowHide);
            if (ownedForeground && _lastForeignForeground != IntPtr.Zero && IsWindow(_lastForeignForeground))
            {
                SetForegroundWindow(_lastForeignForeground);
            }

            return true;
        }

        private static void RememberForeignForeground(IntPtr window)
        {
            if (window != IntPtr.Zero && !IsOwn(window))
            {
                _lastForeignForeground = window;
            }
        }

        private static bool IsOwn(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(window, out var processId);
            return processId == OwnProcessId;
        }

        private static void Unhook()
        {
            EditorApplication.update -= HideOwnWindows;
            AssemblyReloadEvents.beforeAssemblyReload -= Unhook;
            EditorApplication.quitting -= Unhook;
            if (_showHook != IntPtr.Zero)
            {
                UnhookWinEvent(_showHook);
                _showHook = IntPtr.Zero;
            }

            if (_foregroundHook != IntPtr.Zero)
            {
                UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module,
            WinEventProc callback, uint processId, uint threadId, uint flags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);
    }
}
#endif
