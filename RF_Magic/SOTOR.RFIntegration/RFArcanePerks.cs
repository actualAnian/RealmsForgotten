using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace SOTOR.RFIntegration;

/// <summary>
/// FASE 5c — os 6 perks de Arcane do RealmsForgotten passam a agir sobre o motor
/// do SOTOR (decisao do autor: opcao 1, "repontar", em vez de aposentar).
///
/// Eles existiam para o motor legado: Talisman multiplicava o dano de Cartridge e
/// Staff o splash do wand. Com o legado desligado ficariam visiveis, compraveis e
/// sem efeito — perk morto na tela. Aqui recebem os dois eixos equivalentes no
/// SOTOR:
///
///   Talisman -> DANO do feitico
///   Staff    -> RAIO de area
///
/// O raio e o unico eixo que o SOTOR NAO cobre com perks proprios, e por isso o
/// par Talisman/Staff continua sendo uma decisao de design que so o RF tem.
///
/// Resolucao por StringId, como em <see cref="RFArcaneSkill" />: assim o RF_Magic
/// nao passa a depender do assembly RealmsForgotten.dll e continua jogavel
/// sozinho (sem o RF, nenhum perk resolve e os fatores ficam neutros).
///
/// Os perks sao ALTERNATIVAS aos pares (nivel 50/100/150, Talisman OU Staff), logo
/// um heroi tem no maximo um por nivel — pode ter Talisman no 50 e Staff no 100.
/// Por isso cada eixo pega o MAIOR tier possuido do seu proprio lado.
/// </summary>
public static class RFArcanePerks
{
    // StringIds registrados em RealmsForgotten.CustomSkills.RFPerks.
    private const string NeophytesTalisman = "NeophytesTalisman";
    private const string InitiatesTalisman = "InitiatesTalisman";
    private const string HierophantsTalisman = "HierophantsTalisman";
    private const string NeophytesStaff = "NeophytesStaff";
    private const string InitiatesStaff = "InitiatesStaff";
    private const string HierophantsStaff = "HierophantsStaff";

    private static bool _logged;

    /// <summary>
    /// Multiplicador de DANO vindo do Talisman de maior tier que o heroi possui.
    /// 1.0 se nenhum (ou sem RealmsForgotten).
    ///
    /// Ordem do maior para o menor de proposito. O consumidor legado testava
    /// Neophytes PRIMEIRO, o que dava o multiplicador MENOR (0.9) a quem tambem
    /// tivesse o de tier alto — inversao que nao faz sentido numa progressao.
    /// </summary>
    public static float GetDamageFactor(Hero hero)
    {
        return Highest(hero, HierophantsTalisman, InitiatesTalisman, NeophytesTalisman);
    }

    /// <summary>
    /// Multiplicador de RAIO de area vindo do Staff de maior tier possuido.
    /// 1.0 se nenhum.
    /// </summary>
    public static float GetRadiusFactor(Hero hero)
    {
        return Highest(hero, HierophantsStaff, InitiatesStaff, NeophytesStaff);
    }

    /// <summary>Primeiro perk possuido na ordem dada; devolve o PrimaryBonus dele.</summary>
    private static float Highest(Hero hero, params string[] idsHighestFirst)
    {
        if (hero == null)
        {
            return 1f;
        }

        try
        {
            for (int i = 0; i < idsHighestFirst.Length; i++)
            {
                PerkObject perk = Resolve(idsHighestFirst[i]);
                if (perk != null && hero.GetPerkValue(perk))
                {
                    float bonus = perk.PrimaryBonus;
                    // PrimaryBonus 0 significa "perk sem valor configurado": tratar
                    // como neutro em vez de zerar dano/raio.
                    return (bonus > 0f) ? bonus : 1f;
                }
            }
        }
        catch (Exception ex)
        {
            SotorLog.Warn("RFArcanePerks: leitura falhou (" + ex.GetType().Name + ") — fator neutro.");
        }

        return 1f;
    }

    private static PerkObject Resolve(string id)
    {
        PerkObject perk = MBObjectManager.Instance?.GetObject<PerkObject>(id);
        if (perk != null && !_logged)
        {
            _logged = true;
            SotorLog.Info("RFArcanePerks: perks de Arcane do RF encontrados — Talisman age no dano, Staff no raio.");
        }
        return perk;
    }
}
