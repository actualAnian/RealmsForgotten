using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using SandBox.ViewModelCollection.Map.Tracker;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;

namespace Homesteads.Patches;

internal static class HomesteadMapTrackerSync
{
	public static void SyncHomesteadTrackers(MapTrackerCollectionVM trackerCollectionVm, string reason)
	{
		if (HomesteadBehavior.Instance == null)
		{
			return;
		}
		for (int num = trackerCollectionVm.Trackers.Count - 1; num >= 0; num--)
		{
			if (trackerCollectionVm.Trackers[num] is HomesteadMapTrackItemVM)
			{
				if (!(trackerCollectionVm.Trackers[num].TrackedObject is MobileParty mobileParty))
				{
					trackerCollectionVm.Trackers.RemoveAt(num);
				}
				else if (mobileParty.IsDisbanding || (Homestead.GetFor(mobileParty) == null && !HomesteadBehavior.Instance.PatrolMobileParties.ContainsKey(mobileParty)))
				{
					trackerCollectionVm.Trackers.RemoveAt(num);
				}
			}
		}
		HashSet<MobileParty> hashSet = new HashSet<MobileParty>((from x in trackerCollectionVm.Trackers
			select x.TrackedObject as MobileParty into x
			where x != null && (Homestead.GetFor(x) != null || HomesteadBehavior.Instance.PatrolMobileParties.ContainsKey(x))
			select x).Cast<MobileParty>());
		foreach (KeyValuePair<MobileParty, Homestead> homesteadMobileParty in HomesteadBehavior.Instance.HomesteadMobileParties)
		{
			if (!homesteadMobileParty.Key.IsDisbanding && !hashSet.Contains(homesteadMobileParty.Key))
			{
				trackerCollectionVm.Trackers.Add((MapTrackerItemVM)(object)new HomesteadMapTrackItemVM(homesteadMobileParty.Key));
			}
		}
		foreach (KeyValuePair<MobileParty, Homestead> patrolMobileParty in HomesteadBehavior.Instance.PatrolMobileParties)
		{
			if (!patrolMobileParty.Key.IsDisbanding && !hashSet.Contains(patrolMobileParty.Key))
			{
				trackerCollectionVm.Trackers.Add((MapTrackerItemVM)(object)new HomesteadMapTrackItemVM(patrolMobileParty.Key));
			}
		}
	}
}
