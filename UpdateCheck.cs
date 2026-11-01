using System;
using System.Collections;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace RoadtripAccessibilityProbe
{
    /// <summary>
    /// Once per game start, asks GitHub for the latest release and says so if it's newer than the
    /// installed mod. Reads the release manifest the installer itself uses
    /// (releases/latest/download/loadstone-release.json: a plain redirect, no API token or rate limit).
    /// Nothing is sent except the request itself, and a failed check is only logged.
    /// </summary>
    internal sealed class UpdateCheck : MonoBehaviour
    {
        private const string ManifestUrl = "https://github.com/shaysters/monster-roadtrip-accessibility-mod/releases/latest/download/loadstone-release.json";
        private static readonly Regex VersionField = new Regex("\"version\"\\s*:\\s*\"v?([0-9.]+)\"");
        private static readonly ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource("Roadtrip Accessibility Update Check");


        private void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            string newerVersion;
            using (UnityWebRequest request = UnityWebRequest.Get(ManifestUrl))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Log.LogInfo("Update check skipped: " + request.error);
                    yield break;
                }

                Match m = VersionField.Match(request.downloadHandler.text);
                Version latest = m.Success ? Parse(m.Groups[1].Value) : null;
                Version installed = Parse(Plugin.Version);
                Log.LogInfo("Update check: installed " + installed + ", latest " + latest);
                if (latest == null || installed == null || latest <= installed)
                {
                    yield break;
                }

                newerVersion = m.Groups[1].Value;
            }

            while (Plugin.Speech == null || !UpdateNotice.Ready(Plugin.Speech.SpeakCount, Time.unscaledTime - Plugin.Speech.LastSpeakTime))
            {
                yield return null;
            }

            // "dialog:" makes it repeatable with the repeat key (G / L3).
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_TTS_UPDATE_AVAILABLE", "version", newerVersion), interrupt: false, sourceTag: "dialog:update-check");
        }

        private static Version Parse(string s)
        {
            try
            {
                return new Version(s);
            }
            catch
            {
                return null;
            }
        }
    }

    // When the update notice may be spoken: after the title menu's first line has been read
    // and the mod has then been quiet for a moment, so it neither cuts off the start-up speech
    // nor gets cut off by it. If the menu never speaks by itself, a longer silence will do.
    internal static class UpdateNotice
    {
        // Lines the mod speaks by itself before the title menu: nothing.
        private const int StartupLines = 0;
        private const float QuietSeconds = 2f;
        private const float FallbackQuietSeconds = 30f;

        internal static bool Ready(int spokenLines, float secondsQuiet)
        {
            if (secondsQuiet < QuietSeconds)
            {
                return false;
            }

            return spokenLines > StartupLines
                || (secondsQuiet >= FallbackQuietSeconds && Time.realtimeSinceStartup >= 30f);
        }
    }
}
