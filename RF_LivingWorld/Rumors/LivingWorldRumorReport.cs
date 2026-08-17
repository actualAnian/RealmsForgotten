using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldRumorReport
    {
        [SaveableField(1)] private string _sourcePartyId;
        [SaveableField(2)] private string _targetId;
        [SaveableField(3)] private int _kind;
        [SaveableField(4)] private CampaignTime _createdAt;
        [SaveableField(5)] private CampaignTime _expiresAt;
        [SaveableField(6)] private float _reliability;
        [SaveableField(7)] private string _text;
        [SaveableField(8)] private float _mapX;
        [SaveableField(9)] private float _mapY;

        public LivingWorldRumorReport(
            string sourcePartyId,
            string targetId,
            LivingWorldRumorKind kind,
            CampaignTime createdAt,
            CampaignTime expiresAt,
            float reliability,
            string text,
            float mapX,
            float mapY)
        {
            _sourcePartyId = sourcePartyId;
            _targetId = targetId;
            _kind = (int)kind;
            _createdAt = createdAt;
            _expiresAt = expiresAt;
            _reliability = reliability;
            _text = text;
            _mapX = mapX;
            _mapY = mapY;
        }

        public string SourcePartyId => _sourcePartyId;
        public string TargetId => _targetId;
        public LivingWorldRumorKind Kind => (LivingWorldRumorKind)_kind;
        public CampaignTime CreatedAt => _createdAt;
        public CampaignTime ExpiresAt => _expiresAt;
        public float Reliability => _reliability;
        public string Text => _text;
        public float MapX => _mapX;
        public float MapY => _mapY;
        public bool IsExpired => CampaignTime.Now >= _expiresAt;
    }

    public sealed class LivingWorldRumorSourceState
    {
        [SaveableField(1)] private string _partyId;
        [SaveableField(2)] private CampaignTime _lastSharedAt;

        public LivingWorldRumorSourceState(string partyId, CampaignTime lastSharedAt)
        {
            _partyId = partyId;
            _lastSharedAt = lastSharedAt;
        }

        public string PartyId => _partyId;
        public CampaignTime LastSharedAt => _lastSharedAt;

        public void MarkShared(CampaignTime time)
        {
            _lastSharedAt = time;
        }
    }
}
