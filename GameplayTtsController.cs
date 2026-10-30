using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    internal sealed class GameplayTtsController : MonoBehaviour
    {
        private const int RewiredLeftStickButtonElementId = 16;
        private const int RewiredRightStickButtonElementId = 17;

        private static readonly KeyCode[] RepeatControllerButtons = new[]
        {
            KeyCode.JoystickButton8,
            KeyCode.Joystick1Button8,
            KeyCode.Joystick2Button8,
            KeyCode.Joystick3Button8,
            KeyCode.Joystick4Button8,
            KeyCode.Joystick5Button8,
            KeyCode.Joystick6Button8,
            KeyCode.Joystick7Button8,
            KeyCode.Joystick8Button8
        };

        private static readonly KeyCode[] StatsControllerButtons = new[]
        {
            KeyCode.JoystickButton9,
            KeyCode.Joystick1Button9,
            KeyCode.Joystick2Button9,
            KeyCode.Joystick3Button9,
            KeyCode.Joystick4Button9,
            KeyCode.Joystick5Button9,
            KeyCode.Joystick6Button9,
            KeyCode.Joystick7Button9,
            KeyCode.Joystick8Button9
        };

        private static Type _rewiredReInputType;
        private static PropertyInfo _rewiredIsReadyProperty;
        private static PropertyInfo _rewiredControllersProperty;
        private static PropertyInfo _rewiredJoysticksProperty;
        private static MethodInfo _rewiredGetButtonDownByIdMethod;
        private bool _wasShowMissionPointsHeld;

        private void Update()
        {
            if (Plugin.Speech == null || !Plugin.IsInteractionAllowed())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.G)
                || IsAnyControllerButtonDown(RepeatControllerButtons, "repeat")
                || IsRewiredStickButtonDown(RewiredLeftStickButtonElementId, "repeat"))
            {
                Plugin.Speech.RepeatLast("hotkey:repeat");
            }

            if (Input.GetKeyDown(KeyCode.T)
                || IsAnyControllerButtonDown(StatsControllerButtons, "stats")
                || IsRewiredStickButtonDown(RewiredRightStickButtonElementId, "stats"))
            {
                string stats = UiTextUtility.BuildStatsSummary();
                Plugin.Speech.Speak(stats, interrupt: true, sourceTag: "hotkey:stats");
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                OnlineCodeAnnouncer.SpeakCurrentCode();
            }

            OnlineCodeAnnouncer.Tick();

            if (IsMissionPointsSummaryPressed())
            {
                string missions = UiTextUtility.BuildMissionPointsSummary();
                if (!string.IsNullOrEmpty(missions))
                {
                    Plugin.Speech.Speak(missions, interrupt: true, sourceTag: "hotkey:prankdollarz");
                }
            }
        }

        private bool IsMissionPointsSummaryPressed()
        {
            bool isHeld = Input.GetKey(KeyCode.Q) || UiTextUtility.IsGameShowMissionPointsHeld();
            bool pressed = isHeld && !_wasShowMissionPointsHeld;
            _wasShowMissionPointsHeld = isHeld;
            return pressed;
        }

        private static bool IsAnyControllerButtonDown(KeyCode[] buttons, string action)
        {
            if (buttons == null)
            {
                return false;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                KeyCode button = buttons[i];
                if (!Input.GetKeyDown(button))
                {
                    continue;
                }

                    return true;
                }

            return false;
        }

        private static bool IsRewiredStickButtonDown(int elementId, string action)
        {
            try
            {
                if (!EnsureRewiredReflection())
                {
                    return false;
                }

                object isReadyValue = _rewiredIsReadyProperty.GetValue(null, null);
                if (!(isReadyValue is bool isReady) || !isReady)
                {
                    return false;
                }

                object controllers = _rewiredControllersProperty.GetValue(null, null);
                object joysticksObject = controllers != null ? _rewiredJoysticksProperty.GetValue(controllers, null) : null;
                IEnumerable joysticks = joysticksObject as IEnumerable;
                if (joysticks == null)
                {
                    return false;
                }

                foreach (object joystick in joysticks)
                {
                    if (joystick == null)
                    {
                        continue;
                    }

                    object down = _rewiredGetButtonDownByIdMethod.Invoke(joystick, new object[] { elementId });
                    if (!(down is bool pressed) || !pressed)
                    {
                        continue;
                    }

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool EnsureRewiredReflection()
        {
            if (_rewiredGetButtonDownByIdMethod != null)
            {
                return true;
            }

            _rewiredReInputType = AccessTools.TypeByName("Rewired.ReInput");
            if (_rewiredReInputType == null)
            {
                LogRewiredUnavailable("Rewired.ReInput type not found");
                return false;
            }

            _rewiredIsReadyProperty = _rewiredReInputType.GetProperty("isReady", BindingFlags.Static | BindingFlags.Public);
            _rewiredControllersProperty = _rewiredReInputType.GetProperty("controllers", BindingFlags.Static | BindingFlags.Public);
            if (_rewiredIsReadyProperty == null || _rewiredControllersProperty == null)
            {
                LogRewiredUnavailable("Rewired.ReInput properties not found");
                return false;
            }

            object controllers = _rewiredControllersProperty.GetValue(null, null);
            if (controllers == null)
            {
                return false;
            }

            _rewiredJoysticksProperty = controllers.GetType().GetProperty("Joysticks", BindingFlags.Instance | BindingFlags.Public);
            if (_rewiredJoysticksProperty == null)
            {
                LogRewiredUnavailable("Rewired controllers.Joysticks property not found");
                return false;
            }

            object joysticksObject = _rewiredJoysticksProperty.GetValue(controllers, null);
            IEnumerable joysticks = joysticksObject as IEnumerable;
            if (joysticks == null)
            {
                return false;
            }

            foreach (object joystick in joysticks)
            {
                if (joystick == null)
                {
                    continue;
                }

                _rewiredGetButtonDownByIdMethod = joystick.GetType().GetMethod("GetButtonDownById", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(int) }, null);
                if (_rewiredGetButtonDownByIdMethod != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogRewiredUnavailable(string message)
        {
}
    }
}
