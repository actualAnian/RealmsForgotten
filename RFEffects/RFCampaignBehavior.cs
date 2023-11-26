﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.RFEffects;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using FaceGen = TaleWorlds.Core.FaceGen;
using TaleWorlds.CampaignSystem.Extensions;
using System.Xml.Linq;
using SandBox.GauntletUI.AutoGenerated0;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade.GauntletUI.AutoGenerated0;
using System.Collections.ObjectModel;
using TaleWorlds.CampaignSystem.Actions;

namespace RealmsForgotten.RFEffects
{
    class RFCampaignBehavior : CampaignBehaviorBase
    {
        private CampaignTime lastMeetingTime;
        private ItemRoster? vendorItemRoster;
        public static readonly string[] skillsIds = new[] { "rfonehanded", "rftwohanded", "rfpolearm", "rfthrowing", "rfbow", "rfcrossbow", "rfmoralizing", "rfdemoralizing", "rfmisc" };
        public override void RegisterEvents()
        {
            CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, imission =>
            {
                Mission mission = (Mission)imission;
                ItemRosterElement elixir = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("elixir_rfmisc"));
                ItemRosterElement berserker = PartyBase.MainParty.ItemRoster.FirstOrDefault(x => x.EquipmentElement.Item.StringId.Contains("berzerker_potion"));
                if (!elixir.IsEmpty || !berserker.IsEmpty)
                    mission?.AddMissionBehavior(new HealingPotionMissionBehavior(elixir, berserker));
            });
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
        }

        private void DailyTick()
        {
            if (lastMeetingTime.ElapsedDaysUntilNow >= 5)
                vendorItemRoster = CreateItemRoster();
        }
        private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        {
            Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
            bool flag = settlement.IsTown && CampaignMission.Current != null;
            if (flag)
            {
                string a = settlement.Name.ToString();
                Location? location = CampaignMission.Current?.Location;
                if (location != null && location.StringId == "tavern")
                {
                    LocationCharacter locationCharacter = CreateEnhancedVendor(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
                    location.AddCharacter(locationCharacter);
                }
            }
        }
        private static LocationCharacter CreateEnhancedVendor(CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject @object = MBObjectManager.Instance.GetObject<CharacterObject>("enchanted_vendor");
            int minValue;
            int maxValue;
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(@object, out minValue, out maxValue, "");
            Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(@object.Race, "_settlement");
            AgentData agentData = new AgentData(new SimpleAgentOrigin(@object, -1, null, default(UniqueTroopDescriptor))).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minValue, maxValue));
            var vendor = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_tavern_townsman", true, relation, null, true, false, null, false, false, true);
            vendor.PrefabNamesForBones.Add(agentData.AgentMonster.OffHandItemBoneIndex, "kitchen_pitcher_b_tavern");
            return vendor;
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (RFEffectsSubModule.undeadRespawnConfig.Count == 0)
                MBInformationManager.AddQuickInformation(new TextObject("undead_respawn_config.json is null!!"));
            this.AddDialogs(starter);
        }

        private void AddDialogs(CampaignGameStarter starter)
        {
            DialogFlow dialog = DialogFlow.CreateDialogFlow("start", 125).PlayerLine(new TextObject("{=playervendorask}Hi, i heard you deal with special items, is that true ?").ToString()).Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "enchanted_vendor" && lastMeetingTime == default)
                .Consequence(()=> lastMeetingTime = CampaignTime.Now).NpcLine(new TextObject("{=playervendoranswer}Yes, what can i do for you ?").ToString()).BeginPlayerOptions().PlayerOption(new TextObject("{=playervendoranswer2}What do you have to offer ?").ToString()).Consequence(() =>
                {
                    Settlement currentSettlement = Settlement.CurrentSettlement;
                    if (Mission.Current != null && currentSettlement != null)
                    {
                        vendorItemRoster = CreateItemRoster();
                        InventoryManager.OpenScreenAsTrade(vendorItemRoster, currentSettlement.SettlementComponent, InventoryManager.InventoryCategoryType.All, () => { });
                    }
                }).GotoDialogState("start").PlayerOption("{=leave}Leave.").CloseDialog().EndPlayerOptions();

            DialogFlow dialog2 = DialogFlow.CreateDialogFlow("start", 125).NpcLine(new TextObject("{=playervendorask2}How can i serve you sir ?").ToString()).Condition(() => CharacterObject.OneToOneConversationCharacter?.StringId == "enchanted_vendor")
                .BeginPlayerOptions().PlayerOption(new TextObject("{=playervendoranswer2}What do you have to offer ?").ToString()).Consequence(() =>
                {
                    Settlement currentSettlement = Settlement.CurrentSettlement;
                    if (Mission.Current != null && currentSettlement != null)
                    {
                        
                        InventoryManager.OpenScreenAsTrade(vendorItemRoster, currentSettlement.SettlementComponent, InventoryManager.InventoryCategoryType.All, () => 
                        {

                        });
                    }
                }).GotoDialogState("start").PlayerOption("{=leave}Leave.").CloseDialog().EndPlayerOptions();

            Campaign.Current.ConversationManager.AddDialogFlow(dialog, this);
            Campaign.Current.ConversationManager.AddDialogFlow(dialog2, this);

        }
        
        private ItemRoster CreateItemRoster()
        {
            //Takes all items with the rf id --> randomly remove some of them --> if rfmisc randomly increases the amount and increases the price based on the level --> returns item roster

            List<ItemObject> randomItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(x => skillsIds.Any(y => x.StringId.Contains(y))).ToList();

            randomItems.Randomize();

            for(int i = 0; i < randomItems.Count / 2; i++)
            {
                if (MBRandom.RandomFloat > 0.5)
                    randomItems.RemoveAt(i);
            }

            ItemRoster itemRoster = new ItemRoster();
            List<ItemObject> itemsToAdd = new();

            if(randomItems.Any(x=>x.StringId.Contains("rfmisc")))
            {
                foreach(ItemObject good in randomItems.Where(x=> x.StringId.Contains("rfmisc")))
                {
                    Random rn = new();
                    int amount = rn.Next(1, 10);
                    for (int i = 0; i < amount; i++)
                        itemsToAdd.Add(good);
                }

            }
            randomItems.AddRange(itemsToAdd);

            foreach (ItemObject item in randomItems.ToList())
            {
                string enchantment = skillsIds.First(x => item.StringId.Contains(x));
                int level = RFMissionBehaviour.GetNumberAfterSkillWord(item.StringId, enchantment);
                
                AccessTools.Property(typeof(ItemObject), "Value").SetValue(item, item.Value + level * 15);
                itemRoster.Add(new ItemRosterElement(item, 1));
            }

            return itemRoster;
        }
        

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("lastMeetingTime", ref lastMeetingTime);
            dataStore.SyncData("vendorItemRoster", ref vendorItemRoster);
        }
    }
}