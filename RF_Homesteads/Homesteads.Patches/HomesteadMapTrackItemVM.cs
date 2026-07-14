using Homesteads.Models;
using SandBox.ViewModelCollection.Map.Tracker;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

internal class HomesteadMapTrackItemVM : MapMobilePartyTrackItemVM
{
	public HomesteadMapTrackItemVM(MobileParty party)
		: base(party)
	{
	}

	protected override bool IsVisibleOnMap()
	{
		MobileParty trackedObject = ((MapTrackerItemVM<MobileParty>)this).TrackedObject;
		if (trackedObject == null || trackedObject.IsDisbanding)
		{
			return false;
		}
		if (Homestead.GetFor(trackedObject) == null)
		{
			return HomesteadBehavior.Instance?.PatrolMobileParties.ContainsKey(trackedObject) ?? false;
		}
		return true;
	}

	protected override void OnShowTooltip()
	{
		MobileParty trackedObject = ((MapTrackerItemVM<MobileParty>)this).TrackedObject;
		if (trackedObject == null)
		{
			return;
		}
		Homestead homestead = Homestead.GetFor(trackedObject);
		if (homestead != null)
		{
			HintViewModel hintViewModel = new HintViewModel(new TextObject("{=homestead_tracker_tooltip}Homestead: {HOMESTEAD_NAME}").SetTextVariable("HOMESTEAD_NAME", homestead.Name));
			InformationManager.ShowTooltip(typeof(HintViewModel), hintViewModel);
			return;
		}
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance != null && instance.PatrolMobileParties.TryGetValue(trackedObject, out Homestead value) && value != null)
		{
			HintViewModel hintViewModel2 = new HintViewModel(new TextObject("{=homestead_patrol_tracker_tooltip}Patrol: {HOMESTEAD_NAME}").SetTextVariable("HOMESTEAD_NAME", value.Name));
			InformationManager.ShowTooltip(typeof(HintViewModel), hintViewModel2);
		}
	}
}
