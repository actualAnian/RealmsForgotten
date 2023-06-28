using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Managers
{
    public static class CulturedStartAction
    {
        public static void Apply(int storyOption, int locationOption)
        {
            Hero mainHero = Hero.MainHero;
            Hero ruler = Hero.FindAll(hero => hero.Culture == mainHero.Culture && hero.IsAlive && hero.IsFactionLeader && !hero.MapFaction.IsMinorFaction).GetRandomElementInefficiently();
            Hero captor = Hero.FindAll(hero => hero.Culture == mainHero.Culture && hero.IsAlive && hero.MapFaction != null && !hero.MapFaction.IsMinorFaction && hero.IsPartyLeader && hero.PartyBelongedTo.DefaultBehavior != AiBehavior.Hold).GetRandomElementInefficiently();

            Settlement? startingSettlement = null;
            GiveGoldAction.ApplyBetweenCharacters(mainHero, null, mainHero.Gold, true);
            mainHero.PartyBelongedTo.ItemRoster.Clear();
            switch (locationOption)
            {
                case 0:
                    startingSettlement = mainHero.HomeSettlement;
                    break;
                case 1:
                    startingSettlement = Settlement.FindAll(settlement => settlement.IsTown).GetRandomElementInefficiently();
                    break;
                case 2:
                    startingSettlement = Settlement.Find("town_A8");
                    break;
                case 3:
                    startingSettlement = Settlement.Find("town_B2");
                    break;
                case 4:
                    startingSettlement = Settlement.Find("town_EW2");
                    break;
                case 5:
                    startingSettlement = Settlement.Find("town_S2");
                    break;
                case 6:
                    startingSettlement = Settlement.Find("town_K4");
                    break;
                case 7:
                    startingSettlement = Settlement.Find("town_V3");
                    break;
                case 8:
                    startingSettlement = Settlement.All.Where(settlement => settlement.Culture == mainHero.Culture && settlement.IsCastle).GetRandomElementInefficiently();
                    break;
                default:
                    break;
            }
            mainHero.PartyBelongedTo.Position2D = locationOption != 9 ? (startingSettlement != null ? startingSettlement.GatePosition : Settlement.Find("tutorial_training_field").Position2D) : captor.PartyBelongedTo.Position2D;
            if (GameStateManager.Current.ActiveState is MapState mapState)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }

            switch (storyOption)
            {
                case 0: // Default
                    ApplyInternal(mainHero, gold: 1000, grain: 2);
                    break;
                case 1: // Merchant
                    ApplyInternal(mainHero, gold: 8000, grain: 250, mules: 25, troops: new int[] { 12, 7 });
                    break;
                case 2: // Exiled
                    ApplyInternal(mainHero, gold: 3000, grain: 15, tier: 4, companions: 1);
                    if (ruler != null)
                    {
                        ChangeCrimeRatingAction.Apply(ruler.MapFaction, 50, false);
                        CharacterRelationManager.SetHeroRelation(mainHero, ruler, -50);
                        foreach (Hero lord in Hero.FindAll(hero => hero.MapFaction == ruler.MapFaction && !hero.IsFactionLeader && hero.IsAlive))
                        {
                            CharacterRelationManager.SetHeroRelation(mainHero, lord, -10);
                        }
                    }
                    break;
                case 3: // Mercenary
                    ApplyInternal(mainHero, gold: 5000, grain: 25, tier: 2, troops: new int[] { 10, 5, 3, 1 }, isMercenary: true);
                    mainHero.PartyBelongedTo.RecentEventsMorale -= 40;
                    break;
                case 4: // Looter
                    ApplyInternal(mainHero, gold: 500, grain: 10, troops: new int[] { 7, 5 }, isLooter: true);
                    foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
                    {
                        ChangeCrimeRatingAction.Apply(kingdom.MapFaction, 50, false);
                    }
                    break;
                case 5: // Vassal
                    ApplyInternal(mainHero, gold: 15000, grain: 40, tier: 3, troops: new int[] { 40, 10 }, ruler: ruler);
                    break;
                case 6: // Kingdom
                    ApplyInternal(mainHero, gold: 45000, grain: 150, tier: 5, troops: new int[] { 60, 30, 20, 10, 6 }, companions: 3, companionParties: 2, hasKingdom: true);
                    break;
                case 7: // Holding
                    ApplyInternal(mainHero, gold: 60000, grain: 30, tier: 3, troops: new int[] { 31, 20, 14, 10, 6 }, companions: 1, companionParties: 1, castle: startingSettlement, hasKingdom: true);
                    break;
                case 8: // Landed Vassal
                    ApplyInternal(mainHero, gold: 35000, grain: 80, tier: 2, troops: new int[] { 60, 20 }, companions: 1, companionParties: 1, ruler: ruler, castle: startingSettlement, isLandedVassal: true);
                    break;
                case 9: // Escaped Prisoner
                    ApplyInternal(mainHero, gold: 0, grain: 1, isLooter: true);
                    if (captor != null)
                    {
                        CharacterRelationManager.SetHeroRelation(mainHero, captor, -50);
                    }
                    break;
                default:
                    break;
            }
        }

        private static void ApplyInternal(Hero mainHero, int gold, int grain, int mules = 0, int tier = -1, int[] troops = null, int companions = 0, int companionParties = 0, Hero ruler = null, Settlement castle = null, bool isMercenary = false, bool isLooter = false, bool isLandedVassal = false, bool hasKingdom = false)
        {
            Settlement? givenCastle = null;
            CharacterObject? idealTroop = null;
            GiveGoldAction.ApplyBetweenCharacters(null, mainHero, gold, true);
            mainHero.PartyBelongedTo.ItemRoster.AddToCounts(DefaultItems.Grain, grain);
            mainHero.PartyBelongedTo.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("mule"), mules);
            if (isMercenary)
            {
                idealTroop = (from character in CharacterObject.All
                              where character.Tier == tier && !character.IsHero && character.Occupation == Occupation.Mercenary && !character.Equipment.IsEmpty() && MatchWildcardString("mercenary*", "" + character.StringId)
                              select character).GetRandomElementInefficiently();
            }
            else if (isLooter)
            {

                if (mainHero.Culture == MBObjectManager.Instance.GetObject<CharacterObject>("aserai_recruit").Culture) // The City States of Athas
                    idealTroop = MBObjectManager.Instance.GetObject<CharacterObject>("mountain_bandits_bandit");
                else if (mainHero.Culture == MBObjectManager.Instance.GetObject<CharacterObject>("imperial_recruit").Culture) // Kingdoms of Men
                    idealTroop = MBObjectManager.Instance.GetObject<CharacterObject>("deserter");
                else if (mainHero.Culture == MBObjectManager.Instance.GetObject<CharacterObject>("khuzait_nomad").Culture) // Al-Kuuhr
                    idealTroop = MBObjectManager.Instance.GetObject<CharacterObject>("steppe_bandits_bandit");
                else if (mainHero.Culture == MBObjectManager.Instance.GetObject<CharacterObject>("sturgian_recruit").Culture) // The Dreaddrealms
                    idealTroop = MBObjectManager.Instance.GetObject<CharacterObject>("sea_raiders_bandit");
                else if (mainHero.Culture == MBObjectManager.Instance.GetObject<CharacterObject>("battanian_volunteer").Culture) // High Kingdom of the Elveans
                    idealTroop = MBObjectManager.Instance.GetObject<CharacterObject>("forest_bandits_bandit");
                else if (mainHero.Culture == MBObjectManager.Instance.GetObject<CharacterObject>("vlandian_recruit").Culture) // Easterners Tribes
                    idealTroop = MBObjectManager.Instance.GetObject<CharacterObject>("desert_bandits_bandit");
                tier = idealTroop.Tier;
            }
            if (idealTroop != null)
            {
                mainHero.BattleEquipment.FillFrom(idealTroop.Equipment);
            }
            for (int i = 0; i < troops?.Length; i++)
            {
                int troopTier = i + 1;
                int num = troops[i];
                CharacterObject troop = (from character in CharacterObject.All
                                         where character.Tier == troopTier && character.Culture == mainHero.Culture && !character.IsHero && character.Occupation == Occupation.Soldier &&
                                         !MatchWildcardString("*_militia_*", "" + character.StringId) && !MatchWildcardString("*armed*", "" + character.StringId) && !MatchWildcardString("*caravan*", "" + character.StringId)
                                         && !MatchWildcardString("*mercenary*", "" + character.StringId)
                                         select character).GetRandomElementInefficiently();
                if (idealTroop?.Occupation == Occupation.Bandit)
                {
                    troop = idealTroop;
                }
                if (idealTroop?.Occupation == Occupation.Mercenary)
                {
                    troop = idealTroop;
                }
                mainHero.PartyBelongedTo.AddElementToMemberRoster(troop, num, false);
            }
            for (int i = 0; i < companions; i++)
            {
                CharacterObject wanderer = (from character in CharacterObject.All
                                            where character.Occupation == Occupation.Wanderer && character.Culture == mainHero.Culture
                                            select character).GetRandomElementInefficiently();
                Settlement randomSettlement = (from settlement in Settlement.All
                                               where settlement.Culture == wanderer.Culture && settlement.IsTown
                                               select settlement).GetRandomElementInefficiently();
                Hero companion = HeroCreator.CreateSpecialHero(wanderer, randomSettlement, null, null, 33);
                companion.HeroDeveloper.DeriveSkillsFromTraits(false, wanderer);
                //companion.HasMet = true;
                companion.Clan = randomSettlement.OwnerClan;
                companion.ChangeState(Hero.CharacterStates.Active);
                if (idealTroop != null)
                {
                    companion.BattleEquipment.FillFrom(idealTroop.Equipment);
                }
                AddCompanionAction.Apply(Clan.PlayerClan, companion);
                AddHeroToPartyAction.Apply(companion, mainHero.PartyBelongedTo, false);
                GiveGoldAction.ApplyBetweenCharacters(null, companion, 2000, true);
                if (i < companionParties)
                {
                    MobilePartyHelper.CreateNewClanMobileParty(companion, mainHero.Clan, out bool fromMainclan);
                }
            }
            if (ruler != null)
            {
                // Adding to prevent crash on custom cultures with no kingdom
                CharacterRelationManager.SetHeroRelation(mainHero, ruler, 10);
                ChangeKingdomAction.ApplyByJoinToKingdom(mainHero.Clan, ruler.Clan.Kingdom, false);
                mainHero.Clan.Influence = 10;
            }
            if (castle != null)
            {
                ChangeOwnerOfSettlementAction.ApplyByKingDecision(mainHero, castle);
                if (isLandedVassal)
                {
                    givenCastle = (from settlement in Settlement.All
                                   where settlement.Culture == mainHero.Culture && settlement.IsCastle
                                   select settlement).GetRandomElementInefficiently();
                    ChangeOwnerOfSettlementAction.ApplyByKingDecision(mainHero, givenCastle);
                }
            }
            if (hasKingdom)
            {
                Campaign.Current.KingdomManager.CreateKingdom(mainHero.Clan.Name, mainHero.Clan.InformalName, mainHero.Clan.Culture, mainHero.Clan);
                mainHero.Clan.Influence = 100;
            }
        }

        public static bool MatchWildcardString(String pattern, String input)
        {
            if (String.Compare(pattern, input) == 0)
            {
                return true;
            }
            else if (String.IsNullOrEmpty(input))
            {
                if (String.IsNullOrEmpty(pattern.Trim(new Char[1] { '*' })))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (pattern.Length == 0)
            {
                return false;
            }
            else if (pattern[0] == '*')
            {
                if (MatchWildcardString(pattern.Substring(1), input))
                {
                    return true;
                }
                else
                {
                    return MatchWildcardString(pattern, input.Substring(1));
                }
            }
            else if (pattern[pattern.Length - 1] == '*')
            {
                if (MatchWildcardString(pattern.Substring(0, pattern.Length - 1), input))
                {
                    return true;
                }
                else
                {
                    return MatchWildcardString(pattern, input.Substring(0, input.Length - 1));
                }
            }
            else if (pattern[0] == input[0])
            {
                return MatchWildcardString(pattern.Substring(1), input.Substring(1));
            }
            return false;
        }
    }
}
