using TaleWorlds.Library;

namespace RF_DualWield
{
    /// <summary>
    /// Log unico do modulo. Escreve no rgl_log do jogo (nao na tela).
    /// Proibido usar InformationManager.DisplayMessage aqui: nada de debug na cara do jogador.
    /// </summary>
    internal static class DualWieldLog
    {
        private const string Prefix = "[RF_DualWield] ";

        internal static void Info(string message)
        {
            Debug.Print(Prefix + message);
        }

        internal static void Warn(string message)
        {
            Debug.Print(Prefix + "AVISO: " + message);
        }
    }
}
