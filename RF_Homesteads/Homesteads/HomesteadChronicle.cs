using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace Homesteads;

/// <summary>
/// Bridge into the RF_AIDialog World Chronicle (the F8 world-history popup).
/// RF_Homesteads has no compile-time reference to RF_AIDialog, but both DLLs
/// load under the RealmsForgotten module, so the store is reached by reflection:
/// RF_AIDialog.WorldHistoryStore.Instance.AddEvent(string, int). Every call is
/// fully guarded — when the chronicle module is absent this is a silent no-op.
/// </summary>
internal static class HomesteadChronicle
{
	private static bool _resolved;

	private static PropertyInfo? _instanceProperty;

	private static MethodInfo? _addEventMethod;

	public static void Record(string description)
	{
		try
		{
			if (Campaign.Current == null || string.IsNullOrWhiteSpace(description))
			{
				return;
			}
			if (!_resolved)
			{
				_resolved = true;
				Type? type = Type.GetType("RF_AIDialog.WorldHistoryStore, RF_AIDialog");
				if (type != null)
				{
					_instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
					_addEventMethod = type.GetMethod("AddEvent", new Type[2]
					{
						typeof(string),
						typeof(int)
					});
				}
				TraceLogger.Write("HomesteadChronicle", (type != null && _addEventMethod != null) ? "World Chronicle bridge resolved." : "World Chronicle not available — homestead events will not be chronicled.");
			}
			object? store = _instanceProperty?.GetValue(null);
			if (store != null && _addEventMethod != null)
			{
				int day;
				try
				{
					day = (int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow;
				}
				catch
				{
					day = 0;
				}
				_addEventMethod.Invoke(store, new object[2] { description, day });
			}
		}
		catch (Exception ex)
		{
			TraceLogger.WriteOnce("ChronicleFail", "HomesteadChronicle", "Record failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}
