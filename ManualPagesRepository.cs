using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RoadtripAccessibilityProbe
{
    internal sealed class ManualPagesRepository
    {
        private static readonly Regex PageBlockRegex = new Regex(
            "\"(?<key>cover|\\d+)\"\\s*:\\s*\\{(?<body>.*?)\\n\\s*\\}",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex VisibleTextRegex = new Regex(
            "\"visible_text\"\\s*:\\s*\\[(?<items>.*?)\\]",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex ImageDescriptionRegex = new Regex(
            "\"image_description\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex JsonStringRegex = new Regex(
            "\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly Dictionary<string, ManualPageEntry> _pages;

        internal ManualPagesRepository(string pluginDirectory)
        {
            _pages = Load(pluginDirectory);
        }

        internal bool TryBuildCoverSpeech(out string speech)
        {
            return TryBuildSpeech("cover", out speech);
        }

        internal bool TryBuildPageSpeech(int zeroBasedPageIndex, out string speech)
        {
            return TryBuildSpeech(zeroBasedPageIndex.ToString(), out speech);
        }

        private bool TryBuildSpeech(string key, out string speech)
        {
            speech = string.Empty;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            ManualPageEntry entry;
            if (!_pages.TryGetValue(key, out entry) || entry == null)
            {
                return false;
            }

            List<string> parts = new List<string>();
            if (entry.VisibleText != null)
            {
                for (int i = 0; i < entry.VisibleText.Length; i++)
                {
                    string cleaned = UiTextUtility.NormalizeForSpeech(entry.VisibleText[i]);
                    if (!string.IsNullOrEmpty(cleaned))
                    {
                        parts.Add(cleaned);
                    }
                }
            }

            string imageDescription = UiTextUtility.NormalizeForSpeech(entry.ImageDescription);
            if (!string.IsNullOrEmpty(imageDescription))
            {
                parts.Add(imageDescription);
            }

            speech = UiTextUtility.JoinSpeechParts(parts);
            return !string.IsNullOrEmpty(speech);
        }

        private Dictionary<string, ManualPageEntry> Load(string pluginDirectory)
        {
            Dictionary<string, ManualPageEntry> pages = new Dictionary<string, ManualPageEntry>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = Path.Combine(pluginDirectory, "ManualPages.en.json");
                if (!File.Exists(path))
                {
return pages;
                }

                string json = File.ReadAllText(path);
                MatchCollection matches = PageBlockRegex.Matches(json);
                for (int i = 0; i < matches.Count; i++)
                {
                    Match match = matches[i];
                    string key = match.Groups["key"].Value;
                    string body = match.Groups["body"].Value;
                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(body))
                    {
                        continue;
                    }

                    pages[key] = new ManualPageEntry
                    {
                        VisibleText = ParseVisibleText(body),
                        ImageDescription = ParseImageDescription(body)
                    };
                }
}
            catch
            {
}

            return pages;
        }

        private static string[] ParseVisibleText(string body)
        {
            Match visibleTextMatch = VisibleTextRegex.Match(body ?? string.Empty);
            if (!visibleTextMatch.Success)
            {
                return new string[0];
            }

            string itemsBlock = visibleTextMatch.Groups["items"].Value;
            MatchCollection itemMatches = JsonStringRegex.Matches(itemsBlock);
            List<string> items = new List<string>();
            for (int i = 0; i < itemMatches.Count; i++)
            {
                string value = UnescapeJsonString(itemMatches[i].Groups["value"].Value);
                if (!string.IsNullOrEmpty(value))
                {
                    items.Add(value);
                }
            }

            return items.ToArray();
        }

        private static string ParseImageDescription(string body)
        {
            Match imageDescriptionMatch = ImageDescriptionRegex.Match(body ?? string.Empty);
            return imageDescriptionMatch.Success
                ? UnescapeJsonString(imageDescriptionMatch.Groups["value"].Value)
                : string.Empty;
        }

        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c != '\\' || i == value.Length - 1)
                {
                    builder.Append(c);
                    continue;
                }

                i++;
                char next = value[i];
                switch (next)
                {
                    case '\\':
                    case '"':
                    case '/':
                        builder.Append(next);
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        if (i + 4 < value.Length)
                        {
                            string hex = value.Substring(i + 1, 4);
                            int codePoint;
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out codePoint))
                            {
                                builder.Append((char)codePoint);
                                i += 4;
                            }
                        }
                        break;
                    default:
                        builder.Append(next);
                        break;
                }
            }

            return builder.ToString();
        }

        private sealed class ManualPageEntry
        {
            public string[] VisibleText { get; set; }

            public string ImageDescription { get; set; }
        }
    }
}
