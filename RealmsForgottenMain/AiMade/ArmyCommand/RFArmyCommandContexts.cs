using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using System;

namespace RealmsForgotten.AiMade.ArmyCommand;

internal sealed class RFArmyManagementUIContext
{
    private MobileParty _currentMainParty;

    public static RFArmyManagementUIContext Instance { get; private set; }

    public ArmyManagementVM CurrentArmyManagementVM { get; }

    public RFArmyManagementVMMixin CurrentArmyManagementVMMixIn { get; }

    public MobileParty CurrentMainParty
    {
        get => _currentMainParty;
        set
        {
            if (ReferenceEquals(_currentMainParty, value))
            {
                return;
            }

            bool sameParty = RFArmyCommandHelpers.IsSameParty(_currentMainParty, value);
            _currentMainParty = value;
            try
            {
                if (sameParty)
                {
                    CurrentArmyManagementVMMixIn?.UpdateWidgets();
                    return;
                }

                if (_currentMainParty != null)
                {
                    CurrentArmyManagementVMMixIn?.UpdateContextOnFirstPartyAdded(_currentMainParty);
                }
                else
                {
                    CurrentArmyManagementVMMixIn?.UpdateContextOnLeaderPartyRemoved();
                }
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[RFArmyCommand] CurrentMainParty context update failed: {ex}");
            }
        }
    }

    public bool MainPartyHasArmy { get; set; }

    public Settlement TargetSettlement { get; set; }

    public RFArmyCommandPlan PendingPlan { get; set; }

    public Army.ArmyTypes ArmyBehavior { get; set; }

    public int InfluenceSent { get; set; }

    public bool MovieIsLoaded { get; set; }

    public RFArmyManagementUIContext(ArmyManagementVM vm, RFArmyManagementVMMixin mixin)
    {
        Instance = this;
        CurrentArmyManagementVM = vm;
        CurrentArmyManagementVMMixIn = mixin;
    }

    public void UnregisterInstance()
    {
        Instance = null;
    }
}

internal sealed class RFArmyOverlayUIContext
{
    private Army _selectedArmy;

    public static RFArmyOverlayUIContext Instance { get; private set; }

    public ArmyMenuOverlayVM CurrentArmyOverlayVM { get; }

    public RFArmyMenuOverlayVMMixin CurrentArmyOverlayVMMixIn { get; }

    public int ArmiesCount;
    public int PartiesInArmiesCount;
    public int PartiesInKingdomCount;
    public int MenInArmiesCount;
    public int MenInKingdomCount;

    public Army SelectedArmy
    {
        get => _selectedArmy;
        set
        {
            if (ReferenceEquals(_selectedArmy, value))
            {
                return;
            }

            bool sameArmy = RFArmyCommandHelpers.IsSameArmy(_selectedArmy, value);
            _selectedArmy = value;
            try
            {
                if (sameArmy)
                {
                    CurrentArmyOverlayVMMixIn?.UpdateLineSelection();
                    CurrentArmyOverlayVMMixIn?.UpdateSelectedArmySummary();
                    return;
                }

                CurrentArmyOverlayVMMixIn?.UpdateLineSelection();
                CurrentArmyOverlayVMMixIn?.UpdateSelectedArmySummary();
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[RFArmyCommand] SelectedArmy context update failed: {ex}");
            }
        }
    }

    public RFArmyOverlayUIContext(ArmyMenuOverlayVM vm, RFArmyMenuOverlayVMMixin mixin)
    {
        Instance = this;
        CurrentArmyOverlayVM = vm;
        CurrentArmyOverlayVMMixIn = mixin;
    }

    public void UnregisterInstance()
    {
        Instance = null;
    }
}

internal sealed class RFArmyLineUIContext
{
    private RFSelectableArmyLineVM _lineVm;

    public System.Collections.Generic.List<MobileParty> AllPartiesFromArmy { get; } = new();

    public System.Collections.Generic.List<MobileParty> PartiesJoiningToday { get; } = new();

    public MobileParty LeaderParty { get; set; }

    public int AttachedPartiesCount { get; set; }

    public int TotalAssignedPartiesCount { get; set; }

    public int PartiesWithinADayDistanceCount { get; set; }

    public int MenCount { get; set; }

    public int PotentialMenCount { get; set; }

    public int MenJoiningToday { get; set; }

    public float CurrentArmyFood { get; set; }

    public float TotalArmyFoodChange { get; set; }

    public float TotalArmyFood { get; set; }

    public float CurrentArmyInfluence { get; set; }

    public float DailyArmyInfluenceChange { get; set; }

    public float CurrentCohesion { get; set; }

    public float DailyCohesionChange { get; set; }

    public int LostCohesionCostValue { get; set; }

    public int SendItemInfluenceCost { get; set; }

    public int DisbandInfluenceCost { get; set; }

    public void RegisterLineVm(RFSelectableArmyLineVM vm)
    {
        _lineVm = vm;
    }

    public RFSelectableArmyLineVM GetLineVm()
    {
        return _lineVm;
    }
}
