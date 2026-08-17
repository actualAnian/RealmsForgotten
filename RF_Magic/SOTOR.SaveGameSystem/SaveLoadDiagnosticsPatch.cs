using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace SOTOR.SaveGameSystem;

[HarmonyPatch(typeof(LoadContext), nameof(LoadContext.Load))]
internal static class SaveLoadDiagnosticsPatch
{
	private static readonly MethodInfo ExceptionMessageGetter = AccessTools.PropertyGetter(typeof(Exception), nameof(Exception.Message));
	private static readonly MethodInfo ReportMethod = AccessTools.Method(typeof(SaveLoadDiagnosticsPatch), nameof(Report));

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> result = new List<CodeInstruction>(instructions);
		int replacements = 0;
		for (int i = 0; i < result.Count; i++)
		{
			if (result[i].Calls(ExceptionMessageGetter))
			{
				result[i].opcode = OpCodes.Call;
				result[i].operand = ReportMethod;
				replacements++;
			}
		}

		if (replacements != 1)
		{
			throw new InvalidOperationException($"Expected one save-load exception message call, found {replacements}.");
		}

		return result;
	}

	private static string Report(Exception exception)
	{
		SotorLog.Error("SAVE LOAD FAILURE (full exception): " + exception);
		return exception.Message;
	}

	[HarmonyPatch]
	private static class ObjectArchiveDiagnostics
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				typeof(LoadContext),
				"CreateLoadData",
				new[] { typeof(LoadData), typeof(int), typeof(ObjectHeaderLoadData) });
		}

		private static Exception Finalizer(Exception __exception, int i, ObjectHeaderLoadData header)
		{
			if (__exception != null)
			{
				string typeName = header?.TypeDefinition?.Type?.AssemblyQualifiedName ?? "<unknown>";
				string saveId = header?.SaveId?.ToString() ?? "<unknown>";
				SotorLog.Error($"SAVE OBJECT ARCHIVE FAILURE: objectIndex={i}; type={typeName}; saveId={saveId}; exception={__exception}");
			}

			return __exception;
		}
	}
}
