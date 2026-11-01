using System;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    internal sealed class SpeechService : IDisposable
    {
        private readonly ISpeechBackend _backend;
        private string _lastText = string.Empty;
        private string _lastSource = string.Empty;
        private string _lastRepeatableText = string.Empty;
        private float _lastSpeakTime = -1000f;

        // Time.unscaledTime of the last thing spoken, so the update notice can wait for a pause.
        internal float LastSpeakTime => _lastSpeakTime;

        // How many lines have been spoken since the game started.
        internal int SpeakCount { get; private set; }

        private SpeechService(ISpeechBackend backend)
        {
            _backend = backend;
        }

        internal static SpeechService Create(string pluginDirectory)
        {
            ISpeechBackend backend = NullSpeechBackend.Instance;

            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                PrismSpeechBackend prism = new PrismSpeechBackend(pluginDirectory);
                if (prism.Initialize())
                {
                    backend = prism;
                }
                else
                {
                    prism.Dispose();
                }
            }
            return new SpeechService(backend);
        }

        internal void Speak(string text, bool interrupt, string sourceTag)
        {
            string cleaned = UiTextUtility.NormalizeForSpeech(text);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (cleaned == _lastText && now - _lastSpeakTime <= 0.5f)
            {
                return;
            }

            _lastText = cleaned;
            _lastSource = sourceTag ?? string.Empty;
            _lastSpeakTime = now;
            SpeakCount++;
            if (IsRepeatableSource(_lastSource))
            {
                _lastRepeatableText = cleaned;
            }

            _backend.Speak(cleaned, interrupt);
        }

        internal bool WasRecentSource(string sourcePrefix, float withinSeconds)
        {
            if (string.IsNullOrEmpty(sourcePrefix))
            {
                return false;
            }

            if (Time.unscaledTime - _lastSpeakTime > withinSeconds)
            {
                return false;
            }

            return !string.IsNullOrEmpty(_lastSource)
                && _lastSource.StartsWith(sourcePrefix, StringComparison.Ordinal);
        }

        internal void RepeatLast(string sourceTag)
        {
            if (string.IsNullOrWhiteSpace(_lastRepeatableText))
            {
                Speak(Plugin.Localization.Get("MOD_TTS_NOTHING_TO_REPEAT"), interrupt: true, sourceTag: sourceTag);
                return;
            }

            _backend.Speak(_lastRepeatableText, true);
            _lastSpeakTime = Time.unscaledTime;
            _lastSource = sourceTag ?? string.Empty;
        }

        private static bool IsRepeatableSource(string sourceTag)
        {
            if (string.IsNullOrEmpty(sourceTag))
            {
                return false;
            }

            return sourceTag.StartsWith("dialog:", StringComparison.Ordinal);
        }

        public void Dispose()
        {
            _backend.Dispose();
        }
    }
}
