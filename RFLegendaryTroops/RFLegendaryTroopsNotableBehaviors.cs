using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace RFLegendaryTroops
{
    public class RFLegendaryTroopsNotableBehaviors : RecruitmentCampaignBehavior
	{
        public static readonly Occupation occupation = Occupation.Headman;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
        }
        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement.IsCastle)
            {
                this.UpdateVolunteersOfNotablesInSettlement(settlement);
                this.DailyNotablePower(settlement);
            }
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            this.SpawnNotablesAtGameStart();
        }

        private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter)
        {
            foreach (Settlement settlement in Settlement.All.WhereQ((Settlement s) => s.IsCastle))
            {
                this.UpdateVolunteersOfNotablesInSettlement(settlement);
            }
        }

        private void DailyNotablePower(Settlement settlement)
        {
            foreach (Hero notable in settlement.Notables)
            {
                bool isStarving = settlement.IsStarving;
                if (isStarving)
                {
                    notable.AddPower(-1f);
                }
                else
                {
                    notable.AddPower(settlement.Town.Prosperity / 10000f - 0.2f);
                }
            }
        }

        private void WeeklyTick()
        {
            foreach (Settlement castle in Settlement.All.WhereQ((Settlement s) => s.IsCastle))
                SpawnNotablesIfNeeded(castle);
        }


        private void UpdateVolunteersOfNotablesInSettlement(Settlement settlement)
        {
            if (settlement.IsCastle)
            {
                foreach (Hero hero in settlement.Notables)
                {
                    if (hero.CanHaveRecruits)
                    {
                        bool flag = false;
                        //CharacterObject basicVolunteer = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(hero);
                        //CharacterObject basicVolunteer = MBObjectManager.Instance.GetObject<CharacterObject>("mercenary_1");
                        CharacterObject basicVolunteer = Helper.ChooseLegendaryTroop(settlement.MapFaction.Culture);
                        for (int i = 0; i < 6; i++)
                        {
                            if (MBRandom.RandomFloat < GetDailyVolunteerProductionProbability(hero, i, settlement))
                            {
                                CharacterObject characterObject = hero.VolunteerTypes[i];
                                if (characterObject == null)
                                {
                                    hero.VolunteerTypes[i] = basicVolunteer;
                                    flag = true;
                                }
                                else if (characterObject.UpgradeTargets.Length != 0 && characterObject.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier)
                                {
                                    float num = MathF.Log(hero.Power / (float)characterObject.Tier, 2f) * 0.01f;
                                    if (MBRandom.RandomFloat < num)
                                    {
                                        hero.VolunteerTypes[i] = characterObject.UpgradeTargets[MBRandom.RandomInt(characterObject.UpgradeTargets.Length)];
                                        flag = true;
                                    }
                                }
                            }
                        }
                        if (flag)
                        {
                            CharacterObject[] volunteerTypes = hero.VolunteerTypes;
                            for (int j = 1; j < 6; j++)
                            {
                                CharacterObject characterObject2 = volunteerTypes[j];
                                if (characterObject2 != null)
                                {
                                    int num2 = 0;
                                    int num3 = j - 1;
                                    CharacterObject characterObject3 = volunteerTypes[num3];
                                    while (num3 >= 0 && (characterObject3 == null || (float)characterObject2.Level + (characterObject2.IsMounted ? 0.5f : 0f) < (float)characterObject3.Level + (characterObject3.IsMounted ? 0.5f : 0f)))
                                    {
                                        if (characterObject3 == null)
                                        {
                                            num3--;
                                            num2++;
                                            if (num3 >= 0)
                                            {
                                                characterObject3 = volunteerTypes[num3];
                                            }
                                        }
                                        else
                                        {
                                            volunteerTypes[num3 + 1 + num2] = characterObject3;
                                            num3--;
                                            num2 = 0;
                                            if (num3 >= 0)
                                            {
                                                characterObject3 = volunteerTypes[num3];
                                            }
                                        }
                                    }
                                    volunteerTypes[num3 + 1 + num2] = characterObject2;
                                }
                            }
                        }
                    }
                }
            }
        }
        public void SpawnNotablesIfNeeded(Settlement settlement)
        {
                int targetCount = Helper.GetTargetNotableCountForSettlement(settlement);
                float countToSpawn = settlement.Notables.Any<Hero>() ? ((float)(targetCount - settlement.Notables.Count) / (float)targetCount) : 1f;
                countToSpawn *= (float)Math.Pow((double)countToSpawn, 0.36000001430511475);
                if (MBRandom.RandomFloat <= countToSpawn)
                {
                    EnterSettlementAction.ApplyForCharacterOnly(HeroCreator.CreateHeroAtOccupation(RFLegendaryTroopsNotableBehaviors.occupation, settlement), settlement);
                }
        }

        public bool CanHaveRecruits(Hero notable)
        {
            bool canHaveRecruits = notable.CanHaveRecruits;
            return canHaveRecruits || notable.CharacterObject.Occupation == RFLegendaryTroopsNotableBehaviors.occupation;
        }

        //public void UpdateVolunteersOfNotables()
        //{
        //    foreach (Settlement settlement in Campaign.Current.Settlements)
        //    {
        //        bool flag = !settlement.IsCastle;
        //        if (!flag)
        //        {
        //            foreach (Hero notable in settlement.Notables)
        //            {
        //                bool flag2 = this.CanHaveRecruits(notable);
        //                if (flag2)
        //                {
        //                    //CultureObject culture = (notable.CurrentSettlement != null) ? notable.CurrentSettlement.Culture : notable.Clan.Culture;
        //                    var aha = MBObjectManager.Instance.GetObject<CharacterObject>("mercenary_1");

        //                    //CharacterObject basicRecruit = culture.BasicTroop;
        //                    //CharacterObject nobleRecruit = culture.EliteBasicTroop;

        //                    CharacterObject basicRecruit = aha;
        //                    CharacterObject nobleRecruit = aha;

        //                    float powerful = 200f;
        //                    bool didProduction = false;
        //                    for (int recruitSlot = 0; recruitSlot < 6; recruitSlot++)
        //                    {
        //                        double recruitTypeUpgradeFactor = (notable.VolunteerTypes[recruitSlot] == basicRecruit) ? 0.25 : 1.0;
        //                        bool flag3 = MBRandom.RandomFloat < this.GetDailyVolunteerProductionProbability(notable, recruitSlot, settlement);
        //                        if (flag3)
        //                        {
        //                            bool flag4 = notable.VolunteerTypes[recruitSlot] == null;
        //                            if (flag4)
        //                            {
        //                                didProduction = true;
        //                                notable.VolunteerTypes[recruitSlot] = ((recruitSlot >= 3) ? nobleRecruit : basicRecruit);
        //                                bool flag5 = notable.VolunteerTypes[recruitSlot] == basicRecruit;
        //                                if (flag5)
        //                                {
        //                                    // breaks if can not upgradeable (final troop)
        //                                    notable.VolunteerTypes[recruitSlot] = notable.VolunteerTypes[recruitSlot].UpgradeTargets[MBRandom.RandomInt(notable.VolunteerTypes[recruitSlot].UpgradeTargets.Length)];
        //                                }
        //                            }
        //                            else
        //                            {
        //                                float powerFactor = powerful * powerful / (Math.Max(50f, notable.Power) * Math.Max(50f, notable.Power));
        //                                int tier = notable.VolunteerTypes[recruitSlot].Tier;
        //                                bool flag6 = MBRandom.RandomInt((int)Math.Max(2.0, (double)((float)tier * powerFactor) * recruitTypeUpgradeFactor * 4.0)) == 0 && notable.VolunteerTypes[recruitSlot].UpgradeTargets != null && notable.VolunteerTypes[recruitSlot].Tier <= 3;
        //                                if (flag6)
        //                                {
        //                                    didProduction = true;
        //                                    bool flag7 = notable.VolunteerTypes[recruitSlot] == basicRecruit;
        //                                    if (flag7)
        //                                    {
        //                                        notable.VolunteerTypes[recruitSlot] = nobleRecruit;
        //                                    }
        //                                    else
        //                                    {
        //                                        notable.VolunteerTypes[recruitSlot] = notable.VolunteerTypes[recruitSlot].UpgradeTargets[MBRandom.RandomInt(notable.VolunteerTypes[recruitSlot].UpgradeTargets.Length)];
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                    bool flag8 = didProduction;
        //                    if (flag8)
        //                    {
        //                        MercenaryAddonNotableBehaviors.SortRecruits(notable);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}


        //public static void SortRecruits(Hero notable)
        //{
        //    for (int i = 0; i < 6; i++)
        //    {
        //        for (int j = 0; j < 6; j++)
        //        {
        //            bool flag = notable.VolunteerTypes[j] != null;
        //            if (flag)
        //            {
        //                for (int k = j + 1; k < 6; k++)
        //                {
        //                    bool flag2 = notable.VolunteerTypes[k] != null;
        //                    if (flag2)
        //                    {
        //                        bool flag3 = (float)notable.VolunteerTypes[j].Level + (notable.VolunteerTypes[j].IsMounted ? 0.5f : 0f) > (float)notable.VolunteerTypes[k].Level + (notable.VolunteerTypes[k].IsMounted ? 0.5f : 0f);
        //                        if (flag3)
        //                        {
        //                            CharacterObject characterObject = notable.VolunteerTypes[j];
        //                            notable.VolunteerTypes[j] = notable.VolunteerTypes[k];
        //                            notable.VolunteerTypes[k] = characterObject;
        //                        }
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        private void SpawnNotablesAtGameStart()
        {
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsCastle)
                {
                    int targetNotableCountForSettlement = Helper.GetTargetNotableCountForSettlement(settlement);
                    for (int i = 0; i < targetNotableCountForSettlement; i++)
                    {
                        HeroCreator.CreateHeroAtOccupation(RFLegendaryTroopsNotableBehaviors.occupation, settlement);
                    }
                }
            }
        }
        public float GetDailyVolunteerProductionProbability(Hero notable, int slot, Settlement settlement)
        {
            float num = 0.72f;
            num += ((notable.CurrentSettlement != null && notable.CurrentSettlement.MapFaction.Fiefs.Count<Town>() < 11) ? ((float)(11 - notable.CurrentSettlement.MapFaction.Fiefs.Count<Town>()) * 0.02f) : 0f);
            float baseNumber = 0.66f * MathF.Clamp(MathF.Pow(num, (float)(slot + 1)), 0f, 1f);
            ExplainedNumber bonuses = new(baseNumber, false, null);
            Clan clan = notable.Clan;
            if ((clan?.Kingdom) != null && notable.Clan.Kingdom.ActivePolicies.Contains(DefaultPolicies.Cantons))
            {
                bonuses.AddFactor(0.2f, null);
            }
            Town town = settlement.Town;
            bool flag2 = PerkHelper.GetPerkValueForTown(DefaultPerks.Riding.CavalryTactics, town) && notable.VolunteerTypes[slot] != null && notable.VolunteerTypes[slot].IsMounted;
            if (flag2)
            {
                Hero leader = town.Settlement.OwnerClan.Leader;
                PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Riding.CavalryTactics, leader.CharacterObject, true, ref bonuses);
            }
            return bonuses.ResultNumber;
        }

    }

}