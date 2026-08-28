using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace RealmsForgotten.AiMade.UIOverhaul;

[PrefabExtension("InitialScreen", "//BrushWidget[@Brush='InitialMenu.Logo.WarSails' and @IsVisible='@IsNavalDLCEnabled']")]
internal sealed class RFInitialScreenWarSailsLogoPatch : PrefabExtensionInsertPatch
{
    public override InsertType Type => InsertType.Replace;

    private IEnumerable<XmlNode> _nodes;

    [PrefabExtensionXmlNodes]
    public IEnumerable<XmlNode> GetNodes()
    {
        if (_nodes == null)
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(
                "<DiscardedRoot>" +
                "<Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='!Logo.WarSails.Width' SuggestedHeight='!Logo.WarSails.Height' HorizontalAlignment='Center' VerticalAlignment='Top' MarginTop='100' IsVisible='@IsNavalDLCEnabled' DoNotAcceptEvents='true'>" +
                "<Children>" +
                "<BrushWidget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='!Logo.Width' SuggestedHeight='!Logo.Height' HorizontalAlignment='Center' VerticalAlignment='Top' Brush='InitialMenu.Logo' ForcePixelPerfectRenderPlacement='true' DoNotAcceptEvents='true' />" +
                "<Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='51.35' VerticalAlignment='Bottom' ClipContents='true' DoNotAcceptEvents='true'>" +
                "<Children>" +
                "<BrushWidget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='!Logo.WarSails.Width' SuggestedHeight='!Logo.WarSails.Height' HorizontalAlignment='Center' VerticalAlignment='Bottom' Brush='InitialMenu.Logo.WarSails' ForcePixelPerfectRenderPlacement='true' DoNotAcceptEvents='true' />" +
                "</Children>" +
                "</Widget>" +
                "</Children>" +
                "</Widget>" +
                "</DiscardedRoot>");
            _nodes = document.DocumentElement.ChildNodes.Cast<XmlNode>();
        }
        return _nodes;
    }
}
