using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace SOTOR.MagicAccessories;

[PrefabExtension("Inventory", "descendant::Widget[@Id='RightEquipmentList']")]
internal sealed class InventoryMagicAccessorySlotsExtension : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _document = new XmlDocument();

	public override InsertType Type => InsertType.Append;

	// [RF-RUNAS R4] Sobre os ImageIdentifierWidget abaixo — dois erros corrigidos:
	//
	// 1) `IsVisible="@Occupied"` (e `@MagicRingSlotOccupied` / `@MagicNecklaceSlotOccupied`)
	//    estava num widget cujo DataSource e `{Image}`, ou seja resolvia contra o
	//    ItemImageIdentifierVM. Conferido na DLL 1.4.8: ImageIdentifierVM expoe APENAS
	//    `Id`, `AdditionalArgs` e `TextureProviderName` como [DataSourceProperty] — nao existe
	//    `Occupied`. O binding era morto.
	// 2) Pior: era inutil de qualquer forma. ImageIdentifierWidget.RefreshVisibility() faz
	//    `if (HideWhenNull) IsVisible = !string.IsNullOrEmpty(ImageId); else IsVisible = true;`
	//    e roda nos setters de ImageId/AdditionalArgs — o widget MANDA no proprio IsVisible.
	//    A forma correta de esconder quando vazio e `HideWhenNull="true"`.
	//
	// O resto segue o idioma vanilla de item->imagem do 1.4.8 instalado
	// (SandBox/GUI/Prefabs/Inventory/InventoryItemTuple.xml, ImageIdentifierWidget sem
	// LoadingIconWidget) — deliberadamente sem `Standard.CircleLoadingWidget` para nao
	// depender de resolucao de prefab externo dentro do XML injetado.
	public InventoryMagicAccessorySlotsExtension()
	{
		_document.LoadXml(@"
<Widget Id=""RFMagicEquipmentExtensions"" DoNotAcceptEvents=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""181"" SuggestedHeight=""565"" HorizontalAlignment=""Right"" VerticalAlignment=""Top"" MarginLeft=""5"">
  <Children>
    <Widget Id=""RFMagicRuneSockets"" DoNotAcceptEvents=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""441"" HorizontalAlignment=""Left"" VerticalAlignment=""Top"" IsVisible=""@IsBattleMode"">
      <Children>
        <ButtonWidget Id=""RFMagicRuneSlot1Widget"" DataSource=""{MagicRuneSlot1}"" DoNotPassEventsToChildren=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""52"" MarginTop=""57"" Brush=""InventoryWeaponSlot"" Brush.ColorFactor=""1.3"" Command.Click=""ExecuteClick"" Command.HoverBegin=""ExecuteHoverBegin"" Command.HoverEnd=""ExecuteHoverEnd"">
          <Children>
            <ImageIdentifierWidget DataSource=""{Image}"" DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" ImageId=""@Id"" AdditionalArgs=""@AdditionalArgs"" TextureProviderName=""@TextureProviderName"" IsBig=""true"" HideWhenNull=""true"" />
            <TextWidget DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Brush=""InventoryDefaultFontBrush"" Brush.FontSize=""16"" Brush.TextHorizontalAlignment=""Center"" Brush.TextVerticalAlignment=""Center"" Text=""R"" IsVisible=""@Empty"" />
          </Children>
        </ButtonWidget>
        <ListPanel DataSource=""{MagicRuneSlot1}"" IsVisible=""@ShowControls"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""32"" MarginTop=""109"" StackLayout.LayoutMethod=""HorizontalLeftToRight"">
          <Children>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed"">
              <Children>
                <ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteSellSingle"" Brush=""ButtonRightArrowBrush1"">
                  <Children>
                    <HintWidget DoNotAcceptEvents=""true"" DataSource=""{SellHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" />
                  </Children>
                </ButtonWidget>
              </Children>
            </Widget>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" MarginLeft=""2"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed"">
              <Children>
                <ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteUnequipItem"" Brush=""InventoryUnequipButton"">
                  <Children>
                    <HintWidget DoNotAcceptEvents=""true"" DataSource=""{UnequipHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" />
                  </Children>
                </ButtonWidget>
              </Children>
            </Widget>
          </Children>
        </ListPanel>
        <ButtonWidget Id=""RFMagicRuneSlot2Widget"" DataSource=""{MagicRuneSlot2}"" DoNotPassEventsToChildren=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""52"" MarginTop=""157"" Brush=""InventoryWeaponSlot"" Brush.ColorFactor=""1.3"" Command.Click=""ExecuteClick"" Command.HoverBegin=""ExecuteHoverBegin"" Command.HoverEnd=""ExecuteHoverEnd"">
          <Children>
            <ImageIdentifierWidget DataSource=""{Image}"" DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" ImageId=""@Id"" AdditionalArgs=""@AdditionalArgs"" TextureProviderName=""@TextureProviderName"" IsBig=""true"" HideWhenNull=""true"" />
            <TextWidget DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Brush=""InventoryDefaultFontBrush"" Brush.FontSize=""16"" Brush.TextHorizontalAlignment=""Center"" Brush.TextVerticalAlignment=""Center"" Text=""R"" IsVisible=""@Empty"" />
          </Children>
        </ButtonWidget>
        <ListPanel DataSource=""{MagicRuneSlot2}"" IsVisible=""@ShowControls"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""32"" MarginTop=""209"" StackLayout.LayoutMethod=""HorizontalLeftToRight"">
          <Children>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed""><Children><ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteSellSingle"" Brush=""ButtonRightArrowBrush1""><Children><HintWidget DoNotAcceptEvents=""true"" DataSource=""{SellHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" /></Children></ButtonWidget></Children></Widget>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" MarginLeft=""2"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed""><Children><ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteUnequipItem"" Brush=""InventoryUnequipButton""><Children><HintWidget DoNotAcceptEvents=""true"" DataSource=""{UnequipHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" /></Children></ButtonWidget></Children></Widget>
          </Children>
        </ListPanel>
        <ButtonWidget Id=""RFMagicRuneSlot3Widget"" DataSource=""{MagicRuneSlot3}"" DoNotPassEventsToChildren=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""52"" MarginTop=""257"" Brush=""InventoryWeaponSlot"" Brush.ColorFactor=""1.3"" Command.Click=""ExecuteClick"" Command.HoverBegin=""ExecuteHoverBegin"" Command.HoverEnd=""ExecuteHoverEnd"">
          <Children>
            <ImageIdentifierWidget DataSource=""{Image}"" DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" ImageId=""@Id"" AdditionalArgs=""@AdditionalArgs"" TextureProviderName=""@TextureProviderName"" IsBig=""true"" HideWhenNull=""true"" />
            <TextWidget DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Brush=""InventoryDefaultFontBrush"" Brush.FontSize=""16"" Brush.TextHorizontalAlignment=""Center"" Brush.TextVerticalAlignment=""Center"" Text=""R"" IsVisible=""@Empty"" />
          </Children>
        </ButtonWidget>
        <ListPanel DataSource=""{MagicRuneSlot3}"" IsVisible=""@ShowControls"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""32"" MarginTop=""309"" StackLayout.LayoutMethod=""HorizontalLeftToRight"">
          <Children>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed""><Children><ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteSellSingle"" Brush=""ButtonRightArrowBrush1""><Children><HintWidget DoNotAcceptEvents=""true"" DataSource=""{SellHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" /></Children></ButtonWidget></Children></Widget>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" MarginLeft=""2"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed""><Children><ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteUnequipItem"" Brush=""InventoryUnequipButton""><Children><HintWidget DoNotAcceptEvents=""true"" DataSource=""{UnequipHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" /></Children></ButtonWidget></Children></Widget>
          </Children>
        </ListPanel>
        <ButtonWidget Id=""RFMagicRuneSlot4Widget"" DataSource=""{MagicRuneSlot4}"" DoNotPassEventsToChildren=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""52"" MarginTop=""357"" Brush=""InventoryWeaponSlot"" Brush.ColorFactor=""1.3"" Command.Click=""ExecuteClick"" Command.HoverBegin=""ExecuteHoverBegin"" Command.HoverEnd=""ExecuteHoverEnd"">
          <Children>
            <ImageIdentifierWidget DataSource=""{Image}"" DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" ImageId=""@Id"" AdditionalArgs=""@AdditionalArgs"" TextureProviderName=""@TextureProviderName"" IsBig=""true"" HideWhenNull=""true"" />
            <TextWidget DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Brush=""InventoryDefaultFontBrush"" Brush.FontSize=""16"" Brush.TextHorizontalAlignment=""Center"" Brush.TextVerticalAlignment=""Center"" Text=""R"" IsVisible=""@Empty"" />
          </Children>
        </ButtonWidget>
        <ListPanel DataSource=""{MagicRuneSlot4}"" IsVisible=""@ShowControls"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""52"" SuggestedHeight=""32"" MarginTop=""409"" StackLayout.LayoutMethod=""HorizontalLeftToRight"">
          <Children>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed""><Children><ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteSellSingle"" Brush=""ButtonRightArrowBrush1""><Children><HintWidget DoNotAcceptEvents=""true"" DataSource=""{SellHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" /></Children></ButtonWidget></Children></Widget>
            <Widget WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""25"" SuggestedHeight=""30"" MarginLeft=""2"" VerticalAlignment=""Center"" Sprite=""Inventory\toolbox_icon_bed""><Children><ButtonWidget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""2"" MarginRight=""2"" MarginTop=""2"" MarginBottom=""2"" Command.Click=""ExecuteUnequipItem"" Brush=""InventoryUnequipButton""><Children><HintWidget DoNotAcceptEvents=""true"" DataSource=""{UnequipHint}"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" /></Children></ButtonWidget></Children></Widget>
          </Children>
        </ListPanel>
      </Children>
    </Widget>
    <ListPanel Id=""RFMagicAccessorySlots"" DoNotAcceptEvents=""true"" WidthSizePolicy=""Fixed"" HeightSizePolicy=""Fixed"" SuggestedWidth=""130"" SuggestedHeight=""140"" StackLayout.LayoutMethod=""VerticalTopToBottom"" HorizontalAlignment=""Right"" VerticalAlignment=""Top"" MarginTop=""425"">
      <Children>
    <ButtonWidget Id=""RFMagicRingSlot"" DoNotPassEventsToChildren=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""65"" MarginBottom=""10"" Brush=""InventoryWeaponSlot"" Brush.ColorFactor=""1.3"" Command.Click=""ExecuteMagicRingSlot"">
      <Children>
        <Widget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" Sprite=""Inventory\portrait_cart"" ColorFactor=""1.3"" />
        <ImageIdentifierWidget DataSource=""{MagicRingImage}"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" ImageId=""@Id"" AdditionalArgs=""@AdditionalArgs"" TextureProviderName=""@TextureProviderName"" HideWhenNull=""true"" />
        <TextWidget DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Brush=""InventoryDefaultFontBrush"" Brush.FontSize=""15"" Brush.TextHorizontalAlignment=""Center"" Brush.TextVerticalAlignment=""Center"" Text=""RING"" IsVisible=""@MagicRingSlotEmpty"" />
        <HintWidget DataSource=""{MagicRingHint}"" IsDisabled=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" />
      </Children>
    </ButtonWidget>
    <ButtonWidget Id=""RFMagicNecklaceSlot"" DoNotPassEventsToChildren=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""Fixed"" SuggestedHeight=""65"" Brush=""InventoryWeaponSlot"" Brush.ColorFactor=""1.3"" Command.Click=""ExecuteMagicNecklaceSlot"">
      <Children>
        <Widget WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" Sprite=""Inventory\portrait_cart"" ColorFactor=""1.3"" />
        <ImageIdentifierWidget DataSource=""{MagicNecklaceImage}"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" MarginLeft=""3"" MarginRight=""4"" MarginTop=""3"" MarginBottom=""4"" ImageId=""@Id"" AdditionalArgs=""@AdditionalArgs"" TextureProviderName=""@TextureProviderName"" HideWhenNull=""true"" />
        <TextWidget DoNotAcceptEvents=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Brush=""InventoryDefaultFontBrush"" Brush.FontSize=""13"" Brush.TextHorizontalAlignment=""Center"" Brush.TextVerticalAlignment=""Center"" Text=""NECKLACE"" IsVisible=""@MagicNecklaceSlotEmpty"" />
        <HintWidget DataSource=""{MagicNecklaceHint}"" IsDisabled=""true"" WidthSizePolicy=""StretchToParent"" HeightSizePolicy=""StretchToParent"" Command.HoverBegin=""ExecuteBeginHint"" Command.HoverEnd=""ExecuteEndHint"" />
      </Children>
    </ButtonWidget>
      </Children>
    </ListPanel>
  </Children>
</Widget>");
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _document;
	}
}
