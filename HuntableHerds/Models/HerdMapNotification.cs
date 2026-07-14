using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.HuntableHerds.Models {
    public class HerdMapNotification : InformationData {
        // Capture the title at construction. Reading the live static in the
        // getter meant a SAVED notification showed the title of whatever herd
        // was randomized LAST (the last XML entry after load), not the one this
        // notification was created for. Fall back to the static for old saves.
        [SaveableField(1)]
        private string _capturedTitle;

        public override TextObject TitleText {
            get {
                return new TextObject(string.IsNullOrEmpty(_capturedTitle)
                    ? HerdBuildData.CurrentHerdBuildData.MessageTitle
                    : _capturedTitle);
            }
        }

        public override string SoundEventPath {
            get {
                return "";
            }
        }

        public HerdMapNotification(TextObject description) : base(description) {
            _capturedTitle = HerdBuildData.CurrentHerdBuildData.MessageTitle;
        }
    }
}
