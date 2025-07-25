using RealmsForgotten.AiMade.Religions;
using RealmsForgotten.Quest.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class EighthQuest : QuestBase
    {
        [SaveableField(0)] private JournalLog _meetPriestessLog;
        [SaveableField(1)] internal JournalLog _findSacredObjectLog;
        [SaveableField(2)] internal JournalLog _returnSacredObjectLog;
        [SaveableField(3)] private CampaignTime _leftHideoutTime = CampaignTime.Never;
        [SaveableField(4)] private bool _hasTriggeredPriestessReturnDialogue = false;
        [SaveableField(5)] private bool _hasTriggeredOwlPostHideout = false;
        [SaveableField(6)] private bool _ambushTriggered = false;
        [SaveableField(7)] private CampaignTime _ambushCheckStartTime = CampaignTime.Never;

        private bool _hasSeenOwlDialogue = false;

        private static Settlement FirstTreeSettlement => Settlement.Find("town_FirstTree");

        private bool _shouldTriggerPostAmbushOwlDialogue = false;
        public EighthQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold)
            : base(questId, questGiver, duration, rewardGold) { }

        protected override void InitializeQuestOnGameLoad()
        {
            SetDialogs();
            ReinforceQuestLogs();
        }

        protected override void OnStartQuest()
        {
            SetDialogs();
            RegisterEvents();

            _findSacredObjectLog = AddDiscreteLog(
                new TextObject("The Priestess Awaits"),
                new TextObject("Strange parties with deformed raiders spread across the land. Investigate the rumours and search for clues."),
                0, 1
            );

            InitializeEighthQuestHideout();
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnLeaveSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);

        }

        private void ReinforceQuestLogs()
        {
            if (_findSacredObjectLog == null)
            {
                _findSacredObjectLog = AddDiscreteLog(
                    new TextObject("The Priestess Awaits"),
                    new TextObject("Strange parties with deformed raiders spread across the land. Investigate the rumours and search for clues."),
                    0, 1
                );
                InformationManager.DisplayMessage(new InformationMessage("🛠 Reinstated missing 'Find Sacred Object' log."));
            }

            if (_returnSacredObjectLog == null && _findSacredObjectLog?.CurrentProgress == 1 && PlayerHasSacredObject())
            {
                _returnSacredObjectLog = AddDiscreteLog(
                    new TextObject("Return the Sacred Object"),
                    new TextObject("Return the sacred object to the First Tree Priestess."),
                    0, 1
                );
                InformationManager.DisplayMessage(new InformationMessage("🛠 Reinstated missing 'Return Sacred Object' log."));
            }
        }

        private void OnLeaveSettlement(MobileParty party, Settlement settlement)
        {
            if (!party.IsMainParty || settlement.StringId != "hideout_mountain_13")
                return;

            if (!_hasSeenOwlDialogue && _findSacredObjectLog?.CurrentProgress == 0)
            {
                _hasSeenOwlDialogue = true;
                StartSacredObjectDialogue();
            }
           
        }

        private void OnTick(float dt)
        {
            if (_shouldTriggerPostAmbushOwlDialogue)
            {
                _shouldTriggerPostAmbushOwlDialogue = false;

                Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
                if (owl != null)
                {
                    Campaign.Current.ConversationManager.AddDialogFlow(PriestessReturnDialog, this);
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(owl.CharacterObject, PartyBase.MainParty)
                    );
                }
            }
        }
        protected override void HourlyTick()
        {
            var hideout = Settlement.Find("hideout_mountain_13")?.Hideout;

            if (!_hasTriggeredOwlPostHideout
                && _findSacredObjectLog?.CurrentProgress == 0
                && hideout != null && !hideout.IsInfested)
            {
                Hero owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
                if (owl != null)
                {
                    _hasTriggeredOwlPostHideout = true;

                    InformationManager.DisplayMessage(
                        new InformationMessage("✅ Owl conversation triggered after hideout cleared."));

                    StartSacredObjectDialogue();
                }
              
            }
        }


        private void OnDailyTick()
        {
            CheckPriestessProximity();
            if (!_ambushTriggered && _ambushCheckStartTime != CampaignTime.Never)
            {
                if ((CampaignTime.Now - _ambushCheckStartTime).ToDays >= 1f)
                {
                    _ambushTriggered = true;
                    TriggerAmbush();
                }
            }
        }

        private void CheckPriestessProximity()
        {
            if (_hasTriggeredPriestessReturnDialogue || _returnSacredObjectLog?.CurrentProgress != 1 || FirstTreeSettlement == null)
                return;

            float distance = MobileParty.MainParty.Position2D.Distance(FirstTreeSettlement.GatePosition);
            if (distance <= 50f)
            {
                var priestess = CharacterObject.Find("elvean_first_tree_druid_quest");
                if (priestess != null)
                {
                    _hasTriggeredPriestessReturnDialogue = true;
                    CampaignMapConversation.OpenConversation(
                        new ConversationCharacterData(CharacterObject.PlayerCharacter),
                        new ConversationCharacterData(priestess)
                    );
                }
            }
        }

        private void TriggerAmbush()
        {
            try
            {
                Clan deformedClan = Clan.FindFirst(c => c.StringId == "deformed_villagers");
                if (deformedClan == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Deformed clan not found."));
                    return;
                }

                // 🧠 Dictionary of troop ID -> count
                Dictionary<string, int> troopPool = new()
        {
            { "deformed_villager_bandit", 30 },
            { "deformed_villager_raider", 15 },
            { "deformed_villager_chief", 5 },
            { "deformed_villager_boss", 1 }
        };

                TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();

                foreach (var kv in troopPool)
                {
                    var character = CharacterObject.Find(kv.Key);
                    if (character != null)
                    {
                        troopRoster.AddToCounts(character, kv.Value);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"⚠️ Troop not found: {kv.Key}"));
                    }
                }

                // 🔒 Unique party ID to avoid conflict with sniffer or reused IDs
                string uniqueId = $"rf_deformed_ambush_{MBRandom.RandomInt(10000)}";
                MobileParty ambushParty = BanditPartyComponent.CreateBanditParty(uniqueId, deformedClan, null, true);

                if (ambushParty == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("❌ Failed to create ambush party."));
                    return;
                }

                ambushParty.InitializeMobilePartyAroundPosition(
                    troopRoster,
                    TroopRoster.CreateDummyTroopRoster(),
                    MobileParty.MainParty.Position2D,
                    0f, // Spawn on top of the player
                    0f);

                ambushParty.Aggressiveness = 100f;
                ambushParty.Ai.SetMoveEngageParty(MobileParty.MainParty);
                ambushParty.SetCustomName(new TextObject("Deformed Ambushers"));

                InformationManager.DisplayMessage(new InformationMessage("☠️ A deformed ambush party has been spawned on your position!"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Ambush spawn error: {ex.Message}"));
            }
        }


        private void StartSacredObjectDialogue()
        {
            var owl = Hero.FindFirst(h => h.StringId == "rf_the_owl");
            if (owl != null)
            {
                Campaign.Current.ConversationManager.AddDialogFlow(SacredObjectDialogue, this);
                CampaignMapConversation.OpenConversation(
                    new ConversationCharacterData(CharacterObject.PlayerCharacter),
                    new ConversationCharacterData(owl.CharacterObject, PartyBase.MainParty)
                );
            }
        }

        private void InitializeEighthQuestHideout()
        {
            var hideout = Settlement.Find("hideout_mountain_13")?.Hideout;
            if (hideout != null)
            {
                QuestLibrary.InitializeHideoutIfNeeded(hideout);
                AddTrackedObject(hideout.Settlement);
            }
        }

        private void CleanupEighthQuestHideout()
        {
            var hideout = Settlement.Find("hideout_mountain_13")?.Hideout;
            if (hideout != null)
            {
                RemoveTrackedObject(hideout.Settlement);
            }
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (!mapEvent.IsPlayerMapEvent)
                return;

            MobileParty ambushParty = null;

            var attackerParty = mapEvent.AttackerSide.LeaderParty?.MobileParty;
            var defenderParty = mapEvent.DefenderSide.LeaderParty?.MobileParty;

            if (attackerParty != null && attackerParty != MobileParty.MainParty &&
                attackerParty.StringId.StartsWith("rf_deformed_ambush_"))
            {
                ambushParty = attackerParty;
            }
            else if (defenderParty != null && defenderParty != MobileParty.MainParty &&
                     defenderParty.StringId.StartsWith("rf_deformed_ambush_"))
            {
                ambushParty = defenderParty;
            }

            if (ambushParty != null)
            {
                ForceRemoveSacredObject();
                _returnSacredObjectLog?.UpdateCurrentProgress(1);
                _shouldTriggerPostAmbushOwlDialogue = true;
            }
        }


        private void ForceRemoveSacredObject()
        {
            var sacredObject = MBObjectManager.Instance.GetObject<ItemObject>("sacred_object");
            if (sacredObject == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Sacred Object definition missing."));
                return;
            }

            int count = MobileParty.MainParty.ItemRoster.GetItemNumber(sacredObject);
            if (count > 0)
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(sacredObject, -count);
                InformationManager.DisplayMessage(new InformationMessage("⚠️ The Sacred Object was forcibly removed.", Colors.Red));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("ℹ️ Sacred Object not found in inventory."));
            }
        }

        protected override void SetDialogs()
        {
            Campaign.Current.ConversationManager.AddDialogFlow(PriestessReturnDialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(SacredObjectDialogue, this);
          
        }

        protected override void OnTimedOut()
        {
            CompleteQuestWithFail();
        }

        protected override void OnFinalize()
        {
            CleanupEighthQuestHideout();
        }

        public override TextObject Title => new TextObject("Eighth Quest: Call of the First Tree");
        public override bool IsSpecialQuest => true;
        public override bool IsRemainingTimeHidden => false;

        private DialogFlow SacredObjectDialogue => DialogFlow.CreateDialogFlow("start", 125)
     .PlayerLine(new TextObject("What the hell were those things? They looked human... but twisted, like corpses rotting from the plague. And I found this among the bodies."))
     .Condition(() =>
         Hero.OneToOneConversationHero?.StringId == "rf_the_owl" &&
         _findSacredObjectLog?.CurrentProgress == 0)
     .NpcLine(new TextObject("It appears to be some kind of vessel."))
     .PlayerLine(new TextObject("The Priestess needs to see this. It might be the clue we’ve been searching for."))
     .Consequence(() =>
     {
         var item = MBObjectManager.Instance.GetObject<ItemObject>("sacred_object");
         if (item != null)
         {
             MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
             InformationManager.DisplayMessage(new InformationMessage("✅ Sacred Object added to your inventory."));

             _findSacredObjectLog?.UpdateCurrentProgress(1);

             _returnSacredObjectLog ??= AddDiscreteLog(
                 new TextObject("Return the Sacred Object"),
                 new TextObject("Return the sacred object to the First Tree Priestess."),
                 0, 1
             );

             _ambushCheckStartTime = CampaignTime.Now;
             InformationManager.DisplayMessage(new InformationMessage("⚠️ You feel like you’re being watched..."));
         }
         else
         {
             InformationManager.DisplayMessage(new InformationMessage("❌ Sacred Object not found.", Colors.Red));
         }
     })
     .CloseDialog();
                   

        private DialogFlow PriestessReturnDialog => DialogFlow.CreateDialogFlow("start", 125)
     .PlayerLine(new TextObject("What the hell was that? They came out of nowhere!"))
     .Condition(() =>
         CharacterObject.OneToOneConversationCharacter?.HeroObject?.StringId == "rf_the_owl" &&
         _returnSacredObjectLog?.CurrentProgress == 1 &&
         !PlayerHasSacredObject())
     .NpcLine(new TextObject("Not even our scouts saw them coming. Could it be some kind of sorcery?"))
     .PlayerLine(new TextObject("The vessel... I lost it during the fight. Damn it."))
     .NpcLine(new TextObject("Or maybe it was stolen from your gear? If sorcery is involved, what if we were followed? That ambush might have just been a distraction."))
     .PlayerLine(new TextObject("It's possible. But we’ve got no trail to follow now."))
     .NpcLine(new TextObject("Unless the mages really are involved. They might help us track whoever took it — if they’re willing."))
     .PlayerLine(new TextObject("You're right. We don’t have any other leads."))
     .Consequence(() =>
     {
         ForceRemoveSacredObject();
         _meetPriestessLog ??= AddDiscreteLog(
             new TextObject("The Mages"),
             new TextObject("To be continued."),
             0, 1
         );
         CompleteQuestWithSuccess();
     })
     .CloseDialog();


        private bool PlayerHasSacredObject()
        {
            var item = MBObjectManager.Instance.GetObject<ItemObject>("sacred_object");
            return item != null && MobileParty.MainParty.ItemRoster.GetItemNumber(item) > 0;
        }
    }
}
