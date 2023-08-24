using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace RealmsForgotten.RFCustomSettlements
{
    internal class SceneBuildData
    {
        public static List<SceneBuildData> allSceneBuildDatas { get; private set; }
        internal static void BuildAll()
        {
            //throw new NotImplementedException();
        }

        internal static SceneBuildData ChooseLocation(Vec2 position2D)
        {
            return null;
        //    List<SceneBuildData> possibleLocations = new();
        //    foreach (SceneBuildData sceneBuildData in allSceneBuildDatas)
        //    {
        //        if(sceneBuildData.x)
        //    }
        //    if (possibleHerds.Count > 0)
        //        CurrentHerdBuildData = possibleHerds.GetRandomElement();
        //    else
        //        CurrentHerdBuildData = null;
        }
    }
}