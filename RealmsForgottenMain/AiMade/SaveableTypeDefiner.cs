using System.Collections.Generic;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.AiMade.Religions;
using TaleWorlds.CampaignSystem;
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
        }

        protected override void DefineEnumTypes()
        {
            base.DefineEnumTypes();
            AddEnumDefinition(typeof(CareerType), 100);
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(ExampleConfig), 1);
            AddClassDefinition(typeof(CeremonyQuestBehavior), 2);
            AddClassDefinition(typeof(MaestersTowerBehavior), 3);
            AddClassDefinition(typeof(PriestCampaignBehavior), 4);
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
            AddClassDefinition(typeof(ProcessionEscortQuestBehavior), 15);
            AddClassDefinition(typeof(BanditHideoutClearedBehavior), 16);
            AddClassDefinition(typeof(BanditDefeatChivalryBehavior), 17);
            AddClassDefinition(typeof(CareerProgressionBehavior), 18);
            AddClassDefinition(typeof(ReligionObject), 19);
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
            AddClassDefinition(typeof(BanditRecruitmentBehavior), 32);
            AddClassDefinition(typeof(ReligionsManager), 33);
            AddClassDefinition(typeof(BattleCryStateBehavior), 34);
            AddClassDefinition(typeof(HashSet<string>), 35);
        }
    }
}
