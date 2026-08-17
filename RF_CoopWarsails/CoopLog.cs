using System;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RF_CoopWarsails
{
    /// <summary>
    /// Log de arquivo + mensagens na tela. Arquivo em
    /// Modules/RF_CoopWarsails/rf_coop_warsails.log, truncado a cada boot.
    /// </summary>
    internal static class CoopLog
    {
        private static readonly object Sync = new object();
        private static string? _path;

        public static void Initialize(string moduleRoot)
        {
            try
            {
                _path = Path.Combine(moduleRoot, "rf_coop_warsails.log");
                File.WriteAllText(_path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] RF_CoopWarsails log start{Environment.NewLine}");
            }
            catch
            {
                _path = null;
            }
        }

        public static void File_(string message)
        {
            if (_path == null) return;
            lock (Sync)
            {
                try { System.IO.File.AppendAllText(_path, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}"); }
                catch { /* log nunca derruba o jogo */ }
            }
        }

        /// <summary>Mensagem no canto da tela + no arquivo.</summary>
        public static void Screen(string message, uint color = 0xFF00FF00)
        {
            File_(message);
            try { InformationManager.DisplayMessage(new InformationMessage("[RF Coop] " + message, Color.FromUint(color))); }
            catch { /* fora de um contexto com UI */ }
        }
    }
}
