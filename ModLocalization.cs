using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace RoadtripAccessibilityProbe
{
    internal sealed class ModLocalization
    {
        private readonly string _directory;
        private readonly Dictionary<string, Dictionary<string, string>> _cache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        internal ModLocalization(string pluginDirectory)
        {
            _directory = Path.Combine(pluginDirectory, "Localization");
        }

        internal string Get(string key, params string[] replacements)
        {
            string language = GetCurrentLanguageName();
            string text = GetFromLanguage(language, key) ?? GetFromLanguage("English", key) ?? key;

            if (replacements != null)
            {
                for (int i = 0; i + 1 < replacements.Length; i += 2)
                {
                    text = text.Replace("[" + replacements[i] + "]", replacements[i + 1]);
                }
            }

            return text;
        }

        internal string GetOptional(string key, params string[] replacements)
        {
            string language = GetCurrentLanguageName();
            string text = GetFromLanguage(language, key) ?? GetFromLanguage("English", key);
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (replacements != null)
            {
                for (int i = 0; i + 1 < replacements.Length; i += 2)
                {
                    text = text.Replace("[" + replacements[i] + "]", replacements[i + 1]);
                }
            }

            return text;
        }

        private string GetFromLanguage(string language, string key)
        {
            Dictionary<string, string> map = LoadLanguage(language);
            string value;
            return map.TryGetValue(key, out value) ? value : null;
        }

        private Dictionary<string, string> LoadLanguage(string language)
        {
            Dictionary<string, string> existing;
            if (_cache.TryGetValue(language, out existing))
            {
                return existing;
            }

            Dictionary<string, string> loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string path = Path.Combine(_directory, language + ".txt");
            if (File.Exists(path))
            {
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#"))
                    {
                        continue;
                    }

                    int split = line.IndexOf('=');
                    if (split <= 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, split).Trim();
                    string value = line.Substring(split + 1).Trim();
                    loaded[key] = value;
                }
            }
            else
            {
}

            _cache[language] = loaded;
            return loaded;
        }

        private static string GetCurrentLanguageName()
        {
            try
            {
                Type generalManagerType = AccessTools.TypeByName("GeneralManager");
                if (generalManagerType == null)
                {
                    return "English";
                }

                PropertyInfo instanceProperty = generalManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                object generalManager = instanceProperty == null ? null : instanceProperty.GetValue(null, null);
                if (generalManager == null)
                {
                    return "English";
                }

                object coreSettingsManager = Traverse.Create(generalManager).Field("coreSettingsManager").GetValue();
                if (coreSettingsManager == null)
                {
                    return "English";
                }

                MethodInfo getLanguageProfile = coreSettingsManager.GetType().GetMethod("GetLanguageProfile", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                object languageProfile = getLanguageProfile == null ? null : getLanguageProfile.Invoke(coreSettingsManager, null);
                if (languageProfile == null)
                {
                    return "English";
                }

                object languageType = Traverse.Create(languageProfile).Field("languageType").GetValue();
                if (languageType == null)
                {
                    languageType = Traverse.Create(languageProfile).Property("languageType").GetValue();
                }

                return languageType == null ? "English" : languageType.ToString();
            }
            catch
            {
                return "English";
            }
        }
    }
}
