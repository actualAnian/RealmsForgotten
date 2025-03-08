using RealmsForgotten.AiMade.Career;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career
{
    internal class RFCareerPerkCampaignBehavior : CampaignBehaviorBase
    {
        [SaveableField(0)] static PlayerClassInfo playerClassInfo;
        ItemRoster raidLootedItems = new();
        public static PlayerClassInfo ClassInfo { get { return playerClassInfo; } set { playerClassInfo = value; } }
        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this, OnItemLooted);
            CampaignEvents.DistributeLootToPartyEvent.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, Dictionary<PartyBase, ItemRoster>>(this.OnLootCaravanParties));
        }

        private void OnLootCaravanParties(MapEvent mapEvent, PartyBase party, Dictionary<PartyBase, ItemRoster> dictionary)
        {
            if (party != PartyBase.MainParty || !PlayerCareerExtension.HasAnyCareer()) return;
            if (!PlayerCareerExtension.GetAllCareerChoices().Contains("MercenaryLordPassive2_3")) return;
            foreach (KeyValuePair<PartyBase, ItemRoster> tuple in dictionary)
            {
                if (!tuple.Key.IsCaravan()) continue;
                for (int i = 0; i < tuple.Value.Count; i++)
                {
                    ItemRosterElement item = tuple.Value[i];
                    item.Amount = (int)(item.Amount * 0.2);
                    if (item.Amount > 0)
                    {
                        MobileParty.MainParty.ItemRoster.Add(item);
                        MBTextManager.SetTextVariable("NUMBER_OF", item.Amount);
                        MBTextManager.SetTextVariable("PRODUCTS", item.EquipmentElement.Item.Name, false);
                        InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_caravan_plunder}You plundered additional {NUMBER_OF} {PRODUCTS}.", null).ToString()));
                    }
                }
            }
        }
        private void OnItemLooted(MobileParty mobileParty, ItemRoster roster)
        {
            if (mobileParty == null
                || mobileParty != MobileParty.MainParty && mobileParty.MapEvent.IsRaid 
                || !PlayerCareerExtension.HasAnyCareer()) 
                return;
            if (PlayerCareerExtension.GetAllCareerChoices().Contains("MercenaryLordPassive1_3"))
            {
                for (int i = 0; i < roster.Count; i++) 
                {
                    ItemRosterElement item = roster[i];
                    if (item.EquipmentElement.GetBaseValue() < 60)
                        raidLootedItems.Add(item);
                }
            }
        }
        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEventComponent)
        {
            if (!raidEventComponent.IsPlayerMapEvent || winnerSide != BattleSideEnum.Attacker || !PlayerCareerExtension.HasAnyCareer()) return;
            if (PlayerCareerExtension.GetAllCareerChoices().Contains("MercenaryLordPassive1_3"))
            {
                for (int i = 0; i < raidLootedItems.Count; i++)
                {
                    ItemRosterElement item = raidLootedItems[i];
                    item.Amount = (int)(item.Amount * 0.2);
                    MobileParty.MainParty.ItemRoster.Add(item);
                    MBTextManager.SetTextVariable("NUMBER_OF",  item.Amount);
                    MBTextManager.SetTextVariable("PRODUCTS", item.EquipmentElement.Item.Name, false);

                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_raid_plunder}You plundered additional {NUMBER_OF} {PRODUCTS}.", null).ToString()));
                }
                raidLootedItems = new();
            }
        }
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("playerClassInfo", ref playerClassInfo);
        }
    }
}