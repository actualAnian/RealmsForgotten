using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using System.Collections.Generic;
using PrefabAttribute = Bannerlord.UIExtenderEx.Prefabs2.PrefabExtensionSetAttributePatch.Attribute;

namespace RealmsForgotten.AiMade.UIOverhaul;

/// <summary>
/// Liga a animação de equipar/desembainhar no boneco 3D da tela de party.
///
/// Contexto (2026-08-18): a feature veio do mod ArtemsBetterUIVisuals, que no Bannerlord
/// 1.2.x reimplementava o CharacterTableau inteiro para ter isso. No 1.4.8 a TaleWorlds
/// absorveu o sistema no CharacterTableau vanilla (StartCustomAnimation, SetIdleAction,
/// RefreshCharacterTableau(oldEquipment)...) e o Inventory.xml vanilla já liga
/// IsEquipmentAnimActive="true" — mas o PartyScreen.xml ficou de fora. Este patch de um
/// atributo é o que faltava; nenhum código de tableau precisou ser portado.
/// </summary>
[PrefabExtension("PartyScreen", "descendant::CharacterTableauWidget")]
internal sealed class RFPartyScreenTableauEquipAnimPatch : PrefabExtensionSetAttributePatch
{
    public override List<PrefabAttribute> Attributes => new()
    {
        new PrefabAttribute("IsEquipmentAnimActive", "true")
    };
}
