using System;
using System.Collections.Generic;
using System.Linq;
using RF_warsystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade
{
    public class MilitaryAidDiplomacyBehavior : CampaignBehaviorBase
    {
        private const float AiCheckStartDay = 45f;
        private const float AiRequestCooldownDays = 24f;
        private const float PlayerRequestCooldownDays = 30f;
        private const float CompactAiCheckStartDay = 80f;
        private const float CompactCooldownDays = 120f;
        private const float CompactMinimumPeaceDays = 45f;

        private List<MilitaryAidRequest> _pendingAidRequests = new();
        private List<SecretWarCompactRequest> _pendingWarCompacts = new();
        private Dictionary<string, CampaignTime> _lastAiAidRequest = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CampaignTime> _lastPlayerAidRequest = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CampaignTime> _lastWarCompact = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CampaignTime> _pairPeaceSince = new(StringComparer.OrdinalIgnoreCase);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_pendingAidRequests", ref _pendingAidRequests);
            dataStore.SyncData("_pendingWarCompacts", ref _pendingWarCompacts);
            dataStore.SyncData("_lastAiAidRequest", ref _lastAiAidRequest);
            dataStore.SyncData("_lastPlayerAidRequest", ref _lastPlayerAidRequest);
            dataStore.SyncData("_lastWarCompact", ref _lastWarCompact);
            dataStore.SyncData("_pairPeaceSince", ref _pairPeaceSince);

            _pendingAidRequests ??= new List<MilitaryAidRequest>();
            _pendingWarCompacts ??= new List<SecretWarCompactRequest>();
            _lastAiAidRequest ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
            _lastPlayerAidRequest ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
            _lastWarCompact ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
            _pairPeaceSince ??= new Dictionary<string, CampaignTime>(StringComparer.OrdinalIgnoreCase);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenu(
                "rf_envoys_chamber",
                "Your envoys wait beside sealed letters and waxed banners. From here you may request aid or prepare a secret war compact.",
                null,
                GameMenu.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption(
                "town",
                "rf_envoys_chamber_town",
                "Visit the Envoy's Chamber",
                CanUseEnvoysChamber,
                _ => GameMenu.SwitchToMenu("rf_envoys_chamber"),
                false,
                6);

            starter.AddGameMenuOption(
                "castle",
                "rf_envoys_chamber_castle",
                "Visit the Envoy's Chamber",
                CanUseEnvoysChamber,
                _ => GameMenu.SwitchToMenu("rf_envoys_chamber"),
                false,
                6);

            starter.AddGameMenuOption(
                "rf_envoys_chamber",
                "rf_envoys_request_aid",
                "Send envoys to request military aid",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    Kingdom playerKingdom = GetPlayerKingdom();
                    Kingdom enemy = GetStrongestEnemy(playerKingdom);
                    args.IsEnabled = enemy != null && GetAvailableHelperKingdoms(playerKingdom, enemy).Count > 0;
                    if (!args.IsEnabled)
                    {
                        args.Tooltip = new TextObject("No suitable realm can be approached right now.");
                    }

                    return true;
                },
                _ => ShowPlayerAidInquiry());

            starter.AddGameMenuOption(
                "rf_envoys_chamber",
                "rf_envoys_propose_war_compact",
                "Propose a secret war compact",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    Kingdom playerKingdom = GetPlayerKingdom();
                    args.IsEnabled = GetAvailableCompactTargets(playerKingdom).Count > 0;
                    if (!args.IsEnabled)
                    {
                        args.Tooltip = new TextObject("There is no suitable target for a secret offensive pact right now.");
                    }

                    return true;
                },
                _ => ShowPlayerCompactTargetInquiry());

            starter.AddGameMenuOption(
                "rf_envoys_chamber",
                "rf_envoys_back",
                "Return",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => GameMenu.SwitchToMenu(Settlement.CurrentSettlement?.IsCastle == true ? "castle" : "town"),
                true);
        }

        private bool CanUseEnvoysChamber(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            Kingdom playerKingdom = GetPlayerKingdom();
            Settlement settlement = Settlement.CurrentSettlement;

            if (playerKingdom == null || playerKingdom.Leader != Hero.MainHero)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Only a ruling monarch can send royal envoys.");
                return false;
            }

            if (settlement?.OwnerClan?.Kingdom != playerKingdom)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("Envoys can only be sent from your own towns or castles.");
                return false;
            }

            return true;
        }

        private void OnDailyTick()
        {
            try
            {
                ResolvePendingAidRequests();
                ResolvePendingWarCompacts();
                TrackPeaceDurations();

                if (CampaignTime.Now.ToDays < AiCheckStartDay)
                    return;

                ScanAiAidRequests();

                if (CampaignTime.Now.ToDays >= CompactAiCheckStartDay)
                {
                    ScanAiWarCompacts();
                }
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[MilitaryAid] Daily tick failed: {ex}");
            }
        }

        private void ScanAiAidRequests()
        {
            foreach (Kingdom petitioner in Kingdom.All.Where(IsValidKingdom))
            {
                if (petitioner.Leader == Hero.MainHero)
                    continue;

                Kingdom enemy = GetStrongestEnemy(petitioner);
                if (enemy == null)
                    continue;

                float pressure = GetPressureRatio(petitioner, enemy);
                if (pressure < 1.85f || petitioner.Fiefs.Count() > 8)
                    continue;

                string cooldownKey = MakePairKey(petitioner, enemy);
                if (IsOnCooldown(_lastAiAidRequest, cooldownKey, AiRequestCooldownDays))
                    continue;

                Kingdom helper = GetAvailableHelperKingdoms(petitioner, enemy)
                    .Select(x => new { Kingdom = x, Score = CalculateAidScore(petitioner, x, enemy) })
                    .Where(x => x.Score >= 55)
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault()?.Kingdom;

                if (helper == null)
                    continue;

                QueueAidRequest(petitioner, helper, enemy, false);
                _lastAiAidRequest[cooldownKey] = CampaignTime.Now;
            }
        }

        private void ShowPlayerAidInquiry()
        {
            Kingdom playerKingdom = GetPlayerKingdom();
            Kingdom enemy = GetStrongestEnemy(playerKingdom);
            if (playerKingdom == null || enemy == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("There is no active kingdom war to request aid for.", Colors.Yellow));
                return;
            }

            string cooldownKey = MakePairKey(playerKingdom, enemy);
            if (IsOnCooldown(_lastPlayerAidRequest, cooldownKey, PlayerRequestCooldownDays))
            {
                InformationManager.DisplayMessage(new InformationMessage("Your envoys have already pressed this matter recently.", Colors.Yellow));
                return;
            }

            List<InquiryElement> helpers = GetAvailableHelperKingdoms(playerKingdom, enemy)
                .Select(helper =>
                {
                    int score = CalculateAidScore(playerKingdom, helper, enemy);
                    string mood = score >= 75 ? "likely" : score >= 55 ? "possible" : "unlikely";
                    string label = $"{helper.Name} - {mood} ({score})";
                    return new InquiryElement(helper, label, null);
                })
                .ToList();

            if (helpers.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No realm is in a position to answer your call.", Colors.Yellow));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    "Send Envoys",
                    $"Choose a realm to ask for help against {enemy.Name}. Their answer will arrive after several days.",
                    helpers,
                    true,
                    1,
                    1,
                    "Send Envoys",
                    "Cancel",
                    selected =>
                    {
                        Kingdom helper = selected.FirstOrDefault()?.Identifier as Kingdom;
                        if (helper == null)
                            return;

                        QueueAidRequest(playerKingdom, helper, enemy, true);
                        _lastPlayerAidRequest[cooldownKey] = CampaignTime.Now;
                    },
                    null,
                    string.Empty,
                    false),
                false,
                false);
        }

        private void QueueAidRequest(Kingdom petitioner, Kingdom helper, Kingdom enemy, bool isPlayerRequest)
        {
            int responseDays = MBRandom.RandomInt(2, 5);
            MilitaryAidRequest request = new(
                petitioner,
                helper,
                enemy,
                CampaignTime.Now,
                CampaignTime.DaysFromNow(responseDays),
                isPlayerRequest,
                CalculateGoldDemand(petitioner, helper),
                "military_aid");

            _pendingAidRequests.Add(request);

            if (isPlayerRequest)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Envoys have departed for {helper.Name}. A reply is expected in {responseDays} days.",
                    Colors.Cyan));
            }
            else
            {
                RFLogger.Log($"[MilitaryAid] Queued AI request | petitioner={petitioner.StringId} | helper={helper.StringId} | enemy={enemy.StringId} | responseDays={responseDays}");
            }
        }

        private void ResolvePendingAidRequests()
        {
            for (int i = _pendingAidRequests.Count - 1; i >= 0; i--)
            {
                MilitaryAidRequest request = _pendingAidRequests[i];
                if (request == null || CampaignTime.Now < request.ResponseAt)
                    continue;

                _pendingAidRequests.RemoveAt(i);

                if (!IsValidAidRequest(request))
                    continue;

                int score = CalculateAidScore(request.Petitioner, request.Helper, request.Enemy);
                bool accepted = score >= 58 || (score >= 48 && MBRandom.RandomFloat < 0.25f);

                if (!accepted)
                {
                    NotifyRejected(request);
                    continue;
                }

                if (request.IsPlayerRequest)
                {
                    ShowPlayerAidResponse(request, score);
                }
                else
                {
                    ApplyAid(request);
                }
            }
        }

        private void ShowPlayerAidResponse(MilitaryAidRequest request, int score)
        {
            string terms = request.GoldDemand > 0
                ? $"{request.GoldDemand} gold as campaign support"
                : "no payment, only future goodwill";

            InformationManager.ShowInquiry(
                new InquiryData(
                    "Envoys Return",
                    $"{request.Helper.Name} will aid you against {request.Enemy.Name}, but asks for {terms}.",
                    true,
                    true,
                    "Accept Terms",
                    "Refuse",
                    () =>
                    {
                        if (request.GoldDemand > 0 && Hero.MainHero.Gold < request.GoldDemand)
                        {
                            InformationManager.DisplayMessage(new InformationMessage("You cannot afford the demanded support.", Colors.Red));
                            return;
                        }

                        if (request.GoldDemand > 0)
                        {
                            Hero.MainHero.ChangeHeroGold(-request.GoldDemand);
                        }

                        ApplyAid(request);
                    },
                    () => ChangeRelation(request.Petitioner?.Leader, request.Helper?.Leader, -1)),
                true);
        }

        private void ApplyAid(MilitaryAidRequest request)
        {
            RFWarExternalIntentApi.ReinforceCollectiveDefense(request.Enemy, request.Petitioner, new[] { request.Helper });
            ChangeRelation(request.Petitioner?.Leader, request.Helper?.Leader, request.IsPlayerRequest ? 2 : 1);

            if (request.IsPlayerRequest)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{request.Helper.Name} has answered your call and prepares for war against {request.Enemy.Name}.",
                    Colors.Green));
            }
            else
            {
                RFLogger.Log($"[MilitaryAid] Accepted | petitioner={request.Petitioner.StringId} | helper={request.Helper.StringId} | enemy={request.Enemy.StringId}");
            }
        }

        private void NotifyRejected(MilitaryAidRequest request)
        {
            ChangeRelation(request.Petitioner?.Leader, request.Helper?.Leader, -1);

            if (request.IsPlayerRequest)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{request.Helper.Name} refuses to enter the war against {request.Enemy.Name}.",
                    Colors.Yellow));
            }
            else
            {
                RFLogger.Log($"[MilitaryAid] Rejected | petitioner={request.Petitioner?.StringId ?? "null"} | helper={request.Helper?.StringId ?? "null"} | enemy={request.Enemy?.StringId ?? "null"}");
            }
        }

        private void ScanAiWarCompacts()
        {
            List<Kingdom> kingdoms = Kingdom.All.Where(IsValidKingdom).ToList();
            SecretWarCompactRequest best = null;
            int bestScore = 0;

            foreach (Kingdom instigator in kingdoms)
            {
                if (instigator.Leader == Hero.MainHero || instigator.FactionsAtWarWith.OfType<Kingdom>().Count() >= 2)
                    continue;

                foreach (Kingdom ally in kingdoms)
                {
                    if (ally == instigator || ally.Leader == Hero.MainHero || ally.FactionsAtWarWith.OfType<Kingdom>().Count() >= 2)
                        continue;

                    if (!CanConsiderCompactPair(instigator, ally))
                        continue;

                    foreach (Kingdom target in kingdoms)
                    {
                        if (target == instigator || target == ally)
                            continue;

                        if (FactionManager.IsAtWarAgainstFaction(instigator, target) || FactionManager.IsAtWarAgainstFaction(ally, target))
                            continue;

                        string key = MakeTripleKey(instigator, ally, target);
                        if (IsOnCooldown(_lastWarCompact, key, CompactCooldownDays))
                            continue;

                        Settlement promisedSettlement = FindBestCompactSettlement(instigator, ally, target);
                        int score = CalculateWarCompactScore(instigator, ally, target, promisedSettlement);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = new SecretWarCompactRequest(
                                instigator,
                                ally,
                                target,
                                promisedSettlement,
                                CampaignTime.Now,
                                CampaignTime.DaysFromNow(MBRandom.RandomInt(7, 15)),
                                false,
                                0,
                                false,
                                0,
                                0);
                        }
                    }
                }
            }

            if (best == null || bestScore < 72)
                return;

            _pendingWarCompacts.Add(best);
            _lastWarCompact[MakeTripleKey(best.Instigator, best.Ally, best.Target)] = CampaignTime.Now;
            RFLogger.Log($"[WarCompact] Queued AI compact | instigator={best.Instigator.StringId} | ally={best.Ally.StringId} | target={best.Target.StringId} | score={bestScore}");
        }

        private void ShowPlayerCompactTargetInquiry()
        {
            Kingdom playerKingdom = GetPlayerKingdom();
            List<InquiryElement> targets = GetAvailableCompactTargets(playerKingdom)
                .Select(target => new InquiryElement(target, $"{target.Name}", null))
                .ToList();

            if (targets.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No suitable target can be proposed right now.", Colors.Yellow));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    "Secret War Compact",
                    "Choose the realm you want to target.",
                    targets,
                    true,
                    1,
                    1,
                    "Choose Target",
                    "Cancel",
                    selected =>
                    {
                        Kingdom target = selected.FirstOrDefault()?.Identifier as Kingdom;
                        if (target != null)
                        {
                            ShowPlayerCompactAllyInquiry(target);
                        }
                    },
                    null,
                    string.Empty,
                    false),
                false,
                false);
        }

        private void ShowPlayerCompactAllyInquiry(Kingdom target)
        {
            Kingdom playerKingdom = GetPlayerKingdom();
            List<InquiryElement> allies = GetAvailableCompactAllies(playerKingdom, target)
                .Select(ally =>
                {
                    Settlement promisedSettlement = FindBestCompactSettlement(playerKingdom, ally, target);
                    int score = CalculateWarCompactScore(playerKingdom, ally, target, promisedSettlement);
                    string mood = score >= 78 ? "likely" : score >= 62 ? "possible" : "unlikely";
                    string reward = promisedSettlement != null ? $"promised claim: {promisedSettlement.Name}" : "no clear promised fief";
                    return new InquiryElement(ally, $"{ally.Name} - {mood} ({score}) - {reward}", null);
                })
                .ToList();

            if (allies.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No realm has enough reason to join this compact.", Colors.Yellow));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    "Choose Ally",
                    $"Choose a realm to approach for a secret war against {target.Name}.",
                    allies,
                    true,
                    1,
                    1,
                    "Send Envoys",
                    "Cancel",
                    selected =>
                    {
                        Kingdom ally = selected.FirstOrDefault()?.Identifier as Kingdom;
                        if (ally != null)
                        {
                            QueuePlayerWarCompact(playerKingdom, ally, target);
                        }
                    },
                    null,
                    string.Empty,
                    false),
                false,
                false);
        }

        private void QueuePlayerWarCompact(Kingdom instigator, Kingdom ally, Kingdom target)
        {
            Settlement promisedSettlement = FindBestCompactSettlement(instigator, ally, target);
            int score = CalculateWarCompactScore(instigator, ally, target, promisedSettlement);
            int goldDemand = CalculateCompactGoldDemand(score, instigator, ally, target, promisedSettlement);
            int responseDays = MBRandom.RandomInt(5, 10);

            SecretWarCompactRequest compact = new(
                instigator,
                ally,
                target,
                promisedSettlement,
                CampaignTime.Now,
                CampaignTime.DaysFromNow(responseDays),
                true,
                goldDemand,
                false,
                0,
                0);

            _pendingWarCompacts.Add(compact);
            _lastWarCompact[MakeTripleKey(instigator, ally, target)] = CampaignTime.Now;

            InformationManager.DisplayMessage(new InformationMessage(
                $"Envoys travel to {ally.Name} with a secret proposal against {target.Name}. A reply is expected in {responseDays} days.",
                Colors.Cyan));
        }

        private void ResolvePendingWarCompacts()
        {
            for (int i = _pendingWarCompacts.Count - 1; i >= 0; i--)
            {
                SecretWarCompactRequest compact = _pendingWarCompacts[i];
                if (compact == null)
                {
                    _pendingWarCompacts.RemoveAt(i);
                    continue;
                }

                if (compact.IsTriggered)
                {
                    if ((CampaignTime.Now - compact.CreatedAt).ToDays > 180f)
                    {
                        _pendingWarCompacts.RemoveAt(i);
                    }

                    continue;
                }

                if (CampaignTime.Now < compact.TriggerAt)
                    continue;

                if (!IsValidWarCompact(compact))
                {
                    _pendingWarCompacts.RemoveAt(i);
                    continue;
                }

                int score = CalculateWarCompactScore(compact.Instigator, compact.Ally, compact.Target, compact.PromisedSettlement);
                bool accepted = score >= 68 || (score >= 58 && MBRandom.RandomFloat < 0.3f);

                if (!accepted)
                {
                    _pendingWarCompacts.RemoveAt(i);
                    NotifyCompactRejected(compact);
                    continue;
                }

                if (compact.IsPlayerRequest)
                {
                    ShowPlayerCompactResponse(compact, score);
                }
                else
                {
                    TriggerWarCompact(compact);
                }
            }
        }

        private void ShowPlayerCompactResponse(SecretWarCompactRequest compact, int score)
        {
            string settlementText = compact.PromisedSettlement != null
                ? $" They expect a claim on {compact.PromisedSettlement.Name} if the war succeeds."
                : string.Empty;
            string terms = compact.GoldDemand > 0
                ? $"{compact.GoldDemand} gold in campaign support."
                : "no immediate payment.";

            InformationManager.ShowInquiry(
                new InquiryData(
                    "Secret Envoys Return",
                    $"{compact.Ally.Name} accepts the war compact against {compact.Target.Name}, asking for {terms}{settlementText}",
                    true,
                    true,
                    "Accept Compact",
                    "Refuse",
                    () =>
                    {
                        if (compact.GoldDemand > 0 && Hero.MainHero.Gold < compact.GoldDemand)
                        {
                            InformationManager.DisplayMessage(new InformationMessage("You cannot afford the demanded support.", Colors.Red));
                            return;
                        }

                        if (compact.GoldDemand > 0)
                        {
                            Hero.MainHero.ChangeHeroGold(-compact.GoldDemand);
                        }

                        TriggerWarCompact(compact);
                    },
                    () => ChangeRelation(compact.Instigator?.Leader, compact.Ally?.Leader, -2)),
                true);
        }

        private void TriggerWarCompact(SecretWarCompactRequest compact)
        {
            compact.MarkTriggered();
            RFWarExternalIntentApi.ReinforceStrategicIntrigueWar(compact.Instigator, compact.Target, FindBestCompactSettlement(compact.Instigator, compact.Instigator, compact.Target));
            RFWarExternalIntentApi.ReinforceStrategicIntrigueWar(compact.Ally, compact.Target, compact.PromisedSettlement);
            ChangeRelation(compact.Instigator?.Leader, compact.Ally?.Leader, compact.IsPlayerRequest ? 3 : 1);

            if (compact.IsPlayerRequest)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{compact.Ally.Name} has sealed the compact. Both realms prepare war against {compact.Target.Name}.",
                    Colors.Green));
            }
            else
            {
                RFLogger.Log($"[WarCompact] Triggered | instigator={compact.Instigator.StringId} | ally={compact.Ally.StringId} | target={compact.Target.StringId}");
            }
        }

        private void NotifyCompactRejected(SecretWarCompactRequest compact)
        {
            ChangeRelation(compact.Instigator?.Leader, compact.Ally?.Leader, -1);

            if (compact.IsPlayerRequest)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{compact.Ally.Name} refuses the secret compact against {compact.Target.Name}.",
                    Colors.Yellow));
            }
            else
            {
                RFLogger.Log($"[WarCompact] Rejected | instigator={compact.Instigator?.StringId ?? "null"} | ally={compact.Ally?.StringId ?? "null"} | target={compact.Target?.StringId ?? "null"}");
            }
        }

        private void OnSettlementOwnerChanged(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement == null || newOwner?.Clan?.Kingdom == null)
                return;

            foreach (SecretWarCompactRequest compact in _pendingWarCompacts.Where(x => x?.IsTriggered == true).ToList())
            {
                if (!IsValidKingdom(compact.Instigator) || !IsValidKingdom(compact.Ally) || !IsValidKingdom(compact.Target))
                    continue;

                Kingdom ownerKingdom = newOwner.Clan.Kingdom;
                if (ownerKingdom != compact.Instigator && ownerKingdom != compact.Ally)
                    continue;

                if (oldOwner?.Clan?.Kingdom != compact.Target)
                    continue;

                compact.RegisterCapture(ownerKingdom);

                if (compact.CapturesByInstigator + compact.CapturesByAlly < 2)
                    continue;

                TryShareCompactSpoils(compact, settlement, ownerKingdom);
            }
        }

        private static void TryShareCompactSpoils(SecretWarCompactRequest compact, Settlement settlement, Kingdom currentOwnerKingdom)
        {
            Kingdom recipientKingdom = null;
            if (compact.CapturesByInstigator >= 2 && compact.CapturesByAlly == 0)
            {
                recipientKingdom = compact.Ally;
            }
            else if (compact.CapturesByAlly >= 2 && compact.CapturesByInstigator == 0)
            {
                recipientKingdom = compact.Instigator;
            }

            if (recipientKingdom?.Leader == null || currentOwnerKingdom == recipientKingdom)
                return;

            try
            {
                ChangeOwnerOfSettlementAction.ApplyByGift(settlement, recipientKingdom.Leader);
                ChangeRelation(compact.Instigator?.Leader, compact.Ally?.Leader, 2);
                RFLogger.Log($"[WarCompact] Shared spoil | settlement={settlement.StringId} | recipient={recipientKingdom.StringId}");
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[WarCompact] Failed to share spoil | settlement={settlement.StringId} | recipient={recipientKingdom.StringId} | {ex.Message}");
            }
        }

        private void TrackPeaceDurations()
        {
            List<Kingdom> kingdoms = Kingdom.All.Where(IsValidKingdom).ToList();
            foreach (Kingdom left in kingdoms)
            {
                foreach (Kingdom right in kingdoms)
                {
                    if (left == right)
                        continue;

                    string key = MakePairKey(left, right);
                    if (FactionManager.IsAtWarAgainstFaction(left, right))
                    {
                        _pairPeaceSince.Remove(key);
                    }
                    else if (!_pairPeaceSince.ContainsKey(key))
                    {
                        _pairPeaceSince[key] = CampaignTime.Now - CampaignTime.Days(CompactMinimumPeaceDays);
                    }
                }
            }
        }

        private static int CalculateWarCompactScore(Kingdom instigator, Kingdom ally, Kingdom target, Settlement promisedSettlement)
        {
            if (!IsValidKingdom(instigator) || !IsValidKingdom(ally) || !IsValidKingdom(target))
                return 0;

            int score = 25;
            int relation = instigator.Leader?.GetRelation(ally.Leader) ?? 0;
            score += MBMath.ClampInt(relation / 2, -20, 25);

            if (instigator.Culture == ally.Culture)
                score += 8;

            if (SharesFrontier(instigator, target))
                score += 14;

            if (SharesFrontier(ally, target))
                score += 14;

            if (promisedSettlement != null)
                score += 12;

            if (target.Fiefs.Count() >= 3)
                score += 6;

            if (instigator.FactionsAtWarWith.OfType<Kingdom>().Any() || ally.FactionsAtWarWith.OfType<Kingdom>().Any())
                score -= 18;

            float combinedStrength = Math.Max(1f, instigator.CurrentTotalStrength + ally.CurrentTotalStrength);
            float targetStrength = Math.Max(1f, target.CurrentTotalStrength);
            if (combinedStrength > targetStrength * 1.2f)
                score += 12;
            else if (combinedStrength < targetStrength * 0.85f)
                score -= 18;

            if (ally.Fiefs.Count() <= 2)
                score -= 8;

            return MBMath.ClampInt(score, 0, 100);
        }

        private static int CalculateCompactGoldDemand(int score, Kingdom instigator, Kingdom ally, Kingdom target, Settlement promisedSettlement)
        {
            int relation = instigator?.Leader?.GetRelation(ally?.Leader) ?? 0;
            if (score >= 88 || relation >= 60)
                return 0;

            int baseDemand;
            if (score >= 78 || relation >= 40)
            {
                baseDemand = 25000;
            }
            else if (score >= 68 || relation >= 25)
            {
                baseDemand = 45000;
            }
            else if (score >= 58 || relation >= 10)
            {
                baseDemand = 70000;
            }
            else
            {
                baseDemand = 100000;
            }

            float allyStrength = Math.Max(1f, ally?.CurrentTotalStrength ?? 1f);
            float targetStrength = Math.Max(1f, target?.CurrentTotalStrength ?? 1f);
            int strengthCost = MBMath.ClampInt((int)(allyStrength * 8f), 10000, 80000);
            int riskCost = 0;

            if (targetStrength > allyStrength * 1.25f)
                riskCost += 25000;

            if (targetStrength > allyStrength * 1.75f)
                riskCost += 35000;

            int activeWars = ally?.FactionsAtWarWith?.OfType<Kingdom>().Count() ?? 0;
            riskCost += MBMath.ClampInt(activeWars * 20000, 0, 60000);

            if (ally?.Fiefs.Count() <= 2)
                riskCost += 25000;

            if (promisedSettlement == null)
                riskCost += 15000;

            int relationDiscount = MBMath.ClampInt(Math.Max(0, relation) * 500, 0, 30000);
            int total = baseDemand + strengthCost + riskCost - relationDiscount;
            return MBMath.ClampInt((total / 500) * 500, 15000, 220000);
        }

        private List<Kingdom> GetAvailableCompactTargets(Kingdom instigator)
        {
            if (!IsValidKingdom(instigator))
                return new List<Kingdom>();

            return Kingdom.All
                .Where(IsValidKingdom)
                .Where(target => target != instigator)
                .Where(target => !FactionManager.IsAtWarAgainstFaction(instigator, target))
                .Where(target => GetAvailableCompactAllies(instigator, target).Count > 0)
                .ToList();
        }

        private List<Kingdom> GetAvailableCompactAllies(Kingdom instigator, Kingdom target)
        {
            if (!IsValidKingdom(instigator) || !IsValidKingdom(target))
                return new List<Kingdom>();

            return Kingdom.All
                .Where(IsValidKingdom)
                .Where(ally => ally != instigator && ally != target)
                .Where(ally => CanConsiderCompactPair(instigator, ally))
                .Where(ally => !FactionManager.IsAtWarAgainstFaction(ally, target))
                .Where(ally => CalculateWarCompactScore(instigator, ally, target, FindBestCompactSettlement(instigator, ally, target)) >= 45)
                .OrderByDescending(ally => CalculateWarCompactScore(instigator, ally, target, FindBestCompactSettlement(instigator, ally, target)))
                .ToList();
        }

        private bool CanConsiderCompactPair(Kingdom left, Kingdom right)
        {
            if (!IsValidKingdom(left) || !IsValidKingdom(right) || left == right)
                return false;

            if (FactionManager.IsAtWarAgainstFaction(left, right))
                return false;

            int relation = left.Leader?.GetRelation(right.Leader) ?? 0;
            if (relation < 10)
                return false;

            string key = MakePairKey(left, right);
            return _pairPeaceSince.TryGetValue(key, out CampaignTime peaceSince)
                && (CampaignTime.Now - peaceSince).ToDays >= CompactMinimumPeaceDays;
        }

        private static Settlement FindBestCompactSettlement(Kingdom instigator, Kingdom ally, Kingdom target)
        {
            if (!IsValidKingdom(instigator) || !IsValidKingdom(ally) || !IsValidKingdom(target))
                return null;

            return target.Fiefs
                .Where(town => town?.Settlement != null)
                .Select(town => town.Settlement)
                .Select(settlement => new
                {
                    Settlement = settlement,
                    Score = GetNearestKingdomDistance(instigator, settlement) + GetNearestKingdomDistance(ally, settlement)
                })
                .Where(x => x.Score < 260f)
                .OrderBy(x => x.Score)
                .FirstOrDefault()?.Settlement;
        }

        private static float GetNearestKingdomDistance(Kingdom kingdom, Settlement settlement)
        {
            if (kingdom?.Fiefs == null || settlement == null)
                return 9999f;

            float best = 9999f;
            foreach (Town town in kingdom.Fiefs)
            {
                if (town?.Settlement == null)
                    continue;

                float distance = town.Settlement.GetPosition2D.Distance(settlement.GetPosition2D);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private static bool IsValidWarCompact(SecretWarCompactRequest compact)
        {
            return compact != null
                && IsValidKingdom(compact.Instigator)
                && IsValidKingdom(compact.Ally)
                && IsValidKingdom(compact.Target)
                && compact.Instigator != compact.Ally
                && compact.Instigator != compact.Target
                && compact.Ally != compact.Target
                && !FactionManager.IsAtWarAgainstFaction(compact.Instigator, compact.Ally)
                && !FactionManager.IsAtWarAgainstFaction(compact.Instigator, compact.Target)
                && !FactionManager.IsAtWarAgainstFaction(compact.Ally, compact.Target);
        }

        private static int CalculateAidScore(Kingdom petitioner, Kingdom helper, Kingdom enemy)
        {
            if (!IsValidKingdom(petitioner) || !IsValidKingdom(helper) || !IsValidKingdom(enemy))
                return 0;

            int score = 35;
            int relation = petitioner.Leader?.GetRelation(helper.Leader) ?? 0;
            score += MBMath.ClampInt(relation / 2, -20, 25);

            if (helper.Culture == petitioner.Culture)
                score += 12;

            if (SharesFrontier(helper, enemy))
                score += 10;

            if (SharesFrontier(helper, petitioner))
                score += 6;

            if (helper.Fiefs.Count() <= 2)
                score -= 12;

            if (helper.FactionsAtWarWith.OfType<Kingdom>().Count() >= 2)
                score -= 18;

            if (FactionManager.IsAtWarAgainstFaction(helper, enemy))
                score += 30;

            if (helper.CurrentTotalStrength > enemy.CurrentTotalStrength * 0.75f)
                score += 8;

            return MBMath.ClampInt(score, 0, 100);
        }

        private static int CalculateGoldDemand(Kingdom petitioner, Kingdom helper)
        {
            int relation = petitioner?.Leader?.GetRelation(helper?.Leader) ?? 0;
            if (relation >= 35)
                return 0;

            if (relation >= 15)
                return 3500;

            if (relation >= 0)
                return 7500;

            return 14000;
        }

        private static List<Kingdom> GetAvailableHelperKingdoms(Kingdom petitioner, Kingdom enemy)
        {
            if (!IsValidKingdom(petitioner) || !IsValidKingdom(enemy))
                return new List<Kingdom>();

            return Kingdom.All
                .Where(IsValidKingdom)
                .Where(helper => helper != petitioner && helper != enemy)
                .Where(helper => !FactionManager.IsAtWarAgainstFaction(helper, petitioner))
                .Where(helper => CalculateAidScore(petitioner, helper, enemy) >= 40)
                .OrderByDescending(helper => CalculateAidScore(petitioner, helper, enemy))
                .ToList();
        }

        private static Kingdom GetStrongestEnemy(Kingdom kingdom)
        {
            return kingdom?.FactionsAtWarWith?
                .OfType<Kingdom>()
                .Where(IsValidKingdom)
                .OrderByDescending(enemy => enemy.CurrentTotalStrength)
                .FirstOrDefault();
        }

        private static float GetPressureRatio(Kingdom weak, Kingdom enemy)
        {
            float weakStrength = Math.Max(1f, weak?.CurrentTotalStrength ?? 1f);
            float enemyStrength = Math.Max(1f, enemy?.CurrentTotalStrength ?? 1f);
            return enemyStrength / weakStrength;
        }

        private static Kingdom GetPlayerKingdom()
        {
            return Hero.MainHero?.Clan?.Kingdom;
        }

        private static bool IsValidAidRequest(MilitaryAidRequest request)
        {
            return request != null
                && IsValidKingdom(request.Petitioner)
                && IsValidKingdom(request.Helper)
                && IsValidKingdom(request.Enemy)
                && request.Petitioner != request.Helper
                && request.Petitioner != request.Enemy
                && request.Helper != request.Enemy
                && FactionManager.IsAtWarAgainstFaction(request.Petitioner, request.Enemy)
                && !FactionManager.IsAtWarAgainstFaction(request.Helper, request.Petitioner);
        }

        private static bool IsValidKingdom(Kingdom kingdom)
        {
            return kingdom != null
                && !kingdom.IsEliminated
                && kingdom.Leader != null;
        }

        private static bool IsOnCooldown(Dictionary<string, CampaignTime> cooldowns, string key, float days)
        {
            if (cooldowns == null || string.IsNullOrWhiteSpace(key))
                return true;

            return cooldowns.TryGetValue(key, out CampaignTime last)
                && (CampaignTime.Now - last).ToDays < days;
        }

        private static string MakePairKey(Kingdom petitioner, Kingdom enemy)
        {
            return $"{petitioner?.StringId ?? "null"}:{enemy?.StringId ?? "null"}";
        }

        private static string MakeTripleKey(Kingdom first, Kingdom second, Kingdom third)
        {
            string[] pair = { first?.StringId ?? "null", second?.StringId ?? "null" };
            Array.Sort(pair, StringComparer.OrdinalIgnoreCase);
            return $"{pair[0]}:{pair[1]}:{third?.StringId ?? "null"}";
        }

        private static bool SharesFrontier(Kingdom left, Kingdom right)
        {
            if (left == null || right == null)
                return false;

            foreach (Town leftFief in left.Fiefs)
            {
                if (leftFief?.Settlement == null)
                    continue;

                foreach (Town rightFief in right.Fiefs)
                {
                    if (rightFief?.Settlement == null)
                        continue;

                    float distance = leftFief.Settlement.GetPosition2D.Distance(rightFief.Settlement.GetPosition2D);
                    if (distance < 90f)
                        return true;
                }
            }

            return false;
        }

        private static void ChangeRelation(Hero left, Hero right, int change)
        {
            if (left == null || right == null || left == right || change == 0)
                return;

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(left, right, change, false);
        }
    }

    public class MilitaryAidRequest
    {
        [SaveableField(1)] private Kingdom _petitioner;
        [SaveableField(2)] private Kingdom _helper;
        [SaveableField(3)] private Kingdom _enemy;
        [SaveableField(4)] private CampaignTime _createdAt;
        [SaveableField(5)] private CampaignTime _responseAt;
        [SaveableField(6)] private bool _isPlayerRequest;
        [SaveableField(7)] private int _goldDemand;
        [SaveableField(8)] private string _reason;

        public Kingdom Petitioner => _petitioner;
        public Kingdom Helper => _helper;
        public Kingdom Enemy => _enemy;
        public CampaignTime ResponseAt => _responseAt;
        public bool IsPlayerRequest => _isPlayerRequest;
        public int GoldDemand => _goldDemand;

        private MilitaryAidRequest()
        {
        }

        public MilitaryAidRequest(
            Kingdom petitioner,
            Kingdom helper,
            Kingdom enemy,
            CampaignTime createdAt,
            CampaignTime responseAt,
            bool isPlayerRequest,
            int goldDemand,
            string reason)
        {
            _petitioner = petitioner;
            _helper = helper;
            _enemy = enemy;
            _createdAt = createdAt;
            _responseAt = responseAt;
            _isPlayerRequest = isPlayerRequest;
            _goldDemand = goldDemand;
            _reason = reason;
        }
    }

    public class SecretWarCompactRequest
    {
        [SaveableField(1)] private Kingdom _instigator;
        [SaveableField(2)] private Kingdom _ally;
        [SaveableField(3)] private Kingdom _target;
        [SaveableField(4)] private Settlement _promisedSettlement;
        [SaveableField(5)] private CampaignTime _createdAt;
        [SaveableField(6)] private CampaignTime _triggerAt;
        [SaveableField(7)] private bool _isPlayerRequest;
        [SaveableField(8)] private int _goldDemand;
        [SaveableField(9)] private bool _isTriggered;
        [SaveableField(10)] private int _capturesByInstigator;
        [SaveableField(11)] private int _capturesByAlly;

        public Kingdom Instigator => _instigator;
        public Kingdom Ally => _ally;
        public Kingdom Target => _target;
        public Settlement PromisedSettlement => _promisedSettlement;
        public CampaignTime CreatedAt => _createdAt;
        public CampaignTime TriggerAt => _triggerAt;
        public bool IsPlayerRequest => _isPlayerRequest;
        public int GoldDemand => _goldDemand;
        public bool IsTriggered => _isTriggered;
        public int CapturesByInstigator => _capturesByInstigator;
        public int CapturesByAlly => _capturesByAlly;

        private SecretWarCompactRequest()
        {
        }

        public SecretWarCompactRequest(
            Kingdom instigator,
            Kingdom ally,
            Kingdom target,
            Settlement promisedSettlement,
            CampaignTime createdAt,
            CampaignTime triggerAt,
            bool isPlayerRequest,
            int goldDemand,
            bool isTriggered,
            int capturesByInstigator,
            int capturesByAlly)
        {
            _instigator = instigator;
            _ally = ally;
            _target = target;
            _promisedSettlement = promisedSettlement;
            _createdAt = createdAt;
            _triggerAt = triggerAt;
            _isPlayerRequest = isPlayerRequest;
            _goldDemand = goldDemand;
            _isTriggered = isTriggered;
            _capturesByInstigator = capturesByInstigator;
            _capturesByAlly = capturesByAlly;
        }

        public void MarkTriggered()
        {
            _isTriggered = true;
        }

        public void RegisterCapture(Kingdom capturer)
        {
            if (capturer == _instigator)
            {
                _capturesByInstigator++;
            }
            else if (capturer == _ally)
            {
                _capturesByAlly++;
            }
        }
    }
}
