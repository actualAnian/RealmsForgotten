namespace RF_AliveScenes.Data;

/// <summary>
/// Tipo de figurante que fala. Define quais falas do banco servem para o agente.
/// </summary>
public enum ActorType
{
    GENERIC,
    KID_TOWN,
    TEEN_TOWN,
    ADULT_TOWN,
    ADULT_VILLAGE,
    TEEN_VILLAGE,
    KID_VILLAGE,
    GUARD,
    BLACKSMITH,
    ARMORER,
    WEAPONSMITH,
    TRADER,
    BEGGAR,
    THUG,
    GANG_LEADER,
    NOTABLE_CITY,
    NOTABLE_VILLAGE,
    TAVERN_OWNER,
    MUSICIAN,
    BARBER,
    BATTLE_CAVALRY,
    BATTLE_INFANTRY,
    BATTLE_RANGED,
    SEA_ROW,
    SEA_STANDING,
    NOBLE,
    COMPANION,
    /// <summary>Novo no RF: mestre de estaleiro / gente do porto (Occupation.ShipWright).</summary>
    SHIPWRIGHT
}

/// <summary>
/// Pre-requisito de contexto para uma fala. Todas as condicoes de uma fala precisam
/// ser verdadeiras ao mesmo tempo.
/// </summary>
public enum SpeechCondition
{
    /// <summary>Sem restricao. Tambem e o fallback de qualquer valor desconhecido no XML.</summary>
    ANY,
    SEASON_WINTER,
    SEASON_SUMMER,
    SEASON_AUTUMN,
    SEASON_SPRING,
    PLAYER_HAVE_SWORD,
    PLAYER_HAVE_EXPENSIVE_ARMOR,
    PLAYER_HAVE_EXPENSIVE_SWORD,
    PLAYER_HAVE_BOW,
    PLAYER_HAVE_HORSE,
    POOR_SETTLEMENT,
    RICH_SETTLEMENT,
    STAND,
    FARMING,
    IN_TAVERN,
    IN_KEEP,
    /// <summary>Novo no RF: cena de porto (War Sails).</summary>
    IN_PORT,
    OVERPOWERED,
    UNDERPOWERED,
    SIEGE_DEFENDER,
    SIEGE_ATTACKER,
    AGAINST_LOOTERS,
    AGAINST_VILLAGERS,
    DURING_COMBAT,
    AT_SEA_WINDY,
    AT_SEA_RAINY,
    AT_SEA_ANY
}

/// <summary>UNIQUE some do banco depois de dita uma vez na missao; GENERIC pode repetir.</summary>
public enum SpeechFrequency
{
    GENERIC,
    UNIQUE
}
