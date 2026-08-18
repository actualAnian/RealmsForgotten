namespace RF_AliveScenes.Runtime;

/// <summary>Retrato da batalha, montado uma vez quando a missao comeca.</summary>
public sealed class BattleSetup
{
    public bool PlayerSideOverpowered;
    public bool PlayerSideUnderpowered;
    public bool IsSiege;
    public bool AgainstVillagers;
    public bool AgainstLooters;
    public bool AtSea;
    public string AttackerFactionName = string.Empty;
    public string DefenderFactionName = string.Empty;
}
