namespace RF_Settlers
{
    /// <summary>
    /// Marker for parties driven entirely by their OWN campaign behavior:
    /// vanilla's settlement-visit AI (AiVisitSettlementBehavior) must skip
    /// them — they are leaderless, sometimes ownerless (bandit-held zones),
    /// a shape vanilla's scoring dereferences without null checks
    /// (mobileParty.Party.Owner.MapFaction → NRE). Checked by
    /// SettlerAiThinkPatch.
    /// </summary>
    public interface IRFSelfDrivenParty
    {
    }

    /// <summary>
    /// Marker for stationary camp-style PartyComponents that share the settler
    /// camp presentation: the map icon is the siege-camp TENT (banner included)
    /// via SettlerCampVisualPatch, and a non-hostile approach opens a game menu
    /// instead of a conversation via SettlerCampEncounterPatch.
    ///
    /// Implemented by SettlerCampComponent (RF_Settlers) and
    /// ResourceZonePartyComponent (RF_ResourceZones). Living here keeps the
    /// dependency direction single: consumers reference RF_Settlers, and the
    /// two Harmony patches stay applied exactly once (the MobilePartyVisual one
    /// LATE — the folded-character lesson).
    /// </summary>
    public interface IRFStationaryCampParty : IRFSelfDrivenParty
    {
        /// <summary>Game menu id opened when the player peacefully meets the party.</summary>
        string EncounterMenuId { get; }

        /// <summary>Map icon mesh to render instead of the siege-camp tent
        /// (e.g. a vanilla VillageType.MeshName — iron mine, lumberjack...).
        /// Null/empty keeps the tent.</summary>
        string? MapIconMeshName { get; }
    }
}
