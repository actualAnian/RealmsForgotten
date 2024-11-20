using System.Collections.Generic;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Enlistement;
using RealmsForgotten.AiMade.Managers.RealmsForgotten.AiMade.Managers;
using RealmsForgotten.AiMade.Models;
using RealmsForgotten.AiMade.PartyOverrides;
using RealmsForgotten.AiMade.Patches;
using RealmsForgotten.AiMade.Religions;
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
            ConstructContainerDefinition(typeof(List<CareerObject>));
            ConstructContainerDefinition(typeof(List<CareerChoiceObject>));
            ConstructContainerDefinition(typeof(Dictionary<Hero, ReligionObject>));
            ConstructContainerDefinition(typeof(Dictionary<Clan, List<string>>));
            ConstructContainerDefinition(typeof(Dictionary<CultureObject, List<string>>));
            ConstructContainerDefinition(typeof(Dictionary<string, List<string>>));
            ConstructContainerDefinition(typeof(Dictionary<string, TownSlaveData>));
            ConstructContainerDefinition(typeof(Dictionary<Settlement, CampaignTime>));
        }

        protected override void DefineEnumTypes()
        {
            base.DefineEnumTypes();
            AddEnumDefinition(typeof(CareerType), 100);
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(ExampleConfig), 1);
            AddClassDefinition(typeof(MaestersTowerBehavior), 3);
            AddClassDefinition(typeof(MerchantEventBehavior), 5);
            AddClassDefinition(typeof(DuelCampaignBehavior), 6);
            AddClassDefinition(typeof(Story2Behavior), 7);
            AddClassDefinition(typeof(HelpPeregrineBehavior), 8);
            AddClassDefinition(typeof(RetrieveSwordQuestBehavior), 9);
            AddClassDefinition(typeof(MercenaryOfferBehavior), 10);
            AddClassDefinition(typeof(TavernRecruitmentBehavior), 11);
            AddClassDefinition(typeof(CultureAppropriateTroopsBehavior), 12);
            AddClassDefinition(typeof(HouseTroopsTownsBehavior), 13);
            AddClassDefinition(typeof(RecruitPrisonersMissionBehavior), 14);
            AddClassDefinition(typeof(BanditHideoutClearedBehavior), 16);
            AddClassDefinition(typeof(BanditDefeatChivalryBehavior), 17);
            AddClassDefinition(typeof(CareerProgressionBehavior), 18);
            AddClassDefinition(typeof(ReligionObject), 19);
            AddClassDefinition(typeof(ADODInnBehavior), 20);
            AddClassDefinition(typeof(DefendVillagersOrCaravansBehavior), 22);
            AddClassDefinition(typeof(QuestCompletionBehavior), 23);
            AddClassDefinition(typeof(DivineShieldStateBehavior), 24);
            AddClassDefinition(typeof(DivineShieldMissionBehavior), 25);
            AddClassDefinition(typeof(BanditConversionManager), 26);
            AddClassDefinition(typeof(BanditConversionEvent), 27);
            AddClassDefinition(typeof(PietyManager), 28);
            AddClassDefinition(typeof(CareerManager), 29);
            AddClassDefinition(typeof(CareerObject), 30);
            AddClassDefinition(typeof(CareerChoiceObject), 31);
            AddClassDefinition(typeof(ReligionsManager), 33);
            AddClassDefinition(typeof(BattleCryStateBehavior), 34);
            AddClassDefinition(typeof(BanditPartyGrowthBehavior), 35);
            AddClassDefinition(typeof(AggressiveSturgiaBehavior), 36);
            AddClassDefinition(typeof(HumanCohesionBehavior), 37);
            AddClassDefinition(typeof(HashSet<string>), 38);
            AddClassDefinition(typeof(BanditHordeBehavior), 39);
            AddClassDefinition(typeof(DuelsBehavior), 41);
            AddClassDefinition(typeof(BarbarianHordeInvasion), 42);
            AddClassDefinition(typeof(UndeadHordeBehavior), 43);
            AddClassDefinition(typeof(BanditIncrease), 44);
            AddClassDefinition(typeof(BanditPartyManager), 45);
            AddClassDefinition(typeof(DocksMenuBehavior), 46);
            AddClassDefinition(typeof(CustomAIBase), 47);
            AddClassDefinition(typeof(YourFactionAI), 48);
            AddClassDefinition(typeof(MyModEnlistmentBehavior), 49);
            AddClassDefinition(typeof(MyModEnlistmentBehaviorExtension), 50);
            AddClassDefinition(typeof(MyModEnlistmentDialogBehavior), 52);
            AddClassDefinition(typeof(KingsguardSaveDataBehavior), 53);
            AddClassDefinition(typeof(RaceCraftingStaminaBehavior), 54);
            AddClassDefinition(typeof(ADODChamberlainsBehavior), 55);
            AddClassDefinition(typeof(SlaveBehavior), 56);
            AddClassDefinition(typeof(TownSlaveData), 57);
            AddClassDefinition(typeof(ADODSpecialSettlementTroopsModel), 58);
            AddClassDefinition(typeof(ADODCustomLocationsBehavior), 59);
            AddClassDefinition(typeof(NasorianHordeInvasion), 60);
            AddClassDefinition(typeof(FirstTreeTempleLocation), 61);

        }
    }
}
