using System;
using System.Collections.Generic;

namespace SOTOR.AbilitySystem.TriggeredScripts;

public static class TriggeredScriptRegistry
{
	private static readonly Dictionary<string, ITriggeredScript> _scripts = new Dictionary<string, ITriggeredScript>(StringComparer.OrdinalIgnoreCase)
	{
		["SpiritLeech"] = new SpiritLeech(),
		["SummonScript"] = new Summon(),
		["Summon"] = new Summon(),
		// [RF-B] Skull of Terror: unico feitico do RF sem equivalente no SOTOR — ele
		// nao tem NENHUM efeito de moral. Referenciado por ScriptNameToTrigger no
		// rf_triggeredeffects.xml.
		["RfTerror"] = new RfTerror()
	};

	public static ITriggeredScript Resolve(string scriptName)
	{
		if (string.IsNullOrWhiteSpace(scriptName) || scriptName.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		int num = scriptName.LastIndexOf('.');
		string key = ((num >= 0) ? scriptName.Substring(num + 1) : scriptName);
		if (!_scripts.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}
}
