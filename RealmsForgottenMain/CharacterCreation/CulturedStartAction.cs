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
using static TaleWorlds.Core.Equipment;

namespace RealmsForgotten.CharacterCreation
{
    public static class CulturedStartAction
    {
        public static void Apply(StartType startType, string startSettlement)
        {
            try
            {
                Hero mainHero = Hero.MainHero;

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

                Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k.Culture == mainHero.Culture);
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
                        mainHero.Clan.Influence = 500;
                        Settlement city = kingdom.Settlements.GetRandomElementWithPredicate(settlement => settlement.IsTown);
                        ChangeOwnerOfSettlementAction.ApplyByDefault(mainHero, city);
                        Settlement castle = kingdom.Settlements.GetRandomElementWithPredicate(settlement => settlement.IsCastle);
                        ChangeOwnerOfSettlementAction.ApplyByDefault(mainHero, castle);
                        break;

                    case StartType.Knight:
                        CharacterRelationManager.SetHeroRelation(mainHero, kingdom.RulingClan.Leader, 20);
                        ChangeKingdomAction.ApplyByJoinToKingdom(mainHero.Clan, kingdom, default, false);
                        mainHero.Clan.Influence = 100;
                        break;

                    case StartType.Usurper:
                        Settlement settlement = kingdom.Settlements.GetRandomElementWithPredicate(s => s.IsCastle);
                        ChangeOwnerOfSettlementAction.ApplyByDefault(mainHero, settlement);
                        Campaign.Current.KingdomManager.CreateKingdom(mainHero.Clan.Name, mainHero.Clan.InformalName, mainHero.Clan.Culture, mainHero.Clan);
                        mainHero.Clan.Influence = 300;
                        break;

                    case StartType.Outlaw:
                        foreach (Kingdom k in Campaign.Current.Kingdoms)
                            ChangeCrimeRatingAction.Apply(k.MapFaction, 50, false);
                        break;
                }
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Error in character creation for start type: {startType}, settlement: {startSettlement}. Message: {e.Message}",
                    Colors.Red));
            }

            Settlement startingSettlement = startSettlement == RFCharacterCreationCampaignBehavior.IS_PLAYER_SETTLEMENT
                ? Clan.PlayerClan.Settlements.GetRandomElement()
                : Settlement.Find(startSettlement);

            if (startingSettlement == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Error, settlement with id {startSettlement} not found.",
                    Colors.Red));
            }
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
                CharacterObject wanderer = CharacterObject.All
                    .Where(character => character.Occupation == Occupation.Wanderer && character.Culture == Hero.MainHero.Culture)
                    .GetRandomElementInefficiently();

                if (wanderer == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("CULTURED START WANDERER ERROR", Colors.Red));
                    break;
                }

                Settlement randomSettlement = Settlement.All
                    .Where(settlement => settlement.Culture == wanderer.Culture && settlement.IsTown)
                    .GetRandomElementInefficiently();

                Hero companion = HeroCreator.CreateSpecialHero(wanderer, randomSettlement, null, null, 33);
                companion.Clan = randomSettlement.OwnerClan;
                companion.ChangeState(Hero.CharacterStates.Active);

                if (startOption == StartType.King || startOption == StartType.Usurper || startOption == StartType.Knight)
                {
                    try
                    {
                        companion.BattleEquipment.FillFrom(
                            Campaign.Current.Models.EquipmentSelectionModel
                                .GetEquipmentForHeroComeOfAge(companion, EquipmentType.Battle));

                        companion.CivilianEquipment.FillFrom(
                            Campaign.Current.Models.EquipmentSelectionModel
                                .GetEquipmentForHeroComeOfAge(companion, EquipmentType.Civilian));
                    }
                    catch (Exception ex)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"ERROR FILLING EQUIPMENT ON COMPANION: {ex.Message}",
                            Colors.Red));
                    }
                }

                AddCompanionAction.Apply(playerClan, companion);
                AddHeroToPartyAction.Apply(companion, MobileParty.MainParty, false);
                GiveGoldAction.ApplyBetweenCharacters(null, companion, 2000, true);

                if (i < companionParties)
                    MobilePartyHelper.CreateNewClanMobileParty(companion, Hero.MainHero.Clan);
            }
        }
    }
}
