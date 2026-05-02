using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RealmsForgotten.AiMade
{
    public static class RFSiegeTransitionProbe
    {
        private static bool _patched;
        private static bool _patching;

        public static void PatchOnce()
        {
            if (_patched || _patching) return;
            _patching = true;

            try
            {
                RFLogger.Log("[Probe] PatchOnce ENTERED");

                var h = new Harmony("realmsforgotten.freezeprobe");

                PatchGameMenuOptionConsequence(h);
                PatchGameStateManagerPushState(h);
                PatchMountAndBladeMissionStateLifecycle(h);

                _patched = true;
                RFLogger.Log("[Probe] PatchOnce SUCCESS");
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] PatchOnce exception: " + (e.InnerException?.Message ?? e.Message));
                _patched = false;
            }
            finally
            {
                _patching = false;
            }
        }

        // 1) GameMenuOption consequence hook (APENAS LOG)
        private static void PatchGameMenuOptionConsequence(Harmony h)
        {
            var campaignAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "TaleWorlds.CampaignSystem");

            if (campaignAsm == null)
            {
                RFLogger.Log("[Probe] TaleWorlds.CampaignSystem not loaded yet.");
                return;
            }

            var gmoType = campaignAsm.GetTypes()
                .FirstOrDefault(t => t.FullName == "TaleWorlds.CampaignSystem.GameMenus.GameMenuOption");

            if (gmoType == null)
            {
                RFLogger.Log("[Probe] GameMenuOption type not found.");
                return;
            }

            var gmoMethods = gmoType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m =>
                {
                    string n = m.Name ?? "";
                    return n.IndexOf("Consequence", StringComparison.OrdinalIgnoreCase) >= 0
                           && (n.IndexOf("Run", StringComparison.OrdinalIgnoreCase) >= 0
                               || n.IndexOf("Execute", StringComparison.OrdinalIgnoreCase) >= 0);
                })
                .ToList();

            int patchedGmo = 0;
            foreach (var m in gmoMethods)
            {
                try
                {
                    var pre = typeof(RFSiegeTransitionProbe).GetMethod(nameof(GameMenuConsequencePrefix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    h.Patch(m, prefix: new HarmonyMethod(pre));
                    patchedGmo++;
                }
                catch (Exception e)
                {
                    RFLogger.Log("[Probe] Failed patching " + m.Name + " : " + (e.InnerException?.Message ?? e.Message));
                }
            }

            RFLogger.Log("[Probe] Patched GameMenuOption consequence methods: " + patchedGmo);
        }

        private static void GameMenuConsequencePrefix(object __instance, MethodBase __originalMethod)
        {
            try
            {
                // loga qual opção foi clicada (id/text)
                string optId = SafeToString(GetAnyMemberValue(__instance, new[] { "IdString", "Id", "_id", "_idString" }));
                string optText = SafeToString(GetAnyMemberValue(__instance, new[] { "Text", "_text", "OptionText", "TextObject" }));

                RFLogger.Log("[Probe] GameMenuOption consequence: " +
                             (__originalMethod.DeclaringType?.FullName ?? "?") + "." + __originalMethod.Name +
                             " | id=" + optId + " | text=" + optText);

                // IMPORTANTE: NÃO alterar PlayerEncounter/MapEvent aqui (Attack usa isso e quebra se mexer)
                DumpCampaignState("OnGameMenuConsequence");
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] GameMenuConsequencePrefix exception: " + (e.InnerException?.Message ?? e.Message));
            }
        }

        // 2) GameStateManager.PushState hook
        private static void PatchGameStateManagerPushState(Harmony h)
        {
            var coreAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "TaleWorlds.Core");

            if (coreAsm == null)
            {
                RFLogger.Log("[Probe] TaleWorlds.Core not loaded (cannot patch GameStateManager).");
                return;
            }

            var gsmType = coreAsm.GetTypes()
                .FirstOrDefault(t => t.FullName == "TaleWorlds.Core.GameStateManager");

            if (gsmType == null)
            {
                RFLogger.Log("[Probe] GameStateManager type not found.");
                return;
            }

            var pushMethods = gsmType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => string.Equals(m.Name, "PushState", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (pushMethods.Count == 0)
            {
                RFLogger.Log("[Probe] GameStateManager.PushState not found (no overloads).");
                return;
            }

            int patched = 0;
            foreach (var m in pushMethods)
            {
                var ps = m.GetParameters();
                if (ps.Length < 1) continue;

                try
                {
                    var pre = typeof(RFSiegeTransitionProbe).GetMethod(nameof(GameStatePushPrefix),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    h.Patch(m, prefix: new HarmonyMethod(pre));
                    patched++;
                }
                catch (Exception e)
                {
                    RFLogger.Log("[Probe] Failed patching GameStateManager.PushState overload: " +
                                 m + " | " + (e.InnerException?.Message ?? e.Message));
                }
            }

            RFLogger.Log("[Probe] Patched GameStateManager.PushState overloads: " + patched);
        }

        private static void GameStatePushPrefix(object gameState, MethodBase __originalMethod)
        {
            try
            {
                string gsType = gameState == null ? "null" : gameState.GetType().FullName;
                RFLogger.Log("[Probe] GameStateManager.PushState: " +
                             (__originalMethod.DeclaringType?.FullName ?? "?") + "." + __originalMethod.Name +
                             " | pushed=" + gsType);

                DumpCampaignState("OnPushState");
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] GameStatePushPrefix exception: " + (e.InnerException?.Message ?? e.Message));
            }
        }

        // 3) MissionState lifecycle hooks (MountAndBlade)
        private static void PatchMountAndBladeMissionStateLifecycle(Harmony h)
        {
            var mbAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "TaleWorlds.MountAndBlade");

            if (mbAsm == null)
            {
                RFLogger.Log("[Probe] TaleWorlds.MountAndBlade not loaded (MissionState lifecycle probes skipped).");
                return;
            }

            var msType = mbAsm.GetTypes()
                .FirstOrDefault(t => t.FullName == "TaleWorlds.MountAndBlade.MissionState");

            if (msType == null)
            {
                RFLogger.Log("[Probe] TaleWorlds.MountAndBlade.MissionState type not found.");
                return;
            }

            int patched = 0;
            patched += TryPatchInstanceMethodAnySig(h, msType, "OnInitialize");
            patched += TryPatchInstanceMethodAnySig(h, msType, "OnActivate");
            patched += TryPatchInstanceMethodAnySig(h, msType, "OnFinalize");

            patched += TryPatchInstanceMethodAnySig(h, msType, "Initialize");
            patched += TryPatchInstanceMethodAnySig(h, msType, "Activate");
            patched += TryPatchInstanceMethodAnySig(h, msType, "Finalize");

            patched += TryPatchInstanceMethodAnySig(h, msType, "StartMission");
            patched += TryPatchInstanceMethodAnySig(h, msType, "StartBattle");
            patched += TryPatchInstanceMethodAnySig(h, msType, "OpenMission");

            RFLogger.Log("[Probe] Patched MountAndBlade.MissionState lifecycle methods total: " + patched);
        }

        private static int TryPatchInstanceMethodAnySig(Harmony h, Type t, string methodName)
        {
            try
            {
                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (methods.Count == 0)
                {
                    RFLogger.Log("[Probe] " + t.FullName + "." + methodName + " not found.");
                    return 0;
                }

                int patched = 0;
                var pre = typeof(RFSiegeTransitionProbe).GetMethod(nameof(MissionStateLifecyclePrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);

                foreach (var m in methods)
                {
                    try
                    {
                        h.Patch(m, prefix: new HarmonyMethod(pre));
                        RFLogger.Log("[Probe] Patched " + t.FullName + "." + m.Name + " | sig=" + m);
                        patched++;
                    }
                    catch (Exception e)
                    {
                        RFLogger.Log("[Probe] Failed patching " + t.FullName + "." + m.Name +
                                     " | " + (e.InnerException?.Message ?? e.Message));
                    }
                }

                return patched;
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] TryPatchInstanceMethodAnySig exception for " + t.FullName + "." + methodName +
                             " : " + (e.InnerException?.Message ?? e.Message));
                return 0;
            }
        }

        private static void MissionStateLifecyclePrefix(object __instance, MethodBase __originalMethod)
        {
            try
            {
                RFLogger.Log("[Probe] MissionState lifecycle: " +
                             (__originalMethod.DeclaringType?.FullName ?? "?") + "." + __originalMethod.Name +
                             " | instance=" + (__instance == null ? "null" : __instance.GetType().FullName));

                DumpCampaignState("OnMissionStateLifecycle");
                DumpMissionDetails(__instance, "OnMissionStateLifecycle");
            }
            catch (TargetInvocationException tie)
            {
                RFLogger.Log("[Probe] MissionStateLifecyclePrefix exception: " +
                             (tie.InnerException?.Message ?? tie.Message));
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] MissionStateLifecyclePrefix exception: " +
                             (e.InnerException?.Message ?? e.Message));
            }
        }

        private static void DumpMissionDetails(object missionStateInstance, string tag)
        {
            try
            {
                if (missionStateInstance == null)
                {
                    RFLogger.Log("[Probe] " + tag + " | DumpMissionDetails: missionStateInstance=null");
                    return;
                }

                object missionObj = GetAnyMemberValue(missionStateInstance, new[]
                {
                    "Mission", "CurrentMission", "_mission", "mission", "_currentMission"
                });

                string missionType = missionObj == null ? "null" : missionObj.GetType().FullName;

                object modeObj = missionObj == null ? null : GetAnyMemberValue(missionObj, new[]
                {
                    "Mode", "MissionMode", "_mode"
                });

                string modeStr = modeObj == null ? "null" : modeObj.ToString();

                object sceneNameObj = missionObj == null ? null : GetAnyMemberValue(missionObj, new[]
                {
                    "SceneName", "SceneNameToLoad", "_sceneName", "_sceneNameToLoad", "Name"
                });

                string sceneNameStr = sceneNameObj == null ? "null" : sceneNameObj.ToString();

                object sceneObj = missionObj == null ? null : GetAnyMemberValue(missionObj, new[]
                {
                    "Scene", "_scene", "_loadedScene", "LoadedScene"
                });

                string sceneType = sceneObj == null ? "null" : sceneObj.GetType().FullName;

                object isReadyObj = GetAnyMemberValue(missionStateInstance, new[]
                {
                    "IsReady", "Ready", "_isReady"
                });

                object initializedObj = GetAnyMemberValue(missionStateInstance, new[]
                {
                    "IsInitialized", "_isInitialized", "Initialized"
                });

                string isReadyStr = isReadyObj == null ? "null" : isReadyObj.ToString();
                string initializedStr = initializedObj == null ? "null" : initializedObj.ToString();

                RFLogger.Log("[Probe] " + tag +
                             " | Mission=" + missionType +
                             " | Mode=" + modeStr +
                             " | SceneName=" + sceneNameStr +
                             " | SceneObj=" + sceneType +
                             " | IsReady=" + isReadyStr +
                             " | IsInitialized=" + initializedStr);
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] " + tag + " | DumpMissionDetails exception: " +
                             (e.InnerException?.Message ?? e.Message));
            }
        }

        private static object GetAnyMemberValue(object obj, string[] names)
        {
            if (obj == null || names == null || names.Length == 0) return null;
            Type t = obj.GetType();

            foreach (var name in names)
            {
                try
                {
                    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null) return p.GetValue(obj);
                }
                catch { }

                try
                {
                    var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) return f.GetValue(obj);
                }
                catch { }
            }

            return null;
        }

        private static string SafeToString(object o)
        {
            try { return o == null ? "null" : o.ToString(); }
            catch { return "<?>"; }
        }

        private static void DumpCampaignState(string tag)
        {
            try
            {
                var campaignAsm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "TaleWorlds.CampaignSystem");
                if (campaignAsm == null)
                {
                    RFLogger.Log("[Probe] " + tag + " | CampaignSystem asm missing");
                    return;
                }

                var settlementType = campaignAsm.GetTypes()
                    .FirstOrDefault(t => t.FullName == "TaleWorlds.CampaignSystem.Settlements.Settlement");
                var curSettlement = GetStaticPropObj(settlementType, "CurrentSettlement");
                string curSettlementName = GetProp(curSettlement, "Name")?.ToString() ?? "null";

                var mpType = campaignAsm.GetTypes()
                    .FirstOrDefault(t => t.FullName == "TaleWorlds.CampaignSystem.Party.MobileParty");
                var mainParty = GetStaticPropObj(mpType, "MainParty");
                var mainCurSettlement = GetProp(mainParty, "CurrentSettlement");
                string mainCurSettlementName = GetProp(mainCurSettlement, "Name")?.ToString() ?? "null";

                var peType = campaignAsm.GetTypes()
                    .FirstOrDefault(t => t.FullName == "TaleWorlds.CampaignSystem.Encounters.PlayerEncounter");
                var peCur = GetStaticPropObj(peType, "Current");

                var meType = campaignAsm.GetTypes()
                    .FirstOrDefault(t => t.FullName == "TaleWorlds.CampaignSystem.MapEvents.MapEvent");
                var playerMapEvent = GetStaticPropObj(meType, "PlayerMapEvent");

                RFLogger.Log("[Probe] " + tag +
                             $" | Settlement.CurrentSettlement={curSettlementName}" +
                             $" | MainParty.CurrentSettlement={mainCurSettlementName}" +
                             $" | PlayerEncounter.Current={(peCur == null ? "null" : peCur.GetType().FullName)}" +
                             $" | MapEvent.PlayerMapEvent={(playerMapEvent == null ? "null" : playerMapEvent.GetType().FullName)}");
            }
            catch (Exception e)
            {
                RFLogger.Log("[Probe] DumpCampaignState exception: " + (e.InnerException?.Message ?? e.Message));
            }
        }

        private static object GetStaticPropObj(Type t, string propName)
        {
            if (t == null) return null;
            var p = t.GetProperty(propName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return p?.GetValue(null);
        }

        private static object GetProp(object obj, string propName)
        {
            if (obj == null) return null;
            var p = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return p?.GetValue(obj);
        }
    }
}
