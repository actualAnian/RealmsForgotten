using System;
using Homesteads.Models;
using TaleWorlds.CampaignSystem.Party;

namespace Homesteads.Patches;

internal static class HomesteadPartyBlocker
{
	internal static bool IsHomesteadOrPatrolParty(MobileParty party)
	{
		if (party == null)
		{
			return false;
		}
		if (Homestead.GetFor(party) != null)
		{
			return true;
		}
		try
		{
			if (HomesteadBehavior.Instance != null && HomesteadBehavior.Instance.PatrolMobileParties.ContainsKey(party))
			{
				return true;
			}
		}
		catch (Exception ex)
		{
			TraceLogger.WriteOnce("PatrolMobilePartiesRace", "HomesteadPartyBlocker", "IsHomesteadOrPatrolParty: PatrolMobileParties lookup threw (rare read/write race): " + ex.GetType().Name + ": " + ex.Message);
		}
		return false;
	}

	internal static bool IsHomesteadMainParty(MobileParty party)
	{
		if (party == null)
		{
			return false;
		}
		return Homestead.GetFor(party) != null;
	}

	internal static bool ShouldBlockMainPartyMovement(MobileParty party)
	{
		if (party == null)
		{
			return false;
		}
		Homestead homestead = Homestead.GetFor(party);
		if (homestead != null)
		{
			return !homestead.IsMoving;
		}
		return false;
	}
}
