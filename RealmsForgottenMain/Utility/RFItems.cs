using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Utility;

public class RFItems
{
    private ItemObject _kardrathium;
    public static ItemObject Kardrathium => Instance._kardrathium;

    public static RFItems Instance { get; private set; }

    public RFItems()
    {
        Instance = this;
    }

    private ItemObject Create(string stringId)
    {
        return Game.Current.ObjectManager.RegisterPresumedObject(new ItemObject(stringId));
    }

    public void RegisterAll()
    {
        _kardrathium = Create("kardrathium");
        InitializeAll();
    }

    private void InitializeAll()
    {
        ItemObject.InitializeTradeGood(_kardrathium,
            new TextObject("{=kardrathium}Kardrathium{@Plural}loads of kardrathium{\\@}"), "iron_a", DefaultItemCategories.Iron,
            60, 0.5f, ItemObject.ItemTypeEnum.Goods);
    }
}