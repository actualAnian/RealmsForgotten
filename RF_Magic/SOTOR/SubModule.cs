using System;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using SOTOR.AbilitySystem;
using SOTOR.AbilitySystem.AI;
using SOTOR.AbilitySystem.StatusEffects;
using SOTOR.CampaignBehaviors;
using SOTOR.Extensions.ExtendedInfoSystem;
using SOTOR.GameManagers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SOTOR;

public class SubModule : MBSubModuleBase
{
	private const string HarmonyId = "sotor.harmony";

	private UIExtender _uiExtender;

	private static Harmony _harmony;

	private bool _mcmSynced;

	public static Harmony HarmonyInstance => _harmony;

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		SotorLog.Info("SubModule load. Log file: " + SotorLog.LogFilePath);
		SotorSettings.Load();
		// [RF-B] registro dos focos arcanos (rf_arcane_foci.xml). Se o XML faltar o
		// registro fica vazio e o gate se desliga sozinho — fail-open deliberado.
		SOTOR.RFIntegration.ArcaneFocusRegistry.Load();
		SOTOR.MagicAccessories.MagicAccessoryRegistry.Load();
		SOTOR.MagicAccessories.MagicRuneRegistry.Load();
		SOTOR.MagicAccessories.MagicRuneLoadoutRegistry.Load();
		// [RF-B] escolas por cultura + piso de Arcane por escola. Ambos fail-open:
		// XML ausente devolve o comportamento do SOTOR puro.
		SOTOR.RFIntegration.RFCultureLores.Load();
		SOTOR.RFIntegration.RFLoreRequirements.Load();
		// [RF-B] economia da magia editavel por XML — o SOTOR distribuia o arquivo
		// mas nenhuma versao dele o lia; este loader implementa o que ele prometia.
		SOTOR.RFIntegration.RFSpellPrices.Load();
		AbilityFactory.LoadTemplates();
		TriggeredEffectManager.LoadTemplates();
		StatusEffectManager.LoadTemplates();
		SotorKeyInputManager.Initialize();
		try
		{
			_harmony = new Harmony("sotor.harmony");
			// [RF-A] PatchAllUncategorized aplica tudo numa unica operacao: basta UMA
			// classe com assinatura incompativel com a versao do jogo (no 1.4.7 e o
			// SotorGraveyardMountPatch) para a excecao abortar a varredura inteira e
			// matar junto todos os patches ainda na fila — falha silenciosa. Isolar
			// por classe restaura o resultado que o SOTOR pretendia.
			ApplyUncategorizedPatchesSafely();
			SOTOR.MagicAccessories.MagicPowerRingCombat.TryPatchLegacyFireTick(_harmony);
		}
		catch (Exception ex)
		{
			SotorLog.Error("Harmony PatchAll failed: " + ex.GetType().Name + ": " + ex.Message);
		}
		_uiExtender = UIExtender.Create("SOTOR");
		_uiExtender.Register(typeof(SubModule).Assembly);
		_uiExtender.Enable();
		SotorLog.Info("UIExtenderEx enabled.");
	}

	/// <summary>
	/// [RF-A] Aplica os patches Harmony NAO categorizados uma classe por vez, cada
	/// uma isolada num try/catch.
	///
	/// Substitui <c>Harmony.PatchAllUncategorized(assembly)</c>. A semantica de
	/// selecao e identica a do Harmony: cria o class processor de cada tipo do
	/// assembly e so aplica os de <c>Category</c> vazia, deixando a categoria
	/// mission-only para o AbilityManagerMissionLogic aplicar depois. Com tudo
	/// passando o resultado e o mesmo; o que muda e que uma classe quebrada agora
	/// custa so ela mesma, e o log diz qual foi.
	/// </summary>
	private static void ApplyUncategorizedPatchesSafely()
	{
		Type[] types;
		try
		{
			types = AccessTools.GetTypesFromAssembly(typeof(SubModule).Assembly);
		}
		catch (Exception ex)
		{
			SotorLog.Error("Harmony sweep: could not enumerate assembly types: " + ex.GetType().Name + ": " + ex.Message);
			return;
		}

		int applied = 0;
		int failed = 0;
		foreach (Type type in types)
		{
			if (type == null)
			{
				continue;
			}

			try
			{
				PatchClassProcessor processor = _harmony.CreateClassProcessor(type);
				if (!string.IsNullOrEmpty(processor.Category))
				{
					continue;
				}

				var patched = processor.Patch();
				if (patched != null && patched.Count > 0)
				{
					applied++;
				}
			}
			catch (Exception ex)
			{
				failed++;
				SotorLog.Error("Harmony patch FAILED for class " + type.FullName + " — " + ex.GetType().Name + ": " + ex.Message + " (continuing with the remaining patch classes)");
			}
		}

		SotorLog.Info("Harmony uncategorized patches applied (mission-only category deferred). patchedClasses=" + applied + " failedClasses=" + failed);
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		if (_mcmSynced)
		{
			return;
		}
		_mcmSynced = true;
		try
		{
			SotorMcmBridge.Initialize();
		}
		catch (Exception ex)
		{
			SotorLog.Warn("MCM bridge init skipped (" + ex.GetType().Name + "): " + ex.Message + ". Using SotorSettings defaults.");
		}
	}

	protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
	{
		if (game.GameType is Campaign && starterObject is CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddBehavior(new ExtendedInfoManager());
			campaignGameStarter.AddBehavior(new SotorRaiseDeadBehavior());
			campaignGameStarter.AddBehavior(new SotorGraveyardBehavior());
			campaignGameStarter.AddBehavior(new SOTOR.MagicAccessories.MagicPowerRingCampaignBehavior());
			SotorLog.Info("ExtendedInfoManager + SotorRaiseDeadBehavior + SotorGraveyardBehavior registered.");
		}
	}

	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		SotorLog.Info($"Mission behaviors init. friendly={mission.IsFriendlyMission} combatType={(int)mission.CombatType} mode={(int)mission.Mode}");
		mission.AddMissionBehavior(new AbilityManagerMissionLogic());
		mission.AddMissionBehavior((MissionBehavior)(object)new AbilityHUDMissionView());
		mission.AddMissionBehavior(new StatusEffectMissionLogic());
		mission.AddMissionBehavior(new TransformationMissionLogic());
		mission.AddMissionBehavior(new SotorUndeadMoraleMissionLogic());
		mission.AddMissionBehavior(new SotorThrownJavelinMissionLogic());
		mission.AddMissionBehavior(new SotorMindControlMissionLogic());
		mission.AddMissionBehavior(new SotorCastingAIMissionLogic());
		mission.AddMissionBehavior(new SotorBurningDeckMissionLogic());
		mission.AddMissionBehavior(new SotorAbandonShipMissionLogic());
		mission.AddMissionBehavior(new SotorSummonNavalGuardMissionLogic());
		mission.AddMissionBehavior(new SOTOR.MagicAccessories.MagicRuneMissionLogic());
		mission.AddMissionBehavior(new SOTOR.MagicAccessories.MagicPowerRingMissionLogic());
	}
}
