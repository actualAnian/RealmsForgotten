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
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(DailyTickSettlement));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreated));
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnNewGameCreatedPartialFollowUpEnd));
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
        }
        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement.IsCastle)
            {
                UpdateVolunteersOfNotablesInSettlement(settlement);
                DailyNotablePower(settlement);
            }
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            SpawnNotablesAtGameStart();
        }

        private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter)
        {
            foreach (Settlement settlement in Settlement.All.WhereQ((s) => s.IsCastle))
            {
                UpdateVolunteersOfNotablesInSettlement(settlement);
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
            foreach (Settlement castle in Settlement.All.WhereQ((s) => s.IsCastle))
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
                                    float num = MathF.Log(hero.Power / characterObject.Tier, 2f) * 0.01f;
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
                                    while (num3 >= 0 && (characterObject3 == null || characterObject2.Level + (characterObject2.IsMounted ? 0.5f : 0f) < characterObject3.Level + (characterObject3.IsMounted ? 0.5f : 0f)))
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
                float countToSpawn = settlement.Notables.Any() ? (targetCount - settlement.Notables.Count) / (float)targetCount : 1f;
                countToSpawn *= (float)Math.Pow((double)countToSpawn, 0.36000001430511475);
                if (MBRandom.RandomFloat <= countToSpawn)
                {
                    EnterSettlementAction.ApplyForCharacterOnly(HeroCreator.CreateNotable(occupation, settlement), settlement);
                }
        }

        public bool CanHaveRecruits(Hero notable)
        {
            bool canHaveRecruits = notable.CanHaveRecruits;
            return canHaveRecruits || notable.CharacterObject.Occupation == occupation;
        }
        private void SpawnNotablesAtGameStart()
        {
            if (Settlement.All == null)
            {
                // Handle the case where Settlement.All is null
                return;
            }

            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement != null && settlement.IsCastle)
                {
                    int targetNotableCountForSettlement = Helper.GetTargetNotableCountForSettlement(settlement);
                    for (int i = 0; i < targetNotableCountForSettlement; i++)
                    {
                        HeroCreator.CreateNotable(occupation, settlement);
                    }
                }
            }
        }
        public float GetDailyVolunteerProductionProbability(Hero notable, int slot, Settlement settlement)
        {
            float num = 0.72f;
            num += notable.CurrentSettlement != null && notable.CurrentSettlement.MapFaction.Fiefs.Count() < 11 ? (11 - notable.CurrentSettlement.MapFaction.Fiefs.Count()) * 0.02f : 0f;
            float baseNumber = 0.66f * MathF.Clamp(MathF.Pow(num, slot + 1), 0f, 1f);
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