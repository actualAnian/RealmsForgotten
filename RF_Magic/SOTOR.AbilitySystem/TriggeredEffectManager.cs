using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;

namespace SOTOR.AbilitySystem;

public static class TriggeredEffectManager
{
	private static readonly Dictionary<string, TriggeredEffectTemplate> Templates = new Dictionary<string, TriggeredEffectTemplate>();

	private const string TemplateFileName = "tor_triggeredeffects.xml";

	public static TriggeredEffectTemplate GetTemplate(string id)
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

	public static void LoadTemplates()
	{
		Templates.Clear();
		string text = Path.Combine(SOTOR.RFIntegration.RFModulePath.Root, "ModuleData", "tor_custom_xmls", "tor_triggeredeffects.xml"); // [RF-D] identidade: o modulo hospedeiro agora e RF_Magic, nao SOTOR
		if (!File.Exists(text))
		{
			SotorLog.Warn("Triggered-effect templates not found at " + text);
			return;
		}
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<TriggeredEffectTemplate>), new XmlRootAttribute("TriggeredEffects"));
		using (FileStream stream = File.OpenRead(text))
		{
			if (xmlSerializer.Deserialize(stream) is List<TriggeredEffectTemplate> source)
			{
				foreach (TriggeredEffectTemplate item in source.Where((TriggeredEffectTemplate t) => t != null && !string.IsNullOrEmpty(t.StringID)))
				{
					Templates[item.StringID] = item;
				}
			}
		}
		// [RF-B] efeitos disparados do RF em rf_*.xml na mesma pasta.
		SOTOR.RFIntegration.RFSpellOverlay.LoadRfXmls<TriggeredEffectTemplate>(text, "TriggeredEffects", delegate(List<TriggeredEffectTemplate> list)
		{
			foreach (TriggeredEffectTemplate item in list.Where((TriggeredEffectTemplate t) => t != null && !string.IsNullOrEmpty(t.StringID)))
			{
				Templates[item.StringID] = item;
			}
		});
		SotorLog.Info($"Loaded {Templates.Count} triggered-effect template(s) from {text}");
	}
}
