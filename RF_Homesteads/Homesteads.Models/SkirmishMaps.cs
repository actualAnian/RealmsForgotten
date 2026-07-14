using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace Homesteads.Models;

public static class SkirmishMaps
{
	public static readonly List<SkirmishMap> All = new List<SkirmishMap>
	{
		new SkirmishMap("hippodrome_ring", "homestead_skirmish_map", "mp_sergeant_map_001", new Vec3(247.549f, 283.431f, 27.292f))
	};

	public static SkirmishMap Default => All[0];

	public static SkirmishMap? ById(string id)
	{
		return All.FirstOrDefault((SkirmishMap m) => m.Id == id);
	}
}
