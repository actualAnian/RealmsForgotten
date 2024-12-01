using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    internal class RFBuildingConstructionModel : DefaultBuildingConstructionModel
    {
        private BuildingConstructionModel _previousModel;
        
        public RFBuildingConstructionModel(BuildingConstructionModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
        {
            ExplainedNumber baseNumber = _previousModel.CalculateDailyConstructionPower(town, includeDescriptions);
            if (town.Owner.Culture.StringId == "aserai")
                baseNumber.AddFactor(0.20f, new TextObject("{=athas_slavery}Athas Slavery"));
            return baseNumber;
        }
    }
}
