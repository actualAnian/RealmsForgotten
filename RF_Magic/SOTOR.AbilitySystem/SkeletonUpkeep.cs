using TaleWorlds.CampaignSystem;

namespace SOTOR.AbilitySystem;

internal static class SkeletonUpkeep
{
	public const string SkeletonCultureId = "sotor_skeleton";

	public static bool IsSkeletonChar(CharacterObject c)
	{
		if (c?.Culture != null)
		{
			return c.Culture.StringId == "sotor_skeleton";
		}
		return false;
	}
}
