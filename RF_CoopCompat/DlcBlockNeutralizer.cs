using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RF_CoopCompat
{
    /// <summary>
    /// Neutraliza o bloqueio de DLC do Coop (ModuleValidator.ValidateNoDlc), que
    /// recusa a conexao quando qualquer DLC oficial (War Sails / NavalDLC) esta
    /// ativo — mesmo identico nos dois lados. O Realms Forgotten depende do War
    /// Sails (mapa/assentamentos/conteudo), entao sem isto host e cliente nunca
    /// conectam. Feito por reflexao (sem referencia de compilacao ao Coop) e
    /// autorizado pela permissao do mantenedor do Coop.
    ///
    /// IMPORTANTE: isto destrava apenas a CONEXAO com War Sails ativo. NAO
    /// sincroniza os sistemas navais em si — o Coop nunca foi construido com o
    /// War Sails ligado, entao movimento/batalha naval e uma fronteira separada
    /// (provavelmente precisa gating/sync proprio, como o resto do RF).
    /// </summary>
    internal static class DlcBlockNeutralizer
    {
        private static bool _done;

        public static void Install(Harmony harmony)
        {
            if (_done) return;

            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name == "GameInterface" || a.GetName().Name == "Coop.Core")
                .SelectMany(SafeGetTypes)
                .SelectMany(SafeGetMethods)
                .Where(m => m.Name == "ValidateNoDlc" && m.ReturnType == typeof(bool))
                .ToList();

            if (methods.Count == 0)
            {
                CompatLog.Info("dlc-block: ValidateNoDlc nao encontrado; versao do Coop diferente? conexao com War Sails vai falhar");
                return;
            }

            foreach (var m in methods)
            {
                try
                {
                    harmony.Patch(m, prefix: new HarmonyMethod(typeof(DlcBlockNeutralizer), nameof(ValidateNoDlcPrefix)));
                    CompatLog.Info($"dlc-block: neutralizado {m.DeclaringType?.FullName}.{m.Name}");
                    _done = true;
                }
                catch (Exception ex)
                {
                    CompatLog.Info($"dlc-block: falha ao patchear {m.DeclaringType?.FullName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Forca "sem DLC" -> conexao aceita mesmo com War Sails ativo. Nao
        /// declara o out 'error' (Harmony o inicializa como null); como
        /// __result=true, o chamador ignora o error. Assim o patch funciona
        /// mesmo se o nome do parametro mudar entre versoes do Coop.
        /// </summary>
        internal static bool ValidateNoDlcPrefix(ref bool __result)
        {
            __result = true;
            return false; // pula o original
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).Select(t => t!); }
        }

        private static IEnumerable<MethodInfo> SafeGetMethods(Type t)
        {
            try
            {
                return t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            }
            catch { return Enumerable.Empty<MethodInfo>(); }
        }
    }
}
