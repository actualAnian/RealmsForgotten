using System;
using TaleWorlds.Library;

namespace Bannerlord.Module1
{
    internal class SimplePopupVM
    {
        private string title;
        private string smallText;
        private string spriteName;
        private Action onContinue;
        private Action hideInquiry;

        public SimplePopupVM(string title, string smallText, string spriteName, Action onContinue, Action hideInquiry)
        {
            this.title = title;
            this.smallText = smallText;
            this.spriteName = spriteName;
            this.onContinue = onContinue;
            this.hideInquiry = hideInquiry;
        }
    }
}