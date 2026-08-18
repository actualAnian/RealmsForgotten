namespace RF_AliveScenes.Config;

/// <summary>
/// Fachada de configuracao usada pelo resto do modulo. Le do MCM
/// (<see cref="Settings"/>) quando ele existe e cai nos defaults embutidos quando nao —
/// assim o modulo continua funcionando se o MCM estiver ausente ou quebrado, sem
/// espalhar try/catch por todo lado.
///
/// O mod original guardava isso num XML proprio; aqui a fonte da verdade e o MCM, como
/// no RF_IsoCam e no RF_PartyVisuals. Os valores sao lidos na hora, entao mexer no menu
/// vale na proxima cena (ou na hora, no caso das chances e do tempo de leitura).
/// </summary>
public sealed class AliveScenesSettings
{
    public static AliveScenesSettings Instance { get; } = new AliveScenesSettings();

    private AliveScenesSettings()
    {
    }

    private static Settings Mcm
    {
        get
        {
            try
            {
                return Settings.Instance;
            }
            catch
            {
                return null;
            }
        }
    }

    // Geral
    public bool Enabled => Mcm?.Enabled ?? true;
    public double WaitTimeMultiplier => Mcm?.WaitTimeMultiplier ?? 0.13;
    public float VisibleDistance => Mcm?.VisibleDistance ?? 10f;
    public bool ProfanityFilterEnabled => Mcm?.ProfanityFilterEnabled ?? false;

    // Cena pacifica
    public bool CasualChatEnabled => Mcm?.CasualChatEnabled ?? true;
    public int CasualCooldown => Mcm?.CasualCooldown ?? 40;
    public int CasualMaxConversations => Mcm?.CasualMaxConversations ?? 5;
    public int CasualChancePerPerson => Mcm?.CasualChancePerPerson ?? 55;
    public bool TavernChatEnabled => Mcm?.TavernChatEnabled ?? true;
    public int TavernCooldown => Mcm?.TavernCooldown ?? 15;

    // Batalha
    public bool BattleChatEnabled => Mcm?.BattleChatEnabled ?? true;
    public int BattleCooldown => Mcm?.BattleCooldown ?? 7;
    public int BattleMaxConversations => Mcm?.BattleMaxConversations ?? 3;
    public int BattleChancePerPerson => Mcm?.BattleChancePerPerson ?? 55;
    public bool BattleAllowDuringCombat => Mcm?.BattleAllowDuringCombat ?? true;
    public bool BattleEnemyChatEnabled => Mcm?.BattleEnemyChatEnabled ?? true;

    // Multidao
    public bool CrowdsEnabled => Mcm?.CrowdsEnabled ?? true;
    public int CrowdMultiplicationMin
    {
        get
        {
            int min = Mcm?.CrowdMultiplicationMin ?? 1;
            return min < 0 ? 0 : min;
        }
    }

    public int CrowdMultiplicationMax
    {
        get
        {
            int max = Mcm?.CrowdMultiplicationMax ?? 2;
            int min = CrowdMultiplicationMin;
            return max < min ? min : max;
        }
    }

    public int CrowdMaxExtraAgents => Mcm?.CrowdMaxExtraAgents ?? 40;
}
