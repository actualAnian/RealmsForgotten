using System;
using System.Collections.Generic;
using RealmsForgotten.AiMade.Adventurer;
using RealmsForgotten.AiMade.AIQuest;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Encounters.Behaviors;
using RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.Managers.RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.MercenaryFaction;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.PartyOverrides;
using RealmsForgotten.AiMade.Patches;
using RealmsForgotten.AiMade.Religions;
using RealmsForgotten.AiMade.RF_Diplomacy;
using RealmsForgotten.AiMade.TradePact;
using RealmsForgotten.AiMade.Utility;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade
{
    public class CustomSaveableTypeDefiner  : SaveableTypeDefiner
    {
        public CustomSaveableTypeDefiner() : base(585242_820)
        {
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<string>));
            ConstructContainerDefinition(typeof(Dictionary<string, ExampleConfig>));
            ConstructContainerDefinition(typeof(Dictionary<Hero, float>));
            ConstructContainerDefinition(typeof(List<Hero>));
            ConstructContainerDefinition(typeof(Dictionary<Hero, ReligionObject>));
            ConstructContainerDefinition(typeof(Dictionary<Clan, List<string>>));
            ConstructContainerDefinition(typeof(Dictionary<CultureObject, List<string>>));
            ConstructContainerDefinition(typeof(Dictionary<string, List<string>>));
            ConstructContainerDefinition(typeof(Dictionary<string, TownSlaveData>));
            ConstructContainerDefinition(typeof(Dictionary<string, TownPrisonerData>));
            ConstructContainerDefinition(typeof(Dictionary<Settlement, CampaignTime>));
            ConstructContainerDefinition(typeof(Dictionary<CharacterObject, CharacterRacialMix>));
            ConstructContainerDefinition(typeof(Dictionary<int, double>));
            ConstructContainerDefinition(typeof(HashSet<(string, string)>));
            ConstructContainerDefinition(typeof(Tuple<string, string>));
            ConstructContainerDefinition(typeof(Dictionary<string, WeatherRegion>));
        }

        protected override void DefineEnumTypes()
        {
            base.DefineEnumTypes();
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(ExampleConfig), 1);
            AddClassDefinition(typeof(MaestersTowerBehavior), 3);
            AddClassDefinition(typeof(MerchantDeliveryBehavior), 5);
            AddClassDefinition(typeof(DuelCampaignBehavior), 6);
            AddClassDefinition(typeof(Story2Behavior), 7);
            AddClassDefinition(typeof(HelpPeregrineBehavior), 8);
            AddClassDefinition(typeof(RetrieveSwordQuestBehavior), 9);
            AddClassDefinition(typeof(MercenaryOfferBehavior), 10);
            AddClassDefinition(typeof(TavernRecruitmentBehavior), 11);
            AddClassDefinition(typeof(CultureAppropriateTroopsBehavior), 12);
            AddClassDefinition(typeof(HouseTroopsTownsBehavior), 13);
            AddClassDefinition(typeof(RecruitPrisonersMissionBehavior), 14);
            AddClassDefinition(typeof(BanditDefeatChivalryBehavior), 17);
            AddClassDefinition(typeof(ReligionObject), 19);
            AddClassDefinition(typeof(ADODInnBehavior), 20);
            AddClassDefinition(typeof(DivineShieldStateBehavior), 24);
            AddClassDefinition(typeof(DivineShieldMissionBehavior), 25);
            AddClassDefinition(typeof(BanditConversionManager), 26);
            AddClassDefinition(typeof(BanditConversionEvent), 27);
            AddClassDefinition(typeof(PietyManager), 28);
            AddClassDefinition(typeof(ReligionsManager), 33);
            AddClassDefinition(typeof(BattleCryStateBehavior), 34);
            AddClassDefinition(typeof(BanditPartyGrowthBehavior), 35);
            AddClassDefinition(typeof(AggressiveSturgiaBehavior), 36);
            AddClassDefinition(typeof(HumanCohesionBehavior), 37);
            AddClassDefinition(typeof(HashSet<string>), 38);
            AddClassDefinition(typeof(BanditHordeBehavior), 39);
            //AddClassDefinition(typeof(DuelsBehavior), 41);
            AddClassDefinition(typeof(BarbarianHordeInvasion), 42);
            AddClassDefinition(typeof(UndeadHordeBehavior), 43);
            AddClassDefinition(typeof(BanditIncrease), 44);
            AddClassDefinition(typeof(BanditPartyManager), 45);
            AddClassDefinition(typeof(DocksMenuBehavior), 46);
            AddClassDefinition(typeof(CustomAIBase), 47);
            AddClassDefinition(typeof(YourFactionAI), 48);
            AddClassDefinition(typeof(KingsguardSaveDataBehavior), 53);
            AddClassDefinition(typeof(RaceCraftingStaminaBehavior), 54);
            AddClassDefinition(typeof(ADODChamberlainsBehavior), 55);
            AddClassDefinition(typeof(SlaveBehavior), 56);
            AddClassDefinition(typeof(TownSlaveData), 57);
            AddClassDefinition(typeof(ADODSpecialSettlementTroopsModel), 58);
            AddClassDefinition(typeof(ADODCustomLocationsBehavior), 59);
            AddClassDefinition(typeof(NasorianHordeInvasion), 60);
            AddClassDefinition(typeof(FirstTreeTempleLocation), 61);
            AddClassDefinition(typeof(AggressiveDwarfUrkhaiBehavior), 62);
            AddClassDefinition(typeof(MineBehavior), 63);
            AddClassDefinition(typeof(TownPrisonerData), 64);
            AddClassDefinition(typeof(SturgiaCultureChangerBehavior), 65);
            AddClassDefinition(typeof(RacialMixingBehavior), 66);
            AddClassDefinition(typeof(CharacterRacialMix), 67);
            AddClassDefinition(typeof(AlignmentWarBehavior), 68);
            AddClassDefinition(typeof(AlignmentMomentumBehavior), 69);
            //AddClassDefinition(typeof(TickProfilerBehavior), 70);
            AddClassDefinition(typeof(MerchantDeliveryQuest), 71);
            AddClassDefinition(typeof(HelpPeregrineQuest), 72);
            //AddClassDefinition(typeof(EncounterSystemBehavior), 73);
            //AddClassDefinition(typeof(ALordDialogueCampaignBehavior), 74);
            //AddClassDefinition(typeof(ARandomEncountersBehavior), 75);
            //AddClassDefinition(typeof(PendingDuelMissionBehavior), 76);
            //AddClassDefinition(typeof(DuelChallengeDialogueBehavior), 77);
            AddClassDefinition(typeof(CapitulationSystemBehavior), 78);
            AddClassDefinition(typeof(MercenaryHireBehavior), 79);
            AddClassDefinition(typeof(CaravanTradePactBehavior), 80);
            AddClassDefinition(typeof(EconomicPactBehavior), 81);
            AddClassDefinition(typeof(AIBreakInBehavior), 82);
            AddClassDefinition(typeof(RFWeatherCampaignBehavior), 83);
            AddClassDefinition(typeof(VassalPromotionBehavior), 84);
            AddClassDefinition(typeof(MercenaryFactionWarPactBehavior), 85);
            AddClassDefinition(typeof(WeatherRegion), 86);
        }
    }
}
