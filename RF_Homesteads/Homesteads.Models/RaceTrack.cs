using TaleWorlds.Library;

namespace Homesteads.Models;

public sealed class RaceTrack
{
	public string Id { get; }

	public string Name { get; }

	public string PrefabName { get; }

	public string Scene { get; }

	public Vec3 Anchor { get; }

	public int Laps { get; }

	public bool ReverseDirection { get; }

	public float GateRadius { get; }

	public float SpeedScale { get; }

	public bool CarefulCorners { get; }

	public bool LapRestartOnStall { get; }

	public float SpawnBackOffset { get; }

	public float AiWaypointRadius { get; }

	public RaceTrack(string id, string name, string prefabName, string scene, Vec3 anchor, int laps = 3, bool reverseDirection = false, float gateRadius = 15f, float speedScale = 1f, bool carefulCorners = false, bool lapRestartOnStall = false, float spawnBackOffset = 0f, float aiWaypointRadius = 0f)
	{
		Id = id;
		Name = name;
		PrefabName = prefabName;
		Scene = scene;
		Anchor = anchor;
		Laps = laps;
		ReverseDirection = reverseDirection;
		GateRadius = gateRadius;
		SpeedScale = speedScale;
		CarefulCorners = carefulCorners;
		LapRestartOnStall = lapRestartOnStall;
		SpawnBackOffset = spawnBackOffset;
		AiWaypointRadius = aiWaypointRadius;
	}
}
