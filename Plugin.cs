using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    [BepInPlugin("com.shaysters.roadtrip.accessibility", "Roadtrip Accessibility Mod", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        internal static SpeechService Speech;
        internal static ModLocalization Localization;
        internal static GalleryDescriptions GalleryDescriptions;
        internal static ManualPagesRepository ManualPages;
        internal static bool IsAppFocused = true;
        internal static float LastAppFocusLostTime = -1000f;

        private Harmony _harmony;
        private FocusWatcher _focusWatcher;
        private GameplayTtsController _gameplayTtsController;

        private void Awake()
        {
            string pluginDirectory = Path.GetDirectoryName(Info.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            Localization = new ModLocalization(pluginDirectory);
            GalleryDescriptions = new GalleryDescriptions(pluginDirectory);
            ManualPages = new ManualPagesRepository(pluginDirectory);
            Speech = SpeechService.Create(pluginDirectory);

            _harmony = new Harmony("com.shaysters.roadtrip.accessibility");
            AccessibilityPatcher.Install(_harmony);
            OnlineCodeAnnouncer.Install(_harmony);

            _focusWatcher = gameObject.AddComponent<FocusWatcher>();
            _gameplayTtsController = gameObject.AddComponent<GameplayTtsController>();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Speech?.Dispose();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            IsAppFocused = hasFocus;
            if (!hasFocus)
            {
                LastAppFocusLostTime = Time.unscaledTime;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            IsAppFocused = !paused;
            if (paused)
            {
                LastAppFocusLostTime = Time.unscaledTime;
            }
        }

        internal static bool IsInteractionAllowed()
        {
            if (!IsAppFocused)
            {
                return false;
            }

            if (Time.unscaledTime - LastAppFocusLostTime <= 0.5f)
            {
                return false;
            }

            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return false;
            }

            uint foregroundProcessId;
            GetWindowThreadProcessId(foregroundWindow, out foregroundProcessId);
            return foregroundProcessId == (uint)Process.GetCurrentProcess().Id;
        }

    }
}
