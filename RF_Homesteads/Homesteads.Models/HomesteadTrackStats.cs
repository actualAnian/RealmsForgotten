using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadTrackStats
{
	[SaveableField(1)]
	public List<string> LapNames = new List<string>();

	[SaveableField(2)]
	public List<int> LapTimes = new List<int>();

	[SaveableField(3)]
	public List<string> TotalNames = new List<string>();

	[SaveableField(4)]
	public List<int> TotalTimes = new List<int>();

	[SaveableField(5)]
	public Dictionary<string, int> Counts = new Dictionary<string, int>();

	[SaveableField(6)]
	public Dictionary<string, string> CountNames = new Dictionary<string, string>();
}
