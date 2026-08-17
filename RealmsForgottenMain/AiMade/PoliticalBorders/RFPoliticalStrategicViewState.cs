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

        foreach (MobileParty party in MobileParty.All)
        {
            if (party == null || party.Party == null)
            {
                continue;
            }

            try
            {
                HidePartyVisual(MobilePartyVisualManager.Current?.GetPartyVisual(party.Party));
            }
            catch
            {
            }

            try
            {
                Hide(NavalMobilePartyVisualManager.Current?.GetPartyVisual(party.Party)?.StrategicEntity);
            }
            catch
            {
            }
        }

        foreach (Settlement settlement in Settlement.All)
        {
            try
            {
                Hide(SettlementVisualManager.Current?.GetSettlementVisual(settlement)?.StrategicEntity);
            }
            catch
            {
            }
        }
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

        if (!HiddenEntities.ContainsKey(entity))
        {
            HiddenEntities.Add(entity, entity.GetVisibilityExcludeParents());
        }
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
