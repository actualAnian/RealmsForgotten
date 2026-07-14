using Helpers;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Homesteads;

public static class PartyScreenHelper
{
	public class PartyScreenResult
	{
		public bool IsCanceled { get; set; }
	}

	public delegate void PartyScreenClosedDelegate(PartyScreenResult result);

	public static bool IsApplyingRosterChanges;

	public static bool IsHomesteadPartyScreenOpen;

	public static void OpenScreenAsManageTroopsAndPrisoners(MobileParty leftParty, MobileParty rightParty, TextObject leftPartyName, PartyScreenClosedDelegate onDone, IsTroopTransferableDelegate transferableDelegate)
	{
		if (leftParty != null && rightParty != null)
		{
			IsHomesteadPartyScreenOpen = true;
			Helpers.PartyScreenHelper.OpenScreenWithCondition(transferableDelegate, null, delegate
			{
				IsHomesteadPartyScreenOpen = false;
				onDone?.Invoke(new PartyScreenResult
				{
					IsCanceled = false
				});
				return true;
			}, delegate
			{
				IsHomesteadPartyScreenOpen = false;
				onDone?.Invoke(new PartyScreenResult
				{
					IsCanceled = true
				});
			}, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, leftPartyName, leftParty.Party.PartySizeLimit, showProgressBar: false, isDonating: false, Helpers.PartyScreenHelper.PartyScreenMode.Normal, leftParty.MemberRoster, leftParty.PrisonRoster);
		}
	}

	public static PartyState GetActivePartyState()
	{
		return Game.Current?.GameStateManager?.ActiveState as PartyState;
	}
}
