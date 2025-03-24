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
using RealmsForgotten.Career.CareerPointsSystem;

namespace RealmsForgotten.Career
{
    public class RFCareerCampaignBehavior : CampaignBehaviorBase
    {
        [SaveableField(0)] PlayerClassInfo playerClassInfo = new();
        ItemRoster raidLootedItems = new();
        public Action<Hero, bool>? onLevelUp;
        [SaveableField(1)] AbstractPointsSystem? pointsSystem;
        public AbstractPointsSystem? PointsSystem { get { return pointsSystem; } }
        public PlayerClassInfo ClassInfo { get { return playerClassInfo; } set { playerClassInfo = value; } }
        private static RFCareerCampaignBehavior? instance;

        public RFCareerCampaignBehavior()
        {
            instance = this;
        }

        public static RFCareerCampaignBehavior Instance
        { 
            get 
            {
                instance ??= new RFCareerCampaignBehavior();
                return instance;
            }
        }
        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this, OnItemLooted);
            CampaignEvents.DistributeLootToPartyEvent.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, Dictionary<PartyBase, ItemRoster>>(this.OnLootCaravanParties));
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, new Action<Hero, bool>(OnLevelUp));
            CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, new Action<Clan, float>(OnClanInfluenceChanged));
            CampaignEvents.RenownGained.AddNonSerializedListener(this, new Action<Hero, int, bool>(OnRenownGained));
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, new Action<QuestBase, QuestBase.QuestCompleteDetails>(OnQuestompletedEvent));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(OnMapEventEnded));
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            PointsSystem?.OnMapEventEnded(mapEvent);
        }

        private void OnQuestompletedEvent(QuestBase quest, QuestBase.QuestCompleteDetails details)
        {
            PointsSystem?.OnQuestCompleted(quest, details);
        }

        private void OnRenownGained(Hero hero, int arg2, bool arg3)
        {
            PointsSystem?.OnRenownGained(hero, arg2, arg3);
        }

        private void OnClanInfluenceChanged(Clan clan, float arg2)
        {
            pointsSystem?.OnClanInfluenceChanged(clan, arg2);
        }

        public void CreatePointsSystem(PointsSystemType type)
        {
            switch (type)
            {
                case PointsSystemType.LevelUp:
                    pointsSystem = new LevelUpPointsSystem();
                    break;
                case PointsSystemType.Renown:
                    pointsSystem = new RenownPointsSystem();
                    break;
                case PointsSystemType.Deeds:
                    pointsSystem = new DeedsPointsSystem();
                    break;
            }
        }
        private void OnLevelUp(Hero hero, bool arg2)
        {
            pointsSystem?.OnLevelUp(hero, arg2);
        }

        private void OnLootCaravanParties(MapEvent mapEvent, PartyBase party, Dictionary<PartyBase, ItemRoster> dictionary)
        {
            if (party != PartyBase.MainParty || !PlayerCareerExtension.HasAnyCareer()) return;
            if (!PlayerCareerExtension.GetAllCareerChoices().Contains("MercenaryLordPassive2_3")) return;
            foreach (KeyValuePair<PartyBase, ItemRoster> tuple in dictionary)
            {
                if (!Globals.IsCaravanParty(tuple.Key)) continue;
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
            dataStore.SyncData("pointsSystem", ref pointsSystem);
        }
    }
}