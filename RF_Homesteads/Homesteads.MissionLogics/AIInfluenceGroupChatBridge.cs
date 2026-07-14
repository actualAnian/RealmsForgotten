using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

internal sealed class AIInfluenceGroupChatBridge : MissionLogic
{
	private const string GroupNs = "AIInfluence.Behaviors.GroupConversation.";

	private const string MissionBehaviorTn = "AIInfluence.Behaviors.GroupConversation.GroupConversationMissionBehavior";

	private const string ManagerTn = "AIInfluence.Behaviors.GroupConversation.GroupConversationManager";

	private const string BehaviorTn = "AIInfluence.AIInfluenceBehavior";

	private const string HarmonyId = "Bannerlord.Windwhistle.HomesteadsReloaded.AIInfluenceBridge";

	private bool _ran;

	public override void AfterStart()
	{
		base.AfterStart();
	}

	public override void OnMissionTick(float dt)
	{
		if (_ran)
		{
			return;
		}
		_ran = true;
		try
		{
			TryEnableGroupChat();
		}
		catch (Exception ex)
		{
			TraceLogger.Write("AIInfluenceGroupChatBridge", "TryEnableGroupChat threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void TryEnableGroupChat()
	{
		Assembly assembly = FindAIInfluenceAssembly();
		if (assembly == null)
		{
			TraceLogger.Write("AIInfluenceGroupChatBridge", "AI Influence not installed — group chat bridge skipped.");
			return;
		}
		Type type = assembly.GetType("AIInfluence.Behaviors.GroupConversation.GroupConversationMissionBehavior");
		DumpDiagnostics(assembly, type);
		PatchIsSupportedMissionContext(assembly);
		if (type != null && MissionAlreadyHas(type))
		{
			TraceLogger.Write("AIInfluenceGroupChatBridge", "GroupConversationMissionBehavior already on mission (added via Activator path); IsSupportedMissionContext patched — group chat should now be active.");
			return;
		}
		object singleton = GetSingleton(assembly, "AIInfluence.AIInfluenceBehavior");
		if (singleton == null || !TryInvokeMissionRegistrar(singleton, "on AIInfluenceBehavior"))
		{
			object singleton2 = GetSingleton(assembly, "AIInfluence.Behaviors.GroupConversation.GroupConversationManager");
			if (singleton2 == null || !TryInvokeMissionRegistrar(singleton2, "on GroupConversationManager"))
			{
				TraceLogger.Write("AIInfluenceGroupChatBridge", "GroupConversationMissionBehavior not on mission and no usable registrar found. IsSupportedMissionContext is patched but group chat may not start without the behavior.");
			}
		}
	}

	private static void PatchIsSupportedMissionContext(Assembly asm)
	{
		try
		{
			Type type = asm.GetType("AIInfluence.Behaviors.GroupConversation.GroupConversationManager");
			if (type == null)
			{
				TraceLogger.Write("AIInfluenceGroupChatBridge", "PatchIsSupportedMissionContext: GroupConversationManager type not found.");
				return;
			}
			MethodInfo method = type.GetMethod("IsSupportedMissionContext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
			if (method == null)
			{
				TraceLogger.Write("AIInfluenceGroupChatBridge", "PatchIsSupportedMissionContext: IsSupportedMissionContext method not found.");
				return;
			}
			MethodInfo method2 = typeof(AIInfluenceGroupChatBridge).GetMethod("IsSupportedMissionContextPostfix", BindingFlags.Static | BindingFlags.NonPublic);
			if (method2 == null)
			{
				TraceLogger.Write("AIInfluenceGroupChatBridge", "PatchIsSupportedMissionContext: postfix method not found via reflection.");
				return;
			}
			new Harmony("Bannerlord.Windwhistle.HomesteadsReloaded.AIInfluenceBridge").Patch(method, null, new HarmonyMethod(method2));
			TraceLogger.Write("AIInfluenceGroupChatBridge", "Patched GroupConversationManager.IsSupportedMissionContext — homestead missions will now report as a supported group-chat context.");
		}
		catch (Exception ex)
		{
			TraceLogger.Write("AIInfluenceGroupChatBridge", "PatchIsSupportedMissionContext threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void IsSupportedMissionContextPostfix(ref bool __result)
	{
		if (!__result && HomesteadBehavior.Instance?.CurrentHomestead != null)
		{
			__result = true;
		}
	}

	private bool TryInvokeMissionRegistrar(object target, string where)
	{
		string[] obj = new string[9] { "AddCriticalMissionBehaviors", "AddMissionBehavior", "AddMissionBehaviors", "RegisterMission", "OnMissionStarted", "InitializeMission", "EnableForMission", "StartForMission", "SetupMission" };
		MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		string[] array = obj;
		foreach (string b in array)
		{
			MethodInfo[] array2 = methods;
			foreach (MethodInfo methodInfo in array2)
			{
				if (!string.Equals(methodInfo.Name, b, StringComparison.Ordinal))
				{
					continue;
				}
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(base.Mission))
				{
					try
					{
						methodInfo.Invoke(target, new object[1] { base.Mission });
						TraceLogger.Write("AIInfluenceGroupChatBridge", "Forced group chat: invoked " + methodInfo.Name + "(Mission) " + where + ".");
						return true;
					}
					catch (Exception ex)
					{
						TraceLogger.Write("AIInfluenceGroupChatBridge", methodInfo.Name + "(Mission) " + where + " threw " + ex.GetType().Name + ": " + (ex.InnerException?.Message ?? ex.Message));
					}
				}
			}
		}
		return false;
	}

	private bool MissionAlreadyHas(Type behaviorType)
	{
		foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
		{
			if (behaviorType.IsInstanceOfType(missionBehavior))
			{
				return true;
			}
		}
		return false;
	}

	private static object? GetSingleton(Assembly asm, string typeName)
	{
		try
		{
			Type type = asm.GetType(typeName);
			if (type == null)
			{
				return null;
			}
			return (type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public) ?? type.GetProperty("Current", BindingFlags.Static | BindingFlags.Public))?.GetValue(null);
		}
		catch
		{
			return null;
		}
	}

	private static Assembly? FindAIInfluenceAssembly()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (string.Equals(assembly.GetName().Name, "AIInfluence", StringComparison.Ordinal))
			{
				return assembly;
			}
		}
		return null;
	}

	private void DumpDiagnostics(Assembly asm, Type? mbType)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("AI Influence group-chat API probe:");
			if (mbType != null)
			{
				stringBuilder.AppendLine("  GroupConversationMissionBehavior constructors:");
				ConstructorInfo[] constructors = mbType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (ConstructorInfo constructorInfo in constructors)
				{
					stringBuilder.AppendLine("    .ctor(" + DescribeParams(constructorInfo.GetParameters()) + ")");
				}
			}
			else
			{
				stringBuilder.AppendLine("  GroupConversationMissionBehavior type NOT found.");
			}
			DumpTypeMethods(stringBuilder, asm, "AIInfluence.AIInfluenceBehavior", "AIInfluenceBehavior");
			DumpTypeMethods(stringBuilder, asm, "AIInfluence.Behaviors.GroupConversation.GroupConversationManager", "GroupConversationManager");
			stringBuilder.AppendLine("  Current mission behaviors:");
			foreach (MissionBehavior missionBehavior in base.Mission.MissionBehaviors)
			{
				string text = missionBehavior.GetType().FullName ?? missionBehavior.GetType().Name;
				if (text.IndexOf("AIInfluence", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					stringBuilder.AppendLine("    * " + text);
				}
			}
			TraceLogger.Write("AIInfluenceGroupChatBridge", stringBuilder.ToString());
		}
		catch (Exception ex)
		{
			TraceLogger.Write("AIInfluenceGroupChatBridge", "DumpDiagnostics threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void DumpTypeMethods(StringBuilder sb, Assembly asm, string typeName, string label)
	{
		Type type = asm.GetType(typeName);
		if (type == null)
		{
			sb.AppendLine("  " + label + ": type NOT found.");
			return;
		}
		sb.AppendLine("  " + label + " methods (name contains Group/Mission/Conversation/Add/Register/Enable/Start):");
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in methods)
		{
			string name = methodInfo.Name;
			if (name.IndexOf("Group", StringComparison.Ordinal) >= 0 || name.IndexOf("Mission", StringComparison.Ordinal) >= 0 || name.IndexOf("Conversation", StringComparison.Ordinal) >= 0 || name.IndexOf("Add", StringComparison.Ordinal) >= 0 || name.IndexOf("Register", StringComparison.Ordinal) >= 0 || name.IndexOf("Enable", StringComparison.Ordinal) >= 0 || name.IndexOf("Start", StringComparison.Ordinal) >= 0)
			{
				string text = (methodInfo.IsStatic ? "static " : "");
				sb.AppendLine("    " + text + methodInfo.ReturnType.Name + " " + name + "(" + DescribeParams(methodInfo.GetParameters()) + ")");
			}
		}
	}

	private static string DescribeParams(ParameterInfo[] ps)
	{
		return string.Join(", ", ps.Select((ParameterInfo p) => p.ParameterType.Name + " " + p.Name));
	}
}
