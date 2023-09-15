using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten
{
    public class RFCulturalFeats
    {
        public RFCulturalFeats()
        {
            this.RegisterAll();
        }

        private void RegisterAll()
        {

            athasFasterConstructions = this.Create("athas_faster_constructions");


            elveanForestMorale = this.Create("elvean_forest_morale_bonus");


            empireAdittionalTier = this.Create("empire_additional_tier");


            allkhuurPrisonersJoinMilitia = this.Create("allkhuur_prisoners_join_militia");


            dreadrealmSoldiersRevive = this.Create("dreadrealm_soldiers_revive");


            nasoriaCheaperMercenaries = this.Create("nasoria_cheaper_mercenaries");

            xilantlacayRaidersBonus = this.Create("xilantlacay_raiders_bonus");

            aqarunRecruitBandits = this.Create("aqarun_recruit_bandits");

            this.InitializeAll();
        }

        private FeatObject Create(string stringId)
        {
            return Game.Current.ObjectManager.RegisterPresumedObject(new FeatObject(stringId));
        }
        private void InitializeAll()
        {
            athasFasterConstructions.Initialize("{=!}athas_faster_constructions", "{=athas_faster_constructions}Due to slavery constructions are build 20% faster.", 0.20f, true, FeatObject.AdditionType.AddFactor);

            elveanForestMorale.Initialize("{=!}elvean_forest_morale_bonus", "{=elvean_forest_morale_bonus}15% bonus to morale in forests.", 0.15f, true, FeatObject.AdditionType.AddFactor);

            empireAdittionalTier.Initialize("{=!}empire_additional_tier", "{=empire_additional_tier}Additional upgrade tier for all militia.", 0.0f, true, FeatObject.AdditionType.AddFactor);

            allkhuurPrisonersJoinMilitia.Initialize("{=!}allkhuur_prisoners_join_militia", "{=allkhuur_prisoners_join_militia}Prisoners in towns have a 20% chance of joining the militia.", 0.20f, true, FeatObject.AdditionType.AddFactor);
            
            dreadrealmSoldiersRevive.Initialize("{=!}dreadrealm_soldiers_revive", "{=dreadrealm_soldiers_revive}15% chance to recover 15% of your troops in battle, the higher your level the greater the number of troops recovered.", 0.15f, true, FeatObject.AdditionType.AddFactor);

            nasoriaCheaperMercenaries.Initialize("{=!}nasoria_cheaper_mercenaries", "{=nasoria_cheaper_mercenaries}Recruiting mercenaries is 20% cheaper.", 0.20f, true, FeatObject.AdditionType.AddFactor);

            xilantlacayRaidersBonus.Initialize("{=!}xilantlacay_raiders_bonus", "{=xilantlacay_raiders_bonus}Raiding villages is 20% faster and give 20% more loot.", 0.20f, true, FeatObject.AdditionType.AddFactor);

            aqarunRecruitBandits.Initialize("{=!}aqarun_recruit_bandits", "{=aqarun_recruit_bandits}No costs in recruiting bandits.", 0.20f, true, FeatObject.AdditionType.AddFactor);
        }


        public FeatObject athasFasterConstructions;

        public FeatObject elveanForestMorale;

        public FeatObject empireAdittionalTier;

        public FeatObject allkhuurPrisonersJoinMilitia;

        public FeatObject dreadrealmSoldiersRevive;

        public FeatObject nasoriaCheaperMercenaries;

        public FeatObject xilantlacayRaidersBonus;

        public FeatObject aqarunRecruitBandits;
    }

}
