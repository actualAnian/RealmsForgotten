using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.Religions
{
    public class ReligionsManager : CampaignBehaviorBase
    {
        private Dictionary<Settlement, SettlementReligionData> settlementReligions = new Dictionary<Settlement, SettlementReligionData>();

        public Dictionary<Settlement, SettlementReligionData> SettlementReligions => settlementReligions;


        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, ApplyReligionBonuses);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("settlementReligions", ref settlementReligions);
        }

        public void InitializeReligions()
        {
            var empireCulture = MBObjectManager.Instance.GetObject<CultureObject>("empire");
            var sturgiaCulture = MBObjectManager.Instance.GetObject<CultureObject>("sturgia");
            var battaniaCulture = MBObjectManager.Instance.GetObject<CultureObject>("battania");
            var vlandiaCulture = MBObjectManager.Instance.GetObject<CultureObject>("vlandia");
            var khuzaitCulture = MBObjectManager.Instance.GetObject<CultureObject>("khuzait");
            var aseraiCulture = MBObjectManager.Instance.GetObject<CultureObject>("aserai");

            var anoriteReligion = new ReligionObject("Anorite Religion", "Faith of Anorite", empireCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    LoyaltyBonus = 0.15f,
                    GrowthPenalty = 0.10f
                }
            };

            var celestialChorus = new ReligionObject("Celestial Chorus", "Faith of Celestial Chorus", empireCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    ProsperityBonus = 0.15f,
                    RecruitmentPenalty = 0.10f
                }
            };

            var sturgianFaith = new ReligionObject("SturgianFaith", "Faith of Sturgia", sturgiaCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    RecruitmentBonus = 0.15f,
                    ProsperityPenalty = 0.15f
                }
            };

            var battanianFaith = new ReligionObject("BattanianFaith", "Faith of Battania", battaniaCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    LoyaltyBonus = 0.15f,
                    WorkshopProductionPenalty = 0.10f
                }
            };

            var vlandianFaith = new ReligionObject("VlandianFaith", "Faith of Vlandia", vlandiaCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    MilitaryBuildingSpeedBonus = 0.15f,
                    LoyaltyPenalty = 0.10f
                }
            };

            var khuzaitFaith = new ReligionObject("KhuzaitFaith", "Faith of Khuzait", khuzaitCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    LoyaltyBonus = 0.15f,
                    BuildingSpeedPenalty = 0.10f
                }
            };

            var aseraiFaith = new ReligionObject("AseraiFaith", "Faith of Aserai", aseraiCulture)
            {
                Bonuses = new ReligionBonuses
                {
                    LoyaltyBonus = 0.20f,
                    GrowthPenalty = 0.15f
                }
            };

            MBObjectManager.Instance.RegisterObject(anoriteReligion);
            MBObjectManager.Instance.RegisterObject(celestialChorus);
            MBObjectManager.Instance.RegisterObject(sturgianFaith);
            MBObjectManager.Instance.RegisterObject(battanianFaith);
            MBObjectManager.Instance.RegisterObject(vlandianFaith);
            MBObjectManager.Instance.RegisterObject(khuzaitFaith);
            MBObjectManager.Instance.RegisterObject(aseraiFaith);

            // Initialize religions for settlements
            foreach (var settlement in Settlement.All)
            {
                if (!settlementReligions.ContainsKey(settlement))
                {
                    var religionData = new SettlementReligionData(settlement);
                    settlementReligions.Add(settlement, religionData);
                }
            }

            // Initialize heroes with their religions
            InitializeFaithfulHeroes();
        }

        private void InitializeFaithfulHeroes()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                DetermineReligionForHero(hero);
            }
        }

        public void DetermineReligionForHero(Hero hero)
        {
            ReligionObject religion = null;

            // Assign religion to clan leader first if they don't have one
            if (hero.Clan != null && hero.Clan.Leader == hero && !hero.HasReligion())
            {
                religion = AssignReligionBasedOnCulture(hero);
                if (religion != null)
                {
                    hero.SetReligion(religion);
                }
            }

            // Follow father's religion
            if (hero.Father != null && hero.Father.HasReligion())
            {
                religion = hero.Father.GetReligion();
            }
            // Follow clan leader's religion
            else if (hero.Clan != null && hero.Clan.Leader != null && hero.Clan.Leader.HasReligion())
            {
                religion = hero.Clan.Leader.GetReligion();
            }
            // Follow culture's religion
            else if (hero.Culture != null && ReligionObject.All.Any(x => x.Culture == hero.Culture))
            {
                religion = ReligionObject.All.Where(x => x.Culture == hero.Culture).OrderBy(r => MBRandom.RandomFloat).FirstOrDefault();
            }

            if (religion != null)
            {
                hero.SetReligion(religion);
            }
        }

        private ReligionObject AssignReligionBasedOnCulture(Hero hero)
        {
            // Randomly assign one of the religions if there are multiple for the culture
            return ReligionObject.All.Where(x => x.Culture == hero.Culture).OrderBy(r => MBRandom.RandomFloat).FirstOrDefault();
        }

        public void AdjustRelationsBasedOnReligion(Hero hero)
        {
            foreach (var otherHero in Hero.AllAliveHeroes)
            {
                if (hero != otherHero)
                {
                    var heroReligion = hero.GetReligion();
                    var otherHeroReligion = otherHero.GetReligion();

                    if (heroReligion != null && otherHeroReligion != null && heroReligion != otherHeroReligion)
                    {
                        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, otherHero, -10);
                    }
                }
            }
        }

        private void ApplyReligionBonuses(Settlement settlement)
        {
            try
            {
                if (settlement.IsTown || settlement.IsVillage)
                {
                    if (settlementReligions.TryGetValue(settlement, out var religionData))
                    {
                        religionData.ApplyBonusesAndPenalties();
                        InformationManager.DisplayMessage(new InformationMessage($"Applied bonuses and penalties for {settlement.Name}"));
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in ApplyReligionBonuses: {ex.Message}"));
            }
        }
    }
  }
