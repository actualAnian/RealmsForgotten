using RealmsForgotten.RFReligions.Helper;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace RealmsForgotten.RFReligions.Overlay;

public class ReligionsSettlementMenuOverlayVM : SettlementMenuOverlayVM
{
    private string _religionLbl;
    private BasicTooltipViewModel _religionHint;

    public ReligionsSettlementMenuOverlayVM(GameOverlays.MenuOverlayType type) : base(type)
    {
        if (Settlement.CurrentSettlement?.IsTown == true)
            ReligionHint =
                new BasicTooltipViewModel(() => ReligionUIHelper.GetTownReligion(Settlement.CurrentSettlement.Town));
    }

    public override void Refresh()
    {
        base.Refresh();
        if (Settlement.CurrentSettlement?.IsTown == true)
            ReligionLbl = string.Format("{0:0.#}", ReligionUIHelper.GetTownReligionLbl());
    }

    [DataSourceProperty]
    public BasicTooltipViewModel ReligionHint
    {
        get => _religionHint;
        set
        {
            if (value == _religionHint) return;
            _religionHint = value;
            OnPropertyChangedWithValue(value);
        }
    }

    [DataSourceProperty]
    public string ReligionLbl
    {
        get => _religionLbl;
        set
        {
            if (value == _religionLbl)
                return;

            _religionLbl = value;
            OnPropertyChangedWithValue(value);
        }
    }
}