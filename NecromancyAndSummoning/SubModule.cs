using System.IO;
using System.Reflection;
using HarmonyLib;
using RealmsForgotten.NecromancyAndSummoning.Config;
using RealmsForgotten.NecromancyAndSummoning.Patch;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.NecromancyAndSummoning
{
	internal class SubModule : MBSubModuleBase
	{
		public static Config.Config Config { get; private set; }
		public static ItemUnitConfig ItemUnitConfig { get; private set; }
		public static UnitUnitConfig UnitUnitConfig { get; private set; }
		public static UnitBuildFromPartConfig UnitBuildFromPartConfig { get; private set; }
        private readonly Harmony harmony = new("necromancyAndSummoning");
        protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			SubModule.LoadConfig();
			harmony.PatchAll();
        }
        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			if(mission.IsSiegeBattle || mission.IsSallyOutBattle || mission.IsFieldBattle)
				mission.AddMissionBehavior(new NecromancyAndSummoningLogic());
		}
		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			harmony.Patch(AccessTools.Method(typeof(Mission), "MissileHitCallback"), null, new HarmonyMethod(typeof(SummoningAndRaiseCorpsePatch), "Postfix"));

			bool flag = game.GameType is Campaign;
			if (flag)
			{
				CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
				campaignGameStarter.AddBehavior(new NecromancyBehaviour());
			}
		}
		private static void LoadConfig()
		{
            SubModule.Config = JsonConvert.DeserializeObject<Config.Config>(
                File.ReadAllText(SubModule.ConfigFilePath)) ?? throw new InvalidDataException("Config.json is invalid or empty.");
            SubModule.ItemUnitConfig = JsonConvert.DeserializeObject<ItemUnitConfig>(
                File.ReadAllText(SubModule.ItemUnitConfigFilePath)) ?? throw new InvalidDataException("ItemUnitConfig.json is invalid or empty.");
            SubModule.UnitUnitConfig = JsonConvert.DeserializeObject<UnitUnitConfig>(
                File.ReadAllText(SubModule.UnitUnitConfigFilePath)) ?? throw new InvalidDataException("UnitUnitConfig.json is invalid or empty.");
            SubModule.UnitBuildFromPartConfig = JsonConvert.DeserializeObject<UnitBuildFromPartConfig>(
                File.ReadAllText(SubModule.UnitBuildFromPartConfigFilePath)) ?? throw new InvalidDataException("UnitBuildFromPartConfig.json is invalid or empty.");
        }
        private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");
		private static readonly string ItemUnitConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "item_unit_config.json");
		private static readonly string UnitUnitConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "unit_unit_config.json");
		private static readonly string UnitBuildFromPartConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "unit_build_from_part_config.json");
	}
}
