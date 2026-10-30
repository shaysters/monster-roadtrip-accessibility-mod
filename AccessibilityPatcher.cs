using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace RoadtripAccessibilityProbe
{
    internal static class AccessibilityPatcher
    {
        private static readonly Dictionary<int, bool> SettingsEntryRedirected = new Dictionary<int, bool>();
        private static readonly Dictionary<string, string> CustomizeNotesByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> SpokenCustomizeNotes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static void Install(Harmony harmony)
        {
            PatchMethod(harmony, "UICamera", "set_controllerNavigationObject", nameof(OnNavigationObjectChanged), typeof(GameObject));
            PatchMethod(harmony, "SimpleTextBox", "Show", nameof(OnSimpleTextShow), typeof(string));
            PatchNamedMethod(harmony, "UIButton", "OnClick", nameof(OnUIButtonClick));
            PatchNamedMethod(harmony, "ButtonHandler", "OnClick", nameof(OnButtonHandlerClick));
            PatchNamedMethod(harmony, "TitleSettings", "Update", nameof(OnTitleSettingsUpdate));
            PatchNamedMethod(harmony, "PauseMenu", "ShowConfirmPanel", nameof(OnPauseMenuShowConfirmPanel));
            PatchNamedMethod(harmony, "PauseMenu", "UiShowConfirmPanel", nameof(OnPauseMenuShowConfirmPanel));
            PatchNamedMethod(harmony, "TitleUIManager", "UiGoToSettingsScreen", nameof(OnUiGoToSettingsScreen));
            PatchNamedMethod(harmony, "TitleUIManager", "UiGoToCustomizeScreen", nameof(OnUiGoToCustomizeScreen));
            PatchNamedMethod(harmony, "TitleUIManager", "UiFadeOutAndLoadGalleryScene", nameof(OnUiGoToGalleryScene));
            PatchNamedMethod(harmony, "TitleUIManager", "UiFadeOutAndLoadStickersScene", nameof(OnUiGoToStickersScene));
            PatchNamedMethod(harmony, "GalleryUIManager", "ShowTab", nameof(OnGalleryShowTab));
            PatchNamedMethod(harmony, "GalleryUIManager", "ShowFocusImage", nameof(OnGalleryShowFocusImage));
            PatchMethod(harmony, "GalleryUIManager", "SetCurrentImages", nameof(OnGallerySetCurrentImages), typeof(bool));
            PatchNamedMethod(harmony, "GalleryPageNavigation", "AdvancePage", nameof(OnGalleryPageChanged));
            PatchNamedMethod(harmony, "GalleryPageNavigation", "GoBackPage", nameof(OnGalleryPageChanged));
            PatchNamedMethod(harmony, "GalleryStats", "SetDataToLabels", nameof(OnGalleryStatsSetDataToLabels));
            PatchNamedMethod(harmony, "StickerUIManager", "GoToStickersMainView", nameof(OnStickerMainView));
            PatchNamedMethod(harmony, "StickerUIManager", "GoToBackgroundSelection", nameof(OnStickerBackgroundSelectionView));
            PatchNamedMethod(harmony, "StickerUIManager", "GoToViewMyDesigns", nameof(OnStickerMyDesignsView));
            PatchNamedMethod(harmony, "StickerUIManager", "GoToViewStickerComposition", nameof(OnStickerCompositionView));
            PatchNamedMethod(harmony, "StickerTabsWidget", "ShowTab", nameof(OnStickerTabChanged));
            PatchNamedMethod(harmony, "StickerPageNavigation", "AdvancePage", nameof(OnStickerPageChanged));
            PatchNamedMethod(harmony, "StickerPageNavigation", "GoBackPage", nameof(OnStickerPageChanged));
            PatchMethod(harmony, "TitleCustomizeRoadtrip", "SetNoteInfo", nameof(OnCustomizeNoteInfo), typeof(string));
            PatchNamedMethod(harmony, "SimpleInfoManualView", "ShowInitialPage", nameof(OnInfoManualShowInitialPage));
            PatchMethod(harmony, "SimpleInfoManualView", "ShowSlide", nameof(OnInfoManualShowSlide), typeof(int));
            PatchNamedMethod(harmony, "FullCreditsView", "ShowInitialPage", nameof(OnFullCreditsShowInitialPage));
            PatchNamedMethod(harmony, "FullCreditsView", "UpdateImage", nameof(OnFullCreditsUpdateImage));
            PatchNamedMethod(harmony, "SimpleOkPopup", "SetData", nameof(OnSimpleOkPopupSetData));
            PatchNamedMethod(harmony, "SimpleYesNoPopup", "SetData", nameof(OnSimpleYesNoPopupSetData));
            PatchNamedMethod(harmony, "MasterPopup", "ShowInfoPopup", nameof(OnMasterPopupShow));
            PatchNamedMethod(harmony, "MasterPopup", "ShowOkPopup", nameof(OnMasterPopupShow));
            PatchNamedMethod(harmony, "MasterPopup", "ShowYesNoPopup", nameof(OnMasterPopupShow));
            PatchNamedMethod(harmony, "MasterPopup", "Show3ButtonPopup", nameof(OnMasterPopupShow));
            PatchNamedMethodPrefix(harmony, "MainGameEventView", "PlayNormalScene", nameof(OnMainGamePlayNormalScene));
            PatchNamedMethodPrefix(harmony, "PrologueEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "CreditsEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "DemoIntroEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "DemoEndEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "GachaShopEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "MetaShopEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "TestEventsEventView", "ShowNormalScene", nameof(OnEventViewShowNormalScene));
            PatchNamedMethodPrefix(harmony, "TestEventsEventView", "PlayNormalScene", nameof(OnMainGamePlayNormalScene));
            PatchNamedMethod(harmony, "ChallengeView", "ShowChallengeDialogue", nameof(OnChallengeDialogue));
            PatchNamedMethod(harmony, "MainGameEventView", "ActivateStatFeedbackFx", nameof(OnMainGameStatFeedback));
            PatchNamedMethod(harmony, "MainGameEventView", "ActivateStatCheckFeedbackFx", nameof(OnMainGameStatCheckFeedback));
            PatchNamedMethod(harmony, "AnnouncementsOverlay", "ShowAnnouncement", nameof(OnAnnouncementShow));
            PatchNamedMethod(harmony, "AnnouncementsOverlay", "ShowPitstopAnnouncement", nameof(OnAnnouncementShow));
            PatchNamedMethod(harmony, "AnnouncementsOverlay", "ShowSideAnnouncement", nameof(OnAnnouncementShow));
            PatchNamedMethod(harmony, "AnnouncementsOverlay", "ShowGameOverAnnouncement", nameof(OnAnnouncementShow));
            PatchNamedMethod(harmony, "AnnouncementsOverlay", "StartMaskAnnouncement", nameof(OnAnnouncementShow));
            PatchNamedMethod(harmony, "TooltipTriggerWarnings", "PrepareTooltip", nameof(OnTriggerWarningsPrepareTooltip));
            PatchNamedMethod(harmony, "MissionsOverlay", "OnIncreaseMissionPointsUI", nameof(OnMissionPointsChanged));
            PatchNamedMethod(harmony, "MissionsOverlay", "OnDecreaseMissionPointsUI", nameof(OnMissionPointsChanged));
            PatchNamedMethod(harmony, "MissionsOverlay", "OnSetMissionPointsUI", nameof(OnMissionPointsChanged));
            PatchNamedMethod(harmony, "MissionsOverlay", "OpenDestinationsTab", nameof(OnMissionsOverlayDestinationsTab));
            PatchNamedMethod(harmony, "MissionsOverlay", "HoverDestinationsTab", nameof(OnMissionsOverlayDestinationsTab));
            PatchNamedMethod(harmony, "MissionsOverlay", "OpenMissionsTab", nameof(OnMissionsOverlayMissionsTab));
            PatchNamedMethod(harmony, "PlayerTurnOverlay", "SetPlayer", nameof(OnTurnOverlaySetPlayer));
            PatchNamedMethod(harmony, "PlayerTurnSimpleOverlay", "SetPlayer", nameof(OnTurnOverlaySetPlayer));
            PatchNamedMethod(harmony, "PrologueUIManager", "ShowView_PlayerNumbers", nameof(OnPrologueUIManagerViewShown));
            PatchNamedMethod(harmony, "PrologueUIManager", "ShowView_PlayerSelect", nameof(OnPrologueUIManagerViewShown));
            PatchNamedMethod(harmony, "PrologueUIManager", "ShowView_ControllerMode", nameof(OnPrologueUIManagerViewShown));
            PatchNamedMethod(harmony, "PrologueUIManager", "ShowView_DifficultyMode", nameof(OnPrologueUIManagerViewShown));
            PatchNamedMethod(harmony, "PrologueUIManager", "ShowView_RunMode", nameof(OnPrologueUIManagerViewShown));
            PatchNamedMethod(harmony, "PrologueUIManager", "ShowView_MusicVariation", nameof(OnPrologueUIManagerViewShown));
            PatchNamedMethod(harmony, "PlayersNumberView", "Show", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "PlayersSelectView", "CoStartChoosePlayers", nameof(OnStartChoosePlayers));
            PatchNamedMethod(harmony, "PlayersSelectView", "PreparePlayerSelection", nameof(OnPreparePlayerSelection));
            PatchNamedMethod(harmony, "PrologueCharacterSelectWidget", "UiHoverOverShowPackButton", nameof(OnCharacterPackHover));
            PatchNamedMethod(harmony, "ControlModeView", "Show", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "DifficultyModeView", "Show", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "RunModeView", "Show", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "MusicVariationView", "Show", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "PrologueMissionSelectView", "ShowActions", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "PrologueMissionSelectView", "ShowPlayerTurnImmediate", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "PlayerCustomizeWidget", "OpenCustomizeName", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "PlayerCustomizeWidget", "OpenCustomizePronoun", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "PlayerCustomizeWidget", "OpenWaitForInputName", nameof(OnPrologueSetupPrompt));
            PatchNamedMethod(harmony, "RoadmapView", "ShowGlobalTurnImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "RoadmapView", "ShowPlayerTurnImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "RoadLocationActionsView", "SetLocationActions", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "RoadLocationActionsView", "PlayGlobalTurnRoadLocationActionsIntroImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "LocationActionButton", "SetData", nameof(OnLocationActionButtonSetData));
            PatchNamedMethod(harmony, "ChooseInfoBoardView", "ShowView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "InfoBoardView", "ShowView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PitstopView", "ShowGlobalTurnChooseImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PitstopView", "ShowPlayerTurnChooseImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "CampView", "ShowGlobalTurnImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "CampView", "ShowPlayerTurnImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "CampfireView", "ShowGlobalTurnImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "CampfireView", "ShowPlayerTurnImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "EndingChooseNpcView", "ShowChooseScene", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "LoveLogComplimentView", "ShowStatChoice", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "LoveLogComplimentView", "SetData", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "RoadGameOverView", "ShowGlobalTurn", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "RoadGameOverView", "ShowFakeGameOver", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "ResultsView", "ShowView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "DestinationMissionResultsView", "ShowView", nameof(OnDestinationMissionResultsShowView));
            PatchNamedMethod(harmony, "DestinationProgressView", "ShowView", nameof(OnDestinationProgressShowView));
            PatchNamedMethod(harmony, "ShopView", "ShowStage_ShopkeeperGreeting", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "ShopView", "SetShopkeeperDialogue_ItemDescription", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "ShopView", "ShowStage_PurchasedItem", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "BarterShopView", "ShowView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "BarterShopView", "ShowSelectPlayerItem", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "BarterShopView", "ShowSelectShopItem", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "BarterShopView", "ConfirmShopItem", nameof(OnBarterShopConfirmShopItem));
            PatchNamedMethod(harmony, "CarView", "ShowView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "CarView", "PlayCarEffects", nameof(OnCarViewPlayCarEffects));
            PatchNamedMethod(harmony, "GachaShopView", "ShowIntroView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "GachaShopView", "ShowPurchaseView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "GachaShopView", "ShowCantPurchaseView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "MetaShopItemsView", "UpdateView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "MetaShopItemsView", "SetNoItemsView", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PitstopPassengerView", "ShowViewAsSelect", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PitstopPassengerView", "ShowViewAsManage", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PitstopPassengerView", "ShowTalkToPassengerImmediate", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PassengerSelectWidget", "Show", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PassengerTalkWidget", "Show", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "HitchhikerManageWidget", "Show", nameof(OnMainGameViewPrompt));
            PatchNamedMethod(harmony, "PassengerFeedbackWidget", "PlayPassengerFxAndApplyStatEffect", nameof(OnPassengerFxAndApplyStatEffect));
            PatchNamedMethod(harmony, "CreditsSceneController", "StartCredits", nameof(OnCreditsStart));
            PatchNamedMethod(harmony, "CreditsEndOfTheRoadSceneController", "StartCredits", nameof(OnCreditsStart));
            PatchNamedMethod(harmony, "CreditsUIManager", "OnFinishEpilogueEvent", nameof(OnCreditsEpilogueFinished));
            PatchNamedMethod(harmony, "AllowSelfLoveWidget", "UiToggleAllowSelfLove", nameof(OnCustomizeWidgetChanged));
            PatchNamedMethod(harmony, "EndOfTheRoadWidget", "UiToggleEndOfTheRoad", nameof(OnCustomizeWidgetChanged));
            PatchNamedMethod(harmony, "EndOfTheRoadWidget", "UiEnableEndOfTheRoad", nameof(OnCustomizeWidgetChanged));
            PatchNamedMethod(harmony, "EndOfTheRoadWidget", "UiDisableEndOfTheRoad", nameof(OnCustomizeWidgetChanged));
            PatchNamedMethod(harmony, "JumpScareWidget", "UiToggleJumpScare", nameof(OnCustomizeWidgetChanged));
            PatchNamedMethod(harmony, "SpecialHitchhikerWidget", "UiToggleSpecialHitchHiker", nameof(OnCustomizeWidgetChanged));
            PatchNamedMethod(harmony, "TriggerWarningWidget", "UiToggleTriggerWarning", nameof(OnCustomizeWidgetChanged));
        }

        private static void PatchNamedMethod(Harmony harmony, string typeName, string methodName, string postfixName)
        {
            PatchNamedMethodCore(harmony, typeName, methodName, postfixName, usePrefix: false);
        }

        private static void PatchNamedMethodPrefix(Harmony harmony, string typeName, string methodName, string prefixName)
        {
            PatchNamedMethodCore(harmony, typeName, methodName, prefixName, usePrefix: true);
        }

        private static void PatchNamedMethodCore(Harmony harmony, string typeName, string methodName, string patchName, bool usePrefix)
        {
            Type type = ResolveType(typeName);
            if (type == null)
            {
                return;
            }

            List<MethodInfo> targets = new List<MethodInfo>();
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName)
                {
                    targets.Add(methods[i]);
                }
            }

            if (targets.Count == 0)
            {
                return;
            }

            MethodInfo patch = AccessTools.Method(typeof(AccessibilityPatcher), patchName);
            for (int i = 0; i < targets.Count; i++)
            {
                if (usePrefix)
                {
                    harmony.Patch(targets[i], prefix: new HarmonyMethod(patch));
                }
                else
                {
                    harmony.Patch(targets[i], postfix: new HarmonyMethod(patch));
                }
            }
        }

        private static void PatchMethod(Harmony harmony, string typeName, string methodName, string postfixName, params Type[] argumentTypes)
        {
            Type type = ResolveType(typeName);
            if (type == null)
            {
                return;
            }

            MethodInfo target = AccessTools.Method(type, methodName, argumentTypes);
            if (target == null)
            {
                return;
            }

            MethodInfo postfix = AccessTools.Method(typeof(AccessibilityPatcher), postfixName);
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static Type ResolveType(string typeName)
        {
            Type type = AccessTools.TypeByName(typeName);
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    if (types[j] != null && types[j].Name == typeName)
                    {
                        return types[j];
                    }
                }
            }

            return null;
        }

        public static void OnNavigationObjectChanged(GameObject value)
        {
            if (!Plugin.IsInteractionAllowed())
            {
                return;
            }

            if (value == null)
            {
                return;
            }

            string text = UiTextUtility.BuildFocusSpeech(value);
            text = AppendCustomizeNoteIfNeeded(value, text);

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (ShouldSuppressInitialSceneChromeFocus(value))
            {
                return;
            }

            if (ShouldSuppressFocusForRecentSpeech(value))
            {
                return;
            }

            bool interrupt = !UiTextUtility.ShouldQueueFocusBehindGallerySpeech(value);
            Plugin.Speech.Speak(text, interrupt: interrupt, sourceTag: "focus:" + UiTextUtility.BuildPath(value.transform));
        }

        public static void LogFocusedObjectDiagnostics(GameObject value, string sourceTag)
        {
        }

        public static void OnSimpleTextShow(object __instance, string aText)
        {
            Component component = __instance as Component;
            if (component != null && IsEventViewTextBox(__instance))
            {
                return;
            }

            bool isEventTextBox = IsEventViewTextBox(__instance);
            if (isEventTextBox && Plugin.Speech.WasRecentSource("dialog:", 0.75f))
            {
                return;
            }

            string labelText = ReadLabelText(__instance, "eventTextLabel");
            string finalText = string.IsNullOrEmpty(labelText) ? aText : labelText;
            string sourceTag = "dialog:textbox";
            if (component != null)
            {
                sourceTag += ":" + UiTextUtility.BuildPath(component.transform);
            }

            Plugin.Speech.Speak(finalText, interrupt: true, sourceTag: sourceTag);
        }

        public static void OnUIButtonClick(MonoBehaviour __instance)
        {
        }

        public static void OnButtonHandlerClick(MonoBehaviour __instance)
        {
        }

        public static void OnPauseMenuShowConfirmPanel(object __instance)
        {
            if (Plugin.Speech != null && Plugin.Speech.WasRecentSource("pause:confirm", 0.75f))
            {
                return;
            }

            string speech = UiTextUtility.BuildPauseMenuConfirmSpeech(__instance, null);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "pause:confirm");
        }

        public static void OnTitleSettingsUpdate(object __instance)
        {
            if (!Plugin.IsInteractionAllowed())
            {
                return;
            }

            if (__instance == null)
            {
                return;
            }

            int instanceId = __instance.GetHashCode();
            GameObject settingsContainer = UiTextUtility.GetGameObjectField(__instance, "settingsContainer");
            if (settingsContainer == null || !settingsContainer.activeInHierarchy)
            {
                SettingsEntryRedirected.Remove(instanceId);
                return;
            }

            if (Traverse.Create(__instance).Field("mTweeningWidgets").GetValue<bool>())
            {
                return;
            }

            if (SettingsEntryRedirected.ContainsKey(instanceId))
            {
                return;
            }

            GameObject current = UiTextUtility.GetCurrentNavigationObject();
            GameObject backButton = UiTextUtility.GetGameObjectField(__instance, "backButton");
            if (current == null || backButton == null || current != backButton)
            {
                return;
            }

            GameObject firstArrow = UiTextUtility.FindFirstActiveSettingsArrow(__instance);
            if (firstArrow == null)
            {
                return;
            }

            object navigationManager = Traverse.Create(__instance).Field("mCoreNavigationManager").GetValue();
            if (navigationManager == null)
            {
                return;
            }

            MethodInfo setDefault = AccessTools.Method(navigationManager.GetType(), "SetDefault", new[] { typeof(GameObject) });
            MethodInfo setHovered = AccessTools.Method(navigationManager.GetType(), "SetHovered", new[] { typeof(GameObject) });
            if (setDefault == null || setHovered == null)
            {
                return;
            }

            setDefault.Invoke(navigationManager, new object[] { firstArrow });
            setHovered.Invoke(navigationManager, new object[] { firstArrow });
            SettingsEntryRedirected[instanceId] = true;
        }

        public static void OnSimpleOkPopupSetData(object __instance)
        {
            string title = ReadLabelText(__instance, "popupOptionalTitleLabel");
            string message = ReadLabelText(__instance, "popupMessageLabel");
            string button = ReadLabelText(__instance, "popupButtonLabel");

            List<string> parts = new List<string>();
            AddIfNotEmpty(parts, title);
            AddIfNotEmpty(parts, message);
            if (parts.Count == 0)
            {
                return;
            }

            AddIfNotEmpty(parts, UiTextUtility.NormalizeButtonLabelForSpeech(button));
            Plugin.Speech.Speak(UiTextUtility.JoinSpeechParts(parts), interrupt: true, sourceTag: "popup:simpleok");
        }

        public static void OnSimpleYesNoPopupSetData(object __instance)
        {
            string message = ReadLabelText(__instance, "popupMessageLabel");
            if (string.IsNullOrEmpty(UiTextUtility.NormalizeForSpeech(message)))
            {
                return;
            }

            string yes = ReadLabelText(__instance, "popupButtonYesLabel");
            string no = ReadLabelText(__instance, "popupButtonNoLabel");

            List<string> parts = new List<string>();
            AddIfNotEmpty(parts, message);
            AddIfNotEmpty(parts, yes);
            AddIfNotEmpty(parts, no);

            Plugin.Speech.Speak(string.Join(". ", parts.ToArray()), interrupt: true, sourceTag: "popup:simpleyesno");
        }

        public static void OnInfoManualShowInitialPage(object __instance)
        {
            string text;
            if (Plugin.ManualPages != null && Plugin.ManualPages.TryBuildCoverSpeech(out text))
            {
                Plugin.Speech.Speak(text, interrupt: true, sourceTag: "infomanual:cover");
                return;
            }

            text = Plugin.Localization.Get("MOD_INFO_MANUAL_COVER") + ". " + Plugin.Localization.Get("MOD_INFO_MANUAL_LIMITATION");
            Plugin.Speech.Speak(text, interrupt: true, sourceTag: "infomanual:cover");
        }

        public static void OnInfoManualShowSlide(object __instance, int aSlideIndex)
        {
            int totalPages = Traverse.Create(__instance).Field("numOfSlides").GetValue<int>();
            string pageText = Plugin.Localization.Get(
                "MOD_INFO_MANUAL_PAGE",
                "page", (aSlideIndex + 1).ToString(),
                "total", totalPages.ToString());
            string manualText;
            if (Plugin.ManualPages != null && Plugin.ManualPages.TryBuildPageSpeech(aSlideIndex, out manualText))
            {
                string combinedText = UiTextUtility.JoinSpeechParts(new[] { pageText, manualText });
                Plugin.Speech.Speak(combinedText, interrupt: true, sourceTag: "infomanual:page:" + aSlideIndex);
                return;
            }

            string text = pageText;
            Plugin.Speech.Speak(text, interrupt: true, sourceTag: "infomanual:page:" + aSlideIndex);
        }

        public static void OnFullCreditsShowInitialPage(object __instance)
        {
            SpeakFullCreditsPage(__instance, "fullcredits:initial");
        }

        public static void OnFullCreditsUpdateImage(object __instance)
        {
            SpeakFullCreditsPage(__instance, "fullcredits:update");
        }

        public static void OnMasterPopupShow(object __instance, MethodBase __originalMethod, object[] __args)
        {
            string message = __args != null && __args.Length > 0 ? __args[0] as string : string.Empty;
            string title = GetMasterPopupTitleFromArgs(__originalMethod.Name, __args);
            string[] buttons = GetMasterPopupButtonTextsFromArgs(__originalMethod.Name, __args);
            List<string> parts = new List<string>();
            AddIfNotEmpty(parts, title);
            AddIfNotEmpty(parts, message);

            if (parts.Count == 0)
            {
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                AddIfNotEmpty(parts, UiTextUtility.NormalizeButtonLabelForSpeech(buttons[i]));
            }

            Plugin.Speech.Speak(UiTextUtility.JoinSpeechParts(parts), interrupt: true, sourceTag: "popup:" + __originalMethod.Name);
        }

        public static void OnMainGamePlayNormalScene(object __instance, object[] __args)
        {
            if (__args == null || __args.Length < 4)
            {
                return;
            }

            object scene = __args[1];
            string text = __args[3] as string;
            string speech = UiTextUtility.BuildEventDialogSpeech(__instance, scene, text);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "dialog:maingame");
        }

        public static void OnEventViewShowNormalScene(object __instance, MethodBase __originalMethod, object[] __args)
        {
            if (__args == null || __args.Length < 2)
            {
                return;
            }

            object scene = null;
            string text = string.Empty;
            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] != null && __args[i].GetType().Name == "EventScene")
                {
                    scene = __args[i];
                }

                if (__args[i] is string stringArg)
                {
                    text = stringArg;
                }
            }

            string speech = UiTextUtility.BuildEventDialogSpeech(__instance, scene, text);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "dialog:" + __originalMethod.DeclaringType.Name);
        }

        public static void OnChallengeDialogue(object[] __args)
        {
            string text = __args != null && __args.Length > 0 ? UiTextUtility.NormalizeForSpeech(__args[0] as string) : string.Empty;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Plugin.Speech.Speak(text, interrupt: true, sourceTag: "dialog:challenge");
        }

        public static void OnAnnouncementShow(MethodBase __originalMethod, object[] __args)
        {
            if (__args == null || __args.Length == 0)
            {
                return;
            }

            string text = UiTextUtility.NormalizeForSpeech(__args[0] as string);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Plugin.Speech.Speak(text, interrupt: true, sourceTag: "announcement:" + __originalMethod.Name);
        }

        public static void OnMainGameStatFeedback(object[] __args)
        {
            if (__args == null || __args.Length < 2)
            {
                return;
            }

            string statName = UiTextUtility.BuildStatProfileName(__args[0]);
            if (string.IsNullOrEmpty(statName))
            {
                return;
            }

            bool positive = __args[1] is bool value && value;
            string key = positive ? "MOD_TTS_STAT_INCREASE" : "MOD_TTS_STAT_DECREASE";
            Plugin.Speech.Speak(Plugin.Localization.Get(key, "stat", statName), interrupt: false, sourceTag: "stat:feedback");
        }

        public static void OnMainGameStatCheckFeedback(object[] __args)
        {
            if (__args == null || __args.Length < 2)
            {
                return;
            }

            string statName = UiTextUtility.BuildStatProfileName(__args[0]);
            if (string.IsNullOrEmpty(statName))
            {
                return;
            }

            bool positive = __args[1] is bool value && value;
            string key = positive ? "MOD_TTS_STAT_CHECK_PASS" : "MOD_TTS_STAT_CHECK_FAIL";
            Plugin.Speech.Speak(Plugin.Localization.Get(key, "stat", statName), interrupt: false, sourceTag: "stat:check");
        }

        public static void OnMissionPointsChanged(MethodBase __originalMethod, object[] __args)
        {
            if (__args == null || __args.Length < 2)
            {
                return;
            }

            string playerName = UiTextUtility.BuildPlayerName(__args[0]);
            int total = __args[1] is int value ? value : -1;
            if (string.IsNullOrEmpty(playerName) || total < 0)
            {
                return;
            }

            string key;
            if (__originalMethod.Name.IndexOf("Increase", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                key = "MOD_TTS_MISSION_POINTS_INCREASE";
            }
            else if (__originalMethod.Name.IndexOf("Decrease", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                key = "MOD_TTS_MISSION_POINTS_DECREASE";
            }
            else
            {
                key = "MOD_TTS_MISSION_POINTS_SET";
            }

            string speech = Plugin.Localization.Get(key, "player", playerName, "total", total.ToString());
            Plugin.Speech.Speak(speech, interrupt: false, sourceTag: "missionpoints:" + __originalMethod.Name);
        }

        public static void OnTriggerWarningsPrepareTooltip(object __instance)
        {
            string text = UiTextUtility.BuildTriggerWarningsTooltipSpeech(__instance);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Plugin.Speech.Speak(text, interrupt: false, sourceTag: "triggerwarnings");
        }

        public static void OnTurnOverlaySetPlayer(object __instance, MethodBase __originalMethod, object[] __args)
        {
            string text = ReadLabelText(__instance, "playerTurnLabel");
            if (string.IsNullOrEmpty(text) && __args != null && __args.Length > 0)
            {
                text = UiTextUtility.BuildPlayerName(__args[0]);
            }

            text = UiTextUtility.NormalizeForSpeech(text);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string speech = Plugin.Localization.Get("MOD_TTS_TURN", "player", text);
            speech = UiTextUtility.JoinSpeechParts(new[] { speech, "Make your choice" });
            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "turn:" + __originalMethod.DeclaringType.Name);
        }

        public static void OnMissionsOverlayDestinationsTab(object __instance, MethodBase __originalMethod)
        {
            if (Plugin.Speech != null && Plugin.Speech.WasRecentSource("missions:destinations:", 0.75f))
            {
                return;
            }

            string speech = UiTextUtility.BuildMissionsDestinationsTabSpeech(__instance);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "missions:destinations:" + __originalMethod.Name);
        }

        public static void OnMissionsOverlayMissionsTab(object __instance, MethodBase __originalMethod)
        {
            if (Plugin.Speech != null && Plugin.Speech.WasRecentSource("missionpoints:", 0.75f))
            {
                return;
            }

            string speech = UiTextUtility.BuildMissionPointsSummary();
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "missionpoints:" + __originalMethod.Name);
        }

        public static void OnPrologueSetupPrompt(object __instance, MethodBase __originalMethod)
        {
            string speech = UiTextUtility.BuildVisibleNonButtonText(
                __instance,
                "choosePlayerLabel",
                "container",
                "customizeDataContainer",
                "customizeNameContainer",
                "customizePronounContainer");
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            bool isMissionSelect = __originalMethod.DeclaringType != null
                && string.Equals(__originalMethod.DeclaringType.Name, "PrologueMissionSelectView", StringComparison.Ordinal);
            Plugin.Speech.Speak(speech, interrupt: !isMissionSelect, sourceTag: "prologue:prompt:" + __originalMethod.DeclaringType.Name + "." + __originalMethod.Name);
        }

        public static void OnPrologueUIManagerViewShown(object __instance, MethodBase __originalMethod)
        {
            string viewField = GetPrologueViewFieldName(__originalMethod.Name);
            if (string.IsNullOrEmpty(viewField))
            {
                return;
            }

            object view = Traverse.Create(__instance).Field(viewField).GetValue();
            string speech = UiTextUtility.BuildVisibleNonButtonText(
                view,
                "choosePlayerLabel",
                "container",
                "customizeDataContainer",
                "customizeNameContainer",
                "customizePronounContainer");
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "prologue:view:" + __originalMethod.Name);
        }

        public static void OnMainGameViewPrompt(object __instance, MethodBase __originalMethod, object[] __args)
        {
            if (__originalMethod.DeclaringType != null
                && string.Equals(__originalMethod.DeclaringType.Name, "ResultsView", StringComparison.Ordinal)
                && string.Equals(__originalMethod.Name, "ShowView", StringComparison.Ordinal))
            {
                string resultSpeech = UiTextUtility.BuildResultsViewSpeech(__args);
                if (!string.IsNullOrEmpty(resultSpeech))
                {
                    Plugin.Speech.Speak(resultSpeech, interrupt: true, sourceTag: "view:results");
                    return;
                }
            }

            string speech = UiTextUtility.BuildVisibleNonButtonText(
                __instance,
                "container",
                "actionTitleLabel",
                "headerLabel",
                "statPromptLabel",
                "labelEndingChooseNpc",
                "memories_ThisRunLabel",
                "memories_TotalLabel",
                "resultMissionPointsLabel",
                "resultStatTypeLabel",
                "thisRun_NewLocationsLabel",
                "thisRun_NewEventsLabel",
                "thisRun_NewOutcomesLabel",
                "thisRun_NewIndividualEndingsLabel",
                "thisRun_NewDestinationLayersLabel");
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "view:prompt:" + __originalMethod.DeclaringType.Name + "." + __originalMethod.Name);
        }

        public static void OnLocationActionButtonSetData(object __instance, object[] __args)
        {
            object eventFlow = __args != null && __args.Length > 0 ? __args[0] : null;
            UiTextUtility.CacheLocationActionCue(__instance, eventFlow);
        }

        public static void OnDestinationMissionResultsShowView(object __instance)
        {
            string speech = UiTextUtility.BuildDestinationMissionResultsSpeech(__instance);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "view:destinationmissionresults");
        }

        public static void OnDestinationProgressShowView(object __instance)
        {
            string speech = UiTextUtility.BuildDestinationProgressSpeech(__instance);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "view:destinationprogress");
        }

        public static void OnBarterShopConfirmShopItem(object __instance)
        {
            string speech = UiTextUtility.BuildBarterShopExchangeResultSpeech(__instance);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: false, sourceTag: "barter:exchange");
        }

        public static void OnCarViewPlayCarEffects(object[] __args)
        {
            string speech = UiTextUtility.BuildCarEffectsSpeech(__args);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: false, sourceTag: "car:effects");
        }

        public static void OnPassengerFxAndApplyStatEffect(object[] __args)
        {
            string speech = UiTextUtility.BuildPassengerStatEffectSpeech(__args);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: false, sourceTag: "passenger:effects");
        }

        public static void OnStartChoosePlayers(object __instance)
        {
            string label = ReadLabelText(__instance, "choosePlayerLabel");
            Plugin.Speech.Speak(label, interrupt: true, sourceTag: "character:start");
        }

        public static void OnPreparePlayerSelection(object __instance)
        {
            string label = ReadLabelText(__instance, "choosePlayerLabel");
            Plugin.Speech.Speak(label, interrupt: true, sourceTag: "character:prepare");
        }

        public static void OnCharacterPackHover(object __instance)
        {
            object availableCharacters = Traverse.Create(__instance).Field("availableCharacters").GetValue();
            int count = GetCollectionCount(availableCharacters);

            string hintKey = count > 0 ? "MOD_CHARACTER_PACK_HINT_COUNT" : "MOD_CHARACTER_PACK_HINT";
            string hint = count > 0
                ? Plugin.Localization.Get(hintKey, "count", count.ToString())
                : Plugin.Localization.Get(hintKey);

            Plugin.Speech.Speak(hint, interrupt: false, sourceTag: "character:packhint");
        }

        public static void OnCreditsStart(MethodBase __originalMethod)
        {
        }

        public static void OnCreditsEpilogueFinished()
        {
            string message = Plugin.Localization.Get("MOD_CREDITS_MESSAGE");
            Plugin.Speech.Speak(message, interrupt: true, sourceTag: "credits:video");
        }

        public static void OnCustomizeWidgetChanged(object __instance, MethodBase __originalMethod)
        {
            string summary = UiTextUtility.BuildCustomizeWidgetSummary(__instance);
            if (string.IsNullOrEmpty(summary))
            {
                return;
            }

            Plugin.Speech.Speak(summary, interrupt: true, sourceTag: "customize:" + __originalMethod.DeclaringType.Name + "." + __originalMethod.Name);
        }

        public static void OnUiGoToSettingsScreen()
        {
        }

        public static void OnUiGoToCustomizeScreen()
        {
            SpokenCustomizeNotes.Clear();
            CustomizeNotesByKey.Clear();
        }

        public static void OnUiGoToGalleryScene()
        {
        }

        public static void OnUiGoToStickersScene()
        {
        }

        public static void OnGalleryShowTab(object __instance)
        {
            UiTextUtility.ExportVisibleGalleryImages(__instance);
            SpeakGalleryStatsIfCurrent(__instance);
        }

        public static void OnGalleryShowFocusImage(object __instance, object aGalleryImage)
        {
            string text = BuildGalleryFocusAnnouncement(__instance, aGalleryImage);
            if (!string.IsNullOrEmpty(text))
            {
                Plugin.Speech.Speak(text, interrupt: true, sourceTag: "gallery:focus");
            }
        }

        public static void OnGalleryPageChanged(object __instance)
        {
            UiTextUtility.ExportVisibleGalleryImagesFromPageNavigation(__instance);
            string text = BuildPagedAnnouncement(
                "MOD_GALLERY_PAGE",
                __instance,
                "page", "total");

            object galleryUiManager = UiTextUtility.GetGalleryUiManagerFromPageNavigation(__instance);
            SpeakGalleryPage(galleryUiManager, text, "gallery:page", includeUnlockedEntries: false);
        }

        public static void OnGallerySetCurrentImages(object __instance, bool aOnlyUpdateSameImages)
        {
            if (!aOnlyUpdateSameImages)
            {
                return;
            }

            UiTextUtility.ExportVisibleGalleryImages(__instance);
            object pageNavigation = Traverse.Create(__instance).Field("pageNavigation").GetValue();
            string pageText = BuildPagedAnnouncement("MOD_GALLERY_PAGE", pageNavigation, "page", "total");
            SpeakGalleryPage(__instance, pageText, "gallery:pagecontent", includeUnlockedEntries: false);
        }

        public static void OnGalleryStatsSetDataToLabels(object __instance)
        {
            string speech = BuildGalleryStatsSpeech(__instance);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: true, sourceTag: "gallery:stats");
        }

        private static void SpeakGalleryStatsIfCurrent(object galleryUiManager)
        {
            if (galleryUiManager == null || Plugin.Speech == null)
            {
                return;
            }

            string tabTitle = UiTextUtility.NormalizeForSpeech(ReadLabelText(galleryUiManager, "tabTitleLabel"));
            if (!string.Equals(tabTitle, "Stats", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            object galleryStats = Traverse.Create(galleryUiManager).Field("galleryStats").GetValue();
            string speech = BuildGalleryStatsSpeech(galleryStats);
            if (string.IsNullOrEmpty(speech))
            {
                return;
            }

            Plugin.Speech.Speak(speech, interrupt: false, sourceTag: "gallery:stats:tab");
        }

        private static string BuildGalleryStatsSpeech(object galleryStats)
        {
            if (galleryStats == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            AddGalleryStatPart(parts, galleryStats, "Runs completed", "runsLabel");
            AddGalleryStatPart(parts, galleryStats, "Runs won", "runsWonLabel");
            AddGalleryStatPart(parts, galleryStats, "Runs lost", "runsLostLabel");
            AddGalleryStatPart(parts, galleryStats, "Locations visited", "locationsVisitedLabel");
            AddGalleryStatPart(parts, galleryStats, "Events", "eventsLabel");
            AddGalleryStatPart(parts, galleryStats, "Outcomes", "outcomesLabel");
            AddGalleryStatPart(parts, galleryStats, "Destinations", "destinationsLabel");
            AddGalleryStatPart(parts, galleryStats, "Destination layers", "destinationLayersLabel");
            AddGalleryStatPart(parts, galleryStats, "Success endings", "successEndingsLabel");
            AddGalleryStatPart(parts, galleryStats, "Date endings", "dateEndingsLabel");
            AddGalleryStatPart(parts, galleryStats, "Secret endings", "secretEndingsLabel");
            AddGalleryStatPart(parts, galleryStats, "Items", "itemsLabel");
            AddGalleryStatPart(parts, galleryStats, "Special stuff", "specialStuffLabel");
            AddGalleryStatPart(parts, galleryStats, "Ending images", "galleryEndingImagesLabel");
            AddGalleryStatPart(parts, galleryStats, "Ending variations", "galleryEndingVariationsLabel");
            AddGalleryStatPart(parts, galleryStats, "Character images", "galleryCharacterImagesLabel");
            AddGalleryStatPart(parts, galleryStats, "Character variations", "galleryCharacterVariationsLabel");
            AddGalleryStatPart(parts, galleryStats, "Location variations", "galleryLocationVariationsLabel");
            AddGalleryStatPart(parts, galleryStats, "Background images", "galleryBackgroundImagesLabel");
            AddGalleryStatPart(parts, galleryStats, "Background variations", "galleryBackgroundVariationsLabel");
            AddGalleryStatPart(parts, galleryStats, "Bench images", "galleryBenchesImagesLabel");
            AddGalleryStatPart(parts, galleryStats, "Sketch images", "gallerySketchImagesLabel");
            AddGalleryStatPart(parts, galleryStats, "Character icons", "galleryCharacterIconsLabel");
            AddGalleryStatPart(parts, galleryStats, "Stickers", "galleryStickersLabel");
            AddGalleryStatPart(parts, galleryStats, "Favourite player character", "favouritePcLabel");
            AddGalleryStatPart(parts, galleryStats, "Most game overs stat", "mostGameOversStatLabel");
            return string.Join(". ", parts.ToArray());
        }

        private static void AddGalleryStatPart(List<string> parts, object galleryStats, string label, string fieldName)
        {
            string value = UiTextUtility.NormalizeForSpeech(ReadLabelText(galleryStats, fieldName));
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            parts.Add(label + ": " + value);
        }

        public static void OnStickerMainView()
        {
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_STICKERS_MAIN_VIEW"), interrupt: true, sourceTag: "stickers:view:main");
        }

        public static void OnStickerBackgroundSelectionView()
        {
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_STICKERS_BACKGROUND_VIEW"), interrupt: true, sourceTag: "stickers:view:background");
        }

        public static void OnStickerMyDesignsView()
        {
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_STICKERS_MY_DESIGNS_VIEW"), interrupt: true, sourceTag: "stickers:view:mydesigns");
        }

        public static void OnStickerCompositionView()
        {
            Plugin.Speech.Speak(Plugin.Localization.Get("MOD_STICKERS_COMPOSITION_VIEW"), interrupt: true, sourceTag: "stickers:view:composition");
        }

        public static void OnStickerTabChanged(object __instance)
        {
            string text = BuildStickerTabAnnouncement(__instance);
            if (!string.IsNullOrEmpty(text))
            {
                Plugin.Speech.Speak(text, interrupt: true, sourceTag: "stickers:tab");
            }
        }

        public static void OnStickerPageChanged(object __instance)
        {
            string text = BuildPagedAnnouncement(
                "MOD_STICKERS_PAGE",
                __instance,
                "page", "total");

            if (!string.IsNullOrEmpty(text))
            {
                int currentPage = GetCurrentPageOneBased(__instance);
                Plugin.Speech.Speak(text, interrupt: true, sourceTag: "stickers:page:" + currentPage);
            }
        }

        public static void OnCustomizeNoteInfo(object __instance, string aNoteType)
        {
            string noteType = UiTextUtility.NormalizeForSpeech(aNoteType);
            if (string.IsNullOrEmpty(noteType) || string.Equals(noteType, "None", StringComparison.OrdinalIgnoreCase) || string.Equals(noteType, "COUNT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            List<string> parts = new List<string>();
            if (string.Equals(noteType, "allowSelfLove", StringComparison.OrdinalIgnoreCase))
            {
                AddIfNotEmpty(parts, Plugin.Localization.Get("MOD_CUSTOMIZE_ALLOW_SELF_LOVE_TITLE"));
                AddIfNotEmpty(parts, ReadLabelText(__instance, "otherNoteDescriptionLabel"));
            }
            else if (string.Equals(noteType, "endOfTheRoad", StringComparison.OrdinalIgnoreCase))
            {
                AddIfNotEmpty(parts, Plugin.Localization.Get("MOD_CUSTOMIZE_END_OF_THE_ROAD_TITLE"));
                AddIfNotEmpty(parts, ReadLabelText(__instance, "otherNoteDescriptionLabel"));
            }
            else
            {
                AddIfNotEmpty(parts, ReadLabelText(__instance, "triggerWarningNoticeTitleLabel"));
                AddIfNotEmpty(parts, ReadLabelText(__instance, "triggerWarningNoticeDescriptionLabel"));
            }

            if (parts.Count == 0)
            {
                return;
            }

            CustomizeNotesByKey[noteType] = string.Join(". ", parts.ToArray());
        }

        internal static string AppendCustomizeNoteIfNeeded(GameObject value, string speech)
        {
            string cleanedSpeech = UiTextUtility.NormalizeForSpeech(speech);
            if (value == null || string.IsNullOrEmpty(cleanedSpeech))
            {
                return cleanedSpeech;
            }

            string noteKey = UiTextUtility.GetCustomizeNoteKey(value);
            if (string.IsNullOrEmpty(noteKey) || SpokenCustomizeNotes.Contains(noteKey))
            {
                return cleanedSpeech;
            }

            if (!CustomizeNotesByKey.TryGetValue(noteKey, out string noteText))
            {
                return cleanedSpeech;
            }

            string cleanedNote = UiTextUtility.NormalizeForSpeech(noteText);
            if (string.IsNullOrEmpty(cleanedNote))
            {
                return cleanedSpeech;
            }

            SpokenCustomizeNotes.Add(noteKey);
            string cleanedSpeechLower = cleanedSpeech.ToLowerInvariant();
            string cleanedNoteLower = cleanedNote.ToLowerInvariant();

            int firstPeriod = cleanedNote.IndexOf('.');
            if (firstPeriod > 0)
            {
                string noteWithoutLead = UiTextUtility.NormalizeForSpeech(cleanedNote.Substring(firstPeriod + 1));
                if (!string.IsNullOrEmpty(noteWithoutLead))
                {
                    string lead = UiTextUtility.NormalizeForSpeech(cleanedNote.Substring(0, firstPeriod));
                    if (!string.IsNullOrEmpty(lead) && cleanedSpeechLower.StartsWith(lead.ToLowerInvariant(), StringComparison.Ordinal))
                    {
                        cleanedNote = noteWithoutLead;
                        cleanedNoteLower = cleanedNote.ToLowerInvariant();
                    }
                }
            }

            if (cleanedSpeechLower.Contains(cleanedNoteLower))
            {
                return cleanedSpeech;
            }

            return cleanedSpeech + ". " + cleanedNote;
        }

        private static string ReadLabelText(object instance, string fieldName)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            object label = Traverse.Create(instance).Field(fieldName).GetValue();
            if (label == null)
            {
                return string.Empty;
            }

            object text = Traverse.Create(label).Property("text").GetValue();
            return text as string ?? string.Empty;
        }

        private static string ReadFirstAvailableLabel(object instance, params string[] fieldNames)
        {
            for (int i = 0; i < fieldNames.Length; i++)
            {
                string text = ReadLabelText(instance, fieldNames[i]);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            return string.Empty;
        }

        private static string GetMasterPopupTitleFromArgs(string methodName, object[] args)
        {
            if (args == null)
            {
                return string.Empty;
            }

            switch (methodName)
            {
                case "ShowInfoPopup":
                    return args.Length > 2 ? args[2] as string : string.Empty;
                case "ShowOkPopup":
                    return args.Length > 3 ? args[3] as string : string.Empty;
                case "ShowYesNoPopup":
                    return args.Length > 5 ? args[5] as string : string.Empty;
                case "Show3ButtonPopup":
                    return args.Length > 6 ? args[6] as string : string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static string[] GetMasterPopupButtonTextsFromArgs(string methodName, object[] args)
        {
            if (args == null)
            {
                return new string[0];
            }

            switch (methodName)
            {
                case "ShowOkPopup":
                    return new[] { args.Length > 5 ? args[5] as string : string.Empty };
                case "ShowYesNoPopup":
                    return new[]
                    {
                        args.Length > 8 ? args[8] as string : string.Empty,
                        args.Length > 9 ? args[9] as string : string.Empty
                    };
                case "Show3ButtonPopup":
                    return new[]
                    {
                        args.Length > 9 ? args[9] as string : string.Empty,
                        args.Length > 10 ? args[10] as string : string.Empty,
                        args.Length > 11 ? args[11] as string : string.Empty
                    };
                default:
                    return new string[0];
            }
        }

        internal static bool ShouldSuppressFocusForRecentSpeech(GameObject value)
        {
            if (value == null || Plugin.Speech == null)
            {
                return false;
            }

            if (UiTextUtility.IsGameplayChoiceOrActionFocus(value))
            {
                return false;
            }

            if (UiTextUtility.FindComponentInParents(value, "ProloguePcButton") != null
                || UiTextUtility.FindComponentInParents(value, "RunModeButton") != null
                || UiTextUtility.FindComponentInParents(value, "MusicVariationView") != null)
            {
                return false;
            }

            string path = UiTextUtility.BuildPath(value.transform);
            if (path.Contains("/View_Roadmap/")
                && UiTextUtility.IsMainGameEventDialogueVisible())
            {
                return true;
            }

            if (path.Contains("/PauseMenu/Container/ConfirmExitPanel/")
                && Plugin.Speech.WasRecentSource("pause:confirm", 2.0f))
            {
                return true;
            }

            if (path.Contains("/View_DestinationProgress/")
                && UiTextUtility.WasRecentDestinationProgressSpeech())
            {
                return true;
            }

            return Plugin.Speech.WasRecentSource("dialog:", 2.5f)
                || Plugin.Speech.WasRecentSource("dialog", 2.5f)
                || Plugin.Speech.WasRecentSource("prologue:", 1.25f)
                || Plugin.Speech.WasRecentSource("view:prompt:", 1.25f)
                || Plugin.Speech.WasRecentSource("popup:", 2.0f)
                || Plugin.Speech.WasRecentSource("announcement:", 2.0f)
                || Plugin.Speech.WasRecentSource("credits:", 2.0f)
                || Plugin.Speech.WasRecentSource("turn:", 1.5f)
                || Plugin.Speech.WasRecentSource("stat:", 1.0f)
                || Plugin.Speech.WasRecentSource("missionpoints:", 1.0f);
        }

        private static bool IsEventViewTextBox(object instance)
        {
            Component component = instance as Component;
            if (component == null)
            {
                return false;
            }

            string[] eventViewTypes = new[]
            {
                "MainGameEventView",
                "PrologueEventView",
                "CreditsEventView",
                "DemoIntroEventView",
                "DemoEndEventView",
                "GachaShopEventView",
                "MetaShopEventView",
                "TestEventsEventView"
            };

            for (int i = 0; i < eventViewTypes.Length; i++)
            {
                if (UiTextUtility.FindComponentInParents(component.gameObject, eventViewTypes[i]) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetCollectionCount(object value)
        {
            System.Collections.ICollection collection = value as System.Collections.ICollection;
            return collection == null ? 0 : collection.Count;
        }

        private static string GetPrologueViewFieldName(string methodName)
        {
            switch (methodName)
            {
                case "ShowView_PlayerNumbers":
                    return "playersNumberView";
                case "ShowView_PlayerSelect":
                    return "playersSelectView";
                case "ShowView_ControllerMode":
                    return "controlModeView";
                case "ShowView_DifficultyMode":
                    return "difficultyModeView";
                case "ShowView_RunMode":
                    return "runModeView";
                case "ShowView_MusicVariation":
                    return "musicVariationView";
                default:
                    return string.Empty;
            }
        }

        private static string BuildGalleryTabAnnouncement(object galleryUiManager)
        {
            if (galleryUiManager == null)
            {
                return string.Empty;
            }

            string tabTitle = UiTextUtility.NormalizeForSpeech(ReadLabelText(galleryUiManager, "tabTitleLabel"));
            if (string.IsNullOrEmpty(tabTitle))
            {
                return string.Empty;
            }

            object pageNavigation = Traverse.Create(galleryUiManager).Field("pageNavigation").GetValue();
            int totalPages = GetNumPages(pageNavigation);
            if (totalPages > 1)
            {
                return Plugin.Localization.Get(
                    "MOD_GALLERY_TAB_PAGE",
                    "tab", tabTitle,
                    "page", GetCurrentPageOneBased(pageNavigation).ToString(),
                    "total", totalPages.ToString());
            }

            return Plugin.Localization.Get("MOD_GALLERY_TAB", "tab", tabTitle);
        }

        private static string BuildGalleryFocusAnnouncement(object galleryUiManager, object galleryImage)
        {
            if (galleryImage == null)
            {
                return string.Empty;
            }

            return UiTextUtility.BuildGalleryImageFocusSpeech((galleryImage as Component)?.gameObject);
        }

        private static void SpeakGalleryPage(object galleryUiManager, string pageText, string sourceTagPrefix, bool includeUnlockedEntries)
        {
            if (Plugin.Speech == null)
            {
                return;
            }

            object pageNavigation = galleryUiManager != null ? Traverse.Create(galleryUiManager).Field("pageNavigation").GetValue() : null;
            int currentPage = GetCurrentPageOneBased(pageNavigation);
            string pageSource = sourceTagPrefix + ":" + currentPage;
            if (!string.IsNullOrEmpty(pageText))
            {
                Plugin.Speech.Speak(pageText, interrupt: true, sourceTag: pageSource);
            }

            List<string> entries = includeUnlockedEntries
                ? UiTextUtility.BuildVisibleGalleryPageEntries(galleryUiManager)
                : UiTextUtility.BuildVisibleLockedGalleryPageEntries(galleryUiManager);
            for (int i = 0; i < entries.Count; i++)
            {
                Plugin.Speech.Speak(entries[i], interrupt: false, sourceTag: pageSource + ":entry:" + (i + 1));
            }
        }

        private static string BuildStickerTabAnnouncement(object stickerTabsWidget)
        {
            if (stickerTabsWidget == null)
            {
                return string.Empty;
            }

            object currentTab = Traverse.Create(stickerTabsWidget).Method("GetCurrentTab").GetValue();
            GameObject currentTabGo = (currentTab as Component)?.gameObject;
            string tabTitle = UiTextUtility.NormalizeForSpeech(UiTextUtility.ExtractUiText(currentTabGo));
            if (string.IsNullOrEmpty(tabTitle))
            {
                return string.Empty;
            }

            object pageNavigation = Traverse.Create(stickerTabsWidget).Field("pageNavigation").GetValue();
            int totalPages = GetNumPages(pageNavigation);
            if (totalPages > 1)
            {
                return Plugin.Localization.Get(
                    "MOD_STICKERS_TAB_PAGE",
                    "tab", tabTitle,
                    "page", GetCurrentPageOneBased(pageNavigation).ToString(),
                    "total", totalPages.ToString());
            }

            return Plugin.Localization.Get("MOD_STICKERS_TAB", "tab", tabTitle);
        }

        private static string BuildPagedAnnouncement(string localizationKey, object pageNavigation, string pageToken, string totalToken)
        {
            if (pageNavigation == null)
            {
                return string.Empty;
            }

            int totalPages = GetNumPages(pageNavigation);
            if (totalPages <= 1)
            {
                return string.Empty;
            }

            return Plugin.Localization.Get(
                localizationKey,
                pageToken, GetCurrentPageOneBased(pageNavigation).ToString(),
                totalToken, totalPages.ToString());
        }

        private static int GetCurrentPageOneBased(object pageNavigation)
        {
            if (pageNavigation == null)
            {
                return 0;
            }

            return Traverse.Create(pageNavigation).Method("GetCurrentPage").GetValue<int>() + 1;
        }

        private static int GetNumPages(object pageNavigation)
        {
            if (pageNavigation == null)
            {
                return 0;
            }

            return Traverse.Create(pageNavigation).Method("GetNumOfPages").GetValue<int>();
        }

        private static void SpeakFullCreditsPage(object instance, string sourceTag)
        {
            if (instance == null)
            {
                return;
            }

            int currentIndex = Traverse.Create(instance).Field("mCurrentContentIndex").GetValue<int>();
            object slides = Traverse.Create(instance).Field("creditsSlides").GetValue();
            int totalPages = GetCollectionCount(slides);
            string pageText = Plugin.Localization.Get(
                "MOD_FULL_CREDITS_PAGE",
                "page", (currentIndex + 1).ToString(),
                "total", totalPages.ToString());

            string left = ReadLabelText(instance, "contentLabelLeft");
            string right = ReadLabelText(instance, "contentLabelRight");
            string content = !string.IsNullOrWhiteSpace(left) ? left : right;

            List<string> parts = new List<string>();
            AddIfNotEmpty(parts, pageText);
            AddIfNotEmpty(parts, content);
            Plugin.Speech.Speak(string.Join(". ", parts.ToArray()), interrupt: true, sourceTag: sourceTag + ":" + currentIndex);
        }

        private static void AddIfNotEmpty(List<string> parts, string value)
        {
            string cleaned = UiTextUtility.NormalizeForSpeech(value);
            if (!string.IsNullOrEmpty(cleaned))
            {
                parts.Add(cleaned);
            }
        }

        private static bool ShouldSuppressInitialSceneChromeFocus(GameObject value)
        {
            if (value == null)
            {
                return false;
            }

            string path = UiTextUtility.BuildPath(value.transform);
            bool isGalleryChrome = path.Contains("/GalleryPageNavigation/")
                || path.Contains("/GalleryTabs/BackButton");
            bool isInfoManualChrome = path.Contains("/InfoManual/Container/Buttons/BackButton")
                || path.Contains("/InfoManual/Container/Viewer/PageNavigation/");

            return (isGalleryChrome && Plugin.Speech != null && Plugin.Speech.WasRecentSource("gallery:", 10f))
                || (isInfoManualChrome && Plugin.Speech != null && Plugin.Speech.WasRecentSource("infomanual:", 10f));
        }
    }
}
