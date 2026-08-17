# RF_CoopCompat — camada de compatibilidade Realms Forgotten × Bannerlord Coop

Data: 2026-08-15. Baseado na análise do repositório de desenvolvimento do Coop
(`BannerlordCoop-development`) e no inventário completo da superfície do RF.
Complementa o `ESTUDO_COOP_BATTLE.md` (que focava batalha custom 2P; este plano
é sobre a campanha coop com o RF carregado).

## O que foi descoberto (fatos que moldam o design)

### Lado Coop
1. **Validação de módulos**: sem whitelist/hash. O servidor compara a lista de
   módulos ativos (Id + versão) com a de cada cliente e recusa qualquer
   divergência. → **O RF pode entrar numa sessão coop, desde que instalado
   idêntico (mesma versão) no host e em todos os clientes.** Isso inclui o
   RF_CoopCompat: mesma versão nos dois lados.
2. **DLC oficial é bloqueado incondicionalmente** (`ValidateNoDlc`), inclusive
   War Sails/NavalDLC — mesmo idêntico nos dois lados. Sessão coop = War Sails
   desligado. O RF tolera a ausência do DLC (patches navais são opcionais e
   guardados — `WarSailsPatchRegister.TryApply` loga "skipped"), mas isso
   precisa ser validado em jogo.
3. **O Coop só se ativa quando a sessão inicia**: `GameInterface.PatchAll()` é
   chamado dentro de `StartAsServer`/`StartAsClient`. Com o Coop instalado mas
   sem sessão, o jogo é singleplayer puro. → gatilho perfeito para a camada de
   compatibilidade (postfix em `PatchAll`/`UnpatchAll`).
4. **Behaviors de mod terceiro não são tocados**: o Coop desliga ~125 behaviors
   vanilla um a um (prefixo em `RegisterEvents`, 75 desligados nos dois lados,
   41 com gate `ModInformation.IsServer`), mas behaviors do RF rodariam soltos
   nas duas máquinas → desync garantido. O padrão de desligamento do Coop é
   exatamente replicável de fora.
5. **Save nativo viaja íntegro**: o save transferido ao cliente é o save normal
   do Bannerlord (InMemDriver + compressão + chunks). `SyncData`,
   `SaveableTypeDefiner` e `[SaveableField]` do RF chegam corretos ao cliente.
   E desligar `RegisterEvents` NÃO afeta `SyncData` — o save continua íntegro.
6. **Batalhas coop reconstroem a lista de MissionBehaviors do zero**
   (`CoopFieldBattleLauncher` etc.). Mods que injetam lógica via patch em
   `OpenBattleMission` são contornados; o hook `OnMissionBehaviorInitialize`
   dos submódulos provavelmente ainda dispara (não confirmado contra a DLL).
7. **O host é um servidor dedicado sem party e sem MainHero no mapa.** Todo
   código do RF que assume `Hero.MainHero` pode quebrar NO HOST. Este é o maior
   risco em aberto da fase 1.
8. **AutoSync é fechado** a assemblies externos (varredura restrita a
   `GameInterface.dll`). Existem seams acidentais (`IAutoRegistry<T>` por
   varredura de AppDomain, `RuntimeTypeModel.Default` global), mas nada
   documentado/estável. Fase 3, se houver.
9. Ao encerrar a sessão, o Coop faz `harmony.UnpatchAll()` global — **voltar ao
   singleplayer depois de uma sessão coop exige reiniciar o jogo** (vale para o
   RF também; o RF_CoopCompat loga isso).

### Lado RF (inventário completo no relatório dos agentes)
- ~158 `CampaignBehaviorBase` em 18 projetos (108 só no Main; 57+ persistem
  estado), ~91+50 `AddBehavior`.
- ~335 atributos `[HarmonyPatch]` em 13 IDs Harmony distintos; os críticos
  mexem em `MobileParty`/`Clan`/`Hero`/`CampaignEventDispatcher`/behaviors
  vanilla (lista no relatório).
- ~105 MissionBehaviors/Logics; RF_BattleAI substitui o cérebro tático via
  patches em `TeamAIComponent` (ID `RealmsForgotten.BattleAI`).
- 13 `SaveableTypeDefiner`, ~209 tipos de save, 12 `PartyComponent` custom,
  `RFCustomSettlement : SettlementComponent`, ~28 quests.
- `RF_AIDialog` chama LLM local (Ollama, HTTP) e cria quests em runtime —
  impossível de sincronizar; desligado por completo em coop.
- Zero consciência de multiplayer no código do RF (`Hero.MainHero` em toda parte).

## O que foi construído (fase 1)

Módulo novo **`RF_CoopCompat/`** (projeto standalone, fora da .sln, deploy para
`Modules\RF_CoopCompat`). **Nenhum arquivo do RF ou do Coop é alterado** — tudo
em runtime, por Harmony e reflexão:

| Arquivo | Papel |
|---|---|
| `SubModule.cs` | Entry point; detecta o Coop, instala gates no menu principal, re-tenta o hook via tick |
| `CoopBridge.cs` | Reflexão para `Common.ModInformation.IsServer` e `GameInterface.GameInterface` — sem referência de compilação ao Coop |
| `CoopSessionHook.cs` | Postfix em `GameInterface.PatchAll`/`UnpatchAll` = sinal exato de sessão coop iniciada/encerrada |
| `BehaviorGater.cs` | Prefixo em `RegisterEvents` de todo CampaignBehavior do RF (mesmo padrão do Coop para o vanilla) + bloqueio de `OnMissionBehaviorInitialize` dos submódulos RF em sessão coop |
| `SessionUnpatcher.cs` | Remove patches Harmony do RF por ID de dono ao iniciar a sessão (`unpatch_client` / `unpatch_session`) |
| `CompatConfig.cs` + `_Module/rf_coop_compat.cfg` | Política por behavior (hostonly/everywhere/disabled), listas de unpatch, flags — ajustável sem recompilar |
| `CompatLog.cs` | Log próprio em `Modules/RF_CoopCompat/rf_coop_compat.log` |

Comportamento (REVISADO 2026-08-15 — estabilidade primeiro):
- **Sem Coop instalado / sem sessão coop**: dormente, zero efeito no RF.
- **Sessão coop ativa**: por padrão **todos os behaviors do RF ficam `disabled`**
  (dormentes). `SyncData` continua rodando nos dois lados → save íntegro e
  conteúdo RF presente. A partir daí, promove-se **um sistema por vez** para
  `hostonly`/`everywhere`, testando cada.

  **Por que não `hostonly` por padrão (correção importante):** o host do Coop é
  um servidor dedicado **sem `Hero.MainHero` e sem `MainParty`**. A maioria dos
  behaviors do RF assume MainHero — rodá-los no host os faria **crashar**. Logo
  `hostonly` só é seguro para simulação de MUNDO comprovadamente livre de
  MainHero; o baseline seguro é tudo desligado.
- MissionLogic do RF não é injetada em missões durante a sessão
  (`disable_mission_logic true`) — magia/dual wield/promoted ficam fora de
  batalha coop na fase 1, em troca de estabilidade.
- No cliente, patches de estado listados são removidos (religião:
  `ChangeRelationAction.ApplyInternal` duplicaria efeitos replicados; sondas de
  diagnóstico). Nos dois lados: `RealmsForgotten.BattleAI` (validar depois).

## O que a fase 1 entrega, na prática

- Conteúdo RF (mapa, tropas, itens, raças, cenas, XMLs) funciona para todos —
  é dado, ambos os lados têm.
- Sistemas de mundo do RF (hordas, war system, bandidos, economia etc.) rodam
  no host; os efeitos que tocam estado vanilla sincronizado (parties, guerras,
  posses) chegam aos clientes pelo AutoSync do Coop.
- Estado *próprio* do RF (ex.: mana/`ExtendedInfoManager`, livros de guerra do
  war system) NÃO é replicado em tempo real — o cliente só recebe o snapshot do
  save ao entrar. Menus/quests/dialogos RF não aparecem para o cliente (o
  registro de eventos é host-only). É a troca aceita na fase 1.

## Roteiro de teste (fase 1)

1. **Sanidade SP**: jogo com RF + RF_CoopCompat, SEM Coop → conferir no log
   `coop module not active; RF_CoopCompat dormant`; RF intacto.
2. **Sanidade SP com Coop instalado mas sem sessão** → log mostra gates
   instalados mas `coop session STARTED` nunca aparece; RF intacto.
3. **Host + 1 cliente** (mesmas versões de tudo; War Sails desligado):
   - subir servidor, conferir `coop session STARTED, role=server`;
   - cliente conecta (validação de módulos passa?), recebe save;
   - conferir `role=client` + linhas `gate: skipped RegisterEvents ...`;
   - jogar 30+ min de campanha: crashes no host? (candidatos: behaviors com
     `Hero.MainHero` → mover para `disabled` no .cfg, iterar);
   - `Coop_server.log`/`Coop_client.log`: procurar `Failed to get id`.
4. **Batalha coop** com RF carregado: entra, termina, comita resultado?
5. Iterar o `.cfg` (é o ciclo esperado: playtest → NRE/desync → política).

## Fases futuras

- **Fase 2 — curadoria fina**: revisar os ~335 patches um a um (quais precisam
  de gate `IsServer`, quais respeitam o caminho de recepção do Coop), liberar
  `everywhere` para behaviors de menu/diálogo comprovadamente locais, decidir
  RF_BattleAI em batalha coop (o modelo dono/fantoche roda a IA de cada lado só
  na máquina do dono — pode até ser compatível).
- **Fase 3 — sincronização real de estado RF**: replicar `ExtendedInfoManager`
  (magia) e afins para os clientes. Exige ou os seams não-documentados do Coop
  (`IAutoRegistry<T>`, `RuntimeTypeModel.Default`) ou colaboração com os
  mantenedores. Não começar sem a fase 1/2 estável.

## Licença — importante

O BannerlordCoop é **source-available desde 17/06/2026** (não open source): o
NOTICE proíbe copiar/reutilizar o código em mods de multiplayer/sync derivados
sem permissão escrita. O RF_CoopCompat **não copia código do Coop** — só
observa em runtime (reflexão/Harmony) e replica um *conceito* (gate de
`RegisterEvents`) sobre os behaviors do próprio RF. Ainda assim, antes de
**distribuir publicamente** qualquer versão coop-compatível, o recomendado
continua sendo pedir permissão/benção aos mantenedores (GitHub/Discord do
Bannerlord-Coop-Team) — também porque a fase 3 depende de boa vontade deles.
