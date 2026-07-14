using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace Homesteads.Models;

public static class RaceTracks
{
	public static readonly List<RaceTrack> All = new List<RaceTrack>
	{
		new RaceTrack("speedy_loop", "Ye Olde Speedy Loop", "homestead_new_race_track", "battle_terrain_007", new Vec3(596.567f, 637.946f, 28.688f)),
		new RaceTrack("ruins_race", "Crumbledown Canter", "homestead_ruins_race", "mp_sergeant_map_011", new Vec3(558.64f, 491.291f, 148.394f), 2, reverseDirection: true),
		new RaceTrack("royal_roundabout", "Royal Roundabout", "homestead_royal_roundabout", "mp_sergeant_map_018", new Vec3(614.752f, 594.534f, 23.469f), 2),
		new RaceTrack("bridge_brigade", "Bridge Brigade", "homestead_bridge_brigade", "battle_terrain_t", new Vec3(742.252f, 585.226f, 7.983f)),
		new RaceTrack("roof_romp", "Roof Romp", "homestead_roof_romp", "mp_sergeant_map_009", new Vec3(477.173f, 642.606f, -3.069f), 2, reverseDirection: false, 10f, 0.5f, carefulCorners: true, lapRestartOnStall: true, 0f, 3f),
		new RaceTrack("happy_hippodrome", "Happy Hippodrome", "homestead_happy_hippodrome", "battle_terrain_009", new Vec3(698.374f, 684.178f, -1.285f), 2, reverseDirection: false, 15f, 1.3f),
		new RaceTrack("scenic_peaks", "Scenic Peaks Pilgrimage", "homestead_scenic_peaks", "battle_terrain_biome_012", new Vec3(864.202f, 396.843f, 149.964f), 2, reverseDirection: false, 15f, 1f, carefulCorners: false, lapRestartOnStall: false, 1.5f),
		new RaceTrack("mini_circuit", "Coliseum Carousel", "homestead_mini_circuit", "battle_terrain_biome_107", new Vec3(819.466f, 869.311f, 5.055f), 2, reverseDirection: false, 10f, 1f, carefulCorners: true, lapRestartOnStall: true, 1.5f, 2.5f),
		new RaceTrack("sultans_serpent", "Sultan's Serpent", "homestead_sultans_serpent", "battle_terrain_b", new Vec3(638.864f, 582.879f, 28.62f), 2, reverseDirection: false, 30f)
	};

	public static RaceTrack Default => All[0];

	public static RaceTrack? ById(string id)
	{
		return All.FirstOrDefault((RaceTrack t) => t.Id == id);
	}
}
