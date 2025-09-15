using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RealmsForgotten.Patches.CameraPositionWhenViewingCharacters
{
    internal static class PatchHelper
    {

        static PatchHelper() { }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            codes.Clear();
            return codes;
        }

        public static TReturn GetFieldValue<TOwner, TReturn>(TOwner owner, string name)
            where TOwner : class
        {
            return (TReturn)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).GetValue(owner);
        }

        public static void SetFieldValue<TOwner>(TOwner owner, string name, dynamic value)
            where TOwner : class
        {
            owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).SetValue(owner, value);
        }
        public static void SetPropertyValue(object owner, string name, dynamic value)
        {
            owner.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).SetValue(owner, value);
        }

        public static object CallPrivateMethod<TType>(TType owner, string name, object[] values)
            where TType : class
        {
            try
            {
                return owner.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(owner, values);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {typeof(TType).Name}: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}
