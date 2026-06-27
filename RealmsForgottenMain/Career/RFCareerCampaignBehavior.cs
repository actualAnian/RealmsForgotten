using RealmsForgotten.Career.CareerPointsSystem;
using System;
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
    public class RFCareerCampaignBehavior : CampaignBehaviorBase
    {
        private const string ClericRetreatProtectionPerkId = "ClericConsecratedWarden1_4";
        private const string ClericPlayerPostBattleHealingPerkId = "ClericBearerOfMercy1_1";
        private const string ClericTroopPostBattleHealingPerkId = "ClericBearerOfMercy1_2";
        private const string ClericMajorBattleRecoveryPerkId = "ClericSaintedIntercessor1_5";
        [SaveableField(0)] PlayerClassInfo playerClassInfo = new();
        ItemRoster raidLootedItems = new();
        public Action<Hero, bool>? onLevelUp;
        [SaveableField(1)] AbstractPointsSystem? pointsSystem;
        public AbstractPointsSystem? PointsSystem { get { return pointsSystem; } }
        public PlayerClassInfo ClassInfo { get { return playerClassInfo; } set { playerClassInfo = value; } }
        private static RFCareerCampaignBehavior? instance;

       
        public RFCareerCampaignBehavior() { instance = this; }

        public static RFCareerCampaignBehavior Instance
        {
            get { instance ??= new RFCareerCampaignBehavior(); return instance; }
        }

        public override void RegisterEvents()
        {
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this, OnItemLooted);
            //CampaignEvents.DistributeLootToPartyEvent.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, Dictionary<PartyBase, ItemRoster>>(this.OnLootCaravanParties));
            CampaignEvents.OnCollectLootsItemsEvent.AddNonSerializedListener(this, OnLoot);
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, new Action<Hero, bool>(OnLevelUp));
            CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, new Action<Clan, float>(OnClanInfluenceChanged));
            CampaignEvents.RenownGained.AddNonSerializedListener(this, new Action<Hero, int, bool>(OnRenownGained));
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, new Action<QuestBase, QuestBase.QuestCompleteDetails>(OnQuestompletedEvent));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(OnMapEventEnded));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(OnMapEventEnded_AdjustRetreatCasualties));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded_WizardPostBattleHealing);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded_ClericPostBattleEffects);

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
                case PointsSystemType.LevelUp: pointsSystem = new LevelUpPointsSystem(); break;
                case PointsSystemType.Renown: pointsSystem = new RenownPointsSystem(); break;
                case PointsSystemType.Deeds: pointsSystem = new DeedsPointsSystem(); break;
            }
        }

        private void OnLevelUp(Hero hero, bool arg2)
        {
            pointsSystem?.OnLevelUp(hero, arg2);
        }
        private void OnLoot(PartyBase party, ItemRoster roster)
        {
            if (party != PartyBase.MainParty || !PlayerCareerExtension.HasAnyCareer()) return;
            if (!PlayerCareerExtension.GetAllCareerChoices().Contains("MercenaryLordPassive2_3")) return;

            foreach (ItemRosterElement item in roster)
            {
                ItemRosterElement newItem = new(item.EquipmentElement, (int)(item.Amount * 0.2));
                if (newItem.Amount > 0)
                {
                    MobileParty.MainParty.ItemRoster.Add(newItem);
                    MBTextManager.SetTextVariable("NUMBER_OF", newItem.Amount);
                    MBTextManager.SetTextVariable("PRODUCTS", newItem.EquipmentElement.Item.Name, false);
                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_caravan_plunder}You plundered additional {NUMBER_OF} {PRODUCTS}.", null).ToString()));
                }
            }
        }


        //private void OnLootCaravanParties(MapEvent mapEvent, PartyBase party, Dictionary<PartyBase, ItemRoster> dictionary)
        //{
        //    if (party != PartyBase.MainParty || !PlayerCareerExtension.HasAnyCareer()) return;
        //    if (!PlayerCareerExtension.GetAllCareerChoices().Contains("MercenaryLordPassive2_3")) return;

        //    foreach (KeyValuePair<PartyBase, ItemRoster> tuple in dictionary)
        //    {
        //        if (!Globals.IsCaravanParty(tuple.Key)) continue;
        //        for (int i = 0; i < tuple.Value.Count; i++)
        //        {
        //            ItemRosterElement item = tuple.Value[i];
        //            item.Amount = (int)(item.Amount * 0.2);
        //            if (item.Amount > 0)
        //            {
        //                MobileParty.MainParty.ItemRoster.Add(item);
        //                MBTextManager.SetTextVariable("NUMBER_OF", item.Amount);
        //                MBTextManager.SetTextVariable("PRODUCTS", item.EquipmentElement.Item.Name, false);
        //                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_caravan_plunder}You plundered additional {NUMBER_OF} {PRODUCTS}.", null).ToString()));
        //            }
        //        }
        //    }
        //}

        private void OnItemLooted(MobileParty mobileParty, ItemRoster roster)
        {
            if (mobileParty == null
                || (mobileParty != MobileParty.MainParty && mobileParty.MapEvent.IsRaid)
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
                    MBTextManager.SetTextVariable("NUMBER_OF", item.Amount);
                    MBTextManager.SetTextVariable("PRODUCTS", item.EquipmentElement.Item.Name, false);

                    InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=rf_raid_plunder}You plundered additional {NUMBER_OF} {PRODUCTS}.", null).ToString()));
                }
                raidLootedItems = new();
            }
        }

        // ————— Retreat casualty reduction —————
        private void OnMapEventEnded_AdjustRetreatCasualties(MapEvent mapEvent)
        {
            try
            {
                if (mapEvent == null || !PlayerCareerExtension.HasAnyCareer()) return;
                if (!mapEvent.IsPlayerMapEvent) return;
                if (!mapEvent.IsRaid && !mapEvent.IsSiegeAssault && !mapEvent.IsFieldBattle) return;

                float reduceLossesFactor = 0f;
                foreach (var choiceId in PlayerCareerExtension.GetAllCareerChoices())
                {
                    var ch = RFCareerChoices.GetChoice(choiceId);
                    if (ch?.Passive == null) continue;
                    if (ch.Description.ToString().Contains("Reduce troop losses when escaping"))
                        reduceLossesFactor = MathF.Clamp(ch.Passive.EffectMagnitude, 0f, 0.9f);
                }
                if (PlayerCareerExtension.HasCareerChoice(ClericRetreatProtectionPerkId))
                    reduceLossesFactor = MathF.Max(reduceLossesFactor, 0.20f);

                if (reduceLossesFactor <= 0f) return;

                var party = MobileParty.MainParty;
                if (party == null) return;

                var roster = party.MemberRoster;
                var list = roster.GetTroopRoster();

                int totalWounded = 0;
                for (int i = 0; i < list.Count; i++)
                    totalWounded += list[i].WoundedNumber;

                if (totalWounded <= 0) return;

                int healCount = (int)MathF.Round(totalWounded * reduceLossesFactor);
                for (int i = 0; i < list.Count && healCount > 0; i++)
                {
                    int wounded = list[i].WoundedNumber;
                    if (wounded <= 0) continue;

                    int toHeal = MathF.Min(wounded, healCount);
                    try { roster.SetElementWoundedNumber(i, MathF.Max(0, wounded - toHeal)); } catch { }
                    healCount -= toHeal;
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Wizard EscapeLosses] " + ex.Message));
            }
        }

        public int ApplyBanditBribeDiscount(int baseCost)
        {
            float discount = 0f; // 0..1
            foreach (var choiceId in PlayerCareerExtension.GetAllCareerChoices())
            {
                var ch = RFCareerChoices.GetChoice(choiceId);
                if (ch?.Passive == null) continue;

                // your perk text: "-30% bandit bribe cost"
                if (ch.Description.ToString().Contains("bandit bribe cost"))
                {
                    // passive magnitude was set as -0.30f in WizardApprentice1_3
                    discount = TaleWorlds.Library.MathF.Max(discount, -ch.Passive.EffectMagnitude);
                }
            }

            discount = TaleWorlds.Library.MathF.Clamp(discount, 0f, 0.95f);
            return (int)TaleWorlds.Library.MathF.Ceiling(baseCost * (1f - discount));
        }

        private void OnMapEventEnded_WizardPostBattleHealing(MapEvent mapEvent)
        {
            try
            {
                if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !PlayerCareerExtension.HasAnyCareer())
                    return;

                float playerFlat = 0f;   // +N HP
                float playerPct = 0f;   // +N% of current/max (we'll use current)
                float troopsFlat = 0f;   // heal N wounded
                float troopsPct = 0f;   // heal N% of wounded

                foreach (var id in PlayerCareerExtension.GetAllCareerChoices())
                {
                    var ch = RFCareerChoices.GetChoice(id);
                    if (ch?.Passive == null) continue;
                    var d = ch.Description.ToString();

                    // T2 M1_2: "+10 post-battle healing (player)"
                    if (d.Contains("+10 post-battle healing (player)")) playerFlat += 10f;

                    // T3 R1_2: "+15% post-battle healing (player)"
                    if (d.Contains("+15% post-battle healing (player)")) playerPct += 0.15f;

                    // T2 M2_2: "+10 post-battle healing (troops)"
                    if (d.Contains("+10 post-battle healing (troops)")) troopsFlat += 10f;

                    // T3 R2_2: "+15% post-battle healing (troops)"
                    if (d.Contains("+15% post-battle healing (troops)")) troopsPct += 0.15f;
                }

                // player heal
                if (playerFlat > 0f || playerPct > 0f)
                {
                    var h = Hero.MainHero;
                    // use current HP as base for % (simple and safe)
                    int baseHp = h.HitPoints;
                    int gain = (int)TaleWorlds.Library.MathF.Round(playerFlat + baseHp * playerPct);
                    if (gain > 0) h.Heal(gain, true);
                }

                // troops heal: convert to “heal wounded” count
                if (troopsFlat > 0f || troopsPct > 0f)
                {
                    var party = MobileParty.MainParty;
                    if (party != null)
                    {
                        var roster = party.MemberRoster;
                        var list = roster.GetTroopRoster();
                        int totalWounded = 0;
                        for (int i = 0; i < list.Count; i++)
                            totalWounded += list[i].WoundedNumber;

                        if (totalWounded > 0)
                        {
                            int heal = (int)TaleWorlds.Library.MathF.Round(troopsFlat + totalWounded * troopsPct);
                            for (int i = 0; i < list.Count && heal > 0; i++)
                            {
                                int wounded = list[i].WoundedNumber;
                                if (wounded <= 0) continue;
                                int toHeal = TaleWorlds.Library.MathF.Min(wounded, heal);
                                try { roster.SetElementWoundedNumber(i, TaleWorlds.Library.MathF.Max(0, wounded - toHeal)); } catch { }
                                heal -= toHeal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Wizard Healing] " + ex.Message));
            }
        }

        private void OnMapEventEnded_ClericPostBattleEffects(MapEvent mapEvent)
        {
            try
            {
                if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !PlayerCareerExtension.HasAnyCareer())
                    return;

                float playerPct = 0f;
                float troopsPct = 0f;

                if (PlayerCareerExtension.HasCareerChoice(ClericPlayerPostBattleHealingPerkId))
                    playerPct += 0.15f;
                if (PlayerCareerExtension.HasCareerChoice(ClericTroopPostBattleHealingPerkId))
                    troopsPct += 0.15f;
                if (PlayerCareerExtension.HasCareerChoice(ClericMajorBattleRecoveryPerkId))
                    troopsPct += 0.10f;

                if (playerPct > 0f)
                {
                    Hero hero = Hero.MainHero;
                    int gain = (int)TaleWorlds.Library.MathF.Round(hero.HitPoints * playerPct);
                    if (gain > 0)
                        hero.Heal(gain, true);
                }

                if (troopsPct > 0f)
                {
                    MobileParty party = MobileParty.MainParty;
                    if (party != null)
                    {
                        TroopRoster roster = party.MemberRoster;
                        var list = roster.GetTroopRoster();
                        int totalWounded = 0;
                        for (int i = 0; i < list.Count; i++)
                            totalWounded += list[i].WoundedNumber;

                        if (totalWounded > 0)
                        {
                            int heal = (int)TaleWorlds.Library.MathF.Round(totalWounded * troopsPct);
                            for (int i = 0; i < list.Count && heal > 0; i++)
                            {
                                int wounded = list[i].WoundedNumber;
                                if (wounded <= 0)
                                    continue;

                                int toHeal = TaleWorlds.Library.MathF.Min(wounded, heal);
                                try { roster.SetElementWoundedNumber(i, TaleWorlds.Library.MathF.Max(0, wounded - toHeal)); } catch { }
                                heal -= toHeal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[Cleric PostBattle] " + ex.Message));
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("playerClassInfo", ref playerClassInfo);
            dataStore.SyncData("pointsSystem", ref pointsSystem);
        }
    }
}
