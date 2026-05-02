//using HarmonyLib;
//using SandBox;
//using System;
//using System.Reflection;
//using System.Runtime.CompilerServices;
//using TaleWorlds.CampaignSystem.ComponentInterfaces;
//using TaleWorlds.CampaignSystem.GameComponents;

//namespace LT_EE1259_Core.Patches
//{
//    [HarmonyPatch(typeof(MapScene), "GetRainAmountAtPosition")]
//    public static class GetRainAmountAtPositionPatch
//    {
//        [HarmonyPrefix]
//        public static bool Prefix(MapScene __instance, ref float __result)
//        {
//            __result = 0f;
//            return false;
//        }
//    }

//    [HarmonyPatch(typeof(MapScene), "GetSnowAmountAtPosition")]
//    public static class GetSnowAmountAtPositionPatch
//    {
//        [HarmonyPrefix]
//        public static bool Prefix(MapScene __instance, ref float __result)
//        {
//            int width = (int)GetSnowAmountAtPositionPatch._snowAndRainDataTextureWidthField.GetValue(__instance);
//            int height = (int)GetSnowAmountAtPositionPatch._snowAndRainDataTextureHeightField.GetValue(__instance);
//            bool flag = width == 0;
//            if (flag)
//            {
//                GetSnowAmountAtPositionPatch._snowAndRainDataTextureWidthField.SetValue(__instance, 1024);
//            }
//            bool flag2 = height == 0;
//            if (flag2)
//            {
//                GetSnowAmountAtPositionPatch._snowAndRainDataTextureHeightField.SetValue(__instance, 1024);
//            }
//            __result = 0f;
//            return false;
//        }
//        private static FieldInfo _snowAndRainDataTextureWidthField = AccessTools.Field(typeof(MapScene), "_snowAndRainDataTextureWidth");
//        private static FieldInfo _snowAndRainDataTextureHeightField = AccessTools.Field(typeof(MapScene), "_snowAndRainDataTextureHeight");
//    }
//    [HarmonyPatch(typeof(DefaultMapWeatherModel), "SetIsRainingOrWetFromFunction")]
//    public static class SetIsRainingOrWetFromFunctionPatch
//    {
//        [HarmonyPrefix]
//        public static bool Prefix(ref MapWeatherModel.WeatherEvent __result)
//        {
//            __result = MapWeatherModel.WeatherEvent.Clear;
//            return false;
//        }
//    }
//}