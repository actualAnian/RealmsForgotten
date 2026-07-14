using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Library;

namespace Homesteads.Console;

public static class HomesteadCommands
{
	[CommandLineFunctionality.CommandLineArgumentFunction("status", "homestead")]
	public static string CheckStatus(List<string> args)
	{
		Settlement settlement = Settlement.All.FirstOrDefault((Settlement x) => x.StringId != null && x.StringId.StartsWith("hsr_settlement_") && x.IsTown);
		if (settlement == null)
		{
			return "Homestead not found.";
		}
		if (settlement.Town == null)
		{
			return "Homestead has no town.";
		}
		Town town = settlement.Town;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"Loyalty: {town.Loyalty} (Change: {town.LoyaltyChange})");
		stringBuilder.AppendLine($"Security: {town.Security} (Change: {town.SecurityChange})");
		stringBuilder.AppendLine($"Prosperity: {town.Prosperity} (Change: {town.ProsperityChange})");
		stringBuilder.AppendLine($"Construction: {town.Construction} (Explanation: {town.ConstructionExplanation.GetExplanations()})");
		Building building = ((town.BuildingsInProgress.Count == 0) ? null : town.BuildingsInProgress.Peek());
		if (building != null)
		{
			stringBuilder.AppendLine($"Current Project: {building.BuildingType.Name} (Lvl {building.CurrentLevel})");
			stringBuilder.AppendLine($"Progress: {building.BuildingProgress} / {building.GetConstructionCost()}");
		}
		else
		{
			stringBuilder.AppendLine("Current Project: NONE");
		}
		if (town.CurrentDefaultBuilding != null)
		{
			stringBuilder.AppendLine($"Default Project: {town.CurrentDefaultBuilding.BuildingType.Name}");
		}
		return stringBuilder.ToString();
	}
}
