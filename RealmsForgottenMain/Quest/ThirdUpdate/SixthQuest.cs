using RealmsForgotten.Quest.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;

namespace RealmsForgotten.Quest.SecondUpdate
{
    public record TroopDetail(string TroopId, int Quantity);
    public record DemonLord(string CharacterId, string ClanId, List<TroopDetail> TroopDetails, Settlement SpawnSettlement);

    internal class SixthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog? declareWorldPeaceLog;
        [SaveableField(1)]
        private JournalLog? talkToLordLog;
        [SaveableField(2)]
        private JournalLog? defeatDemonLordPartiesLog;
        private bool isObjectiveCompleted => defeatDemonLordPartiesLog?.CurrentProgress >= demonLordPartiesToDefeatTarget;

        private const int demonLordPartiesToDefeatTarget = 2;
        private Hero TheOwl => Hero.AllAliveHeroes.FirstOrDefault(hero => hero.StringId == "rf_the_owl");
        private Hero Lord2_1 => Hero.AllAliveHeroes.FirstOrDefault(hero => hero.StringId == "lord_2_1");

        public SixthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold) { }

        public override TextObject Title => GameTexts.FindText("rf_sixth_quest_title");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => true;
        //public static SixthQuest Instance { get; private set; }
        static Dictionary<string, DemonLord>? _demonLords;
        static Dictionary<string, DemonLord> DemonLords 
        { 
            get
            {
                _demonLords ??= new()
                    {
                        ["cs_nurh_raiders"] = new DemonLord("cs_nurh_raiders_boss", "cs_nurh_raiders", new List<TroopDetail>
                            {
                                new TroopDetail("cs_nurh_raiders_bandit", 500),
                                new TroopDetail("cs_nurh_raiders_raider", 250),
                                new TroopDetail("cs_nurh_raiders_chief", 50)
                        }, Settlement.FindFirst(settlement => settlement.StringId == "town_EN1")),
                        ["cs_daimo_raiders"] =
                        new DemonLord("cs_daimo_raiders_boss", "cs_daimo_raiders", new List<TroopDetail>
                            {
                                new TroopDetail("cs_daimo_raiders_bandit", 500),
                                new TroopDetail("cs_daimo_raiders_raider", 250),
                                new TroopDetail("cs_daimo_raiders_chief", 50)
                        }, Settlement.FindFirst(settlement => settlement.StringId == "town_B3")),
                        ["cs_bark_raiders"] =
                        new DemonLord("cs_bark_raiders_boss", "cs_bark_raiders", new List<TroopDetail>
                            {
                                new TroopDetail("cs_bark_raiders_bandit", 500),
                                new TroopDetail("cs_bark_raiders_raider", 250),
                                new TroopDetail("cs_bark_raiders_chief", 50)
                        }, Settlement.FindFirst(settlement => settlement.StringId == "town_V5")),
                        ["cs_sillok_raiders"] =
                        new DemonLord("cs_sillok_raiders_boss", "cs_sillok_raiders", new List<TroopDetail>
                            {
                                new TroopDetail("cs_sillok_raiders_bandit", 500),
                                new TroopDetail("cs_sillok_raiders_raider", 250),
                                new TroopDetail("cs_sillok_raiders_chief", 50)
                        }, Settlement.FindFirst(settlement => settlement.StringId == "town_K2"))
                };
                return _demonLords;
            } 
        }
        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(DeclareWorldPeaceDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TalkToLordDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TalkToLordDialogOwl, this);
            Campaign.Current.ConversationManager.AddDialogFlow(AfterDemonLordsDefeatedDialogOwl, this);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
        }
        protected override void HourlyTick()
        {
        }
        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();
            ShowFirstNotification();
        }

        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyer)
        {
            CountDemonLordPartyDefeat(mobileParty, destroyer);
        }

        private void CountDemonLordPartyDefeat(MobileParty mobileParty, PartyBase destroyer)
        {
            if (!DemonLords.Any(l => l.Value.ClanId.Contains(mobileParty.StringId))) return;
            if (destroyer.LeaderHero != null && destroyer.LeaderHero == Hero.MainHero)
            {
                defeatDemonLordPartiesLog?.UpdateCurrentProgress(defeatDemonLordPartiesLog.CurrentProgress + 1);
                if (isObjectiveCompleted)
                {
                    ShowDemonLordsDefeatedNotification();
                    CampaignEvents.DailyTickEvent.ClearListeners(this);
                }
            }
            else
            {
                DemonLord lord = DemonLords[mobileParty.StringId];
                CreateDemonLordParty(lord.CharacterId, lord.ClanId, lord.SpawnSettlement, lord.TroopDetails);
            }
        }
        private void ShowFirstNotification()
        {
            QuestUIManager.ShowNotification(
                "Huge armies of devils started roaming throgh the kingdoms. Reports of attacks from those hordes have been amassing everyday from all corners of Aeurth...",
                ShowSecondNotification,
                true,
                "demonic_horde");
        }

        private void ShowSecondNotification()
        {
            QuestUIManager.ShowNotification(
                "The kings and Queen of Men, the High King of the Elveans, The AllKhuur Khan and the Nasorian Ulmir came into terms of peace, to be able to face this new treat. ",
                ShowThirdNotification,
                true,
                "king_council");
        }

        private void ShowThirdNotification()
        {
            QuestUIManager.ShowNotification(
                "You are not alone in your war against this evil anymore. The world finally listened to your call.",
                DeclareWorldPeace,
                true,
                "riders_against_sun");
        }


        private void DeclareWorldPeace()
        {
            declareWorldPeaceLog = AddLog(GameTexts.FindText("rf_sixth_quest_first_objective"));
            declareWorldPeaceLog.UpdateCurrentProgress(1);

            SixthQuestBehaviour.DeletePopupVMLayer();


            // Declare peace between factions
            DeclarePeaceBetweenFactions(Kingdom.All.FirstOrDefault(k => k.StringId == "empire"),
                                        Kingdom.All.FirstOrDefault(k => k.StringId == "battania"));
            DeclarePeaceBetweenFactions(Kingdom.All.FirstOrDefault(k => k.StringId == "empire"),
                                        Kingdom.All.FirstOrDefault(k => k.StringId == "vlandia"));
            DeclarePeaceBetweenFactions(Kingdom.All.FirstOrDefault(k => k.StringId == "empire"),
                                        Kingdom.All.FirstOrDefault(k => k.StringId == "khuzait"));
            DeclarePeaceBetweenFactions(Kingdom.All.FirstOrDefault(k => k.StringId == "battania"),
                                        Kingdom.All.FirstOrDefault(k => k.StringId == "vlandia"));
            DeclarePeaceBetweenFactions(Kingdom.All.FirstOrDefault(k => k.StringId == "battania"),
                                        Kingdom.All.FirstOrDefault(k => k.StringId == "khuzait"));
            DeclarePeaceBetweenFactions(Kingdom.All.FirstOrDefault(k => k.StringId == "vlandia"),
                                        Kingdom.All.FirstOrDefault(k => k.StringId == "khuzait"));

            // Start the world peace timer
            StartOwlConversation();
        }
        private void DeclarePeaceBetweenFactions(Kingdom faction1, Kingdom faction2)
        {
            if (faction1 != null && faction2 != null && FactionManager.IsAtWarAgainstFaction(faction1, faction2))
            {
                MakePeaceAction.Apply(faction1, faction2);
                InformationManager.DisplayMessage(new InformationMessage($"{faction1.Name} and {faction2.Name} have declared peace."));
            }
        }

        private void SpawnDemonLords()
        {
            foreach(DemonLord lord in DemonLords.Values)
                CreateDemonLordParty(lord.CharacterId, lord.ClanId, lord.SpawnSettlement, lord.TroopDetails);
        }

        private void CreateDemonLordParty(string demonLordId, string clanId, Settlement nearTown, List<TroopDetail> troopDetails)
        {
            try
            {
                Clan clan = Clan.FindFirst(x => x.StringId == clanId) ?? throw new Exception($"Clan with ID {clanId} not found.");
                MobileParty party = BanditPartyComponent.CreateBanditParty(clanId, clan, null, true) ?? throw new Exception($"Failed to create party for demon lord {demonLordId}.");
                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                Dictionary<CharacterObject, int> initialTroops = new Dictionary<CharacterObject, int>();
                CharacterObject character = CharacterObject.Find(demonLordId) ?? throw new Exception($"lord with id {demonLordId} not found");
                troopRoster.AddToCounts(character, 1);
                initialTroops[character] = 1;
                
                foreach (var troopDetail in troopDetails)
                {
                    CharacterObject troop = CharacterObject.Find(troopDetail.TroopId);
                    if (troop == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Troop with ID {troopDetail.TroopId} not found."));
                        continue;
                    }
                    troopRoster.AddToCounts(troop, troopDetail.Quantity);
                    initialTroops[troop] = troopDetail.Quantity;
                }
                party.InitializeMobilePartyAroundPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), nearTown.Position2D, 50f, 10f);
                party.SetCustomName(new TextObject($"Demon Lord {character.Name} Party"));
                party.Aggressiveness = 10f;
                party.Ai.SetMovePatrolAroundPoint(nearTown.Position2D);
                
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, () =>
                {
                    if (party != null && party.IsActive)
                    {
                        //EngageNearbyEnemies(party);
                        Settlement nearestSettlement = SettlementHelper.FindNearestSettlement(party.Position2D);
                        string nearestSettlementName = nearestSettlement != null ? nearestSettlement.Name.ToString() : "unknown settlement";
                        InformationManager.DisplayMessage(new InformationMessage($"You hear of an army from hell, laying ruin on the lands near the settlement of {nearestSettlementName}."));
                    }
                });
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Exception in RealmsForgotten.Quest.SecondUpdate.SixQuest.CreateDemonLordParty: {ex.Message}"));
            }
        }

        public static class SettlementHelper
        {
            public static Settlement FindNearestSettlement(Vec2 position)
            {
                Settlement nearestSettlement = null;
                float nearestDistance = float.MaxValue;

                foreach (Settlement settlement in Settlement.All)
                {
                    float distance = settlement.Position2D.Distance(position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestSettlement = settlement;
                    }
                }

                return nearestSettlement;
            }
        }

        private void EngageNearbyEnemies(MobileParty party)
        {
            // Get nearby enemy parties and set them as targets
            List<MobileParty> nearbyEnemyParties = MobileParty.All
                .Where(p => (p.IsLordParty || IsVillagerParty(p) || p.IsCaravan || p.IsBandit) && p.MapFaction.IsAtWarWith(party.MapFaction))
                .OrderBy(p => p.Position2D.DistanceSquared(party.Position2D))
                .ToList();

            if (nearbyEnemyParties.Count > 0)
            {
                MobileParty target = nearbyEnemyParties.First();
                party.Ai.SetMoveEngageParty(target);
            }
        }

        private bool IsVillagerParty(MobileParty party)
        {
            return party.PartyComponent is VillagerPartyComponent;
        }

        private void StartOwlConversation()
        {
            if (TheOwl != null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Starting conversation with the Owl"));
                Campaign.Current.ConversationManager.AddDialogFlow(DeclareWorldPeaceDialog, this);
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(TheOwl.CharacterObject)
                );
            }
        }

        private void StartOwlConversationResponse()
        {
            Hero owl = TheOwl;
            if (owl != null)
            {
                Campaign.Current.ConversationManager.AddDialogFlow(TalkToLordDialogOwl, this);
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(owl.CharacterObject)
                );
            }
        }

        private void StartOwlConversationLordsDefeated()
        {
            if (TheOwl != null)
            {
                Campaign.Current.ConversationManager.AddDialogFlow(AfterDemonLordsDefeatedDialogOwl, this);
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(TheOwl.CharacterObject)
                );
            }
        }

        private DialogFlow DeclareWorldPeaceDialog => DialogFlow.CreateDialogFlow("start", 125)
         .NpcLine(GameTexts.FindText("rf_sixth_quest_first_dialog_1"))
         .Condition(() => declareWorldPeaceLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == TheOwl)
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_first_dialog_2"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_first_dialog_3"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_first_dialog_4"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_first_dialog_5"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_first_dialog_6"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_first_dialog_7"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_first_dialog_8"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_first_dialog_9"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_first_dialog_10"))
         .Consequence(() =>
         {
             declareWorldPeaceLog?.UpdateCurrentProgress(2);
             talkToLordLog = AddLog(GameTexts.FindText("rf_sixth_quest_second_objective"));
             talkToLordLog.UpdateCurrentProgress(1);
         })
         .CloseDialog();

        private DialogFlow TalkToLordDialog => DialogFlow.CreateDialogFlow("start", 125)
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_1"))
        .Condition(() => talkToLordLog?.CurrentProgress == 1 && Hero.OneToOneConversationHero == Lord2_1)
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_2"))
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_3"))
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_4"))
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_5"))
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_6"))
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_7"))
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_8"))
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_9"))
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_10"))
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_11"))
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_12"))
        .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_13"))
        .PlayerLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_14"))
        .Consequence(() =>
        {
            talkToLordLog?.UpdateCurrentProgress(2);
            Kingdom sturgia = Kingdom.All.FirstOrDefault(k => k.StringId == "sturgia");
            Kingdom battania = Kingdom.All.FirstOrDefault(k => k.StringId == "battania");
            Kingdom vlandia = Kingdom.All.FirstOrDefault(k => k.StringId == "vlandia");
            Kingdom khuzait = Kingdom.All.FirstOrDefault(k => k.StringId == "khuzait");
            Kingdom empire = Kingdom.All.FirstOrDefault(k => k.StringId == "empire");
            DeclarePeaceBetweenFactions(sturgia, battania);
            DeclarePeaceBetweenFactions(sturgia, vlandia);
            DeclarePeaceBetweenFactions(sturgia, khuzait);
            DeclarePeaceBetweenFactions(sturgia, empire);

            List<(string troopId, int troopCount)> troopsToAdd = new List<(string troopId, int troopCount)>
            {
                ("druzhinnik_champion", 30),
                ("sturgia_veteran_warrior", 50),
                ("sturgian_veteran_bowman", 25)
            };

             // Call the method to add the troops
            GivePlayerTroops(troopsToAdd);

            StartOwlConversationResponse();
        })
        .CloseDialog();

        private DialogFlow TalkToLordDialogOwl => DialogFlow.CreateDialogFlow("start", 125)
         .NpcLine(GameTexts.FindText("rf_sixth_quest_second_dialog_1"))
         .Condition(() => talkToLordLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == TheOwl && defeatDemonLordPartiesLog == null)
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_second_dialog_2"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_second_dialog_3"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_second_dialog_4"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_second_dialog_5"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_second_dialog_6"))
         .Consequence(() =>
         {
             //StartHuntDemonLordsObjective();
             defeatDemonLordPartiesLog = AddDiscreteLog(GameTexts.FindText("rf_sixth_quest_defeat_demon_lord_parties_log"), GameTexts.FindText("rf_sixth_quest_defeat_demon_lord_parties_task"), 0, demonLordPartiesToDefeatTarget);

             SpawnDemonLords();
         })
         .CloseDialog();

        private DialogFlow AfterDemonLordsDefeatedDialogOwl => DialogFlow.CreateDialogFlow("start", 125)
           .NpcLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_1"))
           .Condition(() => isObjectiveCompleted && Hero.OneToOneConversationHero == TheOwl)
           .PlayerLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_2"))
           .NpcLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_3"))
           .PlayerLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_4"))
           .NpcLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_5"))
           .Consequence(() =>
           {
               CompleteQuestWithSuccess();
           })
           .CloseDialog();

        private void GivePlayerTroops(List<(string troopId, int troopCount)> troops)
        {
            foreach (var troopInfo in troops)
            {
                CharacterObject troop = CharacterObject.Find(troopInfo.troopId);
                if (troop != null)
                {
                    MobileParty.MainParty.AddElementToMemberRoster(troop, troopInfo.troopCount);
                    InformationManager.DisplayMessage(new InformationMessage($"You have received {troopInfo.troopCount} {troop.Name}."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Troop with ID {troopInfo.troopId} not found."));
                }
            }
        }

        private void ShowDemonLordsDefeatedNotification()
        {
            string title = "You have defeated the Demon Lords.";
            string description = "You have defeated the Demon Lords, but hundreds of devils armies still roam the land.It seems the source of this evil were not the demon lords themselves, mas something else.";
            InformationManager.ShowInquiry(new InquiryData(title, description, true, false, "Continue", null, null, null));
            StartOwlConversationLordsDefeated();
        }
    }

    public class SixthQuestBehaviour : CampaignBehaviorBase
    {
        private static GauntletLayer _gauntletLayer;
        private static GauntletMovie _gauntletMovie;
        private static SixthQuestPopupVM _popupVM;

        public override void RegisterEvents()
        {
        }

        public override void SyncData(IDataStore dataStore)
        {
            //demonLordPartiesDefeatedCount
        }
        public static void CreatePopupVMLayer(string title, string description, string spriteName, Action continueAction, Action declineAction, string buttonLabel)
        {
            if (_gauntletLayer == null)
            {
                _gauntletLayer = new GauntletLayer(1000, "GauntletLayer", false);
            }

            if (_popupVM == null)
            {
                _popupVM = new SixthQuestPopupVM(title, description, spriteName, continueAction, declineAction, buttonLabel);
            }
            else
            {
                _popupVM.UpdatePopup(title, description, spriteName, continueAction, declineAction, buttonLabel);
            }

            _gauntletMovie = (GauntletMovie)_gauntletLayer.LoadMovie("SixthQuestPopup", _popupVM);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            ScreenManager.TopScreen.AddLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_gauntletLayer);
            _popupVM.Refresh();
        }

        public static void DeletePopupVMLayer()
        {
            if (_gauntletLayer != null)
            {
                _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                _gauntletLayer.IsFocusLayer = false;
                if (_gauntletMovie != null)
                {
                    _gauntletLayer.ReleaseMovie(_gauntletMovie);
                }
                ScreenManager.TopScreen.RemoveLayer(_gauntletLayer);
            }
            _gauntletLayer = null;
            _gauntletMovie = null;
            _popupVM = null;
        }
    }
}