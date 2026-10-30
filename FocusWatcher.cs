using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadtripAccessibilityProbe
{
    internal sealed class FocusWatcher : MonoBehaviour
    {
        private string _lastPath = string.Empty;
        private float _nextCheckTime;

        private void Update()
        {
            if (!Plugin.IsInteractionAllowed())
            {
                return;
            }

            if (Time.unscaledTime < _nextCheckTime)
            {
                return;
            }

            _nextCheckTime = Time.unscaledTime + 0.1f;
            TrySpeakCurrentFocus();
        }

        private static void TrySpeakCurrentFocus()
        {
            System.Type uiCameraType = AccessTools.TypeByName("UICamera");
            if (uiCameraType == null)
            {
                return;
            }

            object current = GetStaticValue(uiCameraType, "controllerNavigationObject")
                ?? GetStaticValue(uiCameraType, "selectedObject");

            GameObject go = current as GameObject;
            if (go == null)
            {
                return;
            }

            FocusWatcher watcher = FindObjectOfType<FocusWatcher>();
            if (watcher == null)
            {
                return;
            }

            string path = UiTextUtility.BuildPath(go.transform);
            if (path == watcher._lastPath)
            {
                return;
            }

            watcher._lastPath = path;
            AccessibilityPatcher.LogFocusedObjectDiagnostics(go, "focuspoll");
            string text = UiTextUtility.BuildFocusSpeech(go);
            text = AccessibilityPatcher.AppendCustomizeNoteIfNeeded(go, text);

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (ShouldSuppressSceneChromeFocusPoll(path))
            {
                return;
            }

            if (AccessibilityPatcher.ShouldSuppressFocusForRecentSpeech(go))
            {
                return;
            }

            bool interrupt = !UiTextUtility.ShouldQueueFocusBehindGallerySpeech(go);
            Plugin.Speech.Speak(text, interrupt: interrupt, sourceTag: "focuspoll:" + path);
        }

        private static bool ShouldSuppressSceneChromeFocusPoll(string path)
        {
            if (string.IsNullOrEmpty(path) || Plugin.Speech == null)
            {
                return false;
            }

            bool isGalleryChrome = path.Contains("/GalleryTabs/BackButton");
            bool isInfoManualChrome = path.Contains("/InfoManual/Container/Buttons/BackButton")
                || path.Contains("/InfoManual/Container/Viewer/PageNavigation/");

            return (isGalleryChrome && Plugin.Speech.WasRecentSource("gallery:", 10f))
                || (isInfoManualChrome && Plugin.Speech.WasRecentSource("infomanual:", 10f));
        }

        private static object GetStaticValue(System.Type type, string name)
        {
            return Traverse.Create(type).Property(name).GetValue()
                ?? Traverse.Create(type).Field(name).GetValue();
        }
    }
}
