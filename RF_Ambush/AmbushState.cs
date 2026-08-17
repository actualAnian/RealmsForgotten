using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace RF_Ambush;

/// <summary>De que lado e a emboscada armada.</summary>
public enum AmbushSide
{
    PlayerAmbushes,
    EnemyAmbushes,
}

/// <summary>
/// Estado transiente do sistema. NADA aqui persiste no save de proposito:
/// carregar um jogo desfaz stance e gatilhos, que e o comportamento seguro
/// (nenhum saveBaseId novo, nenhum risco de corromper save — regra do projeto).
/// O unico dado que sobrevive e o cooldown, persistido pelo proprio behavior.
/// </summary>
public static class AmbushState
{
    // ---------- stance do jogador (fase 1) ----------

    public static bool StanceActive { get; private set; }

    /// <summary>Quando a stance foi armada (para ocultacao progressiva e moral).</summary>
    public static CampaignTime StanceSince { get; private set; }

    /// <summary>Presa atual dentro do alcance de bote, se houver.</summary>
    public static MobileParty? PounceTarget;

    public static void EnterStance()
    {
        StanceActive = true;
        StanceSince = CampaignTime.Now;
        PounceTarget = null;
    }

    public static void ExitStance()
    {
        StanceActive = false;
        PounceTarget = null;
    }

    // ---------- gatilho armado para a proxima missao ----------

    private static AmbushSide? _armedSide;
    private static string? _armedAgainstPartyId;
    private static CampaignTime _armedAt;

    /// <summary>
    /// Arma a proxima batalha como emboscada CONTRA a party dada. O id do alvo
    /// evita o gatilho vazar para outra batalha (ex.: jogador clicou "Leave" no
    /// encounter e atacou outra party depois).
    /// </summary>
    public static void Arm(AmbushSide side, MobileParty against)
    {
        _armedSide = side;
        _armedAgainstPartyId = against?.StringId;
        _armedAt = CampaignTime.Now;
    }

    public static void Disarm()
    {
        _armedSide = null;
        _armedAgainstPartyId = null;
    }

    /// <summary>
    /// Consome o gatilho se ele vale para a party inimiga desta batalha.
    /// Expira sozinho (ArmedExpiryHours) para nunca contaminar batalha errada.
    /// </summary>
    public static bool TryConsume(MobileParty? enemyParty, out AmbushSide side)
    {
        side = AmbushSide.PlayerAmbushes;
        if (!_armedSide.HasValue)
        {
            return false;
        }
        if (_armedAt.ElapsedHoursUntilNow > AmbushConfig.ArmedExpiryHours)
        {
            Disarm();
            return false;
        }
        if (_armedAgainstPartyId != null && enemyParty?.StringId != _armedAgainstPartyId)
        {
            return false;
        }
        side = _armedSide.Value;
        Disarm();
        return true;
    }

    /// <summary>Ha gatilho armado (sem consumir)? Usado só para diagnostico.</summary>
    public static bool IsArmed => _armedSide.HasValue;

    /// <summary>Reset total — chamado ao carregar/criar jogo.</summary>
    public static void ResetAll()
    {
        StanceActive = false;
        PounceTarget = null;
        Disarm();
    }
}
