using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace RealmsForgotten.Models;

public class RFBattleCaptainModel : DefaultBattleCaptainModel
{
    private BattleCaptainModel _previousModel;
        
    public RFBattleCaptainModel(BattleCaptainModel previousModel)
    {
        _previousModel = previousModel;
    }
    public override float GetCaptainRatingForTroopUsages(
        Hero hero,
        TroopUsageFlags flag,
        out List<PerkObject> compatiblePerks)
    {
        if (hero == null)
        {
            compatiblePerks = new List<PerkObject>();
            return 0f;
        }
        
        return _previousModel.GetCaptainRatingForTroopUsages(hero, flag, out compatiblePerks);
    }
}