using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Smithing;

public class RFItems
{
    private ItemObject _kardrathium;
    private ItemCategory _kardrathiumCategory;

    public static ItemObject Kardrathium => Instance._kardrathium;
    public static ItemCategory KardrathiumCategory => Instance._kardrathiumCategory;


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
        _kardrathiumCategory = new ItemCategory("kardrathium").InitializeObject(false, 0, 0, ItemCategory.Property.BonusToProsperity);
        Game.Current.ObjectManager.RegisterPresumedObject(_kardrathiumCategory);
        InitializeAll();
    }

    private void InitializeAll()
    {
        ItemObject.InitializeTradeGood(_kardrathium,
            new TextObject("{=kardrathium}Kardrathium{@Plural}loads of kardrathium{\\@}"), "karthradium_steel", _kardrathiumCategory,
            300, 5f, ItemObject.ItemTypeEnum.Goods);
    }
}