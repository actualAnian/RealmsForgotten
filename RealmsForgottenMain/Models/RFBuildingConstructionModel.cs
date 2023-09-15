using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    internal class RFBuildingConstructionModel : DefaultBuildingConstructionModel
    {
        public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
        {
            ExplainedNumber baseNumber = base.CalculateDailyConstructionPower(town, includeDescriptions);
            if (town.Owner.Culture.StringId == "aserai")
                baseNumber.AddFactor(0.20f, new TextObject("{=SADf3gmami3g}Athas Slavery"));
            return baseNumber;
        }
    }
}
