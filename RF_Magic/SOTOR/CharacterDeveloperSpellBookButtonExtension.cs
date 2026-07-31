using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace SOTOR;

// [RF-C] O RealmsForgotten TROCA o movie da tela de personagem: o
// ReplaceUIPatch dele intercepta GauntletLayer.LoadMovie e substitui
// "CharacterDeveloper" por "RFCharacterDeveloper". PrefabExtension casa pelo
// NOME do movie, entao a extensao original do SOTOR nunca era aplicada com o RF
// ativo — o botao do grimorio nem entrava na arvore de widgets.
//
// Solucao: duas extensoes, uma por movie. O no alvo
// (Standard.TripleDialogCloseButtons) existe nos dois. As classes sao
// duplicadas de proposito: heranca aqui arriscaria a descoberta por reflection
// do UIExtenderEx.
//
// No movie do RF o canto superior direito (MarginTop 30) ja e ocupado pela
// fileira de botoes do proprio RF, por isso a versao RF desce para MarginTop
// 120. O tamanho 162x52 mantem a proporcao do sprite original (233x75) e foi
// reduzido para nao cobrir a cabeca do personagem no retrato.

[PrefabExtension("CharacterDeveloper", "descendant::Standard.TripleDialogCloseButtons")]
internal class CharacterDeveloperSpellBookButtonExtension : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _document = new XmlDocument();

	public override InsertType Type => InsertType.Append;

	public CharacterDeveloperSpellBookButtonExtension()
	{
		_document.LoadXml("<ListPanel DoNotAcceptEvents=\"true\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"CoverChildren\" StackLayout.LayoutMethod=\"HorizontalRightToLeft\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" MarginTop=\"30\" MarginRight=\"70\">\r\n  <Children>\r\n    <ButtonWidget DoNotPassEventsToChildren=\"true\" WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" SuggestedWidth=\"162\" SuggestedHeight=\"52\" HorizontalAlignment=\"Right\" Sprite=\"SPGeneral\\spellbook_button\" Command.Click=\"ExecuteOpenSpellBook\" IsVisible=\"@IsSpellBookButtonVisible\" />\r\n  </Children>\r\n</ListPanel>");
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _document;
	}
}

// [RF-C] Mesma extensao, para o movie que o RealmsForgotten coloca no lugar.
[PrefabExtension("RFCharacterDeveloper", "descendant::Standard.TripleDialogCloseButtons")]
internal class RFCharacterDeveloperSpellBookButtonExtension : PrefabExtensionInsertPatch
{
	private readonly XmlDocument _document = new XmlDocument();

	public override InsertType Type => InsertType.Append;

	public RFCharacterDeveloperSpellBookButtonExtension()
	{
		_document.LoadXml("<ListPanel DoNotAcceptEvents=\"true\" WidthSizePolicy=\"StretchToParent\" HeightSizePolicy=\"CoverChildren\" StackLayout.LayoutMethod=\"HorizontalRightToLeft\" HorizontalAlignment=\"Right\" VerticalAlignment=\"Top\" MarginTop=\"120\" MarginRight=\"70\">\r\n  <Children>\r\n    <ButtonWidget DoNotPassEventsToChildren=\"true\" WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" SuggestedWidth=\"162\" SuggestedHeight=\"52\" HorizontalAlignment=\"Right\" Sprite=\"SPGeneral\\spellbook_button\" Command.Click=\"ExecuteOpenSpellBook\" IsVisible=\"@IsSpellBookButtonVisible\" />\r\n  </Children>\r\n</ListPanel>");
	}

	[PrefabExtensionXmlDocument(false)]
	public XmlDocument GetPrefabExtension()
	{
		return _document;
	}
}
