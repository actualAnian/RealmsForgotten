using TaleWorlds.Library;

namespace Homesteads.Models;

public sealed class SkirmishMap
{
	public string Id { get; }

	public string PrefabName { get; }

	public string Scene { get; }

	public Vec3 Anchor { get; }

	public SkirmishMap(string id, string prefabName, string scene, Vec3 anchor)
	{
		Id = id;
		PrefabName = prefabName;
		Scene = scene;
		Anchor = anchor;
	}
}
