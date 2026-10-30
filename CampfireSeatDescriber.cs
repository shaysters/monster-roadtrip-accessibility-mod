using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    // "Campfire spot 2: Vera, Liam. 2 seats free." Each CampfireLocation has four seats
    // ("positions"), each holding an NPC id, a PlayerId name ("PlayerA"), or "".
    // The campfire spot is a CampfireLocationWidget; its CampfireView holds the manager.
    internal static class CampfireSeatDescriber
    {
        internal static string BuildFocusSpeech(GameObject gameObject)
        {
            object widget = UiTextUtility.FindComponentInParents(gameObject, "CampfireLocationWidget");
            if (widget == null)
            {
                return string.Empty;
            }

            Component button = Traverse.Create(widget).Field("button").GetValue() as Component;
            if (button != null && button.gameObject != gameObject)
            {
                return string.Empty;
            }

            bool isLoveLog = Traverse.Create(widget).Field("isLoveLog").GetValue<bool>();
            int locationId = Traverse.Create(widget).Field("locationId").GetValue<int>();
            string title = isLoveLog
                ? Plugin.Localization.Get("MOD_CAMPFIRE_LOVE_LOG")
                : Plugin.Localization.Get("MOD_CAMPFIRE_SPOT", "number", (locationId + 1).ToString());

            try
            {
                Traverse manager = Traverse.Create(widget).Field("campfireView").Field("mCampfireManager");
                object location = isLoveLog
                    ? manager.Method("GetLoveLog").GetValue()
                    : manager.Method("GetNormalLog", locationId).GetValue();
                List<string> positions = Traverse.Create(location).Field("positions").GetValue<List<string>>();
                if (positions == null)
                {
                    return title;
                }

                List<string> names = new List<string>();
                int free = 0;
                for (int i = 0; i < positions.Count; i++)
                {
                    if (string.IsNullOrEmpty(positions[i]))
                    {
                        free++;
                        continue;
                    }

                    names.Add(GetOccupantName(positions[i]));
                }

                string text = title + ": ";
                if (names.Count > 0)
                {
                    text += string.Join(", ", names.ToArray()) + ". ";
                }

                return text + (free == 0
                    ? Plugin.Localization.Get("MOD_SEATS_FREE_NONE")
                    : free == 1
                        ? Plugin.Localization.Get("MOD_SEATS_FREE_ONE")
                        : Plugin.Localization.Get("MOD_SEATS_FREE", "count", free.ToString()));
            }
            catch
            {
                return title;
            }
        }

        private static string GetOccupantName(string occupant)
        {
            Type generalManagerType = AccessTools.TypeByName("Game.GeneralManager");
            object generalManager = generalManagerType != null ? Traverse.Create(generalManagerType).Property("Instance").GetValue() : null;
            if (generalManager == null)
            {
                return occupant;
            }

            Type playerIdType = AccessTools.TypeByName("BeautifulGlitch.PlayerId") ?? AccessTools.TypeByName("Game.PlayerId");
            if (playerIdType != null && Enum.IsDefined(playerIdType, occupant))
            {
                object playerId = Enum.Parse(playerIdType, occupant);
                object player = Traverse.Create(generalManager).Field("sessionManager").Method("GetPlayer", playerId).GetValue();
                string playerName = UiTextUtility.BuildPlayerName(player);
                return string.IsNullOrEmpty(playerName) ? occupant : playerName;
            }

            string name = UiTextUtility.NormalizeForSpeech(Traverse.Create(generalManager).Field("textManager").Method("GetNpcName", occupant).GetValue() as string);
            return string.IsNullOrEmpty(name) || name.StartsWith("CHR_", StringComparison.Ordinal) ? occupant : name;
        }
    }
}
