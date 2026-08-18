# RF_AliveScenes

Fala ambiente dos figurantes, com balão sobre a cabeça: cidade, vila, taverna, salão do
lorde, batalha campal, cerco e **batalha naval**. Reescrito a partir do estudo do mod
"Alive Scenes" v1.2.0 (ver [ESTUDO_ALIVESCENES.md](../ESTUDO_ALIVESCENES.md)).

Compila para dentro do módulo `RealmsForgotten`, como RF_Ambush e RF_LivingWorld.
Registrado em `RealmsForgottenMain/_Module/SubModule.xml` como `RF_AliveScenes.SubModule`.

## O que entrou, o que ficou de fora

**Entrou (o que valia a pena)**
- Banco de 2.296 falas + 145 diálogos condicionados por ator, estação, riqueza do
  assentamento, equipamento do jogador, moral da batalha e clima no mar.
- Balão Gauntlet posicionado no mundo, com fonte que diminui com a distância e cor
  vermelha para o lado inimigo.
- Diálogo de dois figurantes (nobres e companheiros).
- Multidão de figurantes extras em cena de cidade — **reescrita** (ver abaixo).

**Ficou de fora (não compensava)**
- **Os 10 patches Harmony.** A view agora entra por `MissionScreen.AddMissionView` no
  `OnMissionBehaviorInitialize` (mesmo caminho do RF_IsoCam). Como o gancho é agnóstico
  de tipo de missão, a batalha naval do War Sails é coberta sem conhecer o assembly do
  DLC — o mod original precisava de um patch por reflexão só para isso.
- **O `Finalizer` em `BattleAgentLogic.OnAgentHit`**, que engolia qualquer exceção em
  cena de assentamento e esconderia erro do RF_Magic / RBM_RF / RF_DualWield.
- **`InformationManager.ClearAllMessages()`** ao sair da cena (limpava o log de todo mundo).
- **A cavalaria cosmética** (`MilitaryActions`): dependia das tags de cena vanilla
  `sp_prison_guard` / `spawnpoint_player_outside` e de item hardcoded que nem existe no
  RF (`western_spear_3_t3`). Puro enfeite, fácil de trazer depois se fizer falta.
- As 6 traduções (RU/DE/FR/SP/TR/CN) do mod original: ficaram em `AliveScenes/ModuleData/Languages`
  para não mexer na árvore de idiomas do RF (que hoje só tem EN + PL). As falas têm o
  texto inglês embutido, então nada quebra sem elas.

## Correções em relação ao original

| # | Problema no mod original | Aqui |
|---|---|---|
| 1 | `SpawnNewAgent` inseria tropas na lista viva `Culture.NotableTemplates` a cada clone (contaminava o spawn de notáveis da campanha) | O clone é do **mesmo** `CharacterObject` do morador; a variação de rosto sai do `BodyPropertyRange` dele. Nenhuma lista compartilhada é tocada — e a raça continua certa com RF_Races. |
| 2 | Sorteio em lista vazia dentro do `OnMissionTick` | Sorteio "swap-remove" sobre lista própria, sem sortear de lista vazia |
| 3 | Filtro de palavrão invertido | "Filtro de palavrão" ligado no MCM agora realmente mascara |
| 4 | Multidão sem teto (3–5 clones por ponto) | 1–2 por padrão e **teto duro** por cena (40) |
| 5 | `CasualChat/Cooldown` lido do nó `TavernChat` | Configuração saiu do XML e foi para o MCM; cada opção é um campo tipado |
| 6 | Atributo inválido virava `false` em vez do default | Idem — não há mais parse de XML de config |
| 7 | `EnemyChat Enabled` nunca era consultado | Respeitado em `SpeechDirector.MayAgentSpeak` |
| 8 | `SingleOrDefault` ao remover o balão (lançava com dois balões do mesmo agente) | `FirstOrDefault` |
| 9 | 70 falas de `WEAPONSMITH` nunca usadas; `Occupation.Guard` classificado como THUG | Mapa de `Occupation` refeito com os nomes do enum (Weaponsmith, Guard, CaravanGuard, Merchant, HorseTrader…) |
| 10 | Índice do diálogo vazava de uma cena para a outra | Estado "já falei isso" vive na `SpeechSession` de cada missão; o banco é imutável |
| 11 | Valor desconhecido no XML virava silenciosamente o primeiro item do enum | Cai em `ANY`/`GENERIC` e loga o aviso |
| 12 | `EventFire += ` duas vezes ao trocar o container de marcadores | `-=` no antigo antes do `+=` no novo |
| 13 | As 2.284 falas eram embaralhadas a cada pedido (`OrderBy(random)`) | Indexadas por `ActorType` no load; varredura circular a partir de um ponto aleatório |

## Novo no RF

- `ActorType.SHIPWRIGHT` (mapeado de `Occupation.ShipWright`) e condição `IN_PORT`,
  com 12 falas de porto/estaleiro escritas para o RF (chaves `RFAS0001`–`RFAS0012`).
  O `_isInPort` do mod original era calculado e nunca usado.
- `SEA_ROW` / `SEA_STANDING` desinvertidos: quem está preso a um objeto usável no navio
  (remo, balista) é o remador. **Vale conferir em jogo.**
- `VisibleDistance` configurável (antes eram 10 m fixos no código).

## Arquivos

```
RF_AliveScenes/
  SubModule.cs                     portaria: decide as cenas, monta o BattleSetup, registra a view
  Config/Settings.cs               opções no MCM (AttributeGlobalSettings)
  Config/AliveScenesSettings.cs    fachada: lê do MCM, cai nos defaults se ele faltar
  Data/SpeechEnums.cs              ActorType, SpeechCondition, SpeechFrequency
  Data/ConversationBank.cs         banco imutável indexado por ator + SpeechContext
  Data/SpeechSession.cs            estado por missão + avaliação das condições
  Runtime/BattleSetup.cs           retrato da batalha
  Runtime/SpeechDirector.cs        cérebro: classifica, escolhe, formata, controla cooldown
  Missions/AliveScenesMissionLogic.cs   tick + sorteio de quem fala
  Missions/CrowdMissionLogic.cs         figurantes extras (reescrito)
  UI/AliveScenesBubbleView.cs      camada Gauntlet
  UI/BubbleLayerVM.cs, UI/BubbleVM.cs
  UI/Widgets/RFAliveScenesBubbleWidget.cs, RFAliveScenesScreenWidget.cs
```

Assets no módulo RealmsForgotten:

```
_Module/ModuleData/RFAliveScenes/ConversationData.xml    banco de falas
_Module/GUI/Prefabs/RFAliveScenesBubble.xml              prefab do balão
_Module/GUI/Brushes/RFAliveScenesBrushes.xml             brush do texto
```

## Configuração — MCM

Todas as opções vivem no MCM, em **RF Alive Scenes**, divididas em quatro grupos:

Os rótulos são em inglês, como o resto da UI do mod.

| Grupo (no MCM) | Opções |
|---|---|
| General | chave mestra, tempo de leitura por caractere, distância de audição, filtro de palavrão |
| Towns, Villages and Taverns | liga/desliga, espera por figurante (40 s), falas simultâneas (5), chance por pessoa (55%), taverna liga/desliga + espera própria (15 s) |
| Battle and Sea | liga/desliga, espera por soldado (7 s), falas simultâneas (3), chance por soldado (55%), falar durante o combate, falas do inimigo |
| Crowds | liga/desliga, clones por ponto mín/máx (1–2), teto de figurantes por cena (40) |

Nenhuma exige reiniciar; valem na cena seguinte (chance e tempo de leitura valem na hora).
Não existe mais XML de configuração — se o MCM estiver ausente, o módulo roda com os
defaults acima em vez de quebrar.

## Checklist de teste em jogo

1. **Cidade** — entrar no centro: figurantes soltando falas em balão; multidão um pouco
   mais cheia que o normal (teto de 40 extras).
2. **Taverna** — cooldown menor (15 s), falas de `IN_TAVERN`.
3. **Salão do lorde** — falas de `IN_KEEP`.
4. **Porto (War Sails)** — as falas novas `RFAS0007`–`RFAS0012` e o estaleiro (`SHIPWRIGHT`).
5. **Vila** — `ADULT_VILLAGE`, `FARMING` (quem está colhendo).
6. **Batalha campal** — falas a cada ~22 s; inimigo em vermelho; desligar "Enemy lines"
   no MCM e confirmar que sumiu.
7. **Batalha naval** — falas a cada ~42 s; conferir se `SEA_ROW` sai na boca dos
   remadores e `SEA_STANDING` no resto (item invertido no original).
8. **Cerco** — `SIEGE_ATTACKER` / `SIEGE_DEFENDER`.
9. **Não deve aparecer** em: custom battle, arena/torneio, esconderijo, beco, missão
   disfarçada, raide, Fourberie.
10. **MCM** — abrir Opções > Mod Options > "RF Alive Scenes": os quatro grupos devem
    aparecer; desligar a chave mestra e confirmar que a cena seguinte fica muda.
11. Log do jogo: procurar `[RF_AliveScenes]` — deve ter "Banco carregado: 2296 falas, 145 dialogos."

## Crédito

Conteúdo de falas e o desenho original do sistema vêm do mod **Alive Scenes**
(`com.bloc.alivescenes`). Se o RF for redistribuído com isso, vale o mesmo cuidado de
crédito/permissão dos outros conteúdos emprestados.
