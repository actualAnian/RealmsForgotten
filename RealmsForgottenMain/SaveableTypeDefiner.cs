using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RealmsForgotten.Career;
using Bannerlord.Module1.Religions;
using Bannerlord.Module1.Stories;
using CampaignBehaviors;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Save;
using RealmsForgotten.Behaviors;

namespace Bannerlord.Module1
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
        }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(ExampleConfig), 1);
            AddClassDefinition(typeof(CeremonyQuestBehavior), 2);
            AddClassDefinition(typeof(MaestersTowerBehavior), 3);
            AddClassDefinition(typeof(PriestCampaignBehavior), 4);
            AddClassDefinition(typeof(MerchantEventBehavior), 5);
            AddClassDefinition(typeof(BanditPartyGrowthBehavior), 6);
            AddClassDefinition(typeof(Story2Behavior), 7);
            AddClassDefinition(typeof(HelpPeregrineBehavior), 8);
            AddClassDefinition(typeof(TavernRecruitmentBehavior), 11);
            AddClassDefinition(typeof(CultureAppropriateTroopsBehavior), 12);
            AddClassDefinition(typeof(HouseTroopsTownsBehavior), 13);
            AddClassDefinition(typeof(RecruitPrisonersMissionBehavior), 14);
            AddClassDefinition(typeof(ProcessionEscortQuestBehavior), 15);
            AddClassDefinition(typeof(ReligionObject), 19);
            AddClassDefinition(typeof(BanditConversionManager), 26);
            AddClassDefinition(typeof(BanditConversionEvent), 27);
            AddClassDefinition(typeof(PietyManager), 28);
            AddClassDefinition(typeof(ReligionsManager), 33);
            AddClassDefinition(typeof(HashSet<string>), 35);
        }
    }
}
