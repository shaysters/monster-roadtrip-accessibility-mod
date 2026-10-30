using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    internal static class UiTextUtility
    {
        private static readonly Dictionary<int, string> LocationActionCueByInstanceId = new Dictionary<int, string>();
        private const float RecentDestinationProgressSpeechSeconds = 8.0f;

        internal static string NormalizeForSpeech(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string withoutTags = StripBracketTags(value);
            StringBuilder builder = new StringBuilder(withoutTags.Length);
            bool lastWasWhitespace = false;
            for (int i = 0; i < withoutTags.Length; i++)
            {
                char c = withoutTags[i];
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasWhitespace)
                    {
                        builder.Append(' ');
                        lastWasWhitespace = true;
                    }

                    continue;
                }

                lastWasWhitespace = false;
                builder.Append(c);
            }

            return builder.ToString().Trim();
        }

        internal static string NormalizeButtonLabelForSpeech(string value)
        {
            string cleaned = NormalizeForSpeech(value);
            if (string.IsNullOrEmpty(cleaned))
            {
                return string.Empty;
            }

            string withoutTerminalPunctuation = cleaned.TrimEnd('.', '!', '?', ':', ';').Trim();
            return string.Equals(withoutTerminalPunctuation, "OK", StringComparison.OrdinalIgnoreCase)
                || string.Equals(withoutTerminalPunctuation, "Ok", StringComparison.OrdinalIgnoreCase)
                    ? "Okay"
                    : cleaned;
        }

        internal static string ExtractUiText(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }

            string text = ExtractUILabelText(gameObject);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }

            Component[] components = gameObject.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "UILabel")
                {
                    continue;
                }

                object candidate = Traverse.Create(component).Property("text").GetValue();
                string normalized = NormalizeForSpeech(candidate as string);
                if (!string.IsNullOrEmpty(normalized))
                {
                    return normalized;
                }
            }

            return string.Empty;
        }

        internal static string BuildVisibleNonButtonText(object instance, params string[] fieldNames)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldName = fieldNames[i];
                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

                FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null)
                {
                    continue;
                }

                object fieldValue = field.GetValue(instance);
                if (fieldValue is Component component)
                {
                    AddVisibleNonButtonLabelText(component.gameObject, parts, seen);
                    continue;
                }

                if (fieldValue is GameObject gameObject)
                {
                    AddVisibleNonButtonLabelText(gameObject, parts, seen);
                }
            }

            if (parts.Count == 0 && instance is Component instanceComponent)
            {
                AddVisibleNonButtonLabelText(instanceComponent.gameObject, parts, seen);
            }

            return JoinSpeechParts(parts);
        }

        private static void AddVisibleNonButtonLabelText(GameObject root, List<string> parts, HashSet<string> seen)
        {
            if (root == null || parts == null || seen == null)
            {
                return;
            }

            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "UILabel")
                {
                    continue;
                }

                if (component.gameObject == null || !component.gameObject.activeInHierarchy || IsInsideInteractiveButton(component.transform))
                {
                    continue;
                }

                object candidate = Traverse.Create(component).Property("text").GetValue();
                string normalized = NormalizeForSpeech(candidate as string);
                if (string.IsNullOrEmpty(normalized) || seen.Contains(normalized))
                {
                    continue;
                }

                seen.Add(normalized);
                parts.Add(normalized);
            }
        }

        private static void AddVisibleLabelText(GameObject root, List<string> parts, bool includeInteractiveButtons)
        {
            if (root == null || parts == null)
            {
                return;
            }

            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "UILabel")
                {
                    continue;
                }

                if (component.gameObject == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!includeInteractiveButtons && IsInsideInteractiveButton(component.transform))
                {
                    continue;
                }

                object candidate = Traverse.Create(component).Property("text").GetValue();
                AddNormalizedPart(parts, NormalizeButtonLabelForSpeech(candidate as string));
            }
        }

        private static bool IsInsideInteractiveButton(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component == null)
                    {
                        continue;
                    }

                    string typeName = component.GetType().Name;
                    if (typeName == "UIButton" || typeName == "ButtonHandler" || typeName == "Button")
                    {
                        return true;
                    }
                }

                current = current.parent;
            }

            return false;
        }

        internal static string BuildPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            List<string> parts = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        internal static bool ShouldQueueFocusBehindGallerySpeech(GameObject gameObject)
        {
            if (gameObject == null || Plugin.Speech == null)
            {
                return false;
            }

            string path = BuildPath(gameObject.transform);
            if ((path.Contains("/GalleryPageNavigation/")
                    || path.Contains("/TopPanel/GalleryTabs/")
                    || path.Contains("/TopPanel/GalleryLayouts/")
                    || FindComponentInParents(gameObject, "GalleryTab") != null
                    || FindComponentInParents(gameObject, "FakeGalleryTab") != null)
                && Plugin.Speech.WasRecentSource("gallery:", 20f))
            {
                return true;
            }

            bool isPrologueControl = FindComponentInParents(gameObject, "ProloguePcButton") != null
                || FindComponentInParents(gameObject, "RunModeButton") != null
                || FindComponentInParents(gameObject, "MusicVariationView") != null;
            return isPrologueControl
                && (Plugin.Speech.WasRecentSource("prologue:", 5f) || Plugin.Speech.WasRecentSource("character:", 5f));
        }

        internal static string BuildSettingsWidgetSummary(object settingsWidget)
        {
            if (settingsWidget == null)
            {
                return string.Empty;
            }

            string title = ReadLabelText(settingsWidget, "titleLabel");
            string value = ReadLabelText(settingsWidget, "valueLabel");
            int barValue = ReadIntField(settingsWidget, "mInternalBarValue");
            if (string.IsNullOrEmpty(title))
            {
                object widget = Traverse.Create(settingsWidget).Field("widget").GetValue();
                if (widget is Component)
                {
                    title = ExtractUiText(((Component)widget).gameObject);
                }
            }

            title = NormalizeForSpeech(title);
            value = NormalizeForSpeech(value);
            if (string.IsNullOrEmpty(value) && barValue >= 0)
            {
                value = barValue.ToString();
            }

            if (string.IsNullOrEmpty(title))
            {
                return value;
            }

            if (string.IsNullOrEmpty(value) || string.Equals(title, value, StringComparison.Ordinal))
            {
                return title;
            }

            return title + ". " + value;
        }

        internal static string BuildFocusSpeech(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }

            string galleryOrStickerFocus = BuildGalleryOrStickerFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(galleryOrStickerFocus) || IsHandledGalleryOrStickerFocus(gameObject))
            {
                return galleryOrStickerFocus;
            }

            string infoManual = BuildInfoManualFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(infoManual))
            {
                return infoManual;
            }

            string fullCredits = BuildFullCreditsFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(fullCredits))
            {
                return fullCredits;
            }

            object settingsWidget = FindComponentInParents(gameObject, "SettingsWidget");
            if (settingsWidget != null)
            {
                return BuildSettingsControlSummary(gameObject, settingsWidget);
            }

            string prologueControl = BuildPrologueControlFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(prologueControl))
            {
                return prologueControl;
            }

            object customizeWidget = FindCustomizeWidgetInParents(gameObject);
            if (customizeWidget != null)
            {
                return BuildCustomizeControlSummary(gameObject, customizeWidget);
            }

            string campfireSpot = CampfireSeatDescriber.BuildFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(campfireSpot))
            {
                return campfireSpot;
            }

            string gameplayChoice = BuildGameplayChoiceFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(gameplayChoice))
            {
                return gameplayChoice;
            }

            string gameplayAction = BuildGameplayActionFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(gameplayAction))
            {
                return gameplayAction;
            }

            string pauseConfirm = BuildPauseMenuConfirmFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(pauseConfirm))
            {
                return pauseConfirm;
            }

            string missionsOverlay = BuildMissionsOverlayFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(missionsOverlay))
            {
                return missionsOverlay;
            }

            if (IsRoadmapLocationFocus(gameObject))
            {
                return BuildRoadmapLocationFocusSpeech(gameObject);
            }

            string mapped = MapKnownButtonName(gameObject.name);
            if (!string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }

            string buttonSpeech = BuildButtonFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(buttonSpeech))
            {
                return buttonSpeech;
            }

            return ExtractUiText(gameObject);
        }

        private static string BuildButtonFocusSpeech(GameObject gameObject)
        {
            if (gameObject == null || !IsInsideInteractiveButton(gameObject.transform))
            {
                return string.Empty;
            }

            string barterItemSpeech = BuildBarterShopItemFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(barterItemSpeech))
            {
                return barterItemSpeech;
            }

            string popupButtonSpeech = BuildSimplePopupButtonFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(popupButtonSpeech))
            {
                return popupButtonSpeech;
            }

            string destinationProgressSpeech = BuildDestinationProgressButtonFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(destinationProgressSpeech))
            {
                return destinationProgressSpeech;
            }

            string carChoiceSpeech = BuildCarChoiceButtonFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(carChoiceSpeech))
            {
                return carChoiceSpeech;
            }

            List<string> parts = new List<string>();
            Component[] components = gameObject.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "UILabel" || component.gameObject == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                object candidate = Traverse.Create(component).Property("text").GetValue();
                AddNormalizedPart(parts, candidate as string);
            }

            if (parts.Count == 0 && FindComponentInParents(gameObject, "PlayersNumberView") != null)
            {
                string number = ExtractSmallNumberFromPath(gameObject.transform);
                if (!string.IsNullOrEmpty(number))
                {
                    return number == "1" ? "1 player" : number + " players";
                }
            }

            string visibleText = JoinSpeechParts(parts);
            if (!string.IsNullOrEmpty(visibleText))
            {
                return visibleText;
            }

            return CleanButtonObjectName(gameObject.name);
        }

        private static string BuildBarterShopItemFocusSpeech(GameObject gameObject)
        {
            object itemButton = FindComponentInParents(gameObject, "BarterShopItemButton");
            object barterShopView = FindComponentInParents(gameObject, "BarterShopView");
            if (itemButton == null || barterShopView == null)
            {
                return string.Empty;
            }

            try
            {
                int teamIndex = GetBarterButtonIndex(barterShopView, "teamButtonsPanel", itemButton);
                if (teamIndex >= 0)
                {
                    object item = GetListItem(Traverse.Create(barterShopView).Field("mTeamItems").GetValue(), teamIndex);
                    return BuildBarterItemSpeech(item, "Team item. Trade will remove this item");
                }

                int shopIndex = GetBarterButtonIndex(barterShopView, "shopButtonsPanel", itemButton);
                if (shopIndex >= 0)
                {
                    object item = GetListItem(Traverse.Create(barterShopView).Field("mShopItems").GetValue(), shopIndex);
                    return BuildBarterItemSpeech(item, "Shop item. Trade will add this item");
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string BuildCarChoiceButtonFocusSpeech(GameObject gameObject)
        {
            object carView = FindComponentInParents(gameObject, "CarView");
            if (carView == null)
            {
                return string.Empty;
            }

            try
            {
                GameObject cautiousButton = GetGameObjectField(carView, "cautiousActionButton");
                GameObject riskyButton = GetGameObjectField(carView, "riskyActionButton");
                if (cautiousButton != null && gameObject == cautiousButton)
                {
                    return "Cautious planner. Gain 1 from a lowest resource and lose 1 from a highest resource";
                }

                if (riskyButton != null && gameObject == riskyButton)
                {
                    return "Risky traveller. Gain 1 from a highest resource and lose 1 from a lowest resource";
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        internal static string BuildCarEffectsSpeech(object[] args)
        {
            if (args == null || args.Length < 3)
            {
                return string.Empty;
            }

            string gained = CleanIdForSpeech(args[1]?.ToString());
            string lost = CleanIdForSpeech(args[2]?.ToString());
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(gained))
            {
                AddNormalizedPart(parts, "Gained 1 " + gained);
            }

            if (!string.IsNullOrEmpty(lost))
            {
                AddNormalizedPart(parts, "Lost 1 " + lost);
            }

            return JoinSpeechParts(parts);
        }

        internal static string BuildPassengerStatEffectSpeech(object[] args)
        {
            if (args == null || args.Length < 3)
            {
                return string.Empty;
            }

            try
            {
                List<string> parts = new List<string>();
                string passengerName = BuildPassengerName(args[0]);
                if (!string.IsNullOrEmpty(passengerName))
                {
                    AddNormalizedPart(parts, passengerName + " effect");
                }

                if (args.Length >= 6)
                {
                    AddStatDeltaPart(parts, args[2], ReadIntArg(args[3]));
                    AddStatDeltaPart(parts, args[4], ReadIntArg(args[5]));
                }
                else
                {
                    System.Collections.IEnumerable statChanges = args[2] as System.Collections.IEnumerable;
                    if (statChanges != null)
                    {
                        foreach (object statChange in statChanges)
                        {
                            object stat = Traverse.Create(statChange).Field("statToModify").GetValue();
                            int value = Traverse.Create(statChange).Field("statValue").GetValue<int>();
                            AddStatDeltaPart(parts, stat, value);
                        }
                    }
                }

                return parts.Count > 1 ? JoinSpeechParts(parts) : string.Empty;
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string BuildPassengerName(object passengerProfile)
        {
            if (passengerProfile == null)
            {
                return string.Empty;
            }

            string id = NormalizeForSpeech(Traverse.Create(passengerProfile).Field("id").GetValue() as string);
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            try
            {
                object generalManager = GetSingletonInstance("GeneralManager");
                object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
                string name = textManager != null
                    ? NormalizeForSpeech(Traverse.Create(textManager).Method("GetNpcName", id).GetValue() as string)
                    : string.Empty;
                return !string.IsNullOrEmpty(name) ? name : CleanIdForSpeech(id);
            }
            catch
            {
                return CleanIdForSpeech(id);
            }
        }

        private static void AddStatDeltaPart(List<string> parts, object statType, int value)
        {
            if (parts == null || value == 0)
            {
                return;
            }

            string stat = CleanIdForSpeech(statType?.ToString());
            if (string.IsNullOrEmpty(stat) || string.Equals(stat, "None", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string verb = value > 0 ? "Gained " : "Lost ";
            parts.Add(verb + Math.Abs(value) + " " + stat);
        }

        private static int ReadIntArg(object value)
        {
            return value is int number ? number : 0;
        }

        private static string BuildSimplePopupButtonFocusSpeech(GameObject gameObject)
        {
            object yesNoPopup = FindComponentInParents(gameObject, "SimpleYesNoPopup");
            if (yesNoPopup != null)
            {
                string button = ExtractUiText(gameObject);
                string message = NormalizeForSpeech(ReadLabelText(yesNoPopup, "popupMessageLabel"));
                if (!string.IsNullOrEmpty(button) && !string.IsNullOrEmpty(message))
                {
                    return button + ". " + message;
                }
            }

            object okPopup = FindComponentInParents(gameObject, "SimpleOkPopup");
            if (okPopup != null)
            {
                string button = NormalizeButtonLabelForSpeech(ExtractUiText(gameObject));
                string title = NormalizeForSpeech(ReadLabelText(okPopup, "popupOptionalTitleLabel"));
                string message = NormalizeForSpeech(ReadLabelText(okPopup, "popupMessageLabel"));
                List<string> parts = new List<string>();
                AddNormalizedPart(parts, title);
                AddNormalizedPart(parts, message);
                AddNormalizedPart(parts, button);
                if (parts.Count > 1)
                {
                    return JoinSpeechParts(parts);
                }
            }

            return string.Empty;
        }

        private static string BuildDestinationProgressButtonFocusSpeech(GameObject gameObject)
        {
            object destinationProgressView = FindComponentInParents(gameObject, "DestinationProgressView");
            if (destinationProgressView == null)
            {
                return string.Empty;
            }

            string button = NormalizeButtonLabelForSpeech(ExtractUiText(gameObject));
            string summary = BuildDestinationProgressSpeech(destinationProgressView);
            if (string.IsNullOrEmpty(button) || string.IsNullOrEmpty(summary))
            {
                return string.Empty;
            }

            if (WasRecentDestinationProgressSpeech())
            {
                return button;
            }

            return JoinSpeechParts(new[] { summary, button });
        }

        internal static bool WasRecentDestinationProgressSpeech()
        {
            return Plugin.Speech != null
                && Plugin.Speech.WasRecentSource("view:destinationprogress", RecentDestinationProgressSpeechSeconds);
        }

        private static int GetBarterButtonIndex(object barterShopView, string panelFieldName, object itemButton)
        {
            object panel = Traverse.Create(barterShopView).Field(panelFieldName).GetValue();
            object buttonsObject = panel != null ? Traverse.Create(panel).Field("itemButtons").GetValue() : null;
            System.Collections.IEnumerable buttons = buttonsObject as System.Collections.IEnumerable;
            if (buttons == null)
            {
                return -1;
            }

            int index = 0;
            foreach (object button in buttons)
            {
                if (ReferenceEquals(button, itemButton))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private static string BuildBarterItemSpeech(object itemProfile, string context)
        {
            if (itemProfile == null)
            {
                return string.Empty;
            }

            string id = NormalizeForSpeech(Traverse.Create(itemProfile).Field("id").GetValue() as string);
            string name = GetItemDisplayName(id);
            if (string.IsNullOrEmpty(name))
            {
                name = CleanIdForSpeech(id);
            }

            string stat = BuildItemStatChangeSpeech(itemProfile);
            if (string.IsNullOrEmpty(stat))
            {
                return JoinSpeechParts(new List<string> { context, name });
            }

            return JoinSpeechParts(new List<string> { context, name, stat });
        }

        internal static string BuildBarterShopExchangeResultSpeech(object barterShopView)
        {
            if (barterShopView == null)
            {
                return string.Empty;
            }

            try
            {
                object shopItem = Traverse.Create(barterShopView).Field("mShopItemProfile").GetValue();
                object playerItem = Traverse.Create(barterShopView).Field("mPlayerItemProfile").GetValue();

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, BuildItemStatResultSpeech(shopItem, "Gained"));
                AddNormalizedPart(parts, BuildItemStatResultSpeech(playerItem, "Lost"));

                return JoinSpeechParts(parts);
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string GetItemDisplayName(string id)
        {
            try
            {
                object generalManager = GetSingletonInstance("GeneralManager");
                object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
                return textManager != null && !string.IsNullOrEmpty(id)
                    ? NormalizeForSpeech(Traverse.Create(textManager).Method("GetItemName", id).GetValue() as string)
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildItemStatChangeSpeech(object itemProfile)
        {
            object statValuesObject = Traverse.Create(itemProfile).Field("statValues").GetValue();
            System.Collections.IEnumerable statValues = statValuesObject as System.Collections.IEnumerable;
            if (statValues == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            foreach (object statValue in statValues)
            {
                object statChange = Traverse.Create(statValue).Field("statChange").GetValue();
                if (statChange == null)
                {
                    continue;
                }

                string stat = CleanIdForSpeech(Traverse.Create(statChange).Field("statToModify").GetValue()?.ToString());
                int value = Traverse.Create(statChange).Field("statValue").GetValue<int>();
                if (!string.IsNullOrEmpty(stat))
                {
                    parts.Add((value >= 0 ? "+" : string.Empty) + value + " " + stat);
                }
            }

            return parts.Count > 0 ? "Item stat: " + string.Join(", ", parts.ToArray()) : string.Empty;
        }

        private static string BuildItemStatResultSpeech(object itemProfile, string verb)
        {
            if (itemProfile == null)
            {
                return string.Empty;
            }

            object statValuesObject = Traverse.Create(itemProfile).Field("statValues").GetValue();
            System.Collections.IEnumerable statValues = statValuesObject as System.Collections.IEnumerable;
            if (statValues == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            foreach (object statValue in statValues)
            {
                object statChange = Traverse.Create(statValue).Field("statChange").GetValue();
                if (statChange == null)
                {
                    continue;
                }

                string stat = CleanIdForSpeech(Traverse.Create(statChange).Field("statToModify").GetValue()?.ToString());
                int value = Math.Abs(Traverse.Create(statChange).Field("statValue").GetValue<int>());
                if (!string.IsNullOrEmpty(stat) && value != 0)
                {
                    parts.Add(value + " " + stat);
                }
            }

            return parts.Count > 0 ? verb + " " + string.Join(", ", parts.ToArray()) : string.Empty;
        }

        private static object GetListItem(object listObject, int index)
        {
            System.Collections.IList list = listObject as System.Collections.IList;
            if (list == null || index < 0 || index >= list.Count)
            {
                return null;
            }

            return list[index];
        }

        private static string ExtractSmallNumberFromPath(Transform transform)
        {
            string path = BuildPath(transform);
            for (int i = 1; i <= 4; i++)
            {
                string number = i.ToString();
                if (path.Contains(number))
                {
                    return number;
                }
            }

            return string.Empty;
        }

        internal static string BuildEventDialogSpeech(object eventView, object scene, string text)
        {
            text = NormalizeForSpeech(text);
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string speaker = BuildEventDialogSpeaker(eventView, scene);
            return string.IsNullOrEmpty(speaker)
                ? text
                : Plugin.Localization.Get("MOD_TTS_DIALOG", "speaker", speaker, "text", text);
        }

        internal static string BuildPauseMenuConfirmSpeech(object pauseMenu, GameObject focusedButton)
        {
            GameObject confirmPanel = GetGameObjectField(pauseMenu, "confirmPanelContainer");
            if (confirmPanel == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            AddVisibleLabelText(confirmPanel, parts, includeInteractiveButtons: false);
            if (focusedButton != null)
            {
                AddNormalizedPart(parts, NormalizeButtonLabelForSpeech(ExtractUiText(focusedButton)));
            }

            return JoinSpeechParts(parts);
        }

        private static string BuildPauseMenuConfirmFocusSpeech(GameObject gameObject)
        {
            object pauseMenu = FindComponentInParents(gameObject, "PauseMenu");
            if (pauseMenu == null)
            {
                return string.Empty;
            }

            GameObject confirmPanel = GetGameObjectField(pauseMenu, "confirmPanelContainer");
            if (confirmPanel == null || !confirmPanel.activeInHierarchy)
            {
                return string.Empty;
            }

            Transform current = gameObject.transform;
            while (current != null)
            {
                if (current.gameObject == confirmPanel)
                {
                    return NormalizeButtonLabelForSpeech(ExtractUiText(gameObject));
                }

                current = current.parent;
            }

            return string.Empty;
        }

        internal static string BuildEventDialogSpeaker(object eventView, object scene)
        {
            if (scene == null)
            {
                return string.Empty;
            }

            string speakerId = NormalizeForSpeech(Traverse.Create(scene).Field("whoSpeaksNPC").GetValue() as string);
            if (string.Equals(speakerId, "player", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    object playerTurnManager = Traverse.Create(eventView).Field("mPlayerTurnManager").GetValue();
                    object curPlayer = playerTurnManager != null ? Traverse.Create(playerTurnManager).Field("curPlayer").GetValue() : null;
                    string playerName = BuildPlayerName(curPlayer);
                    return string.IsNullOrEmpty(playerName) ? speakerId : playerName;
                }
                catch
                {
                    return speakerId;
                }
            }

            string npcName = GetNpcDisplayName(speakerId);
            return string.IsNullOrEmpty(npcName) ? CleanIdForSpeech(speakerId) : npcName;
        }

        internal static string BuildPlayerName(object playerData)
        {
            try
            {
                if (playerData == null)
                {
                    return string.Empty;
                }

                string name = NormalizeForSpeech(Traverse.Create(playerData).Field("name").GetValue() as string);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }

                object pcProfile = Traverse.Create(playerData).Field("pcProfile").GetValue();
                if (pcProfile == null)
                {
                    return string.Empty;
                }

                name = NormalizeForSpeech(Traverse.Create(pcProfile).Field("id").GetValue() as string);
                return CleanIdForSpeech(name);
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string GetNpcDisplayName(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return string.Empty;
            }

            try
            {
                object generalManager = GetSingletonInstance("GeneralManager");
                object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
                string npcName = textManager != null
                    ? NormalizeForSpeech(Traverse.Create(textManager).Method("GetNpcName", npcId).GetValue() as string)
                    : string.Empty;
                return IsMissingLocalizedText(npcName) ? string.Empty : npcName;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsMissingLocalizedText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.IndexOf("NOT FOUND", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static string BuildStatsSummary()
        {
            try
            {
                Type mainGameUiManagerType = AccessTools.TypeByName("MainGameUIManager")
                    ?? AccessTools.TypeByName("Game.MainGameUIManager");
                if (mainGameUiManagerType == null)
                {
                    return Plugin.Localization.Get("MOD_TTS_STATS_UNAVAILABLE");
                }

                UnityEngine.Object manager = UnityEngine.Object.FindObjectOfType(mainGameUiManagerType);
                if (manager == null)
                {
                    return Plugin.Localization.Get("MOD_TTS_STATS_UNAVAILABLE");
                }

                object overlay = Traverse.Create(manager).Field("sharedStatsOverlay").GetValue();
                object statsBox = overlay != null ? Traverse.Create(overlay).Field("statsBox").GetValue() : null;
                object widgetsObject = statsBox != null ? Traverse.Create(statsBox).Method("GetStatWidgets").GetValue() : null;
                System.Collections.IEnumerable widgets = widgetsObject as System.Collections.IEnumerable;
                if (widgets == null)
                {
                    return Plugin.Localization.Get("MOD_TTS_STATS_UNAVAILABLE");
                }

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, Plugin.Localization.Get("MOD_TTS_STATS_PREFIX"));
                foreach (object widget in widgets)
                {
                    string name = NormalizeForSpeech(ReadLabelText(widget, "nameLabel"));
                    string value = NormalizeForSpeech(ReadLabelText(widget, "valueLabel"));
                    if (string.IsNullOrEmpty(name))
                    {
                        name = CleanIdForSpeech(Traverse.Create(widget).Field("type").GetValue()?.ToString());
                    }

                    if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    AddNormalizedPart(parts, string.IsNullOrEmpty(value) ? name : name + " " + value);
                }

                return parts.Count > 1
                    ? JoinSpeechParts(parts)
                    : Plugin.Localization.Get("MOD_TTS_STATS_UNAVAILABLE");
            }
            catch
            {

                return Plugin.Localization.Get("MOD_TTS_STATS_UNAVAILABLE");
            }
        }

        internal static string BuildMissionPointsSummary()
        {
            try
            {
                Type mainGameUiManagerType = AccessTools.TypeByName("MainGameUIManager")
                    ?? AccessTools.TypeByName("Game.MainGameUIManager");
                UnityEngine.Object manager = mainGameUiManagerType != null
                    ? UnityEngine.Object.FindObjectOfType(mainGameUiManagerType)
                    : null;
                object overlay = manager != null ? Traverse.Create(manager).Field("missionsOverlay").GetValue() : null;
                object tabsObject = overlay != null ? Traverse.Create(overlay).Field("playerTabs").GetValue() : null;
                System.Collections.IEnumerable tabs = tabsObject as System.Collections.IEnumerable;
                if (tabs == null)
                {
                    return string.Empty;
                }

                object generalManager = GetSingletonInstance("GeneralManager");
                object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
                List<string> parts = new List<string>();
                foreach (object tab in tabs)
                {
                    object player = Traverse.Create(tab).Method("GetPlayerDataAssigned").GetValue();
                    if (player == null)
                    {
                        continue;
                    }

                    string playerName = BuildPlayerName(player);
                    int missionPoints = Traverse.Create(player).Field("missionPoints").GetValue<int>();
                    string playerHeader = string.IsNullOrEmpty(playerName)
                        ? missionPoints + " Prank Dollarz"
                        : playerName + ": " + missionPoints + " Prank Dollarz";

                    List<string> missions = new List<string>();
                    object activeMissionsObject = Traverse.Create(player).Field("activeMissions").GetValue();
                    System.Collections.IEnumerable activeMissions = activeMissionsObject as System.Collections.IEnumerable;
                    if (activeMissions != null && textManager != null)
                    {
                        foreach (object missionData in activeMissions)
                        {
                            object profile = Traverse.Create(missionData).Field("profile").GetValue();
                            string missionName = NormalizeForSpeech(Traverse.Create(textManager).Method("GetMissionName", profile).GetValue() as string);
                            int reward = profile != null ? Traverse.Create(profile).Field("rewardMissionPoints").GetValue<int>() : 0;
                            if (!string.IsNullOrEmpty(missionName))
                            {
                                missions.Add(reward > 0 ? missionName + ", " + reward : missionName);
                            }
                        }
                    }

                    AddNormalizedPart(parts, missions.Count > 0
                        ? playerHeader + ". Current dares: " + string.Join("; ", missions.ToArray())
                        : playerHeader + ". No current dares found");
                }

                return parts.Count > 0
                    ? Plugin.Localization.Get("MOD_TTS_MISSION_POINTS_SUMMARY", "text", JoinSpeechParts(parts))
                    : string.Empty;
            }
            catch
            {

                return string.Empty;
            }
        }

        internal static bool IsGameShowMissionPointsHeld()
        {
            try
            {
                Type inputManagerType = AccessTools.TypeByName("BeautifulGlitch.CoreInputManager")
                    ?? AccessTools.TypeByName("CoreInputManager");
                UnityEngine.Object inputManager = inputManagerType != null
                    ? UnityEngine.Object.FindObjectOfType(inputManagerType)
                    : null;
                if (inputManager == null)
                {
                    return false;
                }

                object result = Traverse.Create(inputManager).Method("IsShowMissionPointsHeld").GetValue();
                return result is bool isHeld && isHeld;
            }
            catch
            {

                return false;
            }
        }

        internal static string BuildTriggerWarningsTooltipSpeech(object tooltipTriggerWarnings)
        {
            try
            {
                if (tooltipTriggerWarnings == null)
                {
                    return string.Empty;
                }

                object labelsObject = Traverse.Create(tooltipTriggerWarnings).Field("tooltipsLabels").GetValue();
                System.Collections.IEnumerable labels = labelsObject as System.Collections.IEnumerable;
                if (labels == null)
                {
                    return string.Empty;
                }

                List<string> parts = new List<string>();
                foreach (object label in labels)
                {
                    object text = Traverse.Create(label).Property("text").GetValue();
                    AddNormalizedPart(parts, text as string);
                }

                if (parts.Count == 0)
                {
                    return string.Empty;
                }

                return Plugin.Localization.Get("MOD_TTS_TRIGGER_WARNINGS", "text", JoinSpeechParts(parts));
            }
            catch
            {

                return string.Empty;
            }
        }

        internal static string BuildStatProfileName(object statProfile)
        {
            try
            {
                if (statProfile == null)
                {
                    return string.Empty;
                }

                string name = NormalizeForSpeech(Traverse.Create(statProfile).Field("name").GetValue() as string);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }

                return CleanIdForSpeech(Traverse.Create(statProfile).Field("statType").GetValue()?.ToString());
            }
            catch
            {

                return string.Empty;
            }
        }

        internal static bool IsGameplayChoiceOrActionFocus(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            return FindComponentInParents(gameObject, "EventChoiceButton") != null
                || FindComponentInParents(gameObject, "LocationActionButton") != null
                || FindComponentInParents(gameObject, "ActionButton") != null;
        }

        private static bool IsHandledGalleryOrStickerFocus(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            return FindComponentInParents(gameObject, "GalleryTab") != null
                || FindComponentInParents(gameObject, "FakeGalleryTab") != null
                || FindComponentInParents(gameObject, "StickerTab") != null;
        }

        internal static GameObject GetCurrentNavigationObject()
        {
            Type uiCameraType = AccessTools.TypeByName("UICamera");
            if (uiCameraType == null)
            {
                return null;
            }

            object current = Traverse.Create(uiCameraType).Property("controllerNavigationObject").GetValue()
                ?? Traverse.Create(uiCameraType).Property("selectedObject").GetValue();
            return current as GameObject;
        }

        internal static GameObject GetGameObjectField(object instance, string fieldName)
        {
            if (instance == null)
            {
                return null;
            }

            object value = Traverse.Create(instance).Field(fieldName).GetValue();
            if (value is GameObject gameObject)
            {
                return gameObject;
            }

            if (value is Component component)
            {
                return component.gameObject;
            }

            return null;
        }

        internal static bool IsMainTitleSettingsSubButton(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            string path = BuildPath(gameObject.transform);
            return path.Contains("/SettingsSectionPanel/SystemSettingsButtonContainer/SystemSettingsButton")
                || path.Contains("/SettingsSectionPanel/CustomizeButtonContainer/CustomizeButton");
        }

        internal static GameObject FindFirstActiveSettingsArrow(object titleSettings)
        {
            if (titleSettings == null)
            {
                return null;
            }

            object widgetsValue = Traverse.Create(titleSettings).Field("mAllSettingsWidgets").GetValue();
            System.Collections.IEnumerable widgets = widgetsValue as System.Collections.IEnumerable;
            if (widgets == null)
            {
                return null;
            }

            foreach (object widget in widgets)
            {
                if (widget == null)
                {
                    continue;
                }

                Component component = widget as Component;
                if (component == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                GameObject leftArrow = GetGameObjectField(widget, "leftArrowGo");
                if (leftArrow != null && leftArrow.activeInHierarchy)
                {
                    return leftArrow;
                }

                GameObject rightArrow = GetGameObjectField(widget, "rightArrowGo");
                if (rightArrow != null && rightArrow.activeInHierarchy)
                {
                    return rightArrow;
                }
            }

            return null;
        }

        internal static object FindComponentInParents(GameObject gameObject, string typeName)
        {
            if (gameObject == null)
            {
                return null;
            }

            Transform current = gameObject.transform;
            while (current != null)
            {
                Component[] components = current.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component != null && component.GetType().Name == typeName)
                    {
                        return component;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        internal static string BuildCustomizeWidgetSummary(object customizeWidget)
        {
            if (customizeWidget == null)
            {
                return string.Empty;
            }

            string title = GetCustomizeWidgetTitle(customizeWidget);
            string value = NormalizeForSpeech(ReadLabelText(customizeWidget, "valueLabel"));

            if (string.IsNullOrEmpty(title))
            {
                return value;
            }

            if (string.IsNullOrEmpty(value) || string.Equals(title, value, StringComparison.Ordinal))
            {
                return title;
            }

            return title + ". " + value;
        }

        internal static string GetCustomizeNoteKey(GameObject gameObject)
        {
            object customizeWidget = FindCustomizeWidgetInParents(gameObject);
            if (customizeWidget == null)
            {
                return string.Empty;
            }

            string typeName = customizeWidget.GetType().Name;
            if (string.Equals(typeName, "TriggerWarningWidget", StringComparison.Ordinal))
            {
                object type = Traverse.Create(customizeWidget).Field("type").GetValue();
                return NormalizeForSpeech(type?.ToString());
            }

            if (string.Equals(typeName, "AllowSelfLoveWidget", StringComparison.Ordinal))
            {
                return "allowSelfLove";
            }

            if (string.Equals(typeName, "EndOfTheRoadWidget", StringComparison.Ordinal))
            {
                return "endOfTheRoad";
            }

            return string.Empty;
        }

        private static string ExtractUILabelText(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "UILabel")
                {
                    continue;
                }

                object candidate = Traverse.Create(component).Property("text").GetValue();
                string normalized = NormalizeForSpeech(candidate as string);
                if (!string.IsNullOrEmpty(normalized))
                {
                    return normalized;
                }
            }

            return string.Empty;
        }

        private static string ReadLabelText(object instance, string fieldName)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return string.Empty;
            }

            object label = field.GetValue(instance);
            if (label == null)
            {
                return string.Empty;
            }

            object text = Traverse.Create(label).Property("text").GetValue();
            return text as string ?? string.Empty;
        }

        private static void AddNormalizedPart(List<string> parts, string value)
        {
            string cleaned = NormalizeForSpeech(value);
            if (string.IsNullOrEmpty(cleaned))
            {
                return;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                if (string.Equals(parts[i], cleaned, StringComparison.Ordinal))
                {
                    return;
                }
            }

            parts.Add(cleaned);
        }

        private static string BuildSettingsControlSummary(GameObject focusedObject, object settingsWidget)
        {
            string summary = BuildSettingsWidgetSummary(settingsWidget);
            if (string.IsNullOrEmpty(summary))
            {
                return string.Empty;
            }

            string title = NormalizeForSpeech(ReadLabelText(settingsWidget, "titleLabel"));
            if (string.IsNullOrEmpty(title))
            {
                title = NormalizeForSpeech(ExtractUiText((settingsWidget as Component)?.gameObject));
            }

            GameObject leftArrow = GetGameObjectField(settingsWidget, "leftArrowGo");
            GameObject rightArrow = GetGameObjectField(settingsWidget, "rightArrowGo");
            if (focusedObject == leftArrow)
            {
                return summary + ". " + GetSettingsArrowAction(title, isIncrease: false);
            }

            if (focusedObject == rightArrow)
            {
                return summary + ". " + GetSettingsArrowAction(title, isIncrease: true);
            }

            return summary;
        }

        private static object FindCustomizeWidgetInParents(GameObject gameObject)
        {
            string[] typeNames = new[]
            {
                "AllowSelfLoveWidget",
                "EndOfTheRoadWidget",
                "JumpScareWidget",
                "SpecialHitchhikerWidget",
                "TriggerWarningWidget"
            };

            for (int i = 0; i < typeNames.Length; i++)
            {
                object found = FindComponentInParents(gameObject, typeNames[i]);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string BuildGameplayChoiceFocusSpeech(GameObject gameObject)
        {
            object choiceButton = FindComponentInParents(gameObject, "EventChoiceButton");
            if (choiceButton == null)
            {
                return string.Empty;
            }

            Component choiceComponent = choiceButton as Component;
            if (choiceComponent == null || choiceComponent.gameObject == null || !choiceComponent.gameObject.activeInHierarchy)
            {
                return string.Empty;
            }

            if (IsMainGameEventTextBoxVisible(gameObject))
            {
                return string.Empty;
            }

            string label = NormalizeForSpeech(ReadLabelText(choiceButton, "buttonLabel"));
            if (string.IsNullOrEmpty(label))
            {
                label = NormalizeForSpeech(ExtractUiText((choiceButton as Component)?.gameObject));
            }

            return string.IsNullOrEmpty(label)
                ? string.Empty
                : Plugin.Localization.Get("MOD_TTS_CHOICE", "text", label);
        }

        private static bool IsMainGameEventTextBoxVisible(GameObject gameObject)
        {
            object eventView = FindComponentInParents(gameObject, "MainGameEventView");
            if (eventView == null)
            {
                return false;
            }

            try
            {
                object textBox = Traverse.Create(eventView).Field("simpleTextBox").GetValue();
                object visible = textBox != null ? Traverse.Create(textBox).Method("IsVisible").GetValue() : null;
                return visible is bool isVisible && isVisible;
            }
            catch
            {
                return false;
            }
        }

        private static string BuildPrologueControlFocusSpeech(GameObject gameObject)
        {
            object pcButton = FindComponentInParents(gameObject, "ProloguePcButton");
            if (pcButton != null)
            {
                string name = BuildPcProfileName(Traverse.Create(pcButton).Field("pcProfile").GetValue());
                if (string.IsNullOrEmpty(name))
                {
                    return string.Empty;
                }

                bool dlcLocked = IsSpriteEnabled(Traverse.Create(pcButton).Field("dlcTag").GetValue());
                return dlcLocked ? name + ". Locked DLC character." : name;
            }

            object runModeButton = FindComponentInParents(gameObject, "RunModeButton");
            if (runModeButton != null)
            {
                return BuildRunModeButtonSpeech(runModeButton);
            }

            object actionButton = FindComponentInParents(gameObject, "ActionButton");
            if (actionButton != null && FindComponentInParents(gameObject, "MusicVariationView") != null)
            {
                return BuildMusicVariationButtonSpeech(actionButton);
            }

            return string.Empty;
        }

        private static string BuildGameplayActionFocusSpeech(GameObject gameObject)
        {
            try
            {
                object actionButton = FindComponentInParents(gameObject, "LocationActionButton")
                    ?? FindComponentInParents(gameObject, "ActionButton");
                if (actionButton == null)
                {
                    return string.Empty;
                }

                Component actionComponent = actionButton as Component;
                if (actionComponent == null || actionComponent.gameObject == null || !actionComponent.gameObject.activeInHierarchy)
                {
                    return string.Empty;
                }

                if (FindComponentInParents(gameObject, "MusicVariationView") != null)
                {
                    string musicVariation = BuildMusicVariationButtonSpeech(actionButton);
                    if (!string.IsNullOrEmpty(musicVariation))
                    {
                        return musicVariation;
                    }
                }

                string label = NormalizeForSpeech(ReadLabelText(actionButton, "buttonLabel"));
                if (string.IsNullOrEmpty(label))
                {
                    label = NormalizeForSpeech(ExtractUiText((actionButton as Component)?.gameObject));
                }

                if (string.IsNullOrEmpty(label))
                {
                    return string.Empty;
                }

                if (string.Equals(actionButton.GetType().Name, "LocationActionButton", StringComparison.Ordinal))
                {
                    object unclickable = Traverse.Create(actionButton).Field("mIsUnclickable").GetValue();
                    if (unclickable is bool isUnclickable && isUnclickable)
                    {
                        return "Unavailable. " + label;
                    }

                    string cue = GetCachedLocationActionCue(actionButton);
                    if (!string.IsNullOrEmpty(cue))
                    {
                        return label + ". " + cue;
                    }
                }

                return label;
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string BuildRoadmapLocationFocusSpeech(GameObject gameObject)
        {
            try
            {
                if (IsMainGameEventDialogueVisible())
                {
                    return string.Empty;
                }

                object locationWidget = FindComponentInParents(gameObject, "RoadmapLocationWidget");
                if (locationWidget == null)
                {
                    return string.Empty;
                }

                object locationData = Traverse.Create(locationWidget).Method("GetRoadmapLocationData").GetValue();
                if (locationData == null)
                {
                    bool isOrigin = Traverse.Create(locationWidget).Method("IsOrigin").GetValue<bool>();
                    return isOrigin ? "Current road position" : string.Empty;
                }

                string name = NormalizeForSpeech(Traverse.Create(locationData).Method("GetLocationName").GetValue() as string);
                string description = NormalizeForSpeech(Traverse.Create(locationData).Method("GetLocationDescription").GetValue() as string);
                if (string.IsNullOrEmpty(name))
                {
                    name = CleanIdForSpeech(Traverse.Create(locationData).Method("GetLocationId").GetValue()?.ToString());
                }

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, name);
                AddNormalizedPart(parts, description);
                return JoinSpeechParts(parts);
            }
            catch
            {

                return string.Empty;
            }
        }

        private static bool IsRoadmapLocationFocus(GameObject gameObject)
        {
            return FindComponentInParents(gameObject, "RoadmapLocationWidget") != null;
        }

        internal static bool IsMainGameEventDialogueVisible()
        {
            Type eventViewType = AccessTools.TypeByName("MainGameEventView");
            if (eventViewType == null)
            {
                return false;
            }

            UnityEngine.Object[] views = UnityEngine.Object.FindObjectsOfType(eventViewType);
            for (int i = 0; i < views.Length; i++)
            {
                object view = views[i];
                if (view == null)
                {
                    continue;
                }

                try
                {
                    GameObject container = GetGameObjectField(view, "container");
                    if (container == null || !container.activeInHierarchy)
                    {
                        continue;
                    }

                    object textBox = Traverse.Create(view).Field("simpleTextBox").GetValue();
                    object visible = textBox != null ? Traverse.Create(textBox).Method("IsVisible").GetValue() : null;
                    if (visible is bool isVisible && isVisible)
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static string BuildRunModeButtonSpeech(object runModeButton)
        {
            string runMode = NormalizeForSpeech(Traverse.Create(runModeButton).Field("runMode").GetValue()?.ToString());
            bool isFullCoop = Traverse.Create(runModeButton).Field("isFullCoop").GetValue<bool>();
            string label;
            if (string.Equals(runMode, "RoadtripMissions", StringComparison.Ordinal))
            {
                label = "Roadtrip Missions";
            }
            else if (string.Equals(runMode, "RoadtripNoMissions", StringComparison.Ordinal))
            {
                label = "Roadtrip";
            }
            else
            {
                label = CleanIdForSpeech(runMode);
            }

            List<string> parts = new List<string>();
            AddNormalizedPart(parts, label);
            if (isFullCoop)
            {
                AddNormalizedPart(parts, "Full co-op");
            }

            object view = FindComponentInParents((runModeButton as Component)?.gameObject, "RunModeView");
            if (view != null
                && string.Equals(runMode, "RoadtripMissions", StringComparison.Ordinal)
                && Traverse.Create(view).Field("mMissionsModeLocked").GetValue<bool>())
            {
                AddNormalizedPart(parts, "Locked");
            }

            return JoinSpeechParts(parts);
        }

        private static string BuildMusicVariationButtonSpeech(object actionButton)
        {
            int index = GetButtonIndexInParentGroup(actionButton, "MusicVariationView", "musicVariationButtons");
            switch (index)
            {
                case 0:
                    return "Desert tune";
                case 1:
                    return "House tune";
                case 2:
                    return "Lo-fi tune";
                case 3:
                    return "Surf rock tune";
                default:
                    return string.Empty;
            }
        }

        private static int GetButtonIndexInParentGroup(object button, string parentTypeName, string groupFieldName)
        {
            Component buttonComponent = button as Component;
            if (buttonComponent == null)
            {
                return -1;
            }

            object parent = FindComponentInParents(buttonComponent.gameObject, parentTypeName);
            object group = parent != null ? Traverse.Create(parent).Field(groupFieldName).GetValue() : null;
            object handlersObject = group != null ? Traverse.Create(group).Field("buttonsHandlers").GetValue() : null;
            System.Collections.IList handlers = handlersObject as System.Collections.IList;
            if (handlers == null)
            {
                return -1;
            }

            for (int i = 0; i < handlers.Count; i++)
            {
                if (object.ReferenceEquals(handlers[i], button))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string BuildPcProfileName(object pcProfile)
        {
            if (pcProfile == null)
            {
                return string.Empty;
            }

            string id = NormalizeForSpeech(Traverse.Create(pcProfile).Field("id").GetValue() as string);
            object generalManager = GetSingletonInstance("GeneralManager");
            object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
            string localized = textManager != null && !string.IsNullOrEmpty(id)
                ? NormalizeForSpeech(Traverse.Create(textManager).Method("GetPcName", id).GetValue() as string)
                : string.Empty;
            return string.IsNullOrEmpty(localized) ? CleanIdForSpeech(id) : localized;
        }

        private static bool IsSpriteEnabled(object spriteObject)
        {
            try
            {
                return spriteObject != null && Traverse.Create(spriteObject).Property("enabled").GetValue<bool>();
            }
            catch
            {
                return false;
            }
        }

        internal static void CacheLocationActionCue(object actionButton, object eventFlow)
        {
            Component component = actionButton as Component;
            if (component == null || eventFlow == null || eventFlow is string)
            {
                return;
            }

            int instanceId = component.gameObject.GetInstanceID();
            string cue = BuildLocationActionCue(eventFlow);
            if (string.IsNullOrEmpty(cue))
            {
                return;
            }

            LocationActionCueByInstanceId[instanceId] = cue;
        }

        internal static string BuildMissionsDestinationsTabSpeech(object missionsOverlay)
        {
            try
            {
                if (missionsOverlay == null)
                {
                    return string.Empty;
                }

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, ReadLabelText(missionsOverlay, "destinationsTitleLabel"));
                if (parts.Count == 0)
                {
                    AddNormalizedPart(parts, "Destinations tab");
                }

                object widgetsObject = Traverse.Create(missionsOverlay).Field("destinationCardWidgets").GetValue();
                System.Collections.IEnumerable widgets = widgetsObject as System.Collections.IEnumerable;
                if (widgets != null)
                {
                    foreach (object widget in widgets)
                    {
                        Component component = widget as Component;
                        if (component == null || component.gameObject == null || !component.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        string cardSpeech = BuildDestinationCardWidgetSpeech(widget);
                        AddNormalizedPart(parts, cardSpeech);
                    }
                }

                return JoinSpeechParts(parts);
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string BuildMissionsOverlayFocusSpeech(GameObject gameObject)
        {
            object missionsOverlay = FindComponentInParents(gameObject, "MissionsOverlay");
            if (missionsOverlay == null)
            {
                return string.Empty;
            }

            string path = BuildPath(gameObject.transform);
            if (path.IndexOf("Destination", StringComparison.OrdinalIgnoreCase) < 0
                && gameObject.name.IndexOf("Destination", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return string.Empty;
            }

            return BuildMissionsDestinationsTabSpeech(missionsOverlay);
        }

        internal static string BuildDestinationMissionResultsSpeech(object destinationMissionResultsView)
        {
            try
            {
                object widgetsObject = Traverse.Create(destinationMissionResultsView).Field("destinationCardWidgets").GetValue();
                System.Collections.IEnumerable widgets = widgetsObject as System.Collections.IEnumerable;
                if (widgets == null)
                {
                    return string.Empty;
                }

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, "Destination mission results");
                foreach (object widget in widgets)
                {
                    Component component = widget as Component;
                    if (component == null || component.gameObject == null || !component.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    string destination = CleanIdForSpeech(Traverse.Create(widget).Field("destinationType").GetValue()?.ToString());
                    string stat = NormalizeForSpeech(ReadLabelText(widget, "statTitleLabel"));
                    string value = NormalizeForSpeech(ReadLabelText(widget, "statValueLabel"));
                    string extra1 = NormalizeForSpeech(ReadLabelText(widget, "missionExtra1Label"));
                    string extra2 = NormalizeForSpeech(ReadLabelText(widget, "missionExtra2Label"));

                    AddNormalizedPart(parts, BuildDestinationCardWidgetSpeech(destination, stat, value, extra1, extra2));
                }

                return JoinSpeechParts(parts);
            }
            catch
            {

                return string.Empty;
            }
        }

        internal static string BuildDestinationProgressSpeech(object destinationProgressView)
        {
            try
            {
                object widgetsObject = Traverse.Create(destinationProgressView).Field("destinationCardWidgets").GetValue();
                System.Collections.IEnumerable widgets = widgetsObject as System.Collections.IEnumerable;
                if (widgets == null)
                {
                    return string.Empty;
                }

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, "Destination progress");
                foreach (object widget in widgets)
                {
                    Component component = widget as Component;
                    if (component == null || component.gameObject == null || !component.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    string destination = CleanIdForSpeech(Traverse.Create(widget).Field("destinationType").GetValue()?.ToString());
                    string stat = NormalizeForSpeech(ReadLabelText(widget, "statTitleLabel"));
                    string value = NormalizeForSpeech(ReadLabelText(widget, "statValueLabel"));
                    string extra1 = NormalizeForSpeech(ReadLabelText(widget, "missionExtra1Label"));
                    string extra2 = NormalizeForSpeech(ReadLabelText(widget, "missionExtra2Label"));

                    AddNormalizedPart(parts, BuildDestinationCardWidgetSpeech(destination, stat, value, extra1, extra2));
                }

                return parts.Count > 1 ? JoinSpeechParts(parts) : string.Empty;
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string BuildDestinationCardWidgetSpeech(object widget)
        {
            string destination = CleanIdForSpeech(Traverse.Create(widget).Field("destinationType").GetValue()?.ToString());
            string stat = NormalizeForSpeech(ReadLabelText(widget, "statTitleLabel"));
            string value = NormalizeForSpeech(ReadLabelText(widget, "statValueLabel"));
            string extra1 = NormalizeForSpeech(ReadLabelText(widget, "missionExtra1Label"));
            string extra2 = NormalizeForSpeech(ReadLabelText(widget, "missionExtra2Label"));
            return BuildDestinationCardWidgetSpeech(destination, stat, value, extra1, extra2);
        }

        private static string BuildDestinationCardWidgetSpeech(string destination, string stat, string value, string extra1, string extra2)
        {
            List<string> cardParts = new List<string>();
            AddNormalizedPart(cardParts, destination);
            if (!string.IsNullOrEmpty(stat) || !string.IsNullOrEmpty(value))
            {
                AddNormalizedPart(cardParts, "Required resource " + JoinSpeechParts(new[] { stat, value }));
            }

            AddNormalizedPart(cardParts, extra1);
            AddNormalizedPart(cardParts, extra2);
            return JoinSpeechParts(cardParts);
        }

        internal static string BuildResultsViewSpeech(object[] args)
        {
            try
            {
                if (args == null || args.Length < 5)
                {
                    return string.Empty;
                }

                object totalProgress = args[0];
                object seenProgress = args[1];
                object newProgress = args[2];
                int totalMemories = args[3] is int total ? total : 0;
                int newMemories = args[4] is int added ? added : 0;

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, "Results");
                AddNormalizedPart(parts, BuildProgressSummary("New this run", newProgress, newMemories, useTotals: false));
                AddNormalizedPart(parts, BuildProgressSummary("Total progress", seenProgress, totalMemories + newMemories, useTotals: true, totalProgress));

                if (args.Length >= 7)
                {
                    AddNormalizedPart(parts, BuildRankingSummary(args[5], args[6]));
                }

                return JoinSpeechParts(parts);
            }
            catch
            {

                return string.Empty;
            }
        }

        private static string GetCachedLocationActionCue(object actionButton)
        {
            Component component = actionButton as Component;
            if (component == null || component.gameObject == null)
            {
                return string.Empty;
            }

            string cue;
            return LocationActionCueByInstanceId.TryGetValue(component.gameObject.GetInstanceID(), out cue)
                ? cue
                : string.Empty;
        }

        private static string BuildLocationActionCue(object eventFlow)
        {
            if (eventFlow == null || eventFlow is string)
            {
                return string.Empty;
            }

            try
            {
                string stat = NormalizeForSpeech(Traverse.Create(eventFlow).Field("knownStat").GetValue()?.ToString());
                if (string.IsNullOrEmpty(stat) || string.Equals(stat, "None", StringComparison.OrdinalIgnoreCase))
                {
                    return "Cue: unknown resource.";
                }

                bool positive = Traverse.Create(eventFlow).Method("IsKnownStatPositive").GetValue<bool>();
                return positive
                    ? "Cue: gain " + CleanIdForSpeech(stat) + "."
                    : "Cue: lose " + CleanIdForSpeech(stat) + ".";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildProgressSummary(string prefix, object progress, int memories, bool useTotals, object totalProgress = null)
        {
            if (progress == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            AddNormalizedPart(parts, prefix);
            AddProgressPart(parts, "locations", progress, totalProgress, useTotals);
            AddProgressPart(parts, "events", progress, totalProgress, useTotals);
            AddProgressPart(parts, "outcomes", progress, totalProgress, useTotals);
            int endings = ReadIntFieldSafe(progress, "mvpEndings") + ReadIntFieldSafe(progress, "dateEndings");
            if (useTotals && totalProgress != null)
            {
                int totalEndings = ReadIntFieldSafe(totalProgress, "mvpEndings") + ReadIntFieldSafe(totalProgress, "dateEndings");
                AddNormalizedPart(parts, "individual endings " + endings + " of " + totalEndings);
            }
            else
            {
                AddNormalizedPart(parts, "individual endings " + endings);
            }
            AddProgressPart(parts, "destinationLayers", progress, totalProgress, useTotals, "destination layers");
            if (memories > 0)
            {
                AddNormalizedPart(parts, useTotals ? "memories " + memories : "memories +" + memories);
            }
            return JoinSpeechParts(parts);
        }

        private static void AddProgressPart(List<string> parts, string fieldName, object progress, object totalProgress, bool useTotals, string labelOverride = null)
        {
            string label = labelOverride ?? fieldName;
            int value = ReadIntFieldSafe(progress, fieldName);
            if (useTotals && totalProgress != null)
            {
                int total = ReadIntFieldSafe(totalProgress, fieldName);
                AddNormalizedPart(parts, SplitCamelForSpeech(label) + " " + value + " of " + total);
            }
            else
            {
                AddNormalizedPart(parts, SplitCamelForSpeech(label) + " " + value);
            }
        }

        private static string BuildRankingSummary(object rankingObject, object positionsObject)
        {
            System.Collections.IList ranking = rankingObject as System.Collections.IList;
            System.Collections.IList positions = positionsObject as System.Collections.IList;
            if (ranking == null || positions == null || ranking.Count == 0)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            AddNormalizedPart(parts, "Ranking");
            int count = Math.Min(ranking.Count, positions.Count);
            for (int i = 0; i < count; i++)
            {
                string player = BuildPlayerName(ranking[i]);
                string position = positions[i]?.ToString();
                if (!string.IsNullOrEmpty(player) && !string.IsNullOrEmpty(position))
                {
                    AddNormalizedPart(parts, player + " position " + position);
                }
            }

            return JoinSpeechParts(parts);
        }

        private static int ReadIntFieldSafe(object instance, string fieldName)
        {
            try
            {
                object value = Traverse.Create(instance).Field(fieldName).GetValue();
                return value is int number ? number : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string SplitCamelForSpeech(string value)
        {
            return NormalizeForSpeech(InsertSpacesBeforeCapitals(value)).ToLowerInvariant();
        }

        private static string CleanIdForSpeech(string value)
        {
            string cleaned = NormalizeForSpeech(value);
            return string.IsNullOrEmpty(cleaned) ? string.Empty : cleaned.Replace('_', ' ');
        }

        private static string CleanButtonObjectName(string value)
        {
            string cleaned = CleanIdForSpeech(value);
            if (string.IsNullOrEmpty(cleaned))
            {
                return string.Empty;
            }

            if (cleaned.IndexOf("dummy", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return string.Empty;
            }

            cleaned = cleaned.Replace("Button", string.Empty)
                .Replace("button", string.Empty)
                .Replace("Container", string.Empty)
                .Replace("container", string.Empty)
                .Replace("Ui", string.Empty)
                .Replace("UI", string.Empty);
            return NormalizeForSpeech(InsertSpacesBeforeCapitals(cleaned));
        }

        private static string InsertSpacesBeforeCapitals(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length * 2);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(value[i - 1]) && !char.IsUpper(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        private static string BuildCustomizeControlSummary(GameObject focusedObject, object customizeWidget)
        {
            string summary = BuildCustomizeWidgetSummary(customizeWidget);
            if (string.IsNullOrEmpty(summary))
            {
                return string.Empty;
            }

            GameObject leftArrow = GetColliderGameObjectField(customizeWidget, "leftArrowCollider");
            GameObject rightArrow = GetColliderGameObjectField(customizeWidget, "rightArrowCollider");
            GameObject infoGo = GetColliderGameObjectField(customizeWidget, "infoCollider");

            if (focusedObject == leftArrow || focusedObject == rightArrow)
            {
                string sideKey = focusedObject == leftArrow
                    ? "MOD_CUSTOMIZE_TOGGLE_ACTION_LEFT"
                    : "MOD_CUSTOMIZE_TOGGLE_ACTION_RIGHT";
                return summary + ". " + Plugin.Localization.Get(sideKey);
            }

            if (focusedObject == infoGo)
            {
                return summary + ". " + Plugin.Localization.Get("MOD_CUSTOMIZE_INFO_ACTION");
            }

            return summary;
        }

        private static string GetCustomizeWidgetTitle(object customizeWidget)
        {
            string title = NormalizeForSpeech(ReadLabelText(customizeWidget, "titleLabel"));
            if (!string.IsNullOrEmpty(title))
            {
                return title;
            }

            GameObject widgetGo = GetGameObjectField(customizeWidget, "widget");
            if (widgetGo == null && customizeWidget is Component component)
            {
                widgetGo = component.gameObject;
            }

            title = ExtractTitleLabelText(widgetGo);
            if (!string.IsNullOrEmpty(title))
            {
                return title;
            }

            string typeName = customizeWidget.GetType().Name;
            switch (typeName)
            {
                case "AllowSelfLoveWidget":
                    return Plugin.Localization.Get("MOD_CUSTOMIZE_ALLOW_SELF_LOVE_TITLE");
                case "EndOfTheRoadWidget":
                    return Plugin.Localization.Get("MOD_CUSTOMIZE_END_OF_THE_ROAD_TITLE");
                case "JumpScareWidget":
                    return Plugin.Localization.Get("MOD_CUSTOMIZE_JUMP_SCARE_TITLE");
                case "SpecialHitchhikerWidget":
                    return Plugin.Localization.Get("MOD_CUSTOMIZE_SPECIAL_HITCHHIKER_TITLE");
                default:
                    return string.Empty;
            }
        }

        private static string ExtractTitleLabelText(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return string.Empty;
            }

            Component[] components = gameObject.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "UILabel")
                {
                    continue;
                }

                if (!component.gameObject.name.EndsWith("TitleLabel", StringComparison.Ordinal))
                {
                    continue;
                }

                object candidate = Traverse.Create(component).Property("text").GetValue();
                string normalized = NormalizeForSpeech(candidate as string);
                if (!string.IsNullOrEmpty(normalized))
                {
                    return normalized;
                }
            }

            return string.Empty;
        }

        private static GameObject GetColliderGameObjectField(object instance, string fieldName)
        {
            if (instance == null)
            {
                return null;
            }

            object value = Traverse.Create(instance).Field(fieldName).GetValue();
            if (value is Component component)
            {
                return component.gameObject;
            }

            return null;
        }

        private static string MapKnownButtonName(string name)
        {
            switch (name)
            {
                case "ArtGalleryButton":
                    return Plugin.Localization.Get("MOD_MAIN_GALLERY_SUBMENU_ART");
                case "AchievementsViewerButton":
                    return Plugin.Localization.Get("MOD_MAIN_GALLERY_SUBMENU_ACHIEVEMENTS");
                case "SystemSettingsButton":
                    return Plugin.Localization.Get("MOD_MAIN_SYSTEM_SETTINGS_SUBMENU_BUTTON");
                case "CustomizeButton":
                    return Plugin.Localization.Get("MOD_MAIN_CUSTOMIZE_SUBMENU_BUTTON");
                case "DiscordButton":
                    return Plugin.Localization.Get("MOD_TITLE_DISCORD");
                case "SteamButton":
                    return Plugin.Localization.Get("MOD_TITLE_STEAM");
                case "GalleryButton":
                    return Plugin.Localization.Get("MOD_TITLE_GALLERY");
                case "LocationButton_None":
                    return Plugin.Localization.Get("MOD_PITSTOP_NO_LOCATION");
                default:
                    return string.Empty;
            }
        }

        private static string BuildGalleryOrStickerFocusSpeech(GameObject gameObject)
        {
            string galleryPageSpeech = BuildGalleryPageNavigationSpeech(gameObject);
            if (!string.IsNullOrEmpty(galleryPageSpeech))
            {
                return galleryPageSpeech;
            }

            string stickerPageSpeech = BuildStickerPageNavigationSpeech(gameObject);
            if (!string.IsNullOrEmpty(stickerPageSpeech))
            {
                return stickerPageSpeech;
            }

            string galleryImageSpeech = BuildGalleryImageFocusSpeech(gameObject);
            if (!string.IsNullOrEmpty(galleryImageSpeech))
            {
                return galleryImageSpeech;
            }

            object galleryTab = FindComponentInParents(gameObject, "GalleryTab");
            if (galleryTab != null)
            {
                object galleryUiManager = Traverse.Create(galleryTab).Field("mGalleryUIManager").GetValue();
                object tabType = Traverse.Create(galleryTab).Field("tabType").GetValue();
                bool isCurrent = false;
                if (galleryUiManager != null && tabType != null)
                {
                    object currentResult = Traverse.Create(galleryUiManager).Method("IsGalleryTabCurrent", tabType).GetValue();
                    if (currentResult is bool currentBool)
                    {
                        isCurrent = currentBool;
                    }
                }

                string tabSpeech = GetGalleryTabSpeech(tabType?.ToString());
                if (string.IsNullOrEmpty(tabSpeech))
                {
                    tabSpeech = CleanIdForSpeech(tabType?.ToString());
                }

                return isCurrent && !string.IsNullOrEmpty(tabSpeech)
                    ? tabSpeech + ". Selected."
                    : tabSpeech;
            }

            object fakeGalleryTab = FindComponentInParents(gameObject, "FakeGalleryTab");
            if (fakeGalleryTab != null)
            {
                object selectedValue = Traverse.Create(fakeGalleryTab).Field("isSelected").GetValue();
                bool isSelected = selectedValue is bool selectedBool && selectedBool;
                string tabSpeech = string.Empty;
                switch ((gameObject.name ?? string.Empty).Trim())
                {
                    case "Fake_GalleryTab_Backgrounds":
                        tabSpeech = Plugin.Localization.Get("MOD_GALLERY_TAB_Backgrounds");
                        break;
                    case "Fake_GalleryTab_Sketches":
                        tabSpeech = Plugin.Localization.Get("MOD_GALLERY_TAB_Sketches");
                        break;
                }

                return isSelected && !string.IsNullOrEmpty(tabSpeech)
                    ? tabSpeech + ". Selected."
                    : tabSpeech;
            }

            object stickerTab = FindComponentInParents(gameObject, "StickerTab");
            if (stickerTab != null)
            {
                return string.Empty;
            }

            string path = BuildPath(gameObject.transform);
            if (path.Contains("/Gallery/") || path.Contains("/TopPanel/GalleryTabs/"))
            {
                switch (gameObject.name)
                {
                    case "pageDown":
                    case "NextPageButton":
                        return Plugin.Localization.Get("MOD_GALLERY_NEXT_PAGE");
                    case "pageUp":
                    case "PrevPageButton":
                        return Plugin.Localization.Get("MOD_GALLERY_PREV_PAGE");
                    case "Fake_GalleryTab_Backgrounds":
                        return Plugin.Localization.Get("MOD_GALLERY_TAB_Backgrounds");
                    case "Fake_GalleryTab_Sketches":
                        return Plugin.Localization.Get("MOD_GALLERY_TAB_Sketches");
                }
            }

            if (path.Contains("/Stickers"))
            {
                switch (gameObject.name)
                {
                    case "pageDown":
                    case "NextPageButton":
                        return Plugin.Localization.Get("MOD_STICKERS_NEXT_PAGE");
                    case "pageUp":
                    case "PrevPageButton":
                        return Plugin.Localization.Get("MOD_STICKERS_PREV_PAGE");
                }
            }

            return string.Empty;
        }

        internal static string BuildGalleryImageFocusSpeech(GameObject gameObject)
        {
            object galleryImage = FindComponentInParents(gameObject, "GalleryImage");
            if (galleryImage == null)
            {
                return string.Empty;
            }

            string reflectedSpeech = BuildGalleryImageFocusSpeechFromData(galleryImage);
            if (!string.IsNullOrEmpty(reflectedSpeech))
            {
                return reflectedSpeech;
            }

            List<string> parts = new List<string>();
            AddNormalizedPart(parts, ReadLabelText(galleryImage, "itemNameLabel"));
            AddNormalizedPart(parts, ReadLabelText(galleryImage, "itemDescriptionLabel"));

            object galleryManager = Traverse.Create(galleryImage).Field("mGalleryManager").GetValue();
            if (galleryManager != null)
            {
                object currentLayout = Traverse.Create(galleryManager).Field("mCurrentLayout").GetValue();
                string focusType = Traverse.Create(currentLayout).Field("galleryFocusType").GetValue()?.ToString() ?? string.Empty;
                if (string.Equals(focusType, "CustomFocus", StringComparison.Ordinal))
                {
                    AddNormalizedPart(parts, ReadLabelText(currentLayout, "nameLabel"));
                    AddNormalizedPart(parts, ReadLabelText(currentLayout, "descriptionLabel"));
                }
                else
                {
                    object focusPanel = Traverse.Create(galleryManager).Field("focusPanel").GetValue();
                    AddNormalizedPart(parts, ReadLabelText(focusPanel, "textLabel"));
                }
            }

            return parts.Count == 0 ? string.Empty : string.Join(". ", parts.ToArray());
        }

        internal static void ExportVisibleGalleryImages(object galleryUiManager)
        {
        }

        internal static void ExportVisibleGalleryImagesFromPageNavigation(object pageNavigation)
        {
        }

        internal static object GetGalleryUiManagerFromPageNavigation(object pageNavigation)
        {
            if (pageNavigation == null)
            {
                return null;
            }

            return Traverse.Create(pageNavigation).Field("mGalleryUIManager").GetValue()
                ?? Traverse.Create(pageNavigation).Field("galleryUIManager").GetValue()
                ?? Traverse.Create(pageNavigation).Field("mGalleryUiManager").GetValue();
        }

        internal static List<string> BuildVisibleGalleryPageEntries(object galleryUiManager)
        {
            object currentLayout = galleryUiManager != null ? Traverse.Create(galleryUiManager).Field("mCurrentLayout").GetValue() : null;
            if (IsAchievementsLayout(currentLayout))
            {
                return BuildVisibleAchievementPageEntries(galleryUiManager);
            }

            object imagesList = currentLayout != null ? Traverse.Create(currentLayout).Method("GetImagesList").GetValue() : null;
            System.Collections.IEnumerable images = imagesList as System.Collections.IEnumerable;
            if (images == null)
            {
                return new List<string>();
            }

            List<string> entries = new List<string>();
            int visibleIndex = 0;
            foreach (object galleryImage in images)
            {
                if (galleryImage == null)
                {
                    continue;
                }

                Component component = galleryImage as Component;
                if (component == null || component.gameObject == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string entrySpeech = BuildGalleryImageFocusSpeechFromData(galleryImage);
                if (string.IsNullOrEmpty(entrySpeech))
                {
                    continue;
                }

                visibleIndex++;
                entries.Add("Gallery item " + visibleIndex + ". " + entrySpeech);
            }

            return entries;
        }

        internal static List<string> BuildVisibleLockedGalleryPageEntries(object galleryUiManager)
        {
            object currentLayout = galleryUiManager != null ? Traverse.Create(galleryUiManager).Field("mCurrentLayout").GetValue() : null;
            if (currentLayout == null || IsAchievementsLayout(currentLayout))
            {
                return new List<string>();
            }

            object imagesList = Traverse.Create(currentLayout).Method("GetImagesList").GetValue();
            System.Collections.IEnumerable images = imagesList as System.Collections.IEnumerable;
            if (images == null)
            {
                return new List<string>();
            }

            List<string> entries = new List<string>();
            int visibleIndex = 0;
            foreach (object galleryImage in images)
            {
                if (galleryImage == null)
                {
                    continue;
                }

                Component component = galleryImage as Component;
                if (component == null || component.gameObject == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                object galleryImageData = Traverse.Create(galleryImage).Method("GetCurrentGalleryImageData").GetValue();
                if (galleryImageData == null)
                {
                    continue;
                }

                visibleIndex++;
                if (IsGalleryImageDataAvailable(galleryImageData))
                {
                    continue;
                }

                string lockedSpeech = BuildLockedGalleryPlaceholderSpeech(galleryImage);
                if (string.IsNullOrEmpty(lockedSpeech))
                {
                    continue;
                }

                entries.Add("Gallery item " + visibleIndex + ". " + lockedSpeech);
            }

            return entries;
        }

        internal static List<string> BuildVisibleAchievementPageEntries(object galleryUiManager)
        {
            object currentLayout = galleryUiManager != null ? Traverse.Create(galleryUiManager).Field("mCurrentLayout").GetValue() : null;
            if (!IsAchievementsLayout(currentLayout))
            {
                return new List<string>();
            }

            object imagesList = Traverse.Create(currentLayout).Method("GetImagesList").GetValue();
            System.Collections.IEnumerable images = imagesList as System.Collections.IEnumerable;
            if (images == null)
            {
                return new List<string>();
            }

            List<string> entries = new List<string>();
            int visibleIndex = 0;
            foreach (object galleryImage in images)
            {
                if (galleryImage == null)
                {
                    continue;
                }

                Component component = galleryImage as Component;
                if (component == null || component.gameObject == null || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                object galleryImageData = Traverse.Create(galleryImage).Method("GetCurrentGalleryImageData").GetValue();
                string id = NormalizeForSpeech(galleryImageData != null
                    ? Traverse.Create(galleryImageData).Method("GetId").GetValue() as string
                    : string.Empty);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                string entrySpeech = BuildAchievementGalleryEntrySpeech(galleryImage);
                if (string.IsNullOrEmpty(entrySpeech))
                {
                    continue;
                }

                visibleIndex++;
                entries.Add("Achievement " + visibleIndex + ". " + entrySpeech);
            }

            return entries;
        }

        internal static bool IsAchievementsGallery(object galleryUiManager)
        {
            object currentLayout = galleryUiManager != null ? Traverse.Create(galleryUiManager).Field("mCurrentLayout").GetValue() : null;
            return IsAchievementsLayout(currentLayout);
        }

        internal static bool IsAchievementsGalleryFromPageNavigation(object pageNavigation)
        {
            if (pageNavigation == null)
            {
                return false;
            }

            object galleryUiManager = GetGalleryUiManagerFromPageNavigation(pageNavigation);
            return IsAchievementsGallery(galleryUiManager);
        }

        private static string BuildGalleryImageFocusSpeechFromData(object galleryImage)
        {
            try
            {
                object galleryImageData = Traverse.Create(galleryImage).Method("GetCurrentGalleryImageData").GetValue();
                if (galleryImageData == null)
                {
                    return string.Empty;
                }

                string id = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetId").GetValue() as string);
                string imageType = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetGalleryImageType").GetValue()?.ToString());
                string spriteName = GetGalleryImageSpriteName(galleryImage);
                string spriteSpecificId = BuildGalleryVariantKey(id, spriteName);
                object galleryManager = Traverse.Create(galleryImage).Field("mGalleryManager").GetValue();
                object currentLayout = galleryManager != null ? Traverse.Create(galleryManager).Field("mCurrentLayout").GetValue() : null;
                string focusType = currentLayout != null
                    ? NormalizeForSpeech(Traverse.Create(currentLayout).Field("galleryFocusType").GetValue()?.ToString())
                    : string.Empty;

                if (string.IsNullOrEmpty(id) || galleryManager == null)
                {
                    return string.Empty;
                }

                if (!IsGalleryImageDataAvailable(galleryImageData))
                {
                    string lockedSpeech = BuildLockedGalleryPlaceholderSpeech(galleryImage);
                    return lockedSpeech;
                }

                string manualSpeech = GetManualGallerySpeech(spriteSpecificId, id, spriteName);
                if (string.IsNullOrEmpty(manualSpeech))
                {
                    manualSpeech = GetLayeredManualGalleryDescription(id);
                }

                if (!string.IsNullOrEmpty(manualSpeech))
                {
                    return NormalizeForSpeech(manualSpeech);
                }

                object generalManager = GetSingletonInstance("GeneralManager");
                object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
                object database = generalManager != null ? Traverse.Create(generalManager).Field("database").GetValue() : null;
                if (textManager == null)
                {
                    return string.Empty;
                }

                string title = string.Empty;
                string description = string.Empty;
                if (string.Equals(focusType, "CustomFocus", StringComparison.Ordinal))
                {
                    BuildCustomFocusGalleryText(galleryImage, galleryImageData, textManager, database, imageType, id, out title, out description);
                }
                else
                {
                    title = BuildStandardFocusGalleryText(galleryImageData, textManager, imageType, id);
                }

                string manualTitle = GetManualGalleryTitle(spriteSpecificId, id, spriteName);
                string manualDescription = GetManualGalleryDescription(spriteSpecificId, id, spriteName);
                if (!string.IsNullOrEmpty(manualTitle))
                {
                    title = manualTitle;
                }

                if (!string.IsNullOrEmpty(manualDescription))
                {
                    description = manualDescription;
                    if (!string.Equals(imageType, "Achievement", StringComparison.Ordinal)
                        && !string.Equals(imageType, "EventChecklist", StringComparison.Ordinal))
                    {
                        title = string.Empty;
                    }
                }

                if (string.IsNullOrEmpty(title))
                {
                    title = NormalizeForSpeech(ReadLabelText(galleryImage, "itemNameLabel"));
                }

                if (string.IsNullOrEmpty(description))
                {
                    description = NormalizeForSpeech(ReadLabelText(galleryImage, "itemDescriptionLabel"));
                }

                List<string> parts = new List<string>();
                AddNormalizedPart(parts, title);
                AddNormalizedPart(parts, description);
                return JoinSpeechParts(parts);
            }
            catch
            {

                return string.Empty;
            }
        }

        private static bool IsGalleryImageDataAvailable(object galleryImageData)
        {
            if (galleryImageData == null)
            {
                return false;
            }

            try
            {
                return Traverse.Create(galleryImageData).Method("IsAvailable").GetValue<bool>();
            }
            catch
            {
                return false;
            }
        }

        private static string BuildLockedGalleryPlaceholderSpeech(object galleryImage)
        {
            List<string> parts = new List<string>();
            AddNormalizedPart(parts, ReadLabelText(galleryImage, "itemNameLabel"));
            AddNormalizedPart(parts, ReadLabelText(galleryImage, "itemDescriptionLabel"));

            string placeholder = string.Empty;
            if (Plugin.GalleryDescriptions != null)
            {
                placeholder = Plugin.GalleryDescriptions.GetDescription("placeholder_ui");
            }

            if (string.IsNullOrEmpty(placeholder))
            {
                placeholder = "Small green square with white x_x text.";
            }

            if (parts.Count == 0)
            {
                parts.Add("Locked");
            }

            AddNormalizedPart(parts, placeholder);
            return JoinSpeechParts(parts);
        }

        private static string GetGalleryImageSpriteName(object galleryImage)
        {
            if (galleryImage == null)
            {
                return string.Empty;
            }

            string methodSprite = GetGalleryImageSpriteNameFromMethod(galleryImage, "GetSprite");
            if (!string.IsNullOrEmpty(methodSprite))
            {
                return methodSprite;
            }

            string[] fieldNames = new[]
            {
                "thumbnailSprite",
                "thumbnailSprite2",
                "thumbnailSprite3",
                "thumbnailSprite4",
                "thumbnailSprite5",
                "thumbnailSprite6"
            };

            for (int i = 0; i < fieldNames.Length; i++)
            {
                string fieldSprite = GetGalleryImageSpriteNameFromUiField(galleryImage, fieldNames[i]);
                if (!string.IsNullOrEmpty(fieldSprite))
                {
                    return fieldSprite;
                }
            }

            string[] methodNames = new[] { "GetSprite2", "GetSprite3", "GetSprite4", "GetSprite5", "GetSprite6" };
            for (int i = 0; i < methodNames.Length; i++)
            {
                string extraMethodSprite = GetGalleryImageSpriteNameFromMethod(galleryImage, methodNames[i]);
                if (!string.IsNullOrEmpty(extraMethodSprite))
                {
                    return extraMethodSprite;
                }
            }

            return string.Empty;
        }

        private static string BuildAchievementGalleryEntrySpeech(object galleryImage)
        {
            if (galleryImage == null)
            {
                return string.Empty;
            }

            object galleryImageData = Traverse.Create(galleryImage).Method("GetCurrentGalleryImageData").GetValue();
            if (galleryImageData == null)
            {
                return string.Empty;
            }

            string id = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetId").GetValue() as string);
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            string spriteName = GetGalleryImageSpriteName(galleryImage);
            string spriteSpecificId = BuildGalleryVariantKey(id, spriteName);

            string title = NormalizeForSpeech(ReadLabelText(galleryImage, "itemNameLabel"));
            string description = NormalizeForSpeech(ReadLabelText(galleryImage, "itemDescriptionLabel"));
            string manualTitle = GetManualGalleryTitle(spriteSpecificId, id, spriteName);
            string manualDescription = GetManualGalleryDescription(spriteSpecificId, id, spriteName);
            string manualSpeech = GetManualGallerySpeech(spriteSpecificId, id, spriteName);

            if (!string.IsNullOrEmpty(manualSpeech))
            {
                return manualSpeech;
            }

            if (!string.IsNullOrEmpty(manualTitle))
            {
                title = manualTitle;
            }

            List<string> parts = new List<string>();
            AddNormalizedPart(parts, title);
            AddNormalizedPart(parts, description);
            AddNormalizedPart(parts, manualDescription);
            return JoinSpeechParts(parts);
        }

        private static string GetManualGallerySpeech(string spriteSpecificId, string id, string spriteName)
        {
            if (Plugin.GalleryDescriptions == null)
            {
                return string.Empty;
            }

            string achievementAlias = BuildAchievementSpriteAlias(id, spriteName);
            if (IsGalleryPlaceholderSprite(spriteName))
            {
                return NormalizeForSpeech(Plugin.GalleryDescriptions.GetSpeech(spriteSpecificId, achievementAlias, spriteName));
            }

            return NormalizeForSpeech(Plugin.GalleryDescriptions.GetSpeech(spriteSpecificId, achievementAlias, id, spriteName));
        }

        private static string GetLayeredManualGalleryDescription(string id)
        {
            if (Plugin.GalleryDescriptions == null || string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            AddLayeredManualGalleryPart(parts, id + "_asset");
            for (int i = 1; i <= 8; i++)
            {
                string layerKey = id + "_asset" + i.ToString();
                AddLayeredManualGalleryPart(parts, layerKey);
            }

            return JoinSpeechParts(parts);
        }

        private static void AddLayeredManualGalleryPart(List<string> parts, string layerKey)
        {
            if (parts == null || Plugin.GalleryDescriptions == null || string.IsNullOrEmpty(layerKey))
            {
                return;
            }

            string layerDescription = Plugin.GalleryDescriptions.GetSpeech(layerKey);
            if (string.IsNullOrEmpty(layerDescription))
            {
                layerDescription = Plugin.GalleryDescriptions.GetDescription(layerKey);
            }

            AddNormalizedPart(parts, layerDescription);
        }

        private static string GetManualGalleryTitle(string spriteSpecificId, string id, string spriteName)
        {
            if (Plugin.GalleryDescriptions == null)
            {
                return string.Empty;
            }

            string achievementAlias = BuildAchievementSpriteAlias(id, spriteName);
            if (IsGalleryPlaceholderSprite(spriteName))
            {
                return NormalizeForSpeech(Plugin.GalleryDescriptions.GetTitle(spriteSpecificId, achievementAlias, spriteName));
            }

            return NormalizeForSpeech(Plugin.GalleryDescriptions.GetTitle(spriteSpecificId, achievementAlias, id, spriteName));
        }

        private static string GetManualGalleryDescription(string spriteSpecificId, string id, string spriteName)
        {
            if (Plugin.GalleryDescriptions == null)
            {
                return string.Empty;
            }

            string achievementAlias = BuildAchievementSpriteAlias(id, spriteName);
            if (IsGalleryPlaceholderSprite(spriteName))
            {
                return NormalizeForSpeech(Plugin.GalleryDescriptions.GetDescription(spriteSpecificId, achievementAlias, spriteName));
            }

            return NormalizeForSpeech(Plugin.GalleryDescriptions.GetDescription(spriteSpecificId, achievementAlias, id, spriteName));
        }

        private static string BuildAchievementSpriteAlias(string id, string spriteName)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(spriteName))
            {
                return string.Empty;
            }

            string achievementSprite = id + "_achievement";
            string uiSprite = id + "_ui2";
            if (string.Equals(spriteName, achievementSprite, StringComparison.OrdinalIgnoreCase)
                || string.Equals(spriteName, uiSprite, StringComparison.OrdinalIgnoreCase))
            {
                return id + "|achievement_" + id;
            }

            return string.Empty;
        }

        private static bool IsGalleryPlaceholderSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return false;
            }

            return string.Equals(spriteName, "placeholder_ui", StringComparison.OrdinalIgnoreCase)
                || string.Equals(spriteName, "achievement_hidden", StringComparison.OrdinalIgnoreCase)
                || string.Equals(spriteName, "background_transparentpixel", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAchievementsLayout(object currentLayout)
        {
            return currentLayout != null
                && string.Equals(
                    NormalizeForSpeech(Traverse.Create(currentLayout).Field("artLayoutType").GetValue()?.ToString()),
                    "Layout_Achievements",
                    StringComparison.Ordinal);
        }

        private static string GetGalleryImageSpriteNameFromMethod(object galleryImage, string methodName)
        {
            try
            {
                object spriteObject = Traverse.Create(galleryImage).Method(methodName).GetValue();
                Sprite sprite = spriteObject as Sprite;
                return NormalizeForSpeech(sprite != null ? sprite.name : string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetGalleryImageSpriteNameFromUiField(object galleryImage, string fieldName)
        {
            object spriteField = Traverse.Create(galleryImage).Field(fieldName).GetValue();
            if (spriteField == null)
            {
                return string.Empty;
            }

            object spriteObject = Traverse.Create(spriteField).Property("sprite2D").GetValue();
            Sprite sprite = spriteObject as Sprite;
            return NormalizeForSpeech(sprite != null ? sprite.name : string.Empty);
        }

        internal static string JoinSpeechParts(IEnumerable<string> parts)
        {
            if (parts == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (string part in parts)
            {
                string cleaned = NormalizeForSpeech(part);
                if (string.IsNullOrEmpty(cleaned))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    char last = builder[builder.Length - 1];
                    if (!EndsWithSentencePunctuation(last))
                    {
                        builder.Append('.');
                    }

                    builder.Append(' ');
                }

                builder.Append(cleaned);
            }

            return builder.ToString().Trim();
        }

        private static bool EndsWithSentencePunctuation(char value)
        {
            return value == '.'
                || value == '!'
                || value == '?'
                || value == ':'
                || value == ';';
        }

        private static string BuildGalleryVariantKey(string id, string spriteName)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(spriteName))
            {
                return string.Empty;
            }

            return id + "|" + spriteName;
        }

        private static string BuildGalleryDataTitle(object galleryImageData, string imageType)
        {
            if (galleryImageData == null)
            {
                return string.Empty;
            }

            object generalManager = GetSingletonInstance("GeneralManager");
            object textManager = generalManager != null ? Traverse.Create(generalManager).Field("textManager").GetValue() : null;
            if (textManager == null)
            {
                return string.Empty;
            }

            string id = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetId").GetValue() as string);
            if (string.Equals(imageType, "Character", StringComparison.Ordinal))
            {
                string characterName = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetCharacterName").GetValue() as string);
                if (!string.IsNullOrEmpty(characterName))
                {
                    return NormalizeForSpeech(Traverse.Create(textManager).Method("GetGalleryCharacterImageText", characterName).GetValue() as string);
                }
            }

            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            return NormalizeForSpeech(Traverse.Create(textManager).Method("GetGalleryImageText", id).GetValue() as string);
        }

        private static void BuildCustomFocusGalleryText(object galleryImage, object galleryImageData, object textManager, object database, string imageType, string id, out string title, out string description)
        {
            title = string.Empty;
            description = string.Empty;

            switch (imageType)
            {
                case "SpecialStuff":
                    InvokeFocusText(textManager, "GetFocusSpecialStuffText", id, out title, out description);
                    return;
                case "Item":
                    BuildItemFocusGalleryText(galleryImage, textManager, database, out title, out description);
                    return;
                case "Location":
                    BuildLocationFocusGalleryText(galleryImage, textManager, database, out title, out description);
                    return;
                case "CharacterIcon":
                    string path = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetPath").GetValue() as string);
                    path = path.Replace("icon_npc_", string.Empty);
                    path = path.Replace("icon_pc_", string.Empty);
                    title = NormalizeForSpeech(Traverse.Create(textManager).Method("GetGalleryCharacterImageText", path).GetValue() as string);
                    return;
                default:
                    InvokeFocusText(textManager, "GetFocusText", id, out title, out description);
                    return;
            }
        }

        private static void BuildItemFocusGalleryText(object galleryImage, object textManager, object database, out string title, out string description)
        {
            title = string.Empty;
            description = string.Empty;
            object sprite = Traverse.Create(galleryImage).Method("GetSprite").GetValue();
            string spriteName = NormalizeForSpeech(Traverse.Create(sprite).Property("name").GetValue() as string);
            if (database == null || string.IsNullOrEmpty(spriteName))
            {
                return;
            }

            object items = Traverse.Create(database).Method("GetItems").GetValue();
            object itemProfile = items != null ? Traverse.Create(items).Method("GetItemFromSpriteName", spriteName).GetValue() : null;
            string profileId = NormalizeForSpeech(Traverse.Create(itemProfile).Field("id").GetValue() as string);
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            InvokeFocusText(textManager, "GetFocusItemText", profileId, out title, out description);
        }

        private static void BuildLocationFocusGalleryText(object galleryImage, object textManager, object database, out string title, out string description)
        {
            title = string.Empty;
            description = string.Empty;
            object sprite = Traverse.Create(galleryImage).Method("GetSprite").GetValue();
            string spriteName = NormalizeForSpeech(Traverse.Create(sprite).Property("name").GetValue() as string);
            if (database == null || string.IsNullOrEmpty(spriteName))
            {
                return;
            }

            object roadmapLocations = Traverse.Create(database).Method("GetRoadmapLocations").GetValue();
            object locationProfile = roadmapLocations != null ? Traverse.Create(roadmapLocations).Method("GetLocationFromSprite", spriteName).GetValue() : null;
            object rawId = locationProfile != null ? Traverse.Create(locationProfile).Field("id").GetValue() : null;
            string profileId = NormalizeForSpeech(rawId?.ToString());
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            InvokeFocusText(textManager, "GetFocusLocationText", profileId, out title, out description);
        }

        private static string BuildStandardFocusGalleryText(object galleryImageData, object textManager, string imageType, string id)
        {
            if (string.Equals(imageType, "Character", StringComparison.Ordinal))
            {
                string characterName = NormalizeForSpeech(Traverse.Create(galleryImageData).Method("GetCharacterName").GetValue() as string);
                if (!string.IsNullOrEmpty(characterName))
                {
                    return NormalizeForSpeech(Traverse.Create(textManager).Method("GetGalleryCharacterImageText", characterName).GetValue() as string);
                }
            }

            return NormalizeForSpeech(Traverse.Create(textManager).Method("GetGalleryImageText", id).GetValue() as string);
        }

        private static void InvokeFocusText(object textManager, string methodName, string id, out string title, out string description)
        {
            object[] args = { id, null, null };
            Traverse.Create(textManager).Method(methodName, args).GetValue();
            title = NormalizeForSpeech(args[1] as string);
            description = NormalizeForSpeech(args[2] as string);
        }

        private static object GetSingletonInstance(string typeName)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type == null)
            {
                return null;
            }

            object instance = Traverse.Create(type).Property("Instance").GetValue();
            if (instance != null)
            {
                return instance;
            }

            return Traverse.Create(type).Field("Instance").GetValue();
        }

        private static string BuildGalleryPageNavigationSpeech(GameObject gameObject)
        {
            object pageNavigation = FindComponentInParents(gameObject, "GalleryPageNavigation");
            if (pageNavigation == null)
            {
                return string.Empty;
            }

            GameObject nextButton = GetGameObjectField(pageNavigation, "pageDownButton");
            if (gameObject == nextButton)
            {
                return Plugin.Localization.Get("MOD_GALLERY_NEXT_PAGE");
            }

            GameObject previousButton = GetGameObjectField(pageNavigation, "pageUpButton");
            if (gameObject == previousButton)
            {
                return Plugin.Localization.Get("MOD_GALLERY_PREV_PAGE");
            }

            return string.Empty;
        }

        private static string BuildStickerPageNavigationSpeech(GameObject gameObject)
        {
            object pageNavigation = FindComponentInParents(gameObject, "StickerPageNavigation");
            if (pageNavigation == null)
            {
                return string.Empty;
            }

            GameObject nextButton = GetGameObjectField(pageNavigation, "pageDownButton");
            if (gameObject == nextButton)
            {
                return Plugin.Localization.Get("MOD_STICKERS_NEXT_PAGE");
            }

            GameObject previousButton = GetGameObjectField(pageNavigation, "pageUpButton");
            if (gameObject == previousButton)
            {
                return Plugin.Localization.Get("MOD_STICKERS_PREV_PAGE");
            }

            return string.Empty;
        }

        private static string GetGalleryTabSpeech(string tabType)
        {
            if (string.IsNullOrEmpty(tabType))
            {
                return string.Empty;
            }

            return Plugin.Localization.Get("MOD_GALLERY_TAB_" + tabType);
        }

        private static string GetStickerTabSpeech(string tabType)
        {
            if (string.IsNullOrEmpty(tabType))
            {
                return string.Empty;
            }

            return Plugin.Localization.Get("MOD_STICKERS_TAB_" + tabType);
        }

        private static string BuildInfoManualFocusSpeech(GameObject gameObject)
        {
            string path = BuildPath(gameObject.transform);
            if (!path.Contains("/InfoManual/"))
            {
                return string.Empty;
            }

            switch (gameObject.name)
            {
                case "NextPageButton":
                    return Plugin.Localization.Get("MOD_INFO_MANUAL_NEXT_PAGE");
                case "PrevPageButton":
                    return Plugin.Localization.Get("MOD_INFO_MANUAL_PREV_PAGE");
                default:
                    return string.Empty;
            }
        }

        private static string BuildFullCreditsFocusSpeech(GameObject gameObject)
        {
            string path = BuildPath(gameObject.transform);
            if (!path.Contains("/FullCredits/"))
            {
                return string.Empty;
            }

            switch (gameObject.name)
            {
                case "NextPageButton":
                    return Plugin.Localization.Get("MOD_FULL_CREDITS_NEXT_PAGE");
                case "PrevPageButton":
                    return Plugin.Localization.Get("MOD_FULL_CREDITS_PREV_PAGE");
                default:
                    return string.Empty;
            }
        }

        private static string GetSettingsArrowAction(string title, bool isIncrease)
        {
            if (string.Equals(title, "Display", StringComparison.OrdinalIgnoreCase))
            {
                return isIncrease ? "Next monitor" : "Previous monitor";
            }

            if (string.Equals(title, "Window mode", StringComparison.OrdinalIgnoreCase))
            {
                return isIncrease ? "Next mode" : "Previous mode";
            }

            if (string.Equals(title, "Language", StringComparison.OrdinalIgnoreCase))
            {
                return isIncrease ? "Next language" : "Previous language";
            }

            return isIncrease ? "Increase" : "Decrease";
        }

        private static int ReadIntField(object instance, string fieldName)
        {
            object value = Traverse.Create(instance).Field(fieldName).GetValue();
            return value is int number ? number : -1;
        }

        private static string StripBracketTags(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length);
            bool inTag = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '[')
                {
                    inTag = true;
                    continue;
                }

                if (inTag)
                {
                    if (c == ']')
                    {
                        inTag = false;
                    }

                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
