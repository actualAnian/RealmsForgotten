using System;
using System.Diagnostics;
using System.Reflection;

namespace RealmsForgotten.SaveShield
{
    /// <summary>
    /// Caminha a stack de uma exceção até o primeiro frame que pertence a um mod, e não à
    /// engine.
    ///
    /// POR QUE ISSO É O VALOR TODO
    /// Uma falha de save estoura dentro de código TaleWorlds, então a exceção nomeia um método
    /// da engine e o jogador conclui que o jogo quebrou. O frame que importa é o primeiro
    /// abaixo dele que pertence ao assembly de algum mod. Nomear esse assembly transforma
    /// "salvar está quebrado" em "desative este mod" — algo que o jogador consegue agir.
    ///
    /// Portado do SaveShield do LOTRAOM (código MIT), prefixos ajustados ao nosso load order.
    /// O RealmsForgotten deliberadamente NÃO se exclui: se o culpado formos nós, o report tem
    /// que dizer isso em vez de culpar o próximo mod da lista.
    /// </summary>
    public static class ModCulpritAttributor
    {
        /// <summary>Prefixos de assembly que são engine/runtime, não mod.</summary>
        private static readonly string[] EnginePrefixes =
        {
            "TaleWorlds.", "SandBox", "StoryMode", "Native", "CustomBattle", "BirthAndDeath",
            "NavalDLC", "System", "mscorlib", "Newtonsoft.", "0Harmony", "netstandard",
        };

        /// <summary>
        /// Primeiro assembly não-engine na stack, ou null quando a falha é inteiramente da
        /// engine.
        /// </summary>
        public static string FindLikelyCulprit(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            try
            {
                StackTrace trace = new StackTrace(exception, fNeedFileInfo: false);
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    string name = SafeAssemblyName(trace.GetFrame(i)?.GetMethod());
                    if (name != null && !IsEngine(name))
                    {
                        return name;
                    }
                }
            }
            catch
            {
                // A atribuição é uma gentileza; nunca pode virar a falha.
            }

            return null;
        }

        private static string SafeAssemblyName(MethodBase method)
        {
            try { return method?.DeclaringType?.Assembly?.GetName()?.Name; }
            catch { return null; }
        }

        private static bool IsEngine(string assemblyName)
        {
            foreach (string prefix in EnginePrefixes)
            {
                if (assemblyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
