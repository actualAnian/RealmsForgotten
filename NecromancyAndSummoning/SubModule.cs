using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NecromancyAndSummoning.Config;
using NecromancyAndSummoning.Patch;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NecromancyAndSummoning
{
	internal class SubModule : MBSubModuleBase
	{
		public static Config.Config Config { get; private set; }

		public static ItemUnitConfig ItemUnitConfig { get; private set; }

		public static UnitUnitConfig UnitUnitConfig { get; private set; }

		public static UnitBuildFromPartConfig UnitBuildFromPartConfig { get; private set; }

		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			SubModule.LoadConfig();
            harmony = new Harmony("necromancyAndSummoning");
			harmony.PatchAll();

        }
		private Harmony harmony;
		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			if(mission.IsSiegeBattle || mission.IsSallyOutBattle || mission.IsFieldBattle)
				mission.AddMissionBehavior(new NecromancyAndSummoningLogic());
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			harmony.Patch(AccessTools.Method(typeof(Mission), "MissileHitCallback"), null, new HarmonyMethod(typeof(SummoningAndRaiseCorpsePatch), "Postfix"));

            InformationManager.DisplayMessage(new InformationMessage("NecromancyAndSummoning OnGameStart"));
			bool flag = game.GameType is Campaign;
			if (flag)
			{
				CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
				campaignGameStarter.AddBehavior(new NecromancyBehaviour());
			}
		}

		private static void LoadConfig()
		{
			bool flag = !File.Exists(SubModule.ConfigFilePath);
			if (!flag)
			{
				bool flag2 = !File.Exists(SubModule.ItemUnitConfigFilePath);
				if (!flag2)
				{
					bool flag3 = !File.Exists(SubModule.UnitUnitConfigFilePath);
					if (!flag3)
					{
						bool flag4 = !File.Exists(SubModule.UnitBuildFromPartConfigFilePath);
						if (!flag4)
						{
							try
							{
								SubModule.Config = JsonConvert.DeserializeObject<Config.Config>(File.ReadAllText(SubModule.ConfigFilePath));
								SubModule.ItemUnitConfig = JsonConvert.DeserializeObject<ItemUnitConfig>(File.ReadAllText(SubModule.ItemUnitConfigFilePath));
								SubModule.UnitUnitConfig = JsonConvert.DeserializeObject<UnitUnitConfig>(File.ReadAllText(SubModule.UnitUnitConfigFilePath));
								SubModule.UnitBuildFromPartConfig = JsonConvert.DeserializeObject<UnitBuildFromPartConfig>(File.ReadAllText(SubModule.UnitBuildFromPartConfigFilePath));
							}
							catch
							{
								throw new Exception("Config Encounter Errors.");
							}
						}
					}
				}
			}
		}

		private static readonly string ConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.json");

		private static readonly string ItemUnitConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "item_unit_config.json");

		private static readonly string UnitUnitConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "unit_unit_config.json");

		private static readonly string UnitBuildFromPartConfigFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "unit_build_from_part_config.json");
	}
}
