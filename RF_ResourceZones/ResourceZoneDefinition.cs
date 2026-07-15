namespace RF_ResourceZones
{
    /// <summary>
    /// One &lt;Zone&gt; entry of ModuleData/rf_resource_zones.xml — pure content,
    /// never saved. Position comes either from absolute posX/posY (authored with
    /// the rf_zones.mark_zone console command) or from an anchor settlement plus
    /// offset, resolved to a reachable point at spawn time.
    /// </summary>
    public sealed class ResourceZoneDefinition
    {
        public string Id = string.Empty;
        public ResourceZoneType Type;
        public string Name = string.Empty;

        public bool HasAbsolutePosition;
        public float PosX;
        public float PosY;

        public string AnchorSettlementId = string.Empty;
        public float OffsetX;
        public float OffsetY;

        /// <summary>Town whose market the zone's caravans feed. Empty = nearest town.</summary>
        public string BoundTownId = string.Empty;
    }
}
