using System;
using System.Collections.Generic;
using HarmonyLib;
using NavalDLC.View.Map.Managers;
using SandBox.View.Map.Managers;
using SandBox.View.Map.Visuals;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal static class RFPoliticalStrategicViewState
{
    private static readonly Dictionary<GameEntity, bool> HiddenEntities = new();
    private static readonly System.Reflection.FieldInfo? PartyVisualBannerEntityField =
        AccessTools.Field(typeof(MobilePartyVisual), "_cachedBannerEntity");

    // ⚠️ TRAVADA DO ZOOM MÁXIMO (RFHitchDetector, 2026-08-26): a versão antiga varria
    // TODAS as parties TODO TICK chamando GetPartyVisual — indexer de Dictionary que
    // LANÇA KeyNotFoundException para party sem visual — engolida em try/catch:
    // milhares de exceções por frame = tela congelada (16,8s medidos; com debugger,
    // pior). Correções: (1) varredura no máximo 1x/segundo (spawn novo some em <1s);
    // (2) lookup SEM exceção — o dicionário interno de cada manager é lido via
    // reflexão e consultado pelo indexer NÃO-genérico de IDictionary, que devolve
    // null em vez de lançar; (3) entidade já escondida não recebe chamada nativa.
    private const int SweepIntervalMs = 1000;
    private static int _lastSweepTickCount = int.MinValue;
    private static System.Reflection.FieldInfo? _partyDictField;
    private static System.Reflection.FieldInfo? _navalDictField;
    private static System.Reflection.FieldInfo? _settlementDictField;
    private static System.Reflection.PropertyInfo? _navalStrategicEntityProp;

    internal static bool IsActive { get; private set; }

    internal static void SetActive(bool active)
    {
        if (IsActive == active)
        {
            if (active)
            {
                HideMapClutter();
            }
            return;
        }

        IsActive = active;
        if (active)
        {
            _lastSweepTickCount = int.MinValue; // ativa = varredura imediata
            HideMapClutter();
        }
        else
        {
            RestoreMapClutter();
        }
    }

    internal static void HideMapClutter()
    {
        if (!IsActive || Campaign.Current == null)
        {
            return;
        }

        int now = Environment.TickCount;
        if (now - _lastSweepTickCount < SweepIntervalMs)
        {
            return;
        }
        _lastSweepTickCount = now;

        System.Collections.IDictionary? partyVisuals = GetVisualDictionary(MobilePartyVisualManager.Current, ref _partyDictField);
        System.Collections.IDictionary? navalVisuals = GetVisualDictionary(NavalMobilePartyVisualManager.Current, ref _navalDictField);

        foreach (MobileParty party in MobileParty.All)
        {
            PartyBase? partyBase = party?.Party;
            if (partyBase == null)
            {
                continue;
            }

            if (partyVisuals?[partyBase] is MobilePartyVisual visual)
            {
                HidePartyVisual(visual);
            }

            object? navalVisual = navalVisuals?[partyBase];
            if (navalVisual != null)
            {
                // O visual naval pode nao herdar de MobilePartyVisual — acesso a
                // StrategicEntity por propriedade cacheada, sem cast fragil.
                _navalStrategicEntityProp ??= AccessTools.Property(navalVisual.GetType(), "StrategicEntity");
                Hide(_navalStrategicEntityProp?.GetValue(navalVisual) as GameEntity);
            }
        }

        System.Collections.IDictionary? settlementVisuals = GetVisualDictionary(SettlementVisualManager.Current, ref _settlementDictField);
        if (settlementVisuals != null)
        {
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlementVisuals[settlement] is SettlementVisual settlementVisual)
                {
                    Hide(settlementVisual.StrategicEntity);
                }
            }
        }
    }

    /// <summary>Primeiro campo de instância que implementa IDictionary no manager —
    /// o cache de visuais. Indexer não-genérico devolve null para chave ausente.</summary>
    private static System.Collections.IDictionary? GetVisualDictionary(object? manager, ref System.Reflection.FieldInfo? cachedField)
    {
        if (manager == null)
        {
            return null;
        }

        if (cachedField == null)
        {
            foreach (System.Reflection.FieldInfo field in AccessTools.GetDeclaredFields(manager.GetType()))
            {
                if (typeof(System.Collections.IDictionary).IsAssignableFrom(field.FieldType))
                {
                    cachedField = field;
                    break;
                }
            }
        }

        return cachedField?.GetValue(manager) as System.Collections.IDictionary;
    }

    private static void HidePartyVisual(MobilePartyVisual? visual)
    {
        if (visual == null)
        {
            return;
        }

        Hide(visual.StrategicEntity);
        Hide(visual.HumanAgentVisuals?.GetEntity());
        Hide(visual.MountAgentVisuals?.GetEntity());
        Hide(visual.CaravanMountAgentVisuals?.GetEntity());
        if (PartyVisualBannerEntityField?.GetValue(visual) is ValueTuple<string, GameEntity> banner)
        {
            Hide(banner.Item2);
        }
    }

    private static void Hide(GameEntity? entity)
    {
        if (entity == null)
        {
            return;
        }

        if (HiddenEntities.ContainsKey(entity))
        {
            // Ja rastreada: so re-esconde se algum sistema a re-exibiu (1 getter
            // nativo por entidade por varredura de 1s — barato; o setter
            // incondicional de antes rodava por FRAME).
            if (entity.GetVisibilityExcludeParents())
            {
                entity.SetVisibilityExcludeParents(false);
            }
            return;
        }

        HiddenEntities.Add(entity, entity.GetVisibilityExcludeParents());
        entity.SetVisibilityExcludeParents(false);
    }

    private static void RestoreMapClutter()
    {
        foreach (KeyValuePair<GameEntity, bool> pair in HiddenEntities)
        {
            try
            {
                pair.Key?.SetVisibilityExcludeParents(pair.Value);
            }
            catch
            {
            }
        }
        HiddenEntities.Clear();
    }
}

[HarmonyPatch(typeof(NameplateVM), nameof(NameplateVM.IsVisibleOnMap), MethodType.Setter)]
internal static class RFPoliticalNameplateVisibilityPatch
{
    private static void Prefix(ref bool value)
    {
        if (RFPoliticalStrategicViewState.IsActive)
        {
            value = false;
        }
    }
}
