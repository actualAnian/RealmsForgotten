using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Barter;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace RealmsForgotten.UI;
public class FaithUIVM : ViewModel
{
    private int FontSize = 60;
    private string _currentOfferedAmountText;
    private string _faithAmountText;
    private Action onLeaveAction;
    private SpriteCategory _category;
    private int _currentOfferedAmount;
    private MBBindingList<BarterItemVM> _rightGoldList;
    private int CurrentFaithAmount;
    public FaithUIVM(int fontSize, Action leaveAction)
    {
        FontSize = fontSize;
        onLeaveAction = leaveAction;

        _category = UIResourceManager.SpriteData.SpriteCategories["ui_barter"];
        _category.Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);

        CurrentOfferedAmount = 50;
    }

    [DataSourceProperty]
    public string LeaveLabel => GameTexts.FindText("rf_leave", null).ToString();
    
    [DataSourceProperty]
    public string DonateLabel => new TextObject("{=donate}Donate").ToString();
    
    [DataSourceProperty]
    public string DonateDescription => new TextObject("{=donate_description}Donating to the temple will allow the priests to teach you the theology of the local religion, in addition to increasing your own faith").ToString();

    [DataSourceProperty]
    public string RFFontSize => FontSize.ToString();

    [DataSourceProperty]
    public string FaithAmountText
    {
        get => _faithAmountText;
        set
        {
            if (_faithAmountText != value)
            {
                _faithAmountText = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }

    [DataSourceProperty]
    public string CurrentOfferedAmountText
    {
        get => _currentOfferedAmountText;
        set
        {
            if (_currentOfferedAmountText != value)
            {
                _currentOfferedAmountText = value;
                OnPropertyChangedWithValue(value);
            }
        }
    }
    
    [DataSourceProperty]
    public int CurrentOfferedAmount
    {
        get => _currentOfferedAmount;
        set
        {
            if (_currentOfferedAmount != value)
            {
                _currentOfferedAmount = value;
                OnPropertyChangedWithValue(value);
                CurrentOfferedAmountText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(value);
                CurrentFaithAmount = (int)(value * 0.25f);
                TextObject text = new TextObject("{=donate_faith_text}+{AMOUNT} Faith XP");
                text.SetTextVariable("AMOUNT", CampaignUIHelper.GetAbbreviatedValueTextFromValue(CurrentFaithAmount));
                FaithAmountText = text.ToString();
            }
        }
    }
    public void ExecuteDonate()
    {
        Hero.MainHero.AddSkillXp(RFSkills.Faith, CurrentFaithAmount);
        GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, CurrentOfferedAmount);
        ExecuteLeave();
    }

    public void ExecuteLeave()
    {
        if (onLeaveAction != null)
            onLeaveAction();
    }
}





