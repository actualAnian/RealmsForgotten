using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using static RealmsForgotten.Globals;

namespace RealmsForgotten.Managers
{
    public static class CulturedStartAction
    {
        public static readonly Dictionary<StartType, Dictionary<string, string>> mainHeroStartingEquipment = new()
        {
            [StartType.Default] = new Dictionary<string, string>
            {
                ["aserai"] = "player_char_creation_default",
                ["empire"] = "player_char_creation_default",
                ["khuzait"] = "player_char_creation_default",
                ["sturgia"] = "player_char_creation_default",
                ["battania"] = "player_char_creation_default",
                ["vlandia"] = "player_char_creation_default",
                ["giant"] = "rf_xilan_default",
                ["aqarun"] = "rf_aqarun_default"
            },
            [StartType.Merchant] = new Dictionary<string, string>
            {
                ["aserai"] = "merchant_start_aserai",
                ["empire"] = "merchant_start_empire",
                ["khuzait"] = "merchant_start_khuzait",
                ["sturgia"] = "merchant_start_sturgia",
                ["battania"] = "rf_elvean_merchant",
                ["vlandia"] = "merchant_start_vlandia",
                ["giant"] = "merchant_start_xilan",
                ["aqarun"] = "merchant_start_aqarun"
            },
            [StartType.Exiled] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_exiled_aserai",
                ["empire"] = "rf_exiled_empire",
                ["khuzait"] = "rf_exiled_khuzait",
                ["sturgia"] = "rf_exiled_sturgia",
                ["battania"] = "rf_exiled_battania",
                ["vlandia"] = "rf_exiled_vlandia",
                ["giant"] = "rf_exiled_battania",
                ["aqarun"] = "rf_exiled_aserai"
            },
            [StartType.EscapedPrisoner] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_athas_mistic",
                ["empire"] = "rf_empire_mistic",
                ["khuzait"] = "rf_khuzait_mistic",
                ["sturgia"] = "rf_sturgia_mistic",
                ["battania"] = "rf_elvean_mistic",
                ["vlandia"] = "rf_nasoria_mistic",
                ["giant"] = "rf_giant_mistic",
                ["aqarun"] = "rf_aqarun_mistic"
            },
            [StartType.Looter] = new Dictionary<string, string>
            {
                ["aserai"] = "rf_looter",
                ["empire"] = "rf_looter",
                ["khuzait"] = "rf_looter",
                ["sturgia"] = "rf_looter",
                ["battania"] = "rf_looter",
                ["vlandia"] = "rf_looter",
                ["giant"] = "rf_looter",
                ["aqarun"] = "rf_looter"
            },
            [StartType.Mercenary] = new Dictionary<string, string>
            {
                ["aserai"] = "merc_athas_start",
                ["empire"] = "merc_realms_start",
                ["khuzait"] = "merc_allkhuur_start",
                ["sturgia"] = "merc_vortiak_start",
                ["battania"] = "merc_elvean_start",
                ["vlandia"] = "merc_nasoria_start",
                ["giant"] = "merc_giant_start",
                ["aqarun"] = "merc_athas_start"
            },
            [StartType.VassalNoFief] = new Dictionary<string, string>
            {
                ["aserai"] = "athas_vassal_nofief_equip",
                ["empire"] = "realms_vassal_nofief",
                ["khuzait"] = "khuzait_vassal_nofief",
                ["sturgia"] = "dreadrealms_vassal_nofief",
                ["battania"] = "elvean_vassal_nofief",
                ["vlandia"] = "nasoria_vassal_nofief",
                ["giant"] = "giant_vassal_nofief",
                ["aqarun"] = "vassalnofief_aqarun_start"
            },
            [StartType.KingdomRuler] = new Dictionary<string, string>
            {
                ["aserai"] = "king_athas_start",
                ["empire"] = "king_realms_start",
                ["khuzait"] = "king_allkhuur_start",
                ["sturgia"] = "king_vortiak_start",
                ["battania"] = "king_elvean_start",
                ["vlandia"] = "king_nasoria_start",
                ["giant"] = "king_giant_start",
                ["aqarun"] = "king_aqarun_start"
            },
            [StartType.CastleRuler] = new Dictionary<string, string>
            {
                ["aserai"] = "vassal_athas_start",
                ["empire"] = "vassal_realms_start",
                ["khuzait"] = "vassal_allkhuur_start",
                ["sturgia"] = "vassal_vortiak_start",
                ["battania"] = "vassal_elvean_start",
                ["vlandia"] = "vassal_nasoria_start",
                ["giant"] = "vassal_giant_start",
                ["aqarun"] = "vassal_aqarun_start"
            },
            [StartType.VassalFief] = new Dictionary<string, string>
            {
                ["aserai"] = "ruler_athas_start",
                ["empire"] = "ruler_realms_start",
                ["khuzait"] = "ruler_allkhuur_start",
                ["sturgia"] = "ruler_dreadrealms_start",
                ["battania"] = "lord_elvean_start",
                ["vlandia"] = "ruler_nasoria_start",
                ["giant"] = "ruler_giant_start",
                ["aqarun"] = "ruler_aqarun_start"
            },
         };
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
                    if (mainHero.Culture.StringId == "giant")
                    {
                        startingSettlement = Settlement.Find("town_G1");
                        if (startingSettlement == null)
                        {
                            // Handle the case where "town_G1" does not exist or is not found
                            // For example, log an error or assign a default settlement
                        }
                    }
                    else
                    {
                        // Fallback for other cultures or default handling
                        // You can define what happens for non-giant cultures here
                    }
                    break;
                case 9:  // New case for starting at a specific location based on culture
                    startingSettlement = Settlement.All.Where(settlement => settlement.Culture == mainHero.Culture && settlement.IsCastle).GetRandomElementInefficiently();
                    ChangeOwnerOfSettlementAction.ApplyByBarter(Hero.MainHero, startingSettlement);
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

            StartType startOption = (StartType)storyOption;
            switch (startOption)
            {
                case StartType.Default: // Default
                    ApplyInternal(mainHero, gold: 1000, grain: 2);
                    break;
                case StartType.Merchant: // Merchant
                    ApplyInternal(mainHero, gold: 8000, grain: 250, mules: 25, troops: new int[] { 12, 7 }, startOption: StartType.Merchant);
                    break;
                case StartType.Exiled: // Exiled
                    ApplyInternal(mainHero, gold: 3000, grain: 15, tier: 4, companions: 1, startOption: StartType.Exiled);
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
                case StartType.Mercenary: // Mercenary
                    ApplyInternal(mainHero, gold: 5000, grain: 25, tier: 2, troops: new int[] { 10, 5, 3, 1 }, startOption: StartType.Mercenary);
                    mainHero.PartyBelongedTo.RecentEventsMorale -= 40;
                    break;
                case StartType.Looter: // Looter
                    ApplyInternal(mainHero, gold: 500, grain: 10, troops: new int[] { 7, 5 }, startOption: StartType.Looter);
                    foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
                    {
                        ChangeCrimeRatingAction.Apply(kingdom.MapFaction, 50, false);
                    }
                    break;
                case StartType.VassalNoFief: // Vassal
                    ApplyInternal(mainHero, gold: 15000, grain: 40, tier: 3, troops: new int[] { 30, 10, 10 }, ruler: ruler, startOption: StartType.VassalNoFief);
                    break;
                case StartType.KingdomRuler: // Kingdom
                    ApplyInternal(mainHero, gold: 45000, grain: 150, tier: 5, troops: new int[] { 30, 50, 25, 10, 10 }, companions: 3, companionParties: 2, startOption: StartType.KingdomRuler);
                    break;
                case StartType.CastleRuler: // Holding
                    ApplyInternal(mainHero, gold: 60000, grain: 30, tier: 3, troops: new int[] { 31, 20, 14, 10, 6 }, companions: 1, companionParties: 1, startingSettlement: startingSettlement, startOption: StartType.CastleRuler);
                    break;
                case StartType.VassalFief: // Landed Vassal
                    ApplyInternal(mainHero, gold: 35000, grain: 80, tier: 2, troops: new int[] { 40, 20, 20, 5 }, companions: 1, companionParties: 1, ruler: ruler, startingSettlement: startingSettlement, startOption: StartType.VassalFief);
                    break;
                case StartType.EscapedPrisoner: // Escaped Prisoner
                    ApplyInternal(mainHero, gold: 1000, grain: 15, startOption: StartType.EscapedPrisoner);
                    if (captor != null)
                    {
                        CharacterRelationManager.SetHeroRelation(mainHero, captor, -50);
                    }
                    break;
                default:
                    break;
            }
        }

        private static void ApplyInternal(Hero mainHero, int gold, int grain, int mules = 0, int tier = -1, int[]? troops = null, int companions = 0, int companionParties = 0, Hero? ruler = null, Settlement? startingSettlement = null, StartType startOption = StartType.Default)
        {
            Settlement? givenCastle = null;
            //MBEquipmentRoster? idealEquipment = null;
            GiveGoldAction.ApplyBetweenCharacters(null, mainHero, gold, true);
            mainHero.PartyBelongedTo.ItemRoster.AddToCounts(DefaultItems.Grain, grain);
            mainHero.PartyBelongedTo.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("mule"), mules);

            //try
            //{
            //    idealEquipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(mainHeroStartingEquipment[startOption][mainHero.Culture.StringId]);
            //}
            //catch (Exception)
            //{
            //    //idealEquipment = MBObjectManager.Instance.GetObject<CharacterObject>("rf_looter").AllEquipments;
            //    idealEquipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("rf_looter");
            //}
            //if (idealEquipment != null)
            //{
            //    mainHero.BattleEquipment.FillFrom(idealEquipment.AllEquipments.GetRandomElement());
            //}
            foreach (SkillObject skill in Skills.All)
            {
                mainHero.SetSkillValue(skill, (int)(mainHero.GetSkillValue(skill) * startingSkillMult[startOption]));
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

                if (troop != null) mainHero.PartyBelongedTo.AddElementToMemberRoster(troop, num, false);
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
                if (startOption == StartType.KingdomRuler || startOption == StartType.CastleRuler || startOption == StartType.VassalFief) // gives companions noble equipment
                {
                    companion.BattleEquipment.FillFrom(Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(companion, false)[0].AllEquipments.GetRandomElement());
                    companion.CivilianEquipment.FillFrom(Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForHeroComeOfAge(companion, true)[0].AllEquipments.GetRandomElement());
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
                if (startOption == StartType.VassalFief)
                {
                    givenCastle = (from settlement in Settlement.All
                                   where settlement.IsCastle && mainHero.Clan.Kingdom == ruler.Clan.Kingdom
                                   select settlement).GetRandomElementInefficiently();
                    ChangeOwnerOfSettlementAction.ApplyByKingDecision(mainHero, givenCastle);
                }

            }

            if (startOption == StartType.KingdomRuler || startOption == StartType.CastleRuler)
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