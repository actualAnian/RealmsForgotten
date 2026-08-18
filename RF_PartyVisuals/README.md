# RF_PartyVisuals

Figuras 3D de tropa nos ícones de party do mapa de campanha (substituto RF-nativo do
ArtemsBetterPartyVisuals, escrito a partir do estudo da técnica, não copiado).

## Passou a ser projeto dentro do RF (2026-08-18)

Antes era um módulo stand-alone (`Modules/RF_PartyVisuals`, com `SubModule.xml` e
`ModuleData` próprios). Agora segue o mesmo padrão de RF_Ambush / RF_AliveScenes:

| Item | Antes | Agora |
|---|---|---|
| DLL | `Modules/RF_PartyVisuals/bin/...` | `Modules/RealmsForgotten/bin/Win64_Shipping_Client/RF_PartyVisuals.dll` (PostBuild copia sozinho, junto do bin local em `RealmsForgottenMain/_Module`) |
| Registro do SubModule | `RF_PartyVisuals/_Module/SubModule.xml` | entrada `RF_PartyVisuals` em `RealmsForgottenMain/_Module/SubModule.xml` |
| `action_sets` dos animais | `RF_PartyVisuals/_Module/ModuleData/action_sets.xml` | fundidos em `RealmsForgottenMain/_Module/ModuleData/action_sets.xml` (o `as_human_warrior`/`blow_horn` que já estava lá ficou intacto) |
| Solution | fora | `RealmsForgotten.sln`, GUID `{3D9F51A6-8E42-4B7C-A1D5-6F2C89E04B13}` |

O `action_sets.xml` é carregado pelo motor **pelo nome do arquivo** (o Native também não
o declara em `<Xmls>`), por isso a fusão no arquivo do RF em vez de um arquivo novo com
uma entrada `<XmlNode>`.

### O stand-alone continua no disco

A pasta `Modules/RF_PartyVisuals` do jogo e a pasta local `RF_PartyVisuals/_Module` **não
foram apagadas**, como combinado. Duas observações:

1. **Não deixe os dois ligados no launcher ao mesmo tempo.** Se ligar, o jogo instancia
   `RF_PartyVisuals.SubModule` duas vezes; para isso não virar behavior e patch em
   duplicata, o `OnGameStart` ganhou uma guarda por `Game` (`_initializedGame`). Ainda
   assim o módulo antigo carrega uma DLL velha — o certo é desmarcar "RF Party Visuals"
   no launcher e deixar só o RealmsForgotten.
2. Quando decidir apagar: some com `Modules/RF_PartyVisuals` e com `RF_PartyVisuals/_Module`.
   Nada mais aponta para lá — o código não usa `ModuleHelper.GetModuleFullPath`, e o
   `FolderName` das settings MCM (`RF_PartyVisuals`) é a pasta de configuração do MCM,
   independente de onde o módulo mora.

## Dependências em runtime

Harmony (do módulo `Bannerlord.Harmony`) e MCM — os dois já são usados por outros
subprojetos do RF, então nada novo precisou entrar em `DependedModules`.

## O que o código faz

- `SubModule.cs` — registra o behavior e aplica o único patch, sempre dentro de uma
  campanha real (nunca no load do módulo).
- `MobilePartyVisualManagerPatch.cs` — postfix em `MobilePartyVisualManager.OnVisualTick`.
  Deliberadamente **não** se toca em `MobilePartyVisual.AddMobileIconComponents`: patch
  ali corrompe pose de agente no jogo inteiro (bug dos "personagens dobrados").
- `PartyVisualsEnhancer.cs` — cria as figuras como entidades próprias da cena do mapa
  (nunca filhas do ícone), usando a mesma receita da vanilla
  (`FaceGen.GetBaseMonsterFromRace` + action set `_map` + `Scene(MapScene)`), com culling
  por distância e teto por party.
- `Settings.cs` — MCM (`AttributeGlobalSettings`), mesmo padrão do RF_IsoCam.
