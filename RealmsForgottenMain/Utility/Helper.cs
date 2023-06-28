using System;
using System.Xml;
using HarmonyLib;
using TaleWorlds.Core;

namespace RealmsForgotten.Utility.Utility
{
    public static class Helper
    {

        internal static BodyProperties GenerateCultureBodyProperties(string culture)
        {
            Tuple<string, string, string, string> fighterMin = SubModule.fighterMin[culture];
            Tuple<string, string, string, string> fighterMax = SubModule.fighterMax[culture];
            XmlDocument bodyPropertiesDocument = new XmlDocument();
            XmlNode bodyPropertyNode = bodyPropertiesDocument.CreateNode(XmlNodeType.Element, "BodyProperty", null);
            bodyPropertiesDocument.AppendChild(bodyPropertyNode);
            XmlAttribute keyAttribute = bodyPropertiesDocument.CreateAttribute("key", null);
            keyAttribute.Value = fighterMin.Item4;
            bodyPropertyNode.Attributes.Append(keyAttribute);
            BodyProperties bodyProperties;
            BodyProperties.FromXmlNode(bodyPropertyNode, out bodyProperties);
            ulong value = Traverse.Create(bodyProperties).Property<ulong>("KeyPart1", null).Value;
            ulong value2 = Traverse.Create(bodyProperties).Property<ulong>("KeyPart2", null).Value;
            ulong value3 = Traverse.Create(bodyProperties).Property<ulong>("KeyPart3", null).Value;
            ulong value4 = Traverse.Create(bodyProperties).Property<ulong>("KeyPart4", null).Value;
            ulong value5 = Traverse.Create(bodyProperties).Property<ulong>("KeyPart5", null).Value;
            ulong value6 = Traverse.Create(bodyProperties).Property<ulong>("KeyPart8", null).Value;
            ulong num = value << 32;
            ulong num2 = value2 << 32;
            ulong num3 = value3 << 32;
            ulong num4 = value4 << 32;
            ulong num5 = value5 << 32;
            ulong num6 = value6 << 32;
            keyAttribute.Value = fighterMax.Item4;
            StaticBodyProperties staticBodyProperties;
            StaticBodyProperties.FromXmlNode(bodyPropertyNode, out staticBodyProperties);
            ulong value7 = Traverse.Create(staticBodyProperties).Property<ulong>("KeyPart1", null).Value;
            ulong value8 = Traverse.Create(staticBodyProperties).Property<ulong>("KeyPart2", null).Value;
            ulong value9 = Traverse.Create(staticBodyProperties).Property<ulong>("KeyPart3", null).Value;
            ulong value10 = Traverse.Create(staticBodyProperties).Property<ulong>("KeyPart4", null).Value;
            ulong value11 = Traverse.Create(staticBodyProperties).Property<ulong>("KeyPart5", null).Value;
            ulong value12 = Traverse.Create(staticBodyProperties).Property<ulong>("KeyPart8", null).Value;
            ulong max = value7 << 32;
            ulong max2 = value8 << 32;
            ulong max3 = value9 << 32;
            ulong max4 = value10 << 32;
            ulong max5 = value11 << 32;
            ulong max6 = value12 << 32;
            ulong num7 = (ulong)SubModule.random.NextLong((long)num, (long)max);
            ulong num8 = (ulong)SubModule.random.NextLong((long)num2, (long)max2);
            ulong num9 = (ulong)SubModule.random.NextLong((long)num3, (long)max3);
            ulong num10 = (ulong)SubModule.random.NextLong((long)num4, (long)max4);
            ulong num11 = (ulong)SubModule.random.NextLong((long)num5, (long)max5);
            ulong num12 = (ulong)SubModule.random.NextLong((long)num6, (long)max6);
            ulong num13 = num7 << 32;
            ulong num14 = num8 << 32;
            ulong num15 = num9 << 32;
            ulong num16 = num10 << 32;
            ulong num17 = num11 << 32;
            ulong num18 = (ulong)SubModule.random.NextLong(long.MinValue, long.MaxValue);
            ulong num19 = (ulong)SubModule.random.NextLong(long.MinValue, long.MaxValue);
            ulong num20 = num12 << 32;
            StaticBodyProperties staticBodyProperties2 = new StaticBodyProperties(num, num2, num3, num4, num5, 0UL, 0UL, num6);
            float age = MBRandom.RandomFloatRanged(Convert.ToSingle(fighterMin.Item1), Convert.ToSingle(fighterMax.Item1));
            float build = MBRandom.RandomFloatRanged(Convert.ToSingle(fighterMin.Item2), Convert.ToSingle(fighterMax.Item2));
            float weight = MBRandom.RandomFloatRanged(Convert.ToSingle(fighterMin.Item3), Convert.ToSingle(fighterMax.Item3));
            return new BodyProperties(new DynamicBodyProperties(age, weight, build), staticBodyProperties2);
        }

        public static long NextLong(this Random random, long min, long max)
        {

            ulong uRange = (ulong)(max - min);
            ulong ulongRand;
            do
            {
                byte[] buf = new byte[8];
                random.NextBytes(buf);
                ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
            } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

            return (long)(ulongRand % uRange) + min;
        }
    }
}
