using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.CharacterCreation.CharacterCreationConfig;
using static RealmsForgotten.Globals;

namespace RealmsForgotten.CharacterCreation
{
    public static class CulturedStartAction
    {
        public static void Apply(StartType startType, string startSettlement)
        {
            try
            {
                Hero mainHero = Hero.MainHero;

                // remove original items
                GiveGoldAction.ApplyBetweenCharacters(mainHero, null, mainHero.Gold, true);
                mainHero.PartyBelongedTo.ItemRoster.Clear();

                CustomStartData data = CustomStartData.All[startType];
                GiveGoldAction.ApplyBetweenCharacters(null, mainHero, data.Gold, true);
                mainHero.PartyBelongedTo.ItemRoster.AddToCounts(DefaultItems.Grain, data.Grain);
                mainHero.PartyBelongedTo.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("mule"), data.Mules);
                mainHero.Clan.AddRenown(Campaign.Current.Models.ClanTierModel.GetRequiredRenownForTier(data.ClanTier));
                AddCompanions(data.NumberOfCompanions, data.CompanionParties, startType);

                List<TroopSpawnInfo> troops = GetTroopsForStartOption(mainHero.Culture.StringId, startType);
                foreach (TroopSpawnInfo troopInfo in troops)
                {
                    CharacterObject troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopInfo.TroopId);
                    if (troop != null)
                        mainHero.PartyBelongedTo.AddElementToMemberRoster(troop, troopInfo.Quantity, false);
                }

                foreach (SkillObject skill in Skills.All)
                    mainHero.SetSkillValue(skill, (int)(mainHero.GetSkillValue(skill) * startingSkillMult[startType]));


                Kingdom kingdom = Kingdom.All.Where(k => k.Culture == mainHero.Culture).FirstOrDefault();
                switch (startType)
                {
                    case StartType.Mercenary:
                        mainHero.PartyBelongedTo.RecentEventsMorale -= 40;
                        break;
                    case StartType.Exiled:
                        ChangeCrimeRatingAction.Apply(kingdom, 50, false);
                        CharacterRelationManager.SetHeroRelation(mainHero, kingdom.RulingClan.Leader, -50);
                        foreach (Hero lord in kingdom.AliveLords)
                            CharacterRelationManager.SetHeroRelation(mainHero, lord, -10);
                        break;
                    case StartType.King:
                        ChangeKingdomAction.ApplyByJoinToKingdom(mainHero.Clan, kingdom);
                        ChangeRulingClanAction.Apply(kingdom, mainHero.Clan);
                        mainHero.Clan.Influence = 200;
                        Settlement city = kingdom.Settlements.GetRandomElementWithPredicate(settlement => settlement.IsTown);
                        ChangeOwnerOfSettlementAction.ApplyByDefault(mainHero, city);
                        Settlement castle = kingdom.Settlements.GetRandomElementWithPredicate(settlement => settlement.IsCastle);
                        ChangeOwnerOfSettlementAction.ApplyByDefault(mainHero, castle);
                        break;
                    case StartType.Knight:
                        CharacterRelationManager.SetHeroRelation(mainHero, kingdom.RulingClan.Leader, 20);
                        ChangeKingdomAction.ApplyByJoinToKingdom(mainHero.Clan, kingdom, default, false);
                        mainHero.Clan.Influence = 500;
                        break;
                    case StartType.Usurper:
                        Settlement settlement = mainHero.Clan.Kingdom.Settlements.GetRandomElementWithPredicate(settlement => settlement.IsCastle);
                        ChangeOwnerOfSettlementAction.ApplyByDefault(mainHero, settlement);
                        Campaign.Current.KingdomManager.CreateKingdom(mainHero.Clan.Name, mainHero.Clan.InformalName, mainHero.Clan.Culture, mainHero.Clan);
                        mainHero.Clan.Influence = 50;
                        break;
                    case StartType.Outlaw:
                        foreach (Kingdom k in Campaign.Current.Kingdoms)
                            ChangeCrimeRatingAction.Apply(k.MapFaction, 50, false);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new($"Error in character creation for start type: {startType}, settlement: {startSettlement}. Message: {e.Message}"));
            }
            Settlement? startingSettlement = startSettlement == RFCharacterCreationCampaignBehavior.IS_PLAYER_SETTLEMENT ? Clan.PlayerClan.Settlements.GetRandomElement() : Settlement.Find(startSettlement);

            if (startingSettlement == null)
                InformationManager.DisplayMessage(new($"Error, settlement with id {startSettlement} not found."));
            else
            {
                MobileParty.MainParty.Position = startingSettlement.GatePosition;
                if (GameStateManager.Current.ActiveState is MapState mapState)
                {
                    mapState.Handler.ResetCamera(true, true);
                    mapState.Handler.TeleportCameraToMainParty();
                }
            }
        }
        private static void AddCompanions(int companions = 0, int companionParties = 0, StartType startOption = StartType.Default)
        {
            Clan playerClan = Clan.PlayerClan;
            for (int i = 0; i < companions; i++)
            {
                CharacterObject wanderer = (from character in CharacterObject.All
                                            where character.Occupation == Occupation.Wanderer && character.Culture == Hero.MainHero.Culture
                                            select character).GetRandomElementInefficiently();
                Settlement randomSettlement = (from settlement in Settlement.All
                                               where settlement.Culture == wanderer?.Culture && settlement.IsTown
                                               select settlement).GetRandomElementInefficiently();
                if (wanderer == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("CULTURED START WANDERER ERROR", Colors.Red));
                    break;
                }
                Hero companion = HeroCreator.CreateSpecialHero(wanderer, randomSettlement, null, null, 33);
                companion.Clan = randomSettlement.OwnerClan;
                companion.ChangeState(Hero.CharacterStates.Active);
                if (startOption == StartType.King || startOption == StartType.Usurper || startOption == StartType.Knight) // gives companions noble equipment
                {
                    try
                    {
                        companion.BattleEquipment.FillFrom(Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(companion, false)[0].AllEquipments.GetRandomElement());
                        if (companion.IsFemale)
                            companion.CivilianEquipment.FillFrom(ChooseLadyCivillianEquipment(companion)[0].AllEquipments.GetRandomElement());
                        else
                            companion.CivilianEquipment.FillFrom(Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(companion, true)[0].AllEquipments.GetRandomElement());

                    }
                    catch (Exception)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("ERROR FILLING EQUIPMENT ON COMPANION", Colors.Red));
                    }
                }
                AddCompanionAction.Apply(Clan.PlayerClan, companion);
                AddHeroToPartyAction.Apply(companion, MobileParty.MainParty, false);
                GiveGoldAction.ApplyBetweenCharacters(null, companion, 2000, true);
                if (i < companionParties)
                    MobilePartyHelper.CreateNewClanMobileParty(companion, Hero.MainHero.Clan);
            }
        }
        private static List<MBEquipmentRoster> ChooseLadyCivillianEquipment(Hero companion)
        {
            return MBEquipmentRosterExtensions.All.Where(a => a.EquipmentCulture == companion.Culture
                    && a.HasEquipmentFlags(EquipmentFlags.IsFemaleTemplate)
                    && a.HasEquipmentFlags(EquipmentFlags.IsNobleTemplate)
                    && a.HasEquipmentFlags(EquipmentFlags.IsNoncombatantTemplate)).ToList();
        }
    }
}