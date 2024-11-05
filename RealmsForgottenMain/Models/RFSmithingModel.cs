using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;

namespace RealmsForgotten.Models;

public class RFSmithingModel : DefaultSmithingModel
{
    private SmithingModel _previousModel;
        
    public RFSmithingModel(SmithingModel previousModel)
    {
        _previousModel = previousModel;
    }
    
    
}