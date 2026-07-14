using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.SaveSystem.Load;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(ObjectLoadData), "Read")]
internal static class HomesteadSaveLoadRescuePatch
{
	private static readonly FieldInfo? ChildStructsField = typeof(ObjectLoadData).GetField("_childStructs", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly FieldInfo? MemberValuesField = typeof(ObjectLoadData).GetField("_memberValues", BindingFlags.Instance | BindingFlags.NonPublic);

	[HarmonyPrefix]
	private static bool Prefix(ObjectLoadData __instance)
	{
		if (ChildStructsField == null || MemberValuesField == null)
		{
			return true;
		}
		try
		{
			SafeReadObject(__instance);
			return false;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadSaveLoadRescuePatch", "Safe save-load reader failed, falling back to native reader: " + ex);
			return true;
		}
	}

	private static void SafeReadObject(ObjectLoadData objectLoadData)
	{
		IList list = ChildStructsField?.GetValue(objectLoadData) as IList;
		IList list2 = MemberValuesField?.GetValue(objectLoadData) as IList;
		if (list == null || list2 == null)
		{
			throw new InvalidOperationException("Could not read ObjectLoadData private fields.");
		}
		foreach (ObjectLoadData item in list)
		{
			SafeReadObject(item);
		}
		foreach (object item2 in list2)
		{
			if (TryReadMember(item2, objectLoadData) && IsCustomStructMember(item2))
			{
				int dataAsInt = GetDataAsInt(item2);
				if (dataAsInt >= 0 && dataAsInt < list.Count)
				{
					ObjectLoadData objectLoadData2 = (ObjectLoadData)list[dataAsInt];
					SetCustomStructData(item2, objectLoadData2.Target);
					continue;
				}
				TraceLogger.Write("HomesteadSaveLoadRescuePatch", string.Format("Skipped invalid custom-struct member while loading save: objectId={0} type='{1}' childStructIndex={2} childStructCount={3}.", objectLoadData.Id, objectLoadData.TypeDefinition?.Type?.FullName ?? "unknown", dataAsInt, list.Count));
			}
		}
	}

	private static bool TryReadMember(object memberValue, ObjectLoadData objectLoadData)
	{
		try
		{
			MethodInfo? method = memberValue.GetType().GetMethod("Read", BindingFlags.Instance | BindingFlags.Public);
			method?.Invoke(memberValue, null);
			return method != null;
		}
		catch (TargetInvocationException ex)
		{
			TraceLogger.Write("HomesteadSaveLoadRescuePatch", string.Format("Skipped unreadable member while loading save: objectId={0} type='{1}' error={2}", objectLoadData.Id, objectLoadData.TypeDefinition?.Type?.FullName ?? "unknown", ex.InnerException?.Message ?? ex.Message));
			return false;
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadSaveLoadRescuePatch", string.Format("Skipped unreadable member while loading save: objectId={0} type='{1}' error={2}", objectLoadData.Id, objectLoadData.TypeDefinition?.Type?.FullName ?? "unknown", ex2.Message));
			return false;
		}
	}

	private static bool IsCustomStructMember(object memberValue)
	{
		object obj = memberValue.GetType().GetProperty("SavedMemberType", BindingFlags.Instance | BindingFlags.Public)?.GetValue(memberValue);
		if (obj != null)
		{
			if (!(obj.ToString() == "CustomStruct"))
			{
				return Convert.ToInt32(obj) == 4;
			}
			return true;
		}
		return false;
	}

	private static int GetDataAsInt(object memberValue)
	{
		object obj = memberValue.GetType().GetProperty("Data", BindingFlags.Instance | BindingFlags.Public)?.GetValue(memberValue);
		if (obj is int)
		{
			return (int)obj;
		}
		return -1;
	}

	private static void SetCustomStructData(object memberValue, object? customStructObject)
	{
		memberValue.GetType().GetMethod("SetCustomStructData", BindingFlags.Instance | BindingFlags.Public)?.Invoke(memberValue, new object[1] { customStructObject });
	}
}
