using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Messengers
{
    /// <summary>
    /// Botão "Send Messenger" na página de herói da enciclopédia (portado do LOTRAOM).
    /// O prefab é inserido após o texto de informações; o mixin fornece comando, custo,
    /// disponibilidade e hint com o motivo quando indisponível. Registrado automaticamente
    /// pelo UIExtender do RealmsForgotten (varre o assembly por atributos UIExtenderEx).
    /// </summary>
    [PrefabExtension("EncyclopediaHeroPage", "descendant::RichTextWidget[@Text='@InformationText']")]
    internal class RFMessengerEncyclopediaExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;

        private IEnumerable<XmlNode> _nodes;

        [PrefabExtensionXmlNodes]
        public IEnumerable<XmlNode> GetNodes()
        {
            if (_nodes == null)
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(
                    "<DiscardedRoot><ListPanel HorizontalAlignment='Center' HeightSizePolicy='CoverChildren' WidthSizePolicy='CoverChildren' MarginTop='10'><Children>" +
                    "<ButtonWidget DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='227' SuggestedHeight='40' MarginLeft='5' MarginRight='5' Brush='ButtonBrush2' HorizontalAlignment='Left' UpdateChildrenStates='true' Command.Click='SendMessenger' IsEnabled='@IsMessengerAvailable'><Children>" +
                    "<TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.GeneralButtons.Text' Text='@SendMessengerActionName' />" +
                    "<Widget UpdateChildrenStates='true' WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' PositionXOffset='255'><Children>" +
                    "<TextWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Left' Brush='Kingdom.GeneralButtons.Text' IntText='@SendMessengerCost'><Children>" +
                    "<BrushWidget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='27' SuggestedHeight='27' HorizontalAlignment='Left' VerticalAlignment='Center' PositionXOffset='-23' Brush='General.Gold.Icon' />" +
                    "</Children></TextWidget></Children></Widget></Children></ButtonWidget>" +
                    "<HintWidget DataSource='{SendMessengerHint}' DoNotAcceptEvents='true' WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' Command.HoverBegin='ExecuteBeginHint' Command.HoverEnd='ExecuteEndHint' IsEnabled='false'/>" +
                    "</Children></ListPanel></DiscardedRoot>");
                _nodes = document.DocumentElement.ChildNodes.Cast<XmlNode>();
            }
            return _nodes;
        }
    }

    [ViewModelMixin("RefreshValues")]
    internal sealed class RFMessengerEncyclopediaViewModel : BaseViewModelMixin<EncyclopediaHeroPageVM>
    {
        private static readonly TextObject SendMessengerText = new TextObject("{=rf_msgr_button}Send Messenger");

        private readonly Hero _hero;
        private readonly bool _initializationFailed;
        private bool _isMessengerAvailable;
        private HintViewModel _sendMessengerHint = new HintViewModel();

        public RFMessengerEncyclopediaViewModel(EncyclopediaHeroPageVM vm) : base(vm)
        {
            try
            {
                _hero = vm?.Obj as Hero;
                if (_hero == null)
                {
                    _initializationFailed = true;
                    return;
                }
                SendMessengerActionName = SendMessengerText.ToString();
            }
            catch (Exception e)
            {
                Debug.Print("[RF_Messengers] Falha ao iniciar o mixin da enciclopédia: " + e.Message);
                _initializationFailed = true;
            }
        }

        public override void OnRefresh()
        {
            if (_initializationFailed || _hero == null)
            {
                return;
            }

            try
            {
                UpdateIsMessengerAvailable();
            }
            catch (Exception e)
            {
                Debug.Print("[RF_Messengers] Refresh do mixin falhou: " + e.Message);
                _isMessengerAvailable = false;
            }
        }

        [DataSourceMethod]
        public void SendMessenger()
        {
            if (_initializationFailed || _hero == null)
            {
                return;
            }

            Campaign.Current?.GetCampaignBehavior<RFMessengerCampaignBehavior>()?.SendMessenger(_hero);
            OnRefresh();
        }

        [DataSourceProperty]
        public int SendMessengerCost => RFMessengerManager.MessengerGoldCost;

        [DataSourceProperty]
        public bool IsMessengerAvailable
        {
            get => _isMessengerAvailable;
            set => _isMessengerAvailable = value;
        }

        [DataSourceProperty]
        public string SendMessengerActionName { get; }

        [DataSourceProperty]
        public HintViewModel SendMessengerHint => _sendMessengerHint;

        private void UpdateIsMessengerAvailable()
        {
            RFMessengerCampaignBehavior behavior = Campaign.Current?.GetCampaignBehavior<RFMessengerCampaignBehavior>();
            TextObject reason;

            if (behavior != null)
            {
                _isMessengerAvailable = behavior.CanSendMessenger(_hero, out reason);
            }
            else
            {
                _isMessengerAvailable = false;
                reason = new TextObject("{=rf_msgr_not_active}Messenger system not active.");
            }

            _sendMessengerHint = _isMessengerAvailable ? new HintViewModel() : new HintViewModel(reason, null);
        }
    }
}
