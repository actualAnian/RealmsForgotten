using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Library;

namespace RealmsForgotten
{
    [HarmonyPatch(typeof(DefaultMapWeatherModel), "GetWeatherEventInPosition")]
    public class DefaultMapWeatherModel_GetWeatherEventInPosition_Patch
    {
        private unsafe static bool Prefix(Vec2 pos, ref MapWeatherModel.WeatherEvent __result, DefaultMapWeatherModel __instance)
        {
            if (Campaign.Current.DefaultWeatherNodeDimension == 0)
            {
                Campaign.Current.DefaultWeatherNodeDimension = 32;
            }
            MethodInfo getNodePositionMethod = AccessTools.Method(typeof(DefaultMapWeatherModel), "GetNodePositionForWeather", null, null);
            object[] array = new object[3];
            array[0] = pos;
            object[] methodArgs = array;
            getNodePositionMethod.Invoke(__instance, methodArgs);
            int num = (int)methodArgs[1];
            int num2 = (int)methodArgs[2];
            int index = num2 * 32 + num;
            MapWeatherModel.WeatherEvent[] weatherDataCache = DefaultMapWeatherModel_GetWeatherEventInPosition_Patch._weatherDataCacheRef.Invoke(__instance);
            if (index >= weatherDataCache.Length || index < 0)
                __result = MapWeatherModel.WeatherEvent.Clear;
            else
                __result = weatherDataCache[index];
            return false;
        }
        private static readonly AccessTools.FieldRef<DefaultMapWeatherModel, MapWeatherModel.WeatherEvent[]> _weatherDataCacheRef = AccessTools.FieldRefAccess<DefaultMapWeatherModel, MapWeatherModel.WeatherEvent[]>("_weatherDataCache");
    }
}
