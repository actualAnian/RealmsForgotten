using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade
{
    public class CommanderDefenseBehavior : CampaignBehaviorBase
    {
        // ====== CONFIG ======
        private const bool DEBUG_TRACE = true;          // mostra na tela (InformationManager)
        private const float FarDistanceThreshold = 30f; // seu limite original
        private const float MaxWaitSeconds = 30f;

        private static readonly Dictionary<string, string> CultureToCommanderTroopId = new()
        {
            { "vlandia",  "castle_commander_vlandia"  },
            { "sturgia",  "castle_commander_sturgia"  },
            { "aserai",   "castle_commander_aserai"   },
            { "khuzait",  "castle_commander_khuzait"  },
            { "battania", "castle_commander_battania" },
            { "empire",   "castle_commander_empire"   },

            // extras / seus
            { "south_realm", "castle_commander_south_realm" },
            { "west_realm", "castle_commander_west_realm" },
            { "giant", "castle_commander_giant" },
            { "mage", "castle_commander_mage" },
            { "dwarf", "castle_commander_dwarf" },
            { "urkhai", "castle_commander_urkhai" },
            { "aqarun", "castle_commander_aqarun" },
            { "wulf", "castle_commander_wulf" },
            { "valthorne", "castle_commander_valthorne" },
        };

        private readonly HashSet<string> _prompted = new();
        private const bool ENABLE_TELEPORT = false;
        internal static bool PendingSwap;
        internal static string PendingTroopId;

        // ====== delayed enter state (crash fix) ======
        private bool _waitingToEnter;
        private Settlement _targetCastleToEnter;
        private float _waitingSeconds;

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);

            // probe (instrumentação)
            try { RFSiegeTransitionProbe.PatchOnce(); }
            catch { }

            Trace("RegisterEvents OK");
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnHourlyTick()
        {
            var clan = Clan.PlayerClan;
            if (clan == null || MobileParty.MainParty == null)
                return;

            foreach (var s in clan.Settlements)
            {
                if (s == null || !s.IsCastle)
                    continue;

                // Se não está sob cerco, libera para poder avisar de novo no futuro
                if (s.SiegeEvent == null)
                {
                    _prompted.Remove(s.StringId);
                    continue;
                }

                if (_prompted.Contains(s.StringId))
                    continue;

                if (!IsPlayerFarFrom(s))
                    continue;

                _prompted.Add(s.StringId);

                Trace($"POPUP: {s.Name} siege detected. SiegeEvent ok. Far ok.");
                ShowPopup(s);
            }
        }

        private bool IsPlayerFarFrom(Settlement castle)
        {
            PartyBase playerParty = MobileParty.MainParty?.Party;
            PartyBase castleParty = castle?.Party;

            if (playerParty == null || castleParty == null)
                return true;

            CampaignVec2 p = playerParty.Position;
            CampaignVec2 c = castleParty.Position;

            float dist = p.Distance(c);
            return dist > FarDistanceThreshold;
        }

        // ***** NÃO TOQUEI NO POPUP *****
        private void ShowPopup(Settlement castle)
        {
            string title = "Castle Under Siege!";
            string body = $"{castle.Name} is under siege.\n\nDo you want to join the defense as the castle commander?";

            InformationManager.ShowInquiry(new InquiryData(
                title,
                body,
                true,
                true,
                "Defend as Commander",
                "Ignore",
                () => OnAccept(castle),
                () => Trace($"Player ignored: {castle.Name}")
            ));
        }

        private void OnAccept(Settlement castle)
        {
            Trace($"ACCEPT clicked: {castle.Name}");

            try { RFSiegeTransitionProbe.PatchOnce(); } catch { }

            string cultureId = castle.Culture?.StringId ?? "";
            if (!CultureToCommanderTroopId.TryGetValue(cultureId, out string troopId))
            {
                Trace($"ERROR: no troop mapping for culture '{cultureId}'");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"No commander troop mapped for culture '{cultureId}'.", Colors.Red));
                return;
            }

            PendingTroopId = troopId;
            PendingSwap = true;

            _waitingToEnter = true;
            _targetCastleToEnter = castle;
            _waitingSeconds = 0f;

            Trace($"WAITING to enter siege. target={castle.Name} troopId={troopId}");
        }

        private void OnTick(float dt)
        {
            if (!_waitingToEnter)
                return;

            _waitingSeconds += dt;

            if (_targetCastleToEnter == null)
            {
                CancelWaiting("Target castle became null.");
                return;
            }

            if (_waitingSeconds > MaxWaitSeconds)
            {
                CancelWaiting("Timed out waiting to enter siege.");
                return;
            }

            if (!IsSiegeReady(_targetCastleToEnter))
                return;

            Trace($"SIEGE READY: {_targetCastleToEnter.Name} BesiegerLeaderParty={_targetCastleToEnter.SiegeEvent?.BesiegerCamp?.LeaderParty?.Name}");

            // Fluxo vanilla: entrar no settlement e deixar o menu do cerco criar a batalha ao clicar Attack.
            try
            {
                // Teleport defensivo SEM Vec2 (resolve mismatch de estado)
                Trace("Calling EnterSettlementAction.ApplyForParty...");
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, _targetCastleToEnter);


                Trace("Calling EnterSettlementAction.ApplyForParty...");
                RFLogger.Log($"[Campaign] About to EnterSettlementAction: {_targetCastleToEnter.Name}");

                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, _targetCastleToEnter);

                RFLogger.Log($"[Campaign] EnterSettlementAction returned: {_targetCastleToEnter.Name}");
                Trace("EnterSettlementAction.ApplyForParty returned OK");
            }
            catch (Exception e)
            {
                PendingSwap = false;
                PendingTroopId = null;

                Trace($"EXCEPTION entering settlement: {e.InnerException?.Message ?? e.Message}");
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Failed to enter settlement for defense: {e.InnerException?.Message ?? e.Message}", Colors.Red));
            }
            finally
            {
                _waitingToEnter = false;
                _targetCastleToEnter = null;
            }
        }

        private static bool IsSiegeReady(Settlement s)
        {
            return s != null
                   && s.SiegeEvent != null
                   && s.SiegeEvent.BesiegerCamp != null
                   && s.SiegeEvent.BesiegerCamp.LeaderParty != null
                   && s.Party != null;
        }

        private void CancelWaiting(string message)
        {
            _waitingToEnter = false;
            _targetCastleToEnter = null;

            PendingSwap = false;
            PendingTroopId = null;

            if (!string.IsNullOrEmpty(message))
            {
                Trace("CancelWaiting: " + message);
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
            }
        }

        private static void Trace(string msg)
        {
            RFLogger.Log("[RF-CommanderDefense] " + msg);
            Debug.Print("[RF-CommanderDefense] " + msg);

            if (DEBUG_TRACE)
                InformationManager.DisplayMessage(new InformationMessage("[RF] " + msg, Colors.Yellow));
        }

        private static bool EnsurePlayerEncounterAgainstBesieger(Settlement castle, out string debug)
        {
            debug = "";
            try
            {
                var besieger = castle?.SiegeEvent?.BesiegerCamp?.LeaderParty;

                if (MobileParty.MainParty?.Party == null)
                {
                    debug = "MobileParty.MainParty.Party is null";
                    return false;
                }

                if (besieger?.Party == null)
                {
                    debug = "besieger.Party is null (LeaderParty missing or siege state changed)";
                    return false;
                }

                Type peType = FindType("TaleWorlds.CampaignSystem.Encounters.PlayerEncounter");
                if (peType == null)
                {
                    debug = "PlayerEncounter type not found";
                    return false;
                }

                var restart = peType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m =>
                    {
                        if (!string.Equals(m.Name, "RestartPlayerEncounter", StringComparison.Ordinal))
                            return false;

                        var ps = m.GetParameters();
                        return ps.Length == 3
                               && ps[0].ParameterType.Name == "PartyBase"
                               && ps[1].ParameterType.Name == "PartyBase"
                               && ps[2].ParameterType == typeof(bool);
                    });

                if (restart == null)
                {
                    debug = "RestartPlayerEncounter overload not found (expected PartyBase, PartyBase, bool)";
                    return false;
                }

                // ✅ CORRETO: MainParty vs BesiegerLeaderParty
                restart.Invoke(null, new object[] { MobileParty.MainParty.Party, besieger.Party, false });

                debug = "RestartPlayerEncounter(def=MainParty.Party, atk=BesiegerLeader.Party, false)";
                return true;
            }
            catch (TargetInvocationException tie)
            {
                debug = tie.InnerException?.Message ?? tie.Message;
                return false;
            }
            catch (Exception e)
            {
                debug = e.InnerException?.Message ?? e.Message;
                return false;
            }
        }


        // helper: property OU field (pra aguentar diferenças de versão)
        private static object GetPropOrField(object obj, string propName, string fieldName)
        {
            if (obj == null) return null;

            try
            {
                var p = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null) return p.GetValue(obj);
            }
            catch { }

            try
            {
                var f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null) return f.GetValue(obj);
            }
            catch { }

            return null;
        }
        private static Type FindType(string fullName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = a.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        // ==========================================================
        // TELEPORT FIX (sem Vec2): escreve CampaignVec2 em Position
        // ==========================================================
        private static bool TryTeleportPartyToSettlement_NoVec2(MobileParty party, Settlement settlement, out string why)
        {
            why = "";
            try
            {
                if (party == null) { why = "party is null"; return false; }
                if (settlement == null) { why = "settlement is null"; return false; }
                if (settlement.Party == null) { why = "settlement.Party is null"; return false; }

                // alvo: CampaignVec2 (já existe no seu projeto e não depende de Vec2)
                CampaignVec2 target = settlement.Party.Position;

                // seta primeiro na PartyBase
                if (party.Party != null && TrySetCampaignPosition(party.Party, target, out var w2))
                {
                    why = "set on PartyBase | " + w2;
                    return true;
                }

                // fallback: tenta no MobileParty se expuser Position CampaignVec2
                if (TrySetCampaignPosition(party, target, out var w1))
                {
                    why = "set on MobileParty | " + w1;
                    return true;
                }

                why = "could not set CampaignVec2 Position on PartyBase/MobileParty";
                return false;
            }
            catch (Exception e)
            {
                why = e.InnerException?.Message ?? e.Message;
                return false;
            }
        }

        private static bool TrySetCampaignPosition(object obj, CampaignVec2 targetPos, out string why)
        {
            why = "";
            if (obj == null) { why = "obj is null"; return false; }

            const string name = "Position";

            // property
            var p = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite && p.PropertyType == typeof(CampaignVec2))
            {
                p.SetValue(obj, targetPos);
                why = $"wrote property {obj.GetType().Name}.{name} (CampaignVec2)";
                return true;
            }

            // field
            var f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(CampaignVec2))
            {
                f.SetValue(obj, targetPos);
                why = $"wrote field {obj.GetType().Name}.{name} (CampaignVec2)";
                return true;
            }

            why = $"no writable CampaignVec2 Position on {obj.GetType().FullName}";
            return false;
        }
    }

   


    // ====== SEU SWAP behavior (mantido) ======
    public class CommanderSwapMissionBehavior : MissionBehavior
    {
        private bool _done;
        private string _troopId;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            base.AfterStart();

            if (!CommanderDefenseBehavior.PendingSwap)
                return;

            _troopId = CommanderDefenseBehavior.PendingTroopId;

            CommanderDefenseBehavior.PendingSwap = false;
            CommanderDefenseBehavior.PendingTroopId = null;

            RFLogger.Log("[Swap] Captured troopId=" + (_troopId ?? "null") + " (will swap when Battle starts)");
        }

        public override void OnMissionTick(float dt)
        {
            if (_done || Mission.Current == null)
                return;

            if (Mission.Current.Mode != MissionMode.Battle)
                return;

            if (string.IsNullOrEmpty(_troopId))
            {
                _done = true;
                return;
            }

            try
            {
                CharacterObject commanderChar = MBObjectManager.Instance.GetObject<CharacterObject>(_troopId);
                if (commanderChar == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Commander troop '{_troopId}' not found. Check XML id.", Colors.Red));
                    _done = true;
                    return;
                }

                Team team = Mission.Current.PlayerTeam ?? FindLikelyDefenderTeam(Mission.Current);

                AgentBuildData abd = new AgentBuildData(commanderChar).Team(team);
                Agent commanderAgent = Mission.Current.SpawnAgent(abd);

                if (!TryInvokeSetPlayerAgent(Mission.Current, commanderAgent))
                {
                    Agent currentPlayer = FindPlayerControlledAgentByReflection(Mission.Current);
                    if (currentPlayer != null)
                        SetAgentControllerByName(currentPlayer, "AI", fallbackName: "Ai");

                    SetAgentControllerByName(commanderAgent, "Player");
                }

                RFLogger.Log("[Swap] Swap executed in MissionMode.Battle");
                InformationManager.DisplayMessage(new InformationMessage(
                    "You are now playing as the castle commander.", Colors.Green));
            }
            catch (Exception e)
            {
                RFLogger.Log("[Swap] Exception: " + (e.InnerException?.Message ?? e.Message));
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Commander swap failed: {e.InnerException?.Message ?? e.Message}", Colors.Red));
            }

            _done = true;
        }

        private static Team FindLikelyDefenderTeam(Mission mission)
        {
            if (mission?.Teams == null) return null;

            Team best = null;
            int bestCount = -1;

            foreach (var t in mission.Teams)
            {
                if (t == null) continue;
                int c = t.ActiveAgents?.Count() ?? 0;
                if (c > bestCount)
                {
                    bestCount = c;
                    best = t;
                }
            }

            return best;
        }

        private static bool TryInvokeSetPlayerAgent(Mission mission, Agent agent)
        {
            try
            {
                var mi = mission.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == "SetPlayerAgent" && m.GetParameters().Length == 1);

                if (mi != null)
                {
                    mi.Invoke(mission, new object[] { agent });
                    return true;
                }
            }
            catch { }
            return false;
        }

        private static Agent FindPlayerControlledAgentByReflection(Mission mission)
        {
            try
            {
                var agents = mission?.Agents;
                if (agents == null) return null;

                foreach (var a in agents)
                {
                    if (a == null) continue;

                    var controllerProp = a.GetType().GetProperty("Controller",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (controllerProp != null)
                    {
                        var v = controllerProp.GetValue(a);
                        if (v != null && v.ToString().IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0)
                            return a;
                    }
                }
            }
            catch { }
            return null;
        }

        private static void SetAgentControllerByName(Agent agent, string desiredName, string fallbackName = null)
        {
            if (agent == null) return;

            var prop = agent.GetType().GetProperty("Controller",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop == null) return;

            Type enumType = prop.PropertyType;
            if (!enumType.IsEnum) return;

            object parsed = null;
            try { parsed = Enum.Parse(enumType, desiredName, ignoreCase: true); } catch { }

            if (parsed == null && !string.IsNullOrEmpty(fallbackName))
                try { parsed = Enum.Parse(enumType, fallbackName, ignoreCase: true); } catch { }

            if (parsed != null)
                prop.SetValue(agent, parsed);
        }
    }
}
