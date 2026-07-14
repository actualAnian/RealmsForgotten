using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Homesteads.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.SaveSystem;

namespace Homesteads.Models;

public class HomesteadScene
{
	private const float HomesteadBallistaHorizontalArcRadians = TaleWorlds.Library.MathF.PI * 35f / 36f;

	private const float HomesteadBallistaVerticalArcHalfRadians = TaleWorlds.Library.MathF.PI * 4f / 9f;

	private const int HomesteadBallistaAmmo = 100;

	private const float MaxValidHorizontalArcRadians = 3.1315928f;

	[SaveableField(1)]
	public Homestead Homestead;

	[SaveableField(2)]
	public string SceneName;

	[SaveableField(3)]
	public List<HomesteadSceneSavedEntity> SavedEntities = new List<HomesteadSceneSavedEntity>();

	[SaveableField(4)]
	public int CurrentlyUsedBuildPoints;

	[SaveableField(5)]
	public int TotalProductivity;

	[SaveableField(6)]
	public int TotalSpace;

	[SaveableField(7)]
	public int TotalLeisure;

	[SaveableField(9)]
	public Vec3 PlayerSpawnPosition = Vec3.Invalid;

	[SaveableField(10)]
	public List<HomesteadScenePlaceableProducedItem> ProduceItems = new List<HomesteadScenePlaceableProducedItem>();

	[SaveableField(11)]
	public Mat3 PlayerSpawnRotation = Mat3.Identity;

	[SaveableField(12)]
	public int TotalMedicalCare;

	private Dictionary<GameEntity, HomesteadSceneSavedEntity> loadedSavedEntities = new Dictionary<GameEntity, HomesteadSceneSavedEntity>();

	private static readonly HashSet<string> ClanBannerPropPrefabNames = new HashSet<string> { "homestead_clan_banner_flag", "homestead_clan_banner_flagpole", "homestead_clan_banner_shield" };

	private const string ClanBannerClothTag = "hsr_clan_banner_cloth";

	private static readonly List<Material> _clanBannerMaterialKeepAlive = new List<Material>();

	public int MaxBuildPoints => Homestead.Tier switch
	{
		0 => 30, 
		1 => 90, 
		2 => 150, 
		3 => 210, 
		4 => 280, 
		_ => 30, 
	};

	public int BuildPointsLeftToUse => MaxBuildPoints - CurrentlyUsedBuildPoints;

	public int PrisonerCapacity => SavedEntities?.Sum((HomesteadSceneSavedEntity x) => GetPrisonerCapacityForPrefab(x?.Placeable?.PrefabName)) ?? 0;

	public IEnumerable<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>> LoadedSavedEntities => loadedSavedEntities;

	private static int GetPrisonerCapacityForPrefab(string? prefabName)
	{
		if (!(prefabName == "homestead_cage_wooden"))
		{
			if (prefabName == "homestead_prison_guardhouse")
			{
				return 10;
			}
			return 0;
		}
		return 2;
	}

	public HomesteadScene(string sceneName, Homestead homestead)
	{
		SceneName = sceneName;
		Homestead = homestead;
	}

	public void AddPlaceableEntityToCurrentScene(HomesteadScenePlaceable placeable, Vec3 position, Mat3 rotation, bool skipItemCheck = false)
	{
		if (placeable.BuildPointsRequired > BuildPointsLeftToUse)
		{
			Utils.PrintLocalizedMessage("homestead_cannot_place_no_build_points", "You cannot place this object! You need to upgrade your tier or remove other objects.", 255f, 80f, 80f);
			return;
		}
		if (placeable.MaxBuildCount > 0 && CountBuiltPlaceables(placeable.PrefabName) >= placeable.MaxBuildCount)
		{
			Utils.PrintLocalizedMessage("homestead_cannot_place_build_limit", "You cannot place any more of this object in this homestead.", 255f, 80f, 80f);
			return;
		}
		if (!skipItemCheck && !Utils.DoesItemRosterHaveItems(Homestead.Stash, placeable.ItemRequirements, takeItems: true))
		{
			Utils.PrintLocalizedMessage("homestead_cannot_place_lacking_items", "You do not have the items required in your homestead's stash for this object!", 255f, 80f, 80f);
			return;
		}
		GameEntity gameEntity;
		try
		{
			gameEntity = Utils.CreateGameEntityWithPrefab(placeable.PrefabName, position, rotation, enablePhysics: true, ShouldMakePrefabStatic(placeable.PrefabName), ShouldApplyHomesteadPhysicsState(placeable.PrefabName));
		}
		catch (Exception ex)
		{
			Utils.PrintDebugMessage("FAILED TO SPAWN HOMESTEAD PREFAB " + placeable.PrefabName, 255f, 0f, 0f);
			TraceLogger.Write("HomesteadScene", "AddPlaceableEntityToCurrentScene failed for '" + placeable.PrefabName + "': " + ex.GetType().Name + ": " + ex.Message);
			Utils.DiagnosePrefabXml(placeable.PrefabName);
			return;
		}
		ConfigureNativeSiegeInteractable(gameEntity, placeable.PrefabName);
		ApplyClanBannerIfNeeded(gameEntity, placeable.PrefabName);
		AddPlaceableEntityValues(placeable);
		HomesteadSceneSavedEntity homesteadSceneSavedEntity = new HomesteadSceneSavedEntity(placeable, gameEntity.GlobalPosition, rotation);
		SavedEntities.Add(homesteadSceneSavedEntity);
		loadedSavedEntities.Add(gameEntity, homesteadSceneSavedEntity);
	}

	private int CountBuiltPlaceables(string prefabName)
	{
		if (SavedEntities == null || string.IsNullOrEmpty(prefabName))
		{
			return 0;
		}
		return SavedEntities.Count((HomesteadSceneSavedEntity x) => x?.Placeable?.PrefabName == prefabName);
	}

	public void RemovePlaceableEntityFromCurrentScene(GameEntity entity)
	{
		GameEntity prefabParentEntity;
		HomesteadScenePlaceable homesteadSceneEntityPlaceable = GetHomesteadSceneEntityPlaceable(entity, out prefabParentEntity);
		if (homesteadSceneEntityPlaceable != null && !(prefabParentEntity == null))
		{
			RemovePlaceableEntityValues(homesteadSceneEntityPlaceable);
			SavedEntities.Remove(loadedSavedEntities[prefabParentEntity]);
			loadedSavedEntities.Remove(prefabParentEntity);
			prefabParentEntity.Remove(0);
		}
	}

	public HomesteadScenePlaceable? GetHomesteadSceneEntityPlaceable(GameEntity entity, out GameEntity? prefabParentEntity)
	{
		GameEntity gameEntity = entity;
		while (gameEntity != null)
		{
			if (loadedSavedEntities.ContainsKey(gameEntity))
			{
				prefabParentEntity = gameEntity;
				return loadedSavedEntities[gameEntity].Placeable;
			}
			gameEntity = gameEntity.Parent;
		}
		prefabParentEntity = null;
		return null;
	}

	public int AddAllSavedEntitiesToCurrentScene(Func<Vec3, Vec3>? positionTransform = null, Func<Mat3, Mat3>? rotationTransform = null)
	{
		loadedSavedEntities = new Dictionary<GameEntity, HomesteadSceneSavedEntity>();
		int num = 0;
		foreach (HomesteadSceneSavedEntity item in SavedEntities.ToList())
		{
			if (AddSavedEntityToCurrentScene(item, positionTransform, rotationTransform))
			{
				num++;
			}
		}
		return num;
	}

	private bool AddSavedEntityToCurrentScene(HomesteadSceneSavedEntity savedEntity, Func<Vec3, Vec3>? positionTransform = null, Func<Mat3, Mat3>? rotationTransform = null)
	{
		Mat3 mat = new Mat3(new Vec3(savedEntity.rotSx, savedEntity.rotSy, savedEntity.rotSz), new Vec3(savedEntity.rotFx, savedEntity.rotFy, savedEntity.rotFz), new Vec3(savedEntity.rotUx, savedEntity.rotUy, savedEntity.rotUz));
		Vec3 vec = new Vec3(savedEntity.posX, savedEntity.posY, savedEntity.posZ);
		if (positionTransform != null)
		{
			vec = positionTransform(vec);
		}
		if (rotationTransform != null)
		{
			mat = rotationTransform(mat);
		}
		string prefabName = savedEntity.Placeable.PrefabName;
		bool flag = GameEntity.PrefabExists(prefabName);
		if (!flag)
		{
			Utils.PrintDebugMessage("CAUGHT NON EXISTING PREFAB NAME " + prefabName + ". Spawning placeholder.", 255f, 128f, 0f);
			InformationManager.DisplayMessage(new InformationMessage("[Homesteads] '" + savedEntity.Placeable.DisplayName + "' (prefab '" + prefabName + "') not found. Spawning barrel stack placeholder.", Color.FromUint(4294934528u)));
		}
		try
		{
			string prefabName2 = (flag ? prefabName : "homestead_native_barrel_stack");
			GameEntity gameEntity = Utils.CreateGameEntityWithPrefab(prefabName2, vec, mat, enablePhysics: true, ShouldMakePrefabStatic(prefabName2), ShouldApplyHomesteadPhysicsState(prefabName2));
			ConfigureNativeSiegeInteractable(gameEntity, prefabName2);
			ApplyClanBannerIfNeeded(gameEntity, prefabName2);
			ForceEntityVisible(gameEntity, prefabName2);
			ReinitializeUsables(gameEntity);
			loadedSavedEntities[gameEntity] = savedEntity;
			if (positionTransform != null)
			{
				TraceLogger.Write("HomesteadScene", "Battle entity '" + prefabName + "' loaded at requested=" + Format(vec) + " actual=" + Format(gameEntity.GlobalPosition) + ".");
			}
			return true;
		}
		catch (Exception ex)
		{
			Utils.PrintDebugMessage("FAILED TO LOAD HOMESTEAD PREFAB " + prefabName + ". Spawning placeholder.", 255f, 128f, 0f);
			TraceLogger.Write("HomesteadScene", "AddSavedEntityToCurrentScene failed for '" + prefabName + "': " + ex.GetType().Name + ": " + ex.Message);
			Utils.DiagnosePrefabXml(prefabName);
			try
			{
				GameEntity gameEntity2 = Utils.CreateGameEntityWithPrefab("homestead_native_barrel_stack", vec, mat);
				ForceEntityVisible(gameEntity2, "homestead_native_barrel_stack");
				loadedSavedEntities[gameEntity2] = savedEntity;
				return true;
			}
			catch (Exception arg)
			{
				TraceLogger.Write("HomesteadScene", $"AddSavedEntityToCurrentScene failed even for placeholder: {arg}");
				RemovePlaceableEntityValues(savedEntity.Placeable);
				SavedEntities.Remove(savedEntity);
				return false;
			}
		}
	}

	private static void ApplyClanBannerIfNeeded(GameEntity entity, string prefabName)
	{
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		if (!ClanBannerPropPrefabNames.Contains(prefabName))
		{
			return;
		}
		try
		{
			Banner banner = Clan.PlayerClan?.Banner;
			TraceLogger.Write("HomesteadScene", $"ApplyClanBannerIfNeeded: prefab='{prefabName}' bannerNull={banner == null} bannerCode='{banner?.BannerCode}'.");
			if (banner == null)
			{
				return;
			}
			List<GameEntity> list = new List<GameEntity>();
			if (entity.HasTag("hsr_clan_banner_cloth"))
			{
				list.Add(entity);
			}
			entity.GetChildrenWithTagRecursive(list, "hsr_clan_banner_cloth");
			List<Mesh> meshesToPaint = new List<Mesh>();
			foreach (GameEntity item in list)
			{
				int count = meshesToPaint.Count;
				for (int i = 0; i < item.MultiMeshComponentCount; i++)
				{
					MetaMesh metaMesh = item.GetMetaMesh(i);
					for (int j = 0; j < metaMesh.MeshCount; j++)
					{
						Mesh meshAtIndex = metaMesh.GetMeshAtIndex(j);
						if (meshAtIndex != null)
						{
							meshesToPaint.Add(meshAtIndex);
						}
					}
				}
				if (meshesToPaint.Count == count)
				{
					Mesh firstMesh = item.GetFirstMesh();
					if (firstMesh != null)
					{
						meshesToPaint.Add(firstMesh);
					}
				}
			}
			TraceLogger.Write("HomesteadScene", $"ApplyClanBannerIfNeeded: taggedEntities={list.Count} meshesToPaint={meshesToPaint.Count}.");
			if (meshesToPaint.Count == 0)
			{
				return;
			}
			BannerDebugInfo val = BannerDebugInfo.CreateManual("HomesteadScene");
			BannerVisualExtensions.GetTableauTextureLarge(banner, ref val, (Action<Texture>)delegate(Texture texture)
			{
				try
				{
					TraceLogger.Write("HomesteadScene", $"ApplyClanBannerIfNeeded: texture callback fired, textureNull={texture == null}, painting {meshesToPaint.Count} mesh(es).");
					foreach (Mesh item2 in meshesToPaint)
					{
						Material material = item2.GetMaterial()?.CreateCopy();
						if (material == null)
						{
							TraceLogger.Write("HomesteadScene", "ApplyClanBannerIfNeeded: mesh.GetMaterial() returned null — skipped.");
						}
						else
						{
							material.SetTexture(Material.MBTextureType.DiffuseMap2, texture);
							Shader shader = material.GetShader();
							if (shader == null)
							{
								TraceLogger.Write("HomesteadScene", "ApplyClanBannerIfNeeded: material.GetShader() returned null — applying texture without the blend flag.");
							}
							else
							{
								uint num = (uint)shader.GetMaterialShaderFlagMask("use_tableau_blending");
								material.SetShaderFlags(material.GetShaderFlags() | num);
							}
							item2.SetMaterial(material);
							_clanBannerMaterialKeepAlive.Add(material);
						}
					}
					TraceLogger.Write("HomesteadScene", "ApplyClanBannerIfNeeded: finished painting.");
				}
				catch (Exception ex2)
				{
					TraceLogger.Write("HomesteadScene", "ApplyClanBannerIfNeeded: texture callback threw " + ex2.GetType().Name + ": " + ex2.Message + "\n" + ex2.StackTrace);
				}
			});
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadScene", "ApplyClanBannerIfNeeded failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
		}
	}

	private static void ForceEntityVisible(GameEntity entity, string prefabName)
	{
		if (IsNativeSiegeInteractablePrefab(prefabName))
		{
			entity.AddTag("homestead_battle_buildable");
		}
		else
		{
			ForceEntityVisibleRecursive(entity);
		}
	}

	private static void ForceEntityVisibleRecursive(GameEntity entity)
	{
		entity.SetVisibilityExcludeParents(visible: true);
		entity.SetReadyToRender(ready: true);
		entity.AddTag("homestead_battle_buildable");
		foreach (GameEntity child in entity.GetChildren())
		{
			ForceEntityVisibleRecursive(child);
		}
	}

	private static bool ShouldMakePrefabStatic(string prefabName)
	{
		return !IsNativeSiegeInteractablePrefab(prefabName);
	}

	private static bool ShouldApplyHomesteadPhysicsState(string prefabName)
	{
		return ShouldMakePrefabStatic(prefabName);
	}

	private static void ConfigureNativeSiegeInteractable(GameEntity entity, string prefabName)
	{
		if (!IsHomesteadBallistaPrefab(prefabName))
		{
			return;
		}
		Ballista firstScriptOfType = entity.GetFirstScriptOfType<Ballista>();
		if (firstScriptOfType != null)
		{
			float num = TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.PI * 35f / 36f, 3.1315928f);
			if (num < 0f)
			{
				num = 0f;
			}
			firstScriptOfType.HorizontalDirectionRestriction = num;
			ConfigureBallistaVerticalArc(firstScriptOfType);
			firstScriptOfType.SetStartAmmo(100);
			firstScriptOfType.SetAmmo(100);
			firstScriptOfType.SetIsDisabledForAI(isDisabledForAI: true);
			float num2 = num * 180f / TaleWorlds.Library.MathF.PI;
			TraceLogger.Write("HomesteadScene", $"Configured '{prefabName}' horizontal half-arc to {num2:0.#} degrees (clamped to valid [0,180] range), vertical arc to 160 degrees, ammo to {100}, AI operation disabled (prevents siege-AI NRE in field battles).");
		}
	}

	private static void ReinitializeUsables(GameEntity entity)
	{
		bool flag = Mission.Current?.GetMissionBehavior<HomesteadBattleSceneMissionLogic>() != null;
		List<ScriptComponentBehavior> list = new List<ScriptComponentBehavior>();
		CollectAllScriptsRecursive(entity, list);
		MethodInfo method = typeof(ScriptComponentBehavior).GetMethod("OnInit", BindingFlags.Instance | BindingFlags.NonPublic);
		foreach (ScriptComponentBehavior item in list)
		{
			if (item != null)
			{
				method?.Invoke(item, null);
			}
		}
		if (flag)
		{
			return;
		}
		MBList<UsableMachine> mBList = entity.CollectScriptComponentsIncludingChildrenRecursive<UsableMachine>();
		FieldInfo field = typeof(UsableMachine).GetField("<StandingPoints>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(UsableMachine).GetMethod("CollectAndSetStandingPoints", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
		foreach (UsableMachine item2 in mBList)
		{
			method2?.Invoke(item2, null);
			MBList<StandingPoint> mBList2 = item2.GameEntity.CollectScriptComponentsIncludingChildrenRecursive<StandingPoint>();
			if (field?.GetValue(item2) is MBList<StandingPoint> mBList3 && mBList2 != null)
			{
				mBList3.Clear();
				foreach (StandingPoint item3 in mBList2)
				{
					mBList3.Add(item3);
				}
			}
			if (Mission.Current != null && !Mission.Current.ActiveMissionObjects.Contains(item2))
			{
				Mission.Current.AddActiveMissionObject(item2);
			}
		}
	}

	private static void CollectAllScriptsRecursive(GameEntity entity, List<ScriptComponentBehavior> list)
	{
		if (entity == null)
		{
			return;
		}
		IEnumerable<ScriptComponentBehavior> scriptComponents = entity.GetScriptComponents();
		if (scriptComponents != null)
		{
			foreach (ScriptComponentBehavior item in scriptComponents)
			{
				if (item != null)
				{
					list.Add(item);
				}
			}
		}
		IEnumerable<GameEntity> children = entity.GetChildren();
		if (children == null)
		{
			return;
		}
		foreach (GameEntity item2 in children)
		{
			CollectAllScriptsRecursive(item2, list);
		}
	}

	private static void ConfigureBallistaVerticalArc(Ballista ballista)
	{
		ballista.TopReleaseAngleRestriction = TaleWorlds.Library.MathF.PI * 4f / 9f;
		ballista.BottomReleaseAngleRestriction = TaleWorlds.Library.MathF.PI * -4f / 9f;
		Type typeFromHandle = typeof(RangedSiegeWeapon);
		typeFromHandle.GetField("ReleaseAngleRestrictionCenter", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(ballista, 0f);
		typeFromHandle.GetField("ReleaseAngleRestrictionAngle", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(ballista, TaleWorlds.Library.MathF.PI * 4f / 9f);
	}

	private static bool IsHomesteadBallistaPrefab(string prefabName)
	{
		switch (prefabName)
		{
		default:
			return prefabName == "ballista_b_fire";
		case "ballista_b":
		case "ballista_a":
		case "ballista_a_fire":
			return true;
		}
	}

	private static bool IsNativeSiegeInteractablePrefab(string prefabName)
	{
		if (!(prefabName == "arrow_barrel"))
		{
			return IsHomesteadBallistaPrefab(prefabName);
		}
		return true;
	}

	private static string Format(Vec3 value)
	{
		return $"({value.x:0.##}, {value.y:0.##}, {value.z:0.##})";
	}

	private void RemovePlaceableEntityValues(HomesteadScenePlaceable placeable, bool doSave = true)
	{
		CurrentlyUsedBuildPoints -= placeable.BuildPointsRequired;
		TotalProductivity -= placeable.ProductivityIncrease;
		TotalSpace -= placeable.SpaceIncrease;
		TotalLeisure -= placeable.LeisureIncrease;
		TotalMedicalCare -= placeable.MedicalCareIncrease;
		Homestead?.MobileParty?.MemberRoster?.UpdateVersion();
		Homestead?.MobileParty?.PrisonRoster?.UpdateVersion();
		try
		{
			foreach (HomesteadScenePlaceableProducedItem produceItem in placeable.ProduceItems)
			{
				ProduceItems.Remove(produceItem);
			}
		}
		catch (NullReferenceException)
		{
			if (ProduceItems == null)
			{
				ProduceItems = new List<HomesteadScenePlaceableProducedItem>();
			}
		}
		HomesteadMissionView.TriggerSceneChanges();
		if (placeable.ItemRequirements == null || placeable.ItemRequirements.Count == 0)
		{
			return;
		}
		int skillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Engineering);
		foreach (KeyValuePair<string, int> itemRequirement in placeable.ItemRequirements)
		{
			string randomElementInefficiently = itemRequirement.Key.Split(new char[1] { '|' }).GetRandomElementInefficiently();
			ItemObject itemObject = Campaign.Current.ObjectManager.GetObject<ItemObject>(randomElementInefficiently);
			if (itemObject != null)
			{
				float num = TaleWorlds.Library.MathF.Lerp(0.05f, 0.5f, (float)skillValue / 300f);
				int num2 = (int)Math.Round((float)itemRequirement.Value * num);
				if (num2 > 0)
				{
					Hero.MainHero.AddSkillXp(DefaultSkills.Engineering, num * 100f);
					Homestead.Stash.AddToCounts(itemObject, num2);
				}
			}
		}
	}

	private void AddPlaceableEntityValues(HomesteadScenePlaceable placeable, bool triggerSceneChanges = true)
	{
		CurrentlyUsedBuildPoints += placeable.BuildPointsRequired;
		TotalProductivity += placeable.ProductivityIncrease;
		TotalSpace += placeable.SpaceIncrease;
		TotalLeisure += placeable.LeisureIncrease;
		TotalMedicalCare += placeable.MedicalCareIncrease;
		Homestead?.MobileParty?.MemberRoster?.UpdateVersion();
		Homestead?.MobileParty?.PrisonRoster?.UpdateVersion();
		try
		{
			if (placeable.ProduceItems != null)
			{
				ProduceItems.AddRange(placeable.ProduceItems);
			}
		}
		catch (NullReferenceException)
		{
			if (ProduceItems == null)
			{
				ProduceItems = new List<HomesteadScenePlaceableProducedItem>();
			}
		}
		if (triggerSceneChanges)
		{
			HomesteadMissionView.TriggerSceneChanges();
		}
	}

	public void RecalculateValues()
	{
		CurrentlyUsedBuildPoints = 0;
		TotalProductivity = 0;
		TotalSpace = 0;
		TotalLeisure = 0;
		TotalMedicalCare = 0;
		ProduceItems = new List<HomesteadScenePlaceableProducedItem>();
		if (SavedEntities == null)
		{
			return;
		}
		foreach (HomesteadSceneSavedEntity savedEntity in SavedEntities)
		{
			if (savedEntity?.Placeable == null)
			{
				continue;
			}
			if (savedEntity.Placeable.PrefabName == "homestead_clay_gatherer")
			{
				string text = savedEntity.Placeable.DisplayName ?? "";
				if (text.IndexOf("Iron", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("demir", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("желез", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("żelaz", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("fer", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("eisen", StringComparison.OrdinalIgnoreCase) >= 0 || text.Contains("铁"))
				{
					savedEntity.Placeable.PrefabName = "homestead_iron_mine";
					TraceLogger.Write("HomesteadScene", "Migrated old save placeable '" + text + "' prefab from homestead_clay_gatherer to homestead_iron_mine.");
				}
				else if (text.IndexOf("Silver", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("gümüş", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("серебр", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("srebr", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("argent", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("silber", StringComparison.OrdinalIgnoreCase) >= 0 || text.Contains("银"))
				{
					savedEntity.Placeable.PrefabName = "homestead_silver_mine";
					TraceLogger.Write("HomesteadScene", "Migrated old save placeable '" + text + "' prefab from homestead_clay_gatherer to homestead_silver_mine.");
				}
			}
			else if (savedEntity.Placeable.PrefabName == "homestead_native_decorated_work_table")
			{
				string text2 = savedEntity.Placeable.DisplayName ?? "";
				if (text2.IndexOf("Jewel", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("kuyum", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("ювелир", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("jubil", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("bijout", StringComparison.OrdinalIgnoreCase) >= 0 || text2.IndexOf("juwel", StringComparison.OrdinalIgnoreCase) >= 0 || text2.Contains("珠") || text2.Contains("首饰") || text2.Contains("宝"))
				{
					savedEntity.Placeable.PrefabName = "homestead_silversmith";
					TraceLogger.Write("HomesteadScene", "Migrated old save placeable '" + text2 + "' prefab from homestead_native_decorated_work_table to homestead_silversmith.");
				}
			}
			AddPlaceableEntityValues(savedEntity.Placeable, triggerSceneChanges: false);
		}
	}
}
