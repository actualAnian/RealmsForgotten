using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.RFCustomSettlements
{
    static class Helper
    {
        public enum RFUsableObjectType
        {
            Pickable,
            Passage,
            Healing
        }
        private static readonly Dictionary<string, RFUsableObjectType> RFObjectEnum = new()
        {
            {"pickable", RFUsableObjectType.Pickable},
            {"passage", RFUsableObjectType.Passage},
            {"healing", RFUsableObjectType.Healing}
        };

        public static float maxPickableDistance = 10f;
        public static RFUsableObjectType? ChooseObjectType(string objectName)
        {
            string objectType = objectName.Split('_')[1];
            if (RFObjectEnum.ContainsKey(objectType)) { return RFObjectEnum[objectType]; }
            else return null;
        }
        public static string GetRFPickableObjectName(string[] data)
        {
            StringBuilder itemIdBuilder = new();
            foreach (string str in data.Skip(2).Take(data.Length - 3))
                itemIdBuilder.Append(str + "_");
            itemIdBuilder.Remove(itemIdBuilder.Length - 1, 1);
            return itemIdBuilder.ToString();
        }
    }
}
