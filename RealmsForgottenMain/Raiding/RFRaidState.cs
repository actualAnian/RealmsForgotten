namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// Sinalizadores globais do sistema de saque a pe (porte do More Raiding,
    /// 2026-08-27 — ver RELATORIO_PORTE_MORERAIDING.md).
    ///
    /// Existem porque os patches de cena (que destravam bater em civil, trancam
    /// passagens e seguram o "sair da missao") precisam saber, sem custo, se o
    /// saque esta em curso. FORA do saque todos eles devolvem true no prefix e o
    /// jogo segue vanilla — essa e a regra de ouro deste sistema.
    /// </summary>
    internal static class RFRaidState
    {
        /// <summary>A cena atual aceita saque (o controller foi anexado).</summary>
        public static bool RaidSceneReady;

        /// <summary>
        /// O jogador escolheu "Storm the village" no menu de acao hostil: a cena
        /// abre com o saque JA declarado, sem precisar do primeiro golpe.
        ///
        /// Vive entre o menu de campanha e o nascimento da missao, por isso NAO
        /// entra em ResetSceneFlags() — quem a zera e quem a consome.
        /// </summary>
        public static bool ArmRaidOnEntry;

        public static bool ConsumeArmedEntry()
        {
            if (!ArmRaidOnEntry)
            {
                return false;
            }
            ArmRaidOnEntry = false;
            return true;
        }

        /// <summary>O primeiro golpe foi dado: o saque comecou de verdade.</summary>
        public static bool RaidInProgress;

        /// <summary>
        /// Ataque sob bandeira negra: as tropas do jogador entram de preto.
        /// Vive ate o fim da missao (e so cosmetico).
        /// </summary>
        public static bool BlackBannerActive;

        /// <summary>
        /// Bloqueio ONE-SHOT da hostilidade do encounter. O original zerava a
        /// flag a cada tick de campanha para limitar o estrago; aqui ela e
        /// CONSUMIDA na primeira chamada, que e determinista e nao depende de
        /// quando o proximo tick cai.
        /// </summary>
        public static bool SuppressNextHostility;

        public static bool ConsumeHostilitySuppression()
        {
            if (!SuppressNextHostility)
            {
                return false;
            }
            SuppressNextHostility = false;
            return true;
        }

        public static void ResetSceneFlags()
        {
            RaidSceneReady = false;
            RaidInProgress = false;
            BlackBannerActive = false;
        }
    }
}
