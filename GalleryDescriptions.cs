using System;
using System.Collections.Generic;
using System.IO;

namespace RoadtripAccessibilityProbe
{
    internal sealed class GalleryDescriptions
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _speechById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _titleById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _descriptionById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal GalleryDescriptions(string pluginDirectory)
        {
            _path = Path.Combine(pluginDirectory, "GalleryDescriptions.txt");
            Load();
        }

        internal string GetSpeech(string id)
        {
            return GetValue(_speechById, id);
        }

        internal string GetTitle(string id)
        {
            return GetValue(_titleById, id);
        }

        internal string GetDescription(string id)
        {
            return GetValue(_descriptionById, id);
        }

        internal string GetSpeech(params string[] keys)
        {
            return GetFirstValue(_speechById, keys);
        }

        internal string GetTitle(params string[] keys)
        {
            return GetFirstValue(_titleById, keys);
        }

        internal string GetDescription(params string[] keys)
        {
            return GetFirstValue(_descriptionById, keys);
        }

        private string GetValue(Dictionary<string, string> map, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            string value;
            return map.TryGetValue(id, out value) ? value : string.Empty;
        }

        private static string GetFirstValue(Dictionary<string, string> map, params string[] keys)
        {
            if (keys == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                string value;
                if (map.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private void Load()
        {
            _speechById.Clear();
            _titleById.Clear();
            _descriptionById.Clear();

            if (!File.Exists(_path))
            {
                return;
            }

            foreach (string rawLine in File.ReadAllLines(_path))
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
                if (key.StartsWith("SPEECH:", StringComparison.OrdinalIgnoreCase))
                {
                    _speechById[key.Substring("SPEECH:".Length).Trim()] = value;
                    continue;
                }

                if (key.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
                {
                    _titleById[key.Substring("TITLE:".Length).Trim()] = value;
                    continue;
                }

                if (key.StartsWith("DESC:", StringComparison.OrdinalIgnoreCase))
                {
                    _descriptionById[key.Substring("DESC:".Length).Trim()] = value;
                }
            }
        }
    }
}
