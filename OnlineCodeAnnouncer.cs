using System.Text;
using HarmonyLib;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    // Online lobby match code.
    //
    // OnlineLobbyOnMatchView / OnlineLobbyOnMatchResumedView .SetMatchInfo() write the
    // room code into matchNameLabel. It runs on Show and again whenever the player
    // list refreshes, so we only announce when the code changes and reset on Hide.
    // The announcement is delayed and queued so the hovered-button focus speech
    // doesn't cut it off. C key (GameplayTtsController) re-reads the code.
    internal static class OnlineCodeAnnouncer
    {
        private const float AnnounceDelay = 0.4f;
        private static readonly string[] LobbyViewTypes = { "OnlineLobbyOnMatchView", "OnlineLobbyOnMatchResumedView" };

        private static object _view;
        private static string _lastCode;
        private static string _pendingCode;
        private static float _pendingAt;

        internal static void Install(Harmony harmony)
        {
            for (int i = 0; i < LobbyViewTypes.Length; i++)
            {
                System.Type type = AccessTools.TypeByName(LobbyViewTypes[i]);
                if (type == null)
                {
                    continue;
                }

                PatchDeclared(harmony, type, "SetMatchInfo", nameof(AfterSetMatchInfo));
                PatchDeclared(harmony, type, "Hide", nameof(AfterHide));
            }
        }

        private static void PatchDeclared(Harmony harmony, System.Type type, string methodName, string postfixName)
        {
            var target = AccessTools.DeclaredMethod(type, methodName);
            if (target == null)
            {
                return;
            }

            harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(OnlineCodeAnnouncer), postfixName)));
        }

        private static void AfterSetMatchInfo(object __instance)
        {
            _view = __instance;
            string code = ReadCode();
            if (string.IsNullOrEmpty(code) || code == _lastCode)
            {
                return;
            }

            _lastCode = code;
            _pendingCode = code;
            _pendingAt = Time.unscaledTime + AnnounceDelay;
        }

        private static void AfterHide(object __instance)
        {
            if (!ReferenceEquals(_view, __instance))
            {
                return;
            }

            _view = null;
            _lastCode = null;
            _pendingCode = null;
        }

        internal static void Tick()
        {
            if (_pendingCode == null || Time.unscaledTime < _pendingAt || Plugin.Speech == null)
            {
                return;
            }

            string code = _pendingCode;
            _pendingCode = null;
            // "dialog:" makes it repeatable with the repeat key (G / L3), so controller
            // players, who have no C key, can hear the code again.
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_TTS_ONLINE_CODE_ANNOUNCE", "code", SpellOut(code)), interrupt: false, sourceTag: "dialog:online-code");
        }

        internal static void SpeakCurrentCode()
        {
            if (Plugin.Speech == null)
            {
                return;
            }

            string code = ReadCode();
            if (string.IsNullOrEmpty(code))
            {
                Plugin.Speech.Speak(Plugin.Localization.Get("MOD_TTS_ONLINE_CODE_NONE"), interrupt: true, sourceTag: "hotkey:onlinecode");
                return;
            }

            _pendingCode = null;
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_TTS_ONLINE_CODE", "code", SpellOut(code)), interrupt: true, sourceTag: "hotkey:onlinecode");
        }

        private static string ReadCode()
        {
            if (_view == null)
            {
                return null;
            }

            Component label = Traverse.Create(_view).Field("matchNameLabel").GetValue() as Component;
            if (label == null || !label.gameObject.activeInHierarchy)
            {
                return null;
            }

            string text = Traverse.Create(label).Property("text").GetValue() as string;
            return UiTextUtility.NormalizeForSpeech(text);
        }

        // "48271" -> "4, 8, 2, 7, 1" so screen readers read digit by digit.
        private static string SpellOut(string code)
        {
            var sb = new StringBuilder();
            foreach (char c in code)
            {
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
