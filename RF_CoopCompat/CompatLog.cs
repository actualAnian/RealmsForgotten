using System;
using System.IO;

namespace RF_CoopCompat
{
    /// <summary>
    /// Logger de arquivo minimalista. Escreve em Modules/RF_CoopCompat/rf_coop_compat.log,
    /// truncado a cada inicializacao do jogo.
    /// </summary>
    internal static class CompatLog
    {
        private static readonly object Sync = new object();
        private static string? _path;

        public static void Initialize(string moduleRoot)
        {
            try
            {
                _path = Path.Combine(moduleRoot, "rf_coop_compat.log");
                File.WriteAllText(_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] RF_CoopCompat log start{Environment.NewLine}");
            }
            catch
            {
                _path = null;
            }
        }

        public static void Info(string message)
        {
            if (_path == null) return;
            lock (Sync)
            {
                try
                {
                    File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                }
                catch
                {
                    // logging nunca pode derrubar o jogo
                }
            }
        }
    }
}
