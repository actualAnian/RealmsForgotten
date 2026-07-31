using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SOTOR.AbilitySystem.Crosshairs;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace SOTOR.AbilitySystem;

public static class AbilityFactory
{
	private static readonly Dictionary<string, AbilityTemplate> Templates = new Dictionary<string, AbilityTemplate>();

	private const string TemplateFileName = "tor_abilitytemplates.xml";

	public static AbilityTemplate GetTemplate(string id)
	{
		if (!Templates.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static IReadOnlyList<AbilityTemplate> GetTemplatesByLore(string loreId)
	{
		return (from t in Templates.Values
			where t.BelongsToLoreID == loreId
			orderby t.SpellTier
			select t).ToList();
	}

	public static void LoadTemplates()
	{
		Templates.Clear();
		string text = Path.Combine(SOTOR.RFIntegration.RFModulePath.Root, "ModuleData", "tor_custom_xmls", "tor_abilitytemplates.xml"); // [RF-D] identidade: o modulo hospedeiro agora e RF_Magic, nao SOTOR
		if (!File.Exists(text))
		{
			SotorLog.Warn("Ability templates not found at " + text);
			return;
		}
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<AbilityTemplate>), new XmlRootAttribute("AbilityTemplates"));
		using (FileStream stream = File.OpenRead(text))
		{
			if (xmlSerializer.Deserialize(stream) is List<AbilityTemplate> source)
			{
				foreach (AbilityTemplate item in source.Where((AbilityTemplate t) => t != null && !string.IsNullOrEmpty(t.StringID)))
				{
					Templates[item.StringID] = item;
				}
			}
		}
		// [RF-B] feiticos do RF vivem em rf_*.xml na mesma pasta; StringID repetido
		// sobrescreve de proposito.
		SOTOR.RFIntegration.RFSpellOverlay.LoadRfXmls<AbilityTemplate>(text, "AbilityTemplates", delegate(List<AbilityTemplate> list)
		{
			foreach (AbilityTemplate item in list.Where((AbilityTemplate t) => t != null && !string.IsNullOrEmpty(t.StringID)))
			{
				Templates[item.StringID] = item;
			}
		});
		SotorLog.Info($"Loaded {Templates.Count} ability template(s) from {text}");
	}

	public static Ability CreateNew(string id, Agent caster)
	{
		if (id == "AmberSpear" && SotorSettings.UseThrownAmberSpear)
		{
			id = "AmberSpearThrown";
		}
		if (!Templates.TryGetValue(id, out var value))
		{
			return null;
		}
		if (id == "AmberSpearThrown")
		{
			return new ThrownWeaponAbility(value);
		}
		if (value.AbilityType == AbilityType.Spell)
		{
			return new Spell(value);
		}
		return null;
	}

	public static AbilityCrosshair InitializeCrosshair(AbilityTemplate template, Mission mission, MissionScreen missionScreen, Agent caster)
	{
		switch (template.CrosshairType)
		{
		case CrosshairType.Missile:
			return new MissileCrosshair(template, mission, missionScreen, caster);
		case CrosshairType.SingleTarget:
			return new SingleTargetCrosshair(template, mission, missionScreen, caster);
		case CrosshairType.Self:
			return new SelfCrosshair(template, mission, missionScreen, caster);
		case CrosshairType.Wind:
			return new WindCrosshair(template, mission, missionScreen, caster);
		case CrosshairType.TargetedAOE:
			return new TargetedAOECrosshair(template, mission, missionScreen, caster);
		default:
			SotorLog.Debug($"InitializeCrosshair: no crosshair impl for {template.CrosshairType} yet.");
			return null;
		}
	}
}
