using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Smithing.Behavior;

public class TownKardrathiumBehavior : CampaignBehaviorBase
{
    // Ids MUST match the map's canonical "town_dwarf_N" form — the last four
    // were missing the underscore, so Settlement.Find returned null and the
    // weekly tick threw, crashing the campaign.
    private readonly string[] _settlementIds = { "town_dwarf_1", "town_dwarf_2", "town_dwarf_3", "town_dwarf_5", "town_dwarf_6", "town_dwarf_7" };
    private const int AvailableCountPerWeek = 5;
    
    public override void RegisterEvents()
    {
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
        CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
    }
    private void OnAfterSessionLaunched(CampaignGameStarter obj)
    {
        if (!ItemCategories.All.Contains(RFItems.KardrathiumCategory))
        {
            ItemCategories.All.Add(RFItems.KardrathiumCategory);
        }
    }

    private void OnWeeklyTick()
    {
        foreach (string id in _settlementIds)
        {
            Settlement settlement = Settlement.Find(id);
            if (settlement == null)
                continue; // missing dwarf town on this map — skip, never crash the tick

            if (settlement.ItemRoster.FindIndexOfItem(RFItems.Kardrathium) > -1)
                continue;
            
            settlement.ItemRoster.AddToCounts(RFItems.Kardrathium, AvailableCountPerWeek);
        }
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
}