using RealmsForgotten.RFReligions.Behavior;
using RealmsForgotten.RFReligions.Helper;
using RF_warsystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RFReligions.Behavior
{
    public class CrusadeBehavior : CampaignBehaviorBase
    {
        private const int CrusadeCheckFrequencyInDays = 30;
        private const int CrusadeDurationInDays = 365;
        private const int MinPietyToCallCrusade = 80;
        private const int CooldownBetweenCrusadesInDays = 730;

        private bool _crusadeActive;
        private CampaignTime _crusadeEndTime;
        private CampaignTime _crusadeCooldownEndTime;
        private Kingdom _crusadeLeader;
        private Kingdom _crusadeTarget;
        private Settlement _crusadeTargetSettlement;
        // FIX: The type 'RFReligions' is now fully qualified to prevent all namespace errors (CS0118).
        private RealmsForgotten.RFReligions.Core.RFReligions _crusadeReligion;
        private List<Kingdom> _crusaderFactions;

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoad);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_crusadeActive", ref _crusadeActive);
            dataStore.SyncData("_crusadeEndTime", ref _crusadeEndTime);
            dataStore.SyncData("_crusadeCooldownEndTime", ref _crusadeCooldownEndTime);
            dataStore.SyncData("_crusadeLeader", ref _crusadeLeader);
            dataStore.SyncData("_crusadeTarget", ref _crusadeTarget);
            dataStore.SyncData("_crusadeTargetSettlement", ref _crusadeTargetSettlement);
            dataStore.SyncData("_crusadeReligion", ref _crusadeReligion);
            dataStore.SyncData("_crusaderFactions", ref _crusaderFactions);
        }

        private void OnGameLoad(CampaignGameStarter starter)
        {
            if (_crusaderFactions == null)
            {
                _crusaderFactions = new List<Kingdom>();
            }
        }

        private void OnDailyTick()
        {
            if (ReligionBehavior.Instance == null) return;

            if (_crusadeActive)
            {
                ReinforceCrusadeIntent();

                if (CampaignTime.Now >= _crusadeEndTime)
                {
                    FailCrusade("The crusade has ended in failure, its allotted time having expired.");
                }
                return;
            }

            if (CampaignTime.Now < _crusadeCooldownEndTime)
            {
                return;
            }

            if ((int)CampaignTime.Now.ToDays % CrusadeCheckFrequencyInDays == 0)
            {
                TryToStartCrusade();
            }
        }

        private void TryToStartCrusade()
        {
            foreach (var potentialLeaderKingdom in Kingdom.All.Where(k => !k.IsMinorFaction && k.Leader != null))
            {
                if (!ReligionBehavior.Instance._heroes.TryGetValue(potentialLeaderKingdom.Leader, out var leaderReligionModel))
                    continue;

                if (leaderReligionModel.GetDevotionToCurrentReligion() < MinPietyToCallCrusade)
                    continue;

                foreach (var potentialTargetKingdom in Kingdom.All.Where(k => !k.IsMinorFaction && k.Leader != null && k != potentialLeaderKingdom))
                {
                    if (!ReligionBehavior.Instance._heroes.TryGetValue(potentialTargetKingdom.Leader, out var targetLeaderReligionModel))
                        continue;

                    // FIX: Arguments now match the corrected method signature, preventing CS1503.
                    if (!AreReligionsIntolerant(leaderReligionModel.Religion, targetLeaderReligionModel.Religion))
                        continue;

                    var targetSettlement = potentialTargetKingdom.Settlements.FirstOrDefault(s =>
                        s.IsTown && ReligionMapHelper.GetCultureReligion(s.Culture.StringId) == leaderReligionModel.Religion);

                    if (targetSettlement != null)
                    {
                        StartCrusade(potentialLeaderKingdom, potentialTargetKingdom, targetSettlement);
                        return;
                    }
                }
            }
        }

        private void StartCrusade(Kingdom leader, Kingdom target, Settlement targetSettlement)
        {
            _crusadeActive = true;
            _crusadeLeader = leader;
            _crusadeTarget = target;
            _crusadeTargetSettlement = targetSettlement;
            _crusadeReligion = ReligionBehavior.Instance._heroes[_crusadeLeader.Leader].Religion;
            _crusadeEndTime = CampaignTime.DaysFromNow(CrusadeDurationInDays);

            _crusaderFactions = new List<Kingdom> { _crusadeLeader };
            ReinforceCrusadeIntent();

            TextObject title = new TextObject("A Crusade has been Called!", null);
            TextObject message = new TextObject("{LEADER_KINGDOM} has called a Holy Crusade against {TARGET_KINGDOM} to reclaim the holy city of {TARGET_SETTLEMENT}! All followers of {RELIGION} are called to arms!", null);
            message.SetTextVariable("LEADER_KINGDOM", _crusadeLeader.Name);
            message.SetTextVariable("TARGET_KINGDOM", _crusadeTarget.Name);
            message.SetTextVariable("TARGET_SETTLEMENT", _crusadeTargetSettlement.Name);
            message.SetTextVariable("RELIGION", ReligionUIHelper.GetReligionName(_crusadeReligion));
            InformationManager.ShowInquiry(new InquiryData(title.ToString(), message.ToString(), true, false, "Close", "", null, null), true);

            CallToArms();
        }

        private void CallToArms()
        {
            if (Hero.MainHero.MapFaction is Kingdom playerKingdom && playerKingdom != _crusadeLeader && playerKingdom != _crusadeTarget)
            {
                if (ReligionBehavior.Instance._heroes.TryGetValue(Hero.MainHero, out var playerReligion) && playerReligion.Religion == _crusadeReligion)
                {
                    ShowPlayerCrusadeInquiry();
                }
            }

            foreach (var kingdom in GetEligibleCrusaderKingdoms())
            {
                JoinCrusade(kingdom);
            }
        }

        private void ReinforceCrusadeIntent()
        {
            if (!_crusadeActive || _crusadeLeader == null || _crusadeTarget == null || _crusadeTargetSettlement == null)
            {
                return;
            }

            RFWarExternalIntentApi.ReinforceHolyWar(_crusadeLeader, _crusadeTarget, _crusadeTargetSettlement, _crusaderFactions);
        }

        private void ShowPlayerCrusadeInquiry()
        {
            TextObject title = new TextObject("A Call to Arms!", null);
            TextObject message = new TextObject("{LEADER_KINGDOM} calls upon you to join their Holy Crusade against {TARGET_KINGDOM}. Will you answer the call and help reclaim {TARGET_SETTLEMENT} for the glory of {RELIGION}?", null);
            message.SetTextVariable("LEADER_KINGDOM", _crusadeLeader.Name);
            message.SetTextVariable("TARGET_KINGDOM", _crusadeTarget.Name);
            message.SetTextVariable("TARGET_SETTLEMENT", _crusadeTargetSettlement.Name);
            message.SetTextVariable("RELIGION", ReligionUIHelper.GetReligionName(_crusadeReligion));

            InformationManager.ShowInquiry(new InquiryData(title.ToString(), message.ToString(), true, true, "Join the Crusade", "Decline",
                () => { JoinCrusade(Hero.MainHero.MapFaction as Kingdom); },
                () => {
                    // ✅ Enhanced decline penalties
                    InformationManager.DisplayMessage(new InformationMessage("You have declined the call to war. Your faith and honor are questioned.", Colors.Yellow));

                    // Primary penalty: Crusade leader
                    ChangeRelationAction.ApplyPlayerRelation(_crusadeLeader.Leader, -10, true, true);

                    // Penalty with all same-religion kingdoms
                    foreach (var kingdom in Kingdom.All.Where(k => k.Leader != null && k != Hero.MainHero.MapFaction))
                    {
                        if (ReligionBehavior.Instance._heroes.TryGetValue(kingdom.Leader, out var kingReligion)
                            && kingReligion.Religion == _crusadeReligion)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(kingdom.Leader, -5, true, true);
                        }
                    }

                    // Small bonus with target kingdom (they appreciate you not attacking)
                    if (_crusadeTarget?.Leader != null)
                    {
                        ChangeRelationAction.ApplyPlayerRelation(_crusadeTarget.Leader, 5, true, true);
                    }

                    // Devotion penalty (increases with Honor trait)
                    if (ReligionBehavior.Instance._heroes.TryGetValue(Hero.MainHero, out var playerReligionModel))
                    {
                        int honorLevel = Hero.MainHero.GetTraitLevel(DefaultTraits.Honor);
                        float devotionPenalty = -5f - (honorLevel * 2f); // -5 base, -2 per honor level
                        playerReligionModel.AddDevotion(devotionPenalty, Hero.MainHero);

                        if (honorLevel > 0)
                        {
                            InformationManager.DisplayMessage(
                                new InformationMessage($"Your honor suffers for refusing the sacred call (Devotion -{Math.Abs(devotionPenalty)}).", Colors.Red));
                        }
                    }
                }), true);
        }

        private void JoinCrusade(Kingdom kingdom)
        {
            if (kingdom == null || _crusaderFactions.Contains(kingdom)) return;

            _crusaderFactions.Add(kingdom);

            // FIX: Method expects Hero objects. Now correctly passing the .Leader property to prevent CS1503.
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(kingdom.Leader, _crusadeLeader.Leader, 20, true);
            foreach (var crusader in _crusaderFactions.Where(f => f != kingdom))
            {
                // FIX: Method expects Hero objects. Now correctly passing the .Leader property to prevent CS1503.
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(kingdom.Leader, crusader.Leader, 10, true);
            }

            TextObject message = new TextObject("{KINGDOM} has joined the Holy Crusade!", null);
            message.SetTextVariable("KINGDOM", kingdom.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (_crusadeActive && settlement == _crusadeTargetSettlement)
            {
                if (newOwner.MapFaction != null && _crusaderFactions.Contains(newOwner.MapFaction as Kingdom))
                {
                    SucceedCrusade(newOwner.MapFaction as Kingdom);
                }
            }
        }

        private void SucceedCrusade(Kingdom conqueringKingdom)
        {
            TextObject message = new TextObject("The Crusade is Victorious! {TARGET_SETTLEMENT} has been reclaimed by {CONQUEROR}! All participants are rewarded for their faith!", null);
            message.SetTextVariable("TARGET_SETTLEMENT", _crusadeTargetSettlement.Name);
            message.SetTextVariable("CONQUEROR", conqueringKingdom.Name);
            InformationManager.ShowInquiry(new InquiryData("Crusade Victorious!", message.ToString(), true, false, "Huzzah!", "", null, null), true);

          
            foreach (var kingdom in _crusaderFactions)
            {
                kingdom.Leader.Clan.AddRenown(200);
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(kingdom.Leader, _crusadeLeader.Leader, 20, true);

               
                Hero participantLeader = kingdom.Leader;
                if (ReligionBehavior.Instance._heroes.TryGetValue(participantLeader, out var religionModel))
                {
                    religionModel.AddDevotion(10f, participantLeader);
                }
            }

         
            Hero conquerorLeader = conqueringKingdom.Leader; // Usando uma variável explícita para o Herói
            conquerorLeader.Clan.AddRenown(100);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(conquerorLeader, _crusadeLeader.Leader, 10, true);

           
            if (ReligionBehavior.Instance._heroes.TryGetValue(conquerorLeader, out var conquerorReligionModel))
            {
                conquerorReligionModel.AddDevotion(15f, conquerorLeader);
            }

           
            if (_crusadeLeader != conqueringKingdom)
            {
                Hero crusadeLeaderHero = _crusadeLeader.Leader; 
                if (ReligionBehavior.Instance._heroes.TryGetValue(crusadeLeaderHero, out var leaderReligionModel))
                {
                    leaderReligionModel.AddDevotion(10f, crusadeLeaderHero);
                }
            }

            EndCrusade();
        }

        private void FailCrusade(string reason)
        {
            TextObject message = new TextObject("The Crusade has failed! {REASON}", null);
            message.SetTextVariable("REASON", reason);
            InformationManager.ShowInquiry(new InquiryData("Crusade Failed", message.ToString(), true, false, "A shameful day...", "", null, null), true);

            foreach (var kingdom in _crusaderFactions)
            {
                kingdom.Leader.Clan.AddRenown(-50);
            }

            EndCrusade();
        }

        private void EndCrusade()
        {
            _crusadeActive = false;
            _crusaderFactions.Clear();
            _crusadeCooldownEndTime = CampaignTime.DaysFromNow(CooldownBetweenCrusadesInDays);
        }

        private IEnumerable<Kingdom> GetEligibleCrusaderKingdoms()
        {
            return Kingdom.All.Where(kingdom =>
                kingdom != _crusadeLeader &&
                kingdom != _crusadeTarget &&
                !kingdom.IsMinorFaction &&
                kingdom.Leader != null &&
                HasCrusadeReligion(kingdom));
        }

        private bool HasCrusadeReligion(Kingdom kingdom)
        {
            return TryGetKingdomReligion(kingdom, out var kingdomReligion) && kingdomReligion == _crusadeReligion;
        }

        private bool TryGetKingdomReligion(Kingdom kingdom, out RealmsForgotten.RFReligions.Core.RFReligions religion)
        {
            religion = RealmsForgotten.RFReligions.Core.RFReligions.None;

            if (kingdom?.Leader == null)
            {
                return false;
            }

            if (ReligionBehavior.Instance._heroes.TryGetValue(kingdom.Leader, out var religionModel))
            {
                religion = religionModel.Religion;
                return true;
            }

            if (kingdom.Leader.Culture == null)
            {
                return false;
            }

            religion = ReligionMapHelper.GetCultureReligion(kingdom.Leader.Culture.StringId);
            return true;
        }

        private bool AreReligionsIntolerant(RealmsForgotten.RFReligions.Core.RFReligions religion1, RealmsForgotten.RFReligions.Core.RFReligions religion2)
        {
            if (religion1 == religion2)
                return false;

            bool religion1Tolerates2 = false;
            // FIX: Type name is fully qualified here.
            if (ReligionLogicHelper.TolerableReligions.TryGetValue(religion1, out RealmsForgotten.RFReligions.Core.RFReligions toleratedBy1))
            {
                // FIX: Enum member access is fully qualified to prevent CS0234.
                if (toleratedBy1 == RealmsForgotten.RFReligions.Core.RFReligions.All || toleratedBy1 == religion2)
                {
                    religion1Tolerates2 = true;
                }
            }

            bool religion2Tolerates1 = false;
            // FIX: Type name is fully qualified here.
            if (ReligionLogicHelper.TolerableReligions.TryGetValue(religion2, out RealmsForgotten.RFReligions.Core.RFReligions toleratedBy2))
            {
                // FIX: Enum member access is fully qualified to prevent CS0234.
                if (toleratedBy2 == RealmsForgotten.RFReligions.Core.RFReligions.All || toleratedBy2 == religion1)
                {
                    religion2Tolerates1 = true;
                }
            }

            if (religion1Tolerates2 || religion2Tolerates1)
                return false;

            return true;
        }
    }
}
