using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Bannerlord.Module1
{
    public class MagicItemFoundSceneNotification : SceneNotificationData
    {
        private readonly string _itemName;
        private readonly string _sceneID; // Add this to store the scene ID

        public override string SceneID => _sceneID;  // Use the stored scene ID
        public override TextObject TitleText => new TextObject($"You found {_itemName}!");
        public override bool PauseActiveState => true;

        public Action onCloseAction;

        public MagicItemFoundSceneNotification(string itemName, string sceneID, Action onclose)
        {
            _itemName = itemName;
            _sceneID = sceneID; // Initialize the scene ID
            onCloseAction = onclose;
        }

        public override void OnCloseAction()
        {
            onCloseAction();
        }
    }
}