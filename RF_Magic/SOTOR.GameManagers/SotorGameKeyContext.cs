using TaleWorlds.InputSystem;

namespace SOTOR.GameManagers;

public class SotorGameKeyContext : GameKeyContext
{
	public const int QuickCastSelectionMenu = 111;

	public SotorGameKeyContext()
		: base("SotorGameKeyContext", 120)
	{
		RegisterGameKey(new GameKey(111, "QuickCastSelectionMenu", "SotorGameKeyContext", InputKey.Q, "SotorGameKeyContext"));
	}
}
