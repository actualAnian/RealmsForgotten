using RealmsForgotten.Quest.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.ScreenSystem;
using static HarmonyLib.Code;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ObjectSystem;
using Helpers;

namespace RealmsForgotten.Quest.SecondUpdate
{
    internal class SixthQuest : QuestBase
    {
        [SaveableField(0)]
        private JournalLog declareWorldPeaceLog;
        [SaveableField(1)]
        private JournalLog talkToLordLog;
        //[SaveableField(2)]
        //private JournalLog huntDemonLordsLog;
        [SaveableField(3)]
        private CampaignTime worldPeaceStartTime;
        [SaveableField(4)]
        private bool isWorldPeaceActive;
        //[SaveableField(5)]
        //private int demonLordPartiesDefeatedCount = 0;
        [SaveableField(6)]
        private JournalLog defeatDemonLordPartiesLog;
        private bool isObjectiveCompleted => defeatDemonLordPartiesLog?.CurrentProgress >= demonLordPartiesToDefeatTarget;

        private const int demonLordPartiesToDefeatTarget = 2;
        private Hero TheOwl => Hero.AllAliveHeroes.FirstOrDefault(hero => hero.StringId == "rf_the_owl");
        private Hero Lord2_1 => Hero.AllAliveHeroes.FirstOrDefault(hero => hero.StringId == "lord_2_1");

        private Dictionary<string, string> demonLordsIds;
        private Dictionary<string, bool> demonLordsDefeated;

        public SixthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold) {}

        public override TextObject Title => GameTexts.FindText("rf_sixth_quest_title");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => true;
        public static SixthQuest Instance { get; private set; }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(DeclareWorldPeaceDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TalkToLordDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(TalkToLordDialogOwl, this);
            //Campaign.Current.ConversationManager.AddDialogFlow(AfterDemonLordsDefeatedDialogOwl, this);
        }

        protected override void InitializeQuestOnGameLoad()
        {
            // Generate demon lord IDs if not already generated
            if (demonLordsIds == null || demonLordsIds.Count == 0)
            {
                demonLordsIds = GenerateDemonLordIds();
            }

            // Initialize the demonLordsDefeated dictionary if not already initialized
            if (demonLordsDefeated == null || demonLordsDefeated.Count == 0)
            {
                demonLordsDefeated = new Dictionary<string, bool>();
                foreach (var id in demonLordsIds.Values)
                {
                    demonLordsDefeated[id] = false;
                }
            }
        }

        protected override void HourlyTick()
        {
            InformationManager.DisplayMessage(new InformationMessage("HourlyTick executed"));

            // Check if the world peace event duration has ended
            if (isWorldPeaceActive && CampaignTime.Now > worldPeaceStartTime + CampaignTime.Days(100))
            {
                InformationManager.DisplayMessage(new InformationMessage("World peace event duration ended"));
                EndWorldPeaceEvent();
            }
        }

        private Dictionary<string, string> GenerateDemonLordIds()
        {
            return new Dictionary<string, string>
        {
            { "cs_nurh_raiders_boss", "cs_nurh_raiders_boss_party" },
            { "cs_daimo_raiders_boss", "cs_daimo_raiders_boss_party" },
            { "cs_bark_raiders_boss", "cs_bark_raiders_boss_party" },
            { "cs_sillok_raiders_boss", "cs_sillok_raiders_boss_party" }
        };
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStart);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        }

        protected override void OnStartQuest()
        {
            declareWorldPeaceLog = AddLog(GameTexts.FindText("rf_sixth_quest_first_objective"));
            //huntDemonLordsLog = AddLog(GameTexts.FindText("rf_sixth_quest_third_objective"));
            talkToLordLog = AddLog(GameTexts.FindText("rf_sixth_quest_second_objective"));
            defeatDemonLordPartiesLog = AddDiscreteLog(GameTexts.FindText("rf_sixth_quest_defeat_demon_lord_parties_log"), GameTexts.FindText("rf_sixth_quest_defeat_demon_lord_parties_task"), 0, demonLordPartiesToDefeatTarget);
            demonLordsIds = GenerateDemonLordIds();
            demonLordsDefeated = demonLordsIds.Values.ToDictionary(id => id, id => false);

            SetDialogs();
            RegisterEvents();
        }

        private void OnTick(float dt)
        {
            // Check if the world peace event duration has ended
            if (isWorldPeaceActive && CampaignTime.Now > worldPeaceStartTime + CampaignTime.Days(100))
            {
                EndWorldPeaceEvent();
            }

            if (declareWorldPeaceLog?.CurrentProgress == 0 && isWorldPeaceActive)
            {
                {
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(TheOwl.CharacterObject));
                }
            }
        }

        private void OnMissionStart(IMission mission)
        {
            // Handle mission start events, if needed for the quest
        }

        private void OnDailyTick()
        {
            // Handle daily tick events, if needed for the quest
        }

        private void OnWeeklyTick()
        {
            // Handle weekly tick events, if needed for the quest
        }

        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyer)
        {

            CountDemonLordPartyDefeat(mobileParty, destroyer);
        }

        private void CompleteDeclareWorldPeaceObjective()
        {
            declareWorldPeaceLog.UpdateCurrentProgress(1);
            InformationManager.DisplayMessage(new InformationMessage("Objective complete: Declared world peace."));
            StartTalkToLordObjective();
        }

        private void StartHuntDemonLordsObjective()
        {
            defeatDemonLordPartiesLog = AddDiscreteLog(GameTexts.FindText("rf_sixth_quest_defeat_demon_lord_parties_log"), GameTexts.FindText("rf_sixth_quest_defeat_demon_lord_parties_task"), 0, demonLordPartiesToDefeatTarget);
            InformationManager.DisplayMessage(new InformationMessage("New objective: Defeat 4 demon lord parties."));
            demonLordsDefeated = demonLordsIds.Values.ToDictionary(id => id, id => false);
            //huntDemonLordsLog.UpdateCurrentProgress(0);
        }

        private void CountDemonLordPartyDefeat(MobileParty mobileParty, PartyBase destroyer)
        {
            if (destroyer.LeaderHero != null && destroyer.LeaderHero == Hero.MainHero)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Checking party ID: {mobileParty.StringId}"));

                if (!string.IsNullOrEmpty(mobileParty.StringId) && demonLordsIds.Values.Contains(mobileParty.StringId))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Demon lord party defeated: {mobileParty.StringId}"));
                    defeatDemonLordPartiesLog.UpdateCurrentProgress(defeatDemonLordPartiesLog.CurrentProgress + 1);

                    if (demonLordsDefeated.ContainsKey(mobileParty.StringId))
                    {
                        demonLordsDefeated[mobileParty.StringId] = true;
                    }

                    InformationManager.DisplayMessage(new InformationMessage($"Updated progress: {defeatDemonLordPartiesLog.CurrentProgress}"));
                    CheckDemonLordsDefeated();
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Party {mobileParty.StringId} not counted as a demon lord party defeat."));
                }
            }
            else if (!string.IsNullOrEmpty(mobileParty.StringId) && demonLordsIds.Values.Contains(mobileParty.StringId))
            {
                // Respawn the demon lord if defeated by someone other than the main hero
                InformationManager.DisplayMessage(new InformationMessage($"Demon lord party {mobileParty.StringId} defeated by non-player party. Respawning..."));
                string demonLordId = demonLordsIds.FirstOrDefault(x => x.Value == mobileParty.StringId).Key;
                if (!string.IsNullOrEmpty(demonLordId))
                {
                    RespawnDemonLord(demonLordId);
                }
            }
        }

        private void RespawnDemonLord(string demonLordId)
        {
            var settlement = Settlement.FindFirst(s => s.StringId == "town_" + demonLordId.Substring(3, 2).ToUpper());
            if (settlement != null)
            {
                var troopDetails = new[]
                {
            (troopId: "cs_nurh_raiders_bandit", quantity: 500),
            (troopId: "cs_nurh_raiders_raider", quantity: 250),
            (troopId: "cs_nurh_raiders_chief", quantity: 50)
        };

                switch (demonLordId)
                {
                    case "cs_daimo_raiders_boss":
                        troopDetails = new[]
                        {
                    (troopId: "cs_daimo_raiders_bandit", quantity: 500),
                    (troopId: "cs_daimo_raiders_raider", quantity: 250),
                    (troopId: "cs_daimo_raiders_chief", quantity: 50)
                };
                        break;
                    case "cs_bark_raiders_boss":
                        troopDetails = new[]
                        {
                    (troopId: "cs_bark_raiders_bandit", quantity: 500),
                    (troopId: "cs_bark_raiders_raider", quantity: 250),
                    (troopId: "cs_bark_raiders_chief", quantity: 50)
                };
                        break;
                    case "cs_sillok_raiders_boss":
                        troopDetails = new[]
                        {
                    (troopId: "cs_sillok_raiders_bandit", quantity: 500),
                    (troopId: "cs_sillok_raiders_raider", quantity: 250),
                    (troopId: "cs_sillok_raiders_chief", quantity: 50)
                };
                        break;
                }

                CreateDemonLordParty(demonLordId, "cs_" + demonLordId.Substring(3, 4) + "_raiders", settlement, troopDetails);
                InformationManager.DisplayMessage(new InformationMessage($"Demon lord {demonLordId} respawned at {settlement.Name}."));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Failed to find a settlement for respawning demon lord {demonLordId}."));
            }
        }

        private void CheckDemonLordsDefeated()
        {
            InformationManager.DisplayMessage(new InformationMessage("Checking if all demon lords are defeated..."));

            bool allDemonLordsDefeated = demonLordsDefeated.All(d => d.Value);
            //bool demonLordPartiesDefeatTargetReached = demonLordPartiesDefeatedCount >= demonLordPartiesToDefeatTarget;

            if (allDemonLordsDefeated)
            {
                InformationManager.DisplayMessage(new InformationMessage("All individual demon lords marked as defeated."));
            }

            //if (demonLordPartiesDefeatTargetReached)
            //{
            //    InformationManager.DisplayMessage(new InformationMessage("Demon lord party defeat target reached."));
            //}

            if (isObjectiveCompleted)
            {
                //huntDemonLordsLog.UpdateCurrentProgress(1);
                InformationManager.DisplayMessage(new InformationMessage("Objective complete: All demon lords have been defeated."));
                CompleteQuestWithSuccess();
                ShowDemonLordsDefeatedNotification();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Not all conditions met for demon lord defeat objective."));
            }
        }


        private void StartTalkToLordObjective()
        {
            talkToLordLog.UpdateCurrentProgress(0);
            InformationManager.DisplayMessage(new InformationMessage("New objective: Talk to the Dreadking."));
        }

        private void CompleteTalkToLordObjective()
        {
            talkToLordLog.UpdateCurrentProgress(2);
            InformationManager.DisplayMessage(new InformationMessage("Objective complete: Talked to Dreadking."));
            // Proceed with next steps of the quest or finalize the quest
            StartHuntDemonLordsObjective();
        }

        public static void StartSixthQuest()
        {
            try
            {
                ShowFirstNotification();
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Exception in StartSixthQuest: {ex.Message}\nStackTrace: {ex.StackTrace}"));
            }
        }

        private static void ShowFirstNotification()
        {
            QuestUIManager.ShowNotification(
                "Huge armies of devils started roaming throgh the kingdoms. Reports of attacks from those hordes have been amassing everyday from all corners of Aeurth...",
                ShowSecondNotification,
                true,
                "demonic_horde");
        }

        private static void ShowSecondNotification()
        {
            QuestUIManager.ShowNotification(
                "The kings and Queen of Men, the High King of the Elveans, The AllKhuur Khan and the Nasorian Ulmir came into terms of peace, to be able to face this new treat. ",
                ShowThirdNotification,
                true,
                "king_council");
        }

        private static void ShowThirdNotification()
        {
            QuestUIManager.ShowNotification(
                "You are not alone in your war against this evil anymore. The world finally listened to your call.",
                DeclareWorldPeace,
                true,
                "riders_against_sun");
        }



        private static void DeclareWorldPeace()
        {
            SixthQuestBehaviour.DeletePopupVMLayer();
            InformationManager.DisplayMessage(new InformationMessage("Transitioning to the 'Declare World Peace' objective."));

            var sixthQuest = new SixthQuest("rf_sixth_quest", null, CampaignTime.Never, 50000);
            Hero theOwl = sixthQuest.TheOwl;

            if (theOwl == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("The Owl hero not found."));
                return;
            }

            sixthQuest = new SixthQuest("rf_sixth_quest", theOwl, CampaignTime.Never, 50000);
            sixthQuest.StartQuest();

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
            sixthQuest.worldPeaceStartTime = CampaignTime.Now;
            sixthQuest.isWorldPeaceActive = true;
            sixthQuest.CompleteDeclareWorldPeaceObjective();

            sixthQuest.StartOwlConversation();

        }

        private void EndWorldPeaceEvent()
        {
            isWorldPeaceActive = false;
            InformationManager.DisplayMessage(new InformationMessage("The 100 days of world peace have ended."));
            // Logic to handle the end of the world peace event, e.g., updating the quest log or triggering the next phase
        }

        private static void DeclarePeaceBetweenFactions(Kingdom faction1, Kingdom faction2)
        {
            if (faction1 != null && faction2 != null && FactionManager.IsAtWarAgainstFaction(faction1, faction2))
            {
                MakePeaceAction.Apply(faction1, faction2);
                InformationManager.DisplayMessage(new InformationMessage($"{faction1.Name} and {faction2.Name} have declared peace."));
            }
        }

        private void SpawnDemonLords()
        {
            var settlements = new[]
      {
        Settlement.FindFirst(settlement => settlement.StringId == "town_EN1"),
        Settlement.FindFirst(settlement => settlement.StringId == "town_B3"),
        Settlement.FindFirst(settlement => settlement.StringId == "town_V5"),
        Settlement.FindFirst(settlement => settlement.StringId == "town_K2")
    };

            var demonLords = new[]
            {
        new { CharacterId = "cs_nurh_raiders_boss", ClanId = "cs_nurh_raiders", TroopDetails = new[] { (troopId: "cs_nurh_raiders_bandit", quantity: 500), (troopId: "cs_nurh_raiders_raider", quantity: 250), (troopId: "cs_nurh_raiders_chief", quantity: 50) } },
        new { CharacterId = "cs_daimo_raiders_boss", ClanId = "cs_daimo_raiders", TroopDetails = new[] { (troopId: "cs_daimo_raiders_bandit", quantity: 500), (troopId: "cs_daimo_raiders_raider", quantity: 250), (troopId: "cs_daimo_raiders_chief", quantity: 50) } },
        new { CharacterId = "cs_bark_raiders_boss", ClanId = "cs_bark_raiders", TroopDetails = new[] { (troopId: "cs_bark_raiders_bandit", quantity: 500), (troopId: "cs_bark_raiders_raider", quantity: 250), (troopId: "cs_bark_raiders_chief", quantity: 50) } },
        new { CharacterId = "cs_sillok_raiders_boss", ClanId = "cs_sillok_raiders", TroopDetails = new[] { (troopId: "cs_sillok_raiders_bandit", quantity: 500), (troopId: "cs_sillok_raiders_raider", quantity: 250), (troopId: "cs_sillok_raiders_chief", quantity: 50) } }
    };

            for (int i = 0; i < demonLords.Length; i++)
            {
                CreateDemonLordParty(demonLords[i].CharacterId, demonLords[i].ClanId, settlements[i], demonLords[i].TroopDetails);
            }
        }

        private void CreateDemonLordParty(string demonLordId, string clanId, Settlement nearTown, (string troopId, int quantity)[] troopDetails)
        {
            try
            {
                string nearTownName = nearTown != null ? nearTown.Name.ToString() : "unknown settlement";
                InformationManager.DisplayMessage(new InformationMessage($"Creating demon lord party for {demonLordId} near {nearTownName}."));

                if (nearTown == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Settlement is null for demon lord {demonLordId}."));
                    return;
                }

                if (string.IsNullOrEmpty(clanId))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Clan ID is null or empty for demon lord {demonLordId}."));
                    return;
                }

                Clan clan = Clan.FindFirst(x => x.StringId == clanId);
                if (clan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Clan with ID {clanId} not found."));
                    return;
                }
                InformationManager.DisplayMessage(new InformationMessage($"Clan {clan.Name} found for demon lord {demonLordId}."));

                string partyStringId = demonLordsIds[demonLordId]; // Use generated ID
                MobileParty party = BanditPartyComponent.CreateBanditParty(partyStringId, clan, null, true);
                if (party == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Failed to create party for demon lord {demonLordId}."));
                    return;
                }
                InformationManager.DisplayMessage(new InformationMessage($"Party created for demon lord {demonLordId}."));

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
                Dictionary<CharacterObject, int> initialTroops = new Dictionary<CharacterObject, int>();

                CharacterObject character = CharacterObject.Find(demonLordId);
                if (character == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Character with ID {demonLordId} not found."));
                    return;
                }
                InformationManager.DisplayMessage(new InformationMessage($"Character {character.Name} found for demon lord {demonLordId}."));

                troopRoster.AddToCounts(character, 1);
                initialTroops[character] = 1;
                InformationManager.DisplayMessage(new InformationMessage($"Added demon lord {character.Name} to the party."));

                foreach (var troopDetail in troopDetails)
                {
                    CharacterObject troop = CharacterObject.Find(troopDetail.troopId);
                    if (troop == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Troop with ID {troopDetail.troopId} not found."));
                        continue;
                    }
                    troopRoster.AddToCounts(troop, troopDetail.quantity);
                    initialTroops[troop] = troopDetail.quantity;
                    InformationManager.DisplayMessage(new InformationMessage($"Added {troopDetail.quantity} of {troop.Name} to the party."));
                }

                party.InitializeMobilePartyAroundPosition(troopRoster, TroopRoster.CreateDummyTroopRoster(), nearTown.Position2D, 100f, 10f);
                InformationManager.DisplayMessage(new InformationMessage($"Initialized demon lord party around {nearTown.Name}."));

                party.SetCustomName(new TextObject($"Demon Lord {character.Name} Party"));
                InformationManager.DisplayMessage(new InformationMessage($"Set custom name for demon lord party: {party.Name}."));


                party.Aggressiveness = 10f;
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Days(1));

                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, () =>
                {
                    if (party != null && party.IsActive)
                    {
                        EngageNearbyEnemies(party);

                        // Find the nearest settlement
                        Settlement nearestSettlement = SettlementHelper.FindNearestSettlement(party.Position2D);

                        // Display message with nearest settlement information
                        string nearestSettlementName = nearestSettlement != null ? nearestSettlement.Name.ToString() : "unknown settlement";
                        InformationManager.DisplayMessage(new InformationMessage($"Party {party.Name} engaging nearby enemies near {nearestSettlementName}."));
                    }
                });

                InformationManager.DisplayMessage(new InformationMessage($"Demon Lord {demonLordId} spawned near {nearTown.Name}."));
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
            Hero owl = TheOwl;
            if (owl != null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Starting conversation with the Owl"));
                Campaign.Current.ConversationManager.AddDialogFlow(DeclareWorldPeaceDialog, this);
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(owl.CharacterObject)
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
            Hero owl = TheOwl;
            if (owl != null)
            {
                Campaign.Current.ConversationManager.AddDialogFlow(AfterDemonLordsDefeatedDialogOwl, this);
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(owl.CharacterObject)
                );
            }
        }

        private DialogFlow DeclareWorldPeaceDialog => DialogFlow.CreateDialogFlow("start", 125)
         .NpcLine(GameTexts.FindText("rf_sixth_quest_first_dialog_1"))
         .Condition(() => declareWorldPeaceLog?.CurrentProgress == 0 && Hero.OneToOneConversationHero == TheOwl)
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
             // Update progress to avoid looping
             declareWorldPeaceLog?.UpdateCurrentProgress(2);
             talkToLordLog = AddLog(GameTexts.FindText("rf_sixth_quest_second_objective"));
             StartTalkToLordObjective();

             if (!isWorldPeaceActive)
             {
                 worldPeaceStartTime = CampaignTime.Now;
                 isWorldPeaceActive = true;


                 InformationManager.DisplayMessage(new InformationMessage("Find the Dreadking."));
             }

         })
         .CloseDialog();

        private DialogFlow TalkToLordDialog => DialogFlow.CreateDialogFlow("start", 125)
         .NpcLine(GameTexts.FindText("rf_sixth_quest_lord_dialog_1"))
         .Condition(() =>
         {
             InformationManager.DisplayMessage(new InformationMessage($"Current Progress: {declareWorldPeaceLog?.CurrentProgress}"));
             InformationManager.DisplayMessage(new InformationMessage($"Hero OneToOneConversationHero: {Hero.OneToOneConversationHero?.Name}"));
             return declareWorldPeaceLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == Lord2_1;
         })
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
             talkToLordLog?.UpdateCurrentProgress(1);
             InformationManager.DisplayMessage(new InformationMessage("Objective complete: Talked to Lord 2_1."));
             CompleteTalkToLordObjective();

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
         .Condition(() => talkToLordLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == TheOwl)
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_second_dialog_2"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_second_dialog_3"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_second_dialog_4"))
         .NpcLine(GameTexts.FindText("rf_sixth_quest_second_dialog_5"))
         .PlayerLine(GameTexts.FindText("rf_sixth_quest_second_dialog_6"))
         .Consequence(() =>
         {
             StartHuntDemonLordsObjective();
             //huntDemonLordsLog = AddLog(GameTexts.FindText("rf_sixth_quest_third_objective"));
             SpawnDemonLords();
         })
         .CloseDialog();

        //private DialogFlow AfterDemonLordsDefeatedDialogOwl => DialogFlow.CreateDialogFlow("start", 125)
        //   .NpcLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_1"))
        //   .Condition(() => huntDemonLordsLog?.CurrentProgress == 2 && Hero.OneToOneConversationHero == TheOwl)
        //   .PlayerLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_2"))
        //   .NpcLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_3"))
        //   .PlayerLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_4"))
        //   .NpcLine(GameTexts.FindText("rf_sixth_quest_after_demon_lords_defeated_owl_dialog_5"))
        //   .Consequence(() =>
        //   {

        //   })
        //   .CloseDialog();

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
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            //demonLordPartiesDefeatedCount
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // Initialization logic when the game is loaded
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            // Initialization logic for a new game
        }

        private void OnTick(float dt)
        {
            // Per-tick logic if needed
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

