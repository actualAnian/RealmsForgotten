using RF_Settlers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldPartyComponent : PartyComponent, IRFSelfDrivenParty
    {
        [SaveableField(1)] private string _definitionId;
        [SaveableField(2)] private int _partyType;
        [SaveableField(3)] private int _herdVariant;
        [SaveableField(4)] private Settlement _origin;
        [SaveableField(5)] private Settlement _destination;
        [SaveableField(6)] private CampaignTime _createdAt;
        [SaveableField(7)] private float _rumorReliability;
        [SaveableField(8)] private int _completedRoutes;
        [SaveableField(9)] private string _contextualReason = string.Empty;
        [SaveableField(10)] private bool _isManualSpawn;

        public LivingWorldPartyComponent(
            string definitionId,
            LivingWorldPartyType partyType,
            LivingWorldHerdVariant herdVariant,
            Settlement origin,
            Settlement destination,
            float rumorReliability,
            string? contextualReason = null,
            bool isManualSpawn = false)
        {
            _definitionId = definitionId;
            _partyType = (int)partyType;
            _herdVariant = (int)herdVariant;
            _origin = origin;
            _destination = destination;
            _createdAt = CampaignTime.Now;
            _rumorReliability = rumorReliability;
            _contextualReason = contextualReason ?? string.Empty;
            _isManualSpawn = isManualSpawn;
        }

        public string DefinitionId => _definitionId;
        public LivingWorldPartyType PartyType => (LivingWorldPartyType)_partyType;
        public LivingWorldHerdVariant HerdVariant => (LivingWorldHerdVariant)_herdVariant;
        public Settlement Origin => _origin;
        public Settlement Destination => _destination;
        public CampaignTime CreatedAt => _createdAt;
        public float RumorReliability => _rumorReliability;
        public int CompletedRoutes => _completedRoutes;
        public string ContextualReason => _contextualReason ?? string.Empty;
        public bool IsContextual => !string.IsNullOrEmpty(ContextualReason);
        public bool IsManualSpawn => _isManualSpawn;
        public bool IsAmbient => !IsContextual && !IsManualSpawn;

        public void SetDestination(Settlement destination)
        {
            _destination = destination;
        }

        public void CompleteRoute()
        {
            _completedRoutes++;
        }

        public override Hero? PartyOwner => null;
        public override Hero? Leader => null;
        public override bool AvoidHostileActions => true;
        public override Settlement? HomeSettlement => _origin;

        public override TextObject Name
        {
            get
            {
                TextObject name = PartyType switch
                {
                    LivingWorldPartyType.Merchant => new TextObject("{=rf_lw_independent_merchants}Independent Merchants"),
                    LivingWorldPartyType.Herder => new TextObject("{=rf_lw_herders}{HERD} Herders"),
                    LivingWorldPartyType.Pilgrim => new TextObject("{=rf_lw_pilgrims}Pilgrims"),
                    LivingWorldPartyType.Leper => new TextObject("{=rf_lw_lepers}Leper Caravan"),
                    LivingWorldPartyType.Hunter => new TextObject("{=rf_lw_hunters}Hunters"),
                    LivingWorldPartyType.ReligiousProcession => new TextObject("{=rf_lw_religious_procession}Religious Procession"),
                    LivingWorldPartyType.Healer => new TextObject("{=rf_lw_itinerant_healers}Itinerant Healers"),
                    LivingWorldPartyType.TaxCollector => new TextObject("{=rf_lw_tax_collectors}Tax Collectors"),
                    LivingWorldPartyType.PrisonerEscort => new TextObject("{=rf_lw_prisoner_escort}Prisoner Escort"),
                    LivingWorldPartyType.DowryProcession => new TextObject("{=rf_lw_dowry_procession}Dowry Procession"),
                    _ => new TextObject("{=rf_lw_wandering}Wandering")
                };
                if (PartyType == LivingWorldPartyType.Herder)
                {
                    name.SetTextVariable("HERD", HerdName(HerdVariant));
                }
                return name;
            }
        }

        public override Banner? GetDefaultComponentBanner() => _origin?.MapFaction?.Banner;

        public override void GetMountAndHarnessVisualIdsForPartyIcon(
            PartyBase party,
            out string mountVisualId,
            out string harnessVisualId)
        {
            mountVisualId = string.Empty;
            harnessVisualId = string.Empty;
            if (PartyType != LivingWorldPartyType.Herder)
            {
                base.GetMountAndHarnessVisualIdsForPartyIcon(party, out mountVisualId, out harnessVisualId);
                return;
            }

            // HERDER NAO USA o slot vanilla de montaria do icone — de proposito.
            // Historia (2026-08-19): passar o animal do rebanho por aqui alimentava
            // MobilePartyVisual.AddMountToPartyIcon, cujo Skeleton.ForceUpdateBoneFrames
            // final NAO tem guarda e da AccessViolation com farm animals (sheep/cow/hog)
            // MESMO com o action set as_*_map carregado — AV capturado no debugger do
            // autor. O rebanho visivel no mapa e desenhado pelo RF_PartyVisuals
            // (PartyVisualsEnhancer, caso "Herder": 5 animais pastando em circulo, com
            // sonda de action set e fallback) — caminho unico, comprovado e sem crash.
        }

        public static string HerdItemId(LivingWorldHerdVariant variant) => variant switch
        {
            LivingWorldHerdVariant.Sheep => "sheep",
            LivingWorldHerdVariant.Cow => "cow",
            LivingWorldHerdVariant.Hog => "hog",
            LivingWorldHerdVariant.Mule => "mule",
            LivingWorldHerdVariant.Camel => "camel",
            _ => string.Empty
        };

        private static TextObject HerdName(LivingWorldHerdVariant variant) => variant switch
        {
            LivingWorldHerdVariant.Sheep => new TextObject("{=rf_lw_sheep}Sheep"),
            LivingWorldHerdVariant.Cow => new TextObject("{=rf_lw_cattle}Cattle"),
            LivingWorldHerdVariant.Hog => new TextObject("{=rf_lw_hogs}Hog"),
            LivingWorldHerdVariant.Mule => new TextObject("{=rf_lw_mules}Mule"),
            LivingWorldHerdVariant.Camel => new TextObject("{=rf_lw_camels}Camel"),
            _ => new TextObject("{=rf_lw_wandering}Wandering")
        };
    }
}
