using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Smithing.Behavior;

public class TownKardrathiumBehavior : CampaignBehaviorBase
{
    private readonly string[] _settlementIds = { "town_V1" };
    private const int AvailableCountPerWeek = 5;
    
    public override void RegisterEvents()
    {
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
    }

    private void OnWeeklyTick()
    {
        foreach (string id in _settlementIds)
        {
            Settlement settlement = Settlement.Find(id);
            if (settlement == null)
                throw new Exception($"Settlement not found on {nameof(TownKardrathiumBehavior)}: {id}");
            
            if (settlement.ItemRoster.FindIndexOfItem(RFItems.Kardrathium) > -1)
                continue;
            
            settlement.ItemRoster.AddToCounts(RFItems.Kardrathium, AvailableCountPerWeek);
        }
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
}