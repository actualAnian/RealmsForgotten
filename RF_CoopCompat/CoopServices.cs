using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace RF_CoopCompat
{
    /// <summary>
    /// Resolve os servicos internos do Coop pelo container DI, no padrao do
    /// Hex Server Pack: polling de GameInterface.ContainerProvider.TryResolve<T>
    /// a cada tick ate a sessao subir. Da acesso a IMessageBroker / INetwork /
    /// ISerializableTypeMapper, que sao a base para SINCRONIZAR estado do RF
    /// (registrar mensagens proprias e publicar servidor<->cliente).
    ///
    /// Tudo por reflexao: sem referencia de compilacao ao Coop. A presenca do
    /// container tambem serve de deteccao de sessao (o Coop so constroi o
    /// container em StartAsServer/StartAsClient e o descarta ao encerrar).
    /// Ver ESTUDO_HEX_SERVERPACK.md.
    /// </summary>
    internal static class CoopServices
    {
        private static Type? _containerProviderType;
        private static MethodInfo? _tryResolveDef;
        private static Type? _brokerType;
        private static Type? _networkType;
        private static Type? _mapperType;
        private static bool _reflectionReady;
        private static int _prepThrottle;

        public static bool Ready { get; private set; }
        public static object? MessageBroker { get; private set; }
        public static object? Network { get; private set; }
        public static object? TypeMapper { get; private set; }

        /// <summary>Chamar a cada frame. Detecta subida/descida da sessao coop.</summary>
        public static void Poll(Harmony harmony, CompatConfig config)
        {
            if (!_reflectionReady)
            {
                if (--_prepThrottle > 0) return;
                _prepThrottle = 60; // tenta preparar ~1x/seg ate o Coop carregar
                if (!PrepareReflection()) return;
            }

            // resolver o broker serve de sonda: se resolve, o container esta de pe
            var broker = Resolve(_brokerType);
            bool containerUp = broker != null;

            if (containerUp && !Ready)
            {
                MessageBroker = broker;
                Network = Resolve(_networkType);
                TypeMapper = Resolve(_mapperType);
                Ready = true;
                CompatLog.Info($"coop services resolved (broker={MessageBroker != null}, " +
                               $"network={Network != null}, mapper={TypeMapper != null})");
                CoopSessionState.BeginSession(CoopBridge.IsServer, harmony, config);
            }
            else if (!containerUp && Ready)
            {
                Ready = false;
                MessageBroker = Network = TypeMapper = null;
                CoopSessionState.EndSession();
            }
        }

        /// <summary>
        /// Registra tipos de mensagem proprios na rede do Coop
        /// (ISerializableTypeMapper.AddTypes). Base para o sync por sistema.
        /// </summary>
        public static bool RegisterMessageTypes(IEnumerable<Type> types)
        {
            if (TypeMapper == null) return false;
            try
            {
                var add = TypeMapper.GetType().GetMethod("AddTypes", new[] { typeof(IEnumerable<Type>) });
                if (add == null)
                {
                    CompatLog.Info("coop services: AddTypes nao encontrado no mapper");
                    return false;
                }
                add.Invoke(TypeMapper, new object[] { types });
                return true;
            }
            catch (Exception ex)
            {
                CompatLog.Info($"coop services: RegisterMessageTypes falhou: {ex.Message}");
                return false;
            }
        }

        private static bool PrepareReflection()
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();

                _containerProviderType = asms
                    .Where(a => a.GetName().Name == "GameInterface")
                    .Select(a => a.GetType("GameInterface.ContainerProvider"))
                    .FirstOrDefault(t => t != null)
                    ?? FindType(asms, "GameInterface", "ContainerProvider");
                if (_containerProviderType == null) return false;

                _tryResolveDef = _containerProviderType
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "TryResolve" &&
                                         m.IsGenericMethodDefinition &&
                                         m.GetParameters().Length == 1);
                if (_tryResolveDef == null) return false;

                _brokerType = FindInterface(asms, "IMessageBroker");
                _networkType = FindInterface(asms, "INetwork");
                _mapperType = FindInterface(asms, "ISerializableTypeMapper");
                if (_brokerType == null) return false;

                _reflectionReady = true;
                CompatLog.Info("coop services: reflexao preparada (ContainerProvider + interfaces)");
                return true;
            }
            catch (Exception ex)
            {
                CompatLog.Info($"coop services: prep de reflexao falhou: {ex.Message}");
                return false;
            }
        }

        private static object? Resolve(Type? ifaceType)
        {
            if (ifaceType == null || _tryResolveDef == null) return null;
            try
            {
                var m = _tryResolveDef.MakeGenericMethod(ifaceType);
                var args = new object?[] { null };
                bool ok = (bool)m.Invoke(null, args)!;
                return ok ? args[0] : null;
            }
            catch
            {
                return null;
            }
        }

        private static Type? FindType(Assembly[] asms, string assemblyName, string simpleName)
        {
            return asms.Where(a => a.GetName().Name == assemblyName)
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.Name == simpleName);
        }

        private static Type? FindInterface(Assembly[] asms, string simpleName)
        {
            return asms.Where(a => a.GetName().Name == "Common")
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.IsInterface && t.Name == simpleName);
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null).Select(t => t!); }
        }
    }
}
