using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Quest
{
    public class FindingRelicMapSceneNotificationItem : SceneNotificationData
    {

        public override string SceneID => "scn_relic_map";
        public override TextObject TitleText => new("");
        public override bool PauseActiveState => true;

        public Action onCloseAction;

        public FindingRelicMapSceneNotificationItem(Action onclose)
        {

            onCloseAction = onclose;
        }

        public override void OnCloseAction()
        {
            onCloseAction();
        }
    }
}
