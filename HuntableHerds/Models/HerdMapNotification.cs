using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.HuntableHerds.Models
{
    /// <summary>
    /// A "herd spotted" map notice. The herd it refers to is stored ON THE NOTIFICATION, so an old
    /// notice opened after a newer one was created still starts the hunt it advertised. Previously
    /// this class read <see cref="HerdBuildData.CurrentHerdBuildData"/>, which meant every notice
    /// silently mutated into whatever herd was rolled last.
    /// </summary>
    public class HerdMapNotification : InformationData
    {
        // Only save-system friendly primitives are persisted; the HerdBuildData itself is resolved
        // on demand, so editing hunting_herds.xml between saves cannot corrupt a save.
        [SaveableField(1)]
        private int _herdIndex;

        [SaveableField(2)]
        private string _herdSpawnId;

        [SaveableField(3)]
        private string _titleText;

        public HerdMapNotification(TextObject description) : base(description)
        {
            _herdIndex = -1;
            _herdSpawnId = string.Empty;
            _titleText = "Herd Spotted";
        }

        public HerdMapNotification(HerdBuildData herd, string title, TextObject description) : base(description)
        {
            _herdIndex = herd?.Index ?? -1;
            _herdSpawnId = herd?.SpawnId ?? string.Empty;
            _titleText = string.IsNullOrEmpty(title) ? "Herd Spotted" : title;
        }

        /// <summary>The herd this notice advertises. Never null while hunting_herds.xml has entries.</summary>
        public HerdBuildData? Herd => HerdBuildData.Resolve(_herdIndex, _herdSpawnId);

        public override TextObject TitleText => new TextObject(_titleText ?? "Herd Spotted");

        public override string SoundEventPath => "";
    }
}
