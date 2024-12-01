using System;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade
{
    internal class ItemFoundSceneNotificationItem : SceneNotificationData
    {
        private Action value;

        public ItemFoundSceneNotificationItem(Action value)
        {
            this.value = value;
        }
    }
}