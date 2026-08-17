using System;
using System.Linq;
using System.Reflection;

namespace RF_CoopCompat
{
    /// <summary>
    /// Ponte por reflexao para as DLLs do Coop (Common.dll / GameInterface.dll).
    /// Nenhuma referencia em tempo de compilacao: se o Coop nao estiver
    /// instalado, tudo aqui resolve para null e o modulo fica dormente.
    /// </summary>
    internal static class CoopBridge
    {
        private static PropertyInfo? _isServerProperty;

        public static MethodInfo? PatchAllMethod { get; private set; }
        public static MethodInfo? UnpatchAllMethod { get; private set; }
        public static bool Resolved { get; private set; }

        /// <summary>Host autoritativo da sessao coop. So faz sentido com sessao ativa.</summary>
        public static bool IsServer
        {
            get
            {
                try
                {
                    return _isServerProperty != null && (bool)_isServerProperty.GetValue(null);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Tenta localizar os tipos do Coop nos assemblies ja carregados.
        /// Retorna true quando a ponte esta completa e o hook pode ser instalado.
        /// </summary>
        public static bool TryResolve()
        {
            if (Resolved) return true;

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var commonType = assemblies
                    .Where(a => a.GetName().Name == "Common")
                    .Select(a => a.GetType("Common.ModInformation"))
                    .FirstOrDefault(t => t != null);

                var gameInterfaceType = assemblies
                    .Where(a => a.GetName().Name == "GameInterface")
                    .Select(a => a.GetType("GameInterface.GameInterface"))
                    .FirstOrDefault(t => t != null);

                if (commonType == null || gameInterfaceType == null) return false;

                _isServerProperty = commonType.GetProperty("IsServer", BindingFlags.Public | BindingFlags.Static);
                PatchAllMethod = gameInterfaceType.GetMethod("PatchAll", BindingFlags.Public | BindingFlags.Instance);
                UnpatchAllMethod = gameInterfaceType.GetMethod("UnpatchAll", BindingFlags.Public | BindingFlags.Instance);

                if (_isServerProperty == null || PatchAllMethod == null)
                {
                    CompatLog.Info("coop assemblies found but expected members are missing; coop version incompatible?");
                    return false;
                }

                Resolved = true;
                CompatLog.Info("coop bridge resolved (Common.ModInformation + GameInterface.GameInterface)");
                return true;
            }
            catch (Exception ex)
            {
                CompatLog.Info($"coop bridge resolve failed: {ex.Message}");
                return false;
            }
        }
    }
}
