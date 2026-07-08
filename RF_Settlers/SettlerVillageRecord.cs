using TaleWorlds.SaveSystem;

namespace RF_Settlers
{
    /// <summary>
    /// One village founded (or about to be founded) by settlers. The full
    /// substituted settlement XML is stored in the SAVE and re-injected into
    /// MBObjectManager in RegisterSubModuleObjects — BEFORE campaign
    /// deserialization — so save references to the settlement always resolve
    /// (the Player Settlement technique). A record with Established=false is a
    /// mature camp whose village materializes on the next session load.
    /// </summary>
    public class SettlerVillageRecord
    {
        [SaveableField(1)]
        public string StringId;

        [SaveableField(2)]
        public string DisplayName;

        [SaveableField(3)]
        public string SettlementXml;

        [SaveableField(4)]
        public string PrefabId;

        [SaveableField(5)]
        public string CampPartyId;

        [SaveableField(6)]
        public string KingdomId;

        [SaveableField(7)]
        public bool Established;
    }
}
