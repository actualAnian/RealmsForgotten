using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Patches
{
    public static class AgentVisualsDataMonsterFix
    {
        public static int TryPatch(Harmony harmony)
        {
            int count = 0;

            var avdType = typeof(AgentVisualsData);
            var methods = avdType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var m in methods)
            {
                if (m.ReturnType != avdType) continue;

                var ps = m.GetParameters();
                if (ps.Length != 1) continue;

                var p0 = ps[0].ParameterType;
                var n = p0.FullName ?? p0.Name;

                if (!n.EndsWith("CharacterObject") && !n.EndsWith("BasicCharacterObject")) continue;

                try
                {
                    harmony.Patch(m, postfix: new HarmonyMethod(
                        typeof(AgentVisualsDataMonsterFix).GetMethod(nameof(Postfix),
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ));
                    count++;
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"[RF] AgentVisualsDataMonsterFix: failed to patch {m.Name}: {ex.Message}");
                }
            }

            return count;
        }

        private static void Postfix(object[] __args, ref AgentVisualsData __result)
        {
            try
            {
                if (__args == null || __args.Length < 1) return;
                if (__result == null) return;

                var character = __args[0];
                if (character == null) return;

                var monster = TryGetMonster(character);
                if (monster == null) return;

                __result = ApplyMonster(__result, monster);
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[RF] AgentVisualsDataMonsterFix: postfix failed, agent keeps default monster: {ex.Message}");
            }
        }

        private static Monster TryGetMonster(object character)
        {
            var t = character.GetType();

            var p = t.GetProperty("Monster", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && typeof(Monster).IsAssignableFrom(p.PropertyType))
                return p.GetValue(character) as Monster;

            var gm = t.GetMethod("get_Monster", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (gm != null && typeof(Monster).IsAssignableFrom(gm.ReturnType))
                return gm.Invoke(character, null) as Monster;

            var m = t.GetMethod("GetMonster", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m != null && typeof(Monster).IsAssignableFrom(m.ReturnType))
                return m.Invoke(character, null) as Monster;

            return null;
        }

        private static AgentVisualsData ApplyMonster(AgentVisualsData avd, Monster monster)
        {
            var mm = typeof(AgentVisualsData).GetMethod("Monster",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Monster) }, null);

            if (mm != null)
                return (AgentVisualsData)mm.Invoke(avd, new object[] { monster });

            var pm = typeof(AgentVisualsData).GetProperty("Monster",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (pm != null && pm.CanWrite && typeof(Monster).IsAssignableFrom(pm.PropertyType))
                pm.SetValue(avd, monster);

            return avd;
        }
    }
}
