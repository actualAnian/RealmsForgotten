using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem.StatusEffects;

public static class StatusEffectManager
{
	private const string TemplateFileName = "tor_statuseffects.xml";

	private static readonly Dictionary<string, StatusEffectTemplate> Templates = new Dictionary<string, StatusEffectTemplate>();

	public static StatusEffectTemplate GetTemplate(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (!Templates.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static List<StatusEffectTemplate> GetStatusEffectTemplatesWithIds(IEnumerable<string> ids)
	{
		List<StatusEffectTemplate> list = new List<StatusEffectTemplate>();
		if (ids == null)
		{
			return list;
		}
		foreach (string id in ids)
		{
			StatusEffectTemplate template = GetTemplate(id);
			if (template != null)
			{
				list.Add(template);
			}
		}
		return list;
	}

	public static StatusEffect CreateNewStatusEffect(string effectId, Agent applierAgent)
	{
		StatusEffectTemplate template = GetTemplate(effectId);
		if (template != null)
		{
			return new StatusEffect(template, applierAgent);
		}
		return null;
	}

	public static void LoadTemplates()
	{
		Templates.Clear();
		string text = Path.Combine(SOTOR.RFIntegration.RFModulePath.Root, "ModuleData", "tor_custom_xmls", "tor_statuseffects.xml"); // [RF-D] identidade: o modulo hospedeiro agora e RF_Magic, nao SOTOR
		if (!File.Exists(text))
		{
			SotorLog.Warn("Status-effect templates not found at " + text);
			return;
		}
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<StatusEffectTemplate>), new XmlRootAttribute("StatusEffects"));
		using (FileStream stream = File.OpenRead(text))
		{
			if (xmlSerializer.Deserialize(stream) is List<StatusEffectTemplate> source)
			{
				foreach (StatusEffectTemplate item in source.Where((StatusEffectTemplate t) => t != null && !string.IsNullOrEmpty(t.StringID)))
				{
					Templates[item.StringID] = item;
				}
			}
		}
		// [RF-B] efeitos de status do RF em rf_*.xml na mesma pasta.
		SOTOR.RFIntegration.RFSpellOverlay.LoadRfXmls<StatusEffectTemplate>(text, "StatusEffects", delegate(List<StatusEffectTemplate> list)
		{
			foreach (StatusEffectTemplate item in list.Where((StatusEffectTemplate t) => t != null && !string.IsNullOrEmpty(t.StringID)))
			{
				Templates[item.StringID] = item;
			}
		});
		SotorLog.Info($"Loaded {Templates.Count} status-effect template(s) from {text}");
	}
}
