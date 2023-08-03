using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFEffects
{
    class NasoriaWageModel : DefaultPartyWageModel
    {
        public override int GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            int baseValue = base.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);
            if (buyerHero == null)
                return baseValue;
            int nasoriaBonus = (int)(baseValue - ((15f / 100f) * baseValue));
            if (buyerHero.Culture.StringId == "vlandia" && troop.Occupation == Occupation.Mercenary && nasoriaBonus > 0)
                return nasoriaBonus;
            return baseValue;
        }
    }
    public class AthasBuildingConstructionModel : DefaultBuildingConstructionModel
    {
        public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
        {
            ExplainedNumber baseNumber = base.CalculateDailyConstructionPower(town, includeDescriptions);
            if(town.Owner.Culture.StringId=="aserai")
                baseNumber.AddFactor(0.20f, new TextObject("{=SADf3gmami3g}Athas Slavery"));
            return baseNumber;
        }   
    }
    class ElveanMoraleModel : DefaultPartyMoraleModel
    {
        public override ExplainedNumber GetEffectivePartyMorale(MobileParty party, bool includeDescription = false)
        {
            ExplainedNumber baseNumber = base.GetEffectivePartyMorale(party, includeDescription);
            if (party == null)
                return baseNumber;
            TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(party.CurrentNavigationFace);
            if (party.Party.Culture.StringId == "battania" && faceTerrainType == TerrainType.Forest)
                baseNumber.AddFactor(0.15f, new TextObject("{=SAvbh23had3}Elvean Forest Morale Bonus"));
            return baseNumber;
        }
    }

    class CulturesCampaignBehavior : CampaignBehaviorBase
    {
        private readonly string sturgiaId = "sturgia";
        private readonly string khuzaitId = "khuzait";

        private List<string> partiesInForests = new();
        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, SturgianBonus);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, KhuzaitBonus);
            
        }
        private void KhuzaitBonus(Settlement settlement)
        {
            if((settlement.IsTown || settlement.IsCastle) && settlement.Owner.Culture.StringId == khuzaitId && settlement.Party.PrisonRoster.TotalRegulars > 0)
            {
                foreach(FlattenedTroopRosterElement troopRosterElement in settlement.Party.PrisonRoster.ToFlattenedRoster())
                {
                    if(MBRandom.RandomFloat < 0.15f)
                    {
                        settlement.Party.PrisonRoster.RemoveTroop(troopRosterElement.Troop);
                        settlement.MilitiaPartyComponent.Party.AddElementToMemberRoster(troopRosterElement.Troop, 1);
                    }
                }
            }
        }
        private void SturgianBonus(MapEvent mapEvent)
        {
            if(mapEvent.HasWinner && mapEvent.Winner.LeaderParty.Culture.StringId == sturgiaId && RFEffectsSubModule.undeadRespawnConfig?.Count > 0 && MBRandom.RandomFloat < 0.15)
            {
                List<MapEventParty> parties = mapEvent.Winner.Parties;
                foreach(MapEventParty party in parties)
                {
                    
                    int wounded = party.HealthyManCountAtStart - party.Party.NumberOfHealthyMembers;
                    if (wounded <= 0)
                        continue;
                    int recovered = (int)((10f / 100f) * wounded);
                    if (recovered > 0)
                    {
                        for (int i = 0; i < recovered; i++)
                        {
                            float random = MBRandom.RandomFloat;
                            string characterId = RFEffectsSubModule.undeadRespawnConfig.RandomElementByWeight(x=>x.Value);
                            CharacterObject characterObject = CharacterObject.Find(characterId);
                            
                            
                            party.Party.AddElementToMemberRoster(characterObject, 1);
                        }
                        var textObject = new TextObject("{=sf4yHsh3JKw}{AMOUNT} undead soldiers rejoined your army");
                        textObject.SetTextVariable("AMOUNT", recovered);
                        MBInformationManager.AddQuickInformation(textObject);   
                    }

                }   

            }
        }
        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}
