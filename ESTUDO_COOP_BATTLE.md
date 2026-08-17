# Estudo: Bannerlord Coop (workshop 3770450698) e o caminho para batalha 2-jogadores

Data: 2026-08-03. Fonte decompilada em `.tmp_coop_decompiled\` (ilspycmd).
Mod: "Coop" v0.1.1, alvo Bannerlord **1.4.7** (a mesma versão do RF — sorte grande).
Projeto de origem: BannerlordCoop (caminhos de CI `/__w/BannerlordCoop/BannerlordCoop/` nos
logs — é o projeto open-source oficial, versão MUITO mais avançada que a pública antiga).

## O que o mod realmente consegue

Não é só campanha sincronizada. **Ele já tem batalha em tempo real entre vários
jogadores** — o que o Coop público histórico nunca entregou. Duas camadas independentes:

### Camada 1 — campanha (client-server, autoritativo no host)
- Servidor (dedicado ou o próprio host) é dono do estado da campanha inteira.
- ~400 serviços de sincronização em `GameInterface.Services.*` (heróis, clãs, reinos,
  caravanas, vilas, torneios, smithing, casamento, TUDO), cada um com o trio
  Patches (intercepta) / Messages (replica) / Handlers (aplica).
- Dezenas de `Patches.Disable` que DESLIGAM behaviors vanilla no cliente — o cliente
  não simula a campanha, só recebe.
- Roslyn embarcado (`Microsoft.CodeAnalysis`, 12 MB): o `GameInterface.AutoSync`
  GERA código de sincronização em runtime a partir de builders.
- Save transferido em chunks do servidor para quem entra.

### Camada 2 — batalha (P2P entre os participantes!) — `Missions.dll`, 638 KB
O achado central. Quando dois jogadores caem no mesmo MapEvent:

1. `BattleMissionStartHandler` (servidor): valida, sorteia **seed de terreno**
   (0..9999) e snapshot de **atmosfera**, e manda `NetworkStartAttackMission` /
   `NetworkStartSiegeMission` para todos os participantes. Todos geram a MESMA cena.
2. `CoopFieldBattleLauncher` / `CoopSiegeBattleLauncher`: recriam a lista de
   MissionBehaviors do vanilla (deployment, formações, moral, reforços...) trocando
   as peças de spawn por `CoopTroopSupplier` + `CoopBattleMissionSpawnHandler`.
3. `MissionManager` (servidor): cria a INSTÂNCIA da missão e faz **NAT punch-through**
   entre os participantes (`NatIntroduce`) — o tráfego de batalha é **P2P direto**
   via `LiteNetP2PClient`, não passa pelo servidor. Fallback/alternativa Steam:
   `SteamMissionBridge` + túneis SteamNetworking (sem port forward) + lobby Steam
   completo (`SteamLobbyBrowser/Advertiser/JoinListener`).
4. Modelo **dono/fantoche** por agente:
   - `OwnedAgentReplicator` — cada jogador é DONO dos agentes do seu lado; a IA
     deles roda só na máquina do dono; ele transmite spawn/movimento/ações.
   - `PuppetSpawner` / `PuppetDeathApplier` / `PuppetRoutApplier` — os agentes dos
     outros são fantoches locais, alimentados por pacotes.
   - Pacotes: `MovementPacket`, `CompressedMovementPacket` (+compressor),
     `MountMovementPacket`, `AgentActionPacket`, `AgentEquipmentPacket`.
5. **Dano com autoridade**: `BattleDamageRouter` — dano em fantoche vira
   `BattlePuppetHit` → vai ao dono → dono aplica e replica `NetworkApplyBattleDamage`.
   Tem fila de dano diferido, reconstrução de projétil (`MissileReconstructed`,
   posição+velocidade+tempo de voo restante) e janela de guarda anti-duplicata.
6. **Host da batalha com época**: `BattleSession` (IsLocalHost, HostEpoch),
   `HostEpochPolicy` (descarta mensagem de época velha), `BattleAuthorityMigrator`
   (se o host da batalha sai, outro assume). `ServerBattleModeArbiter` impede
   auto-resolve e missão real ao mesmo tempo no mesmo MapEvent.
7. Resultado: `CasualtyAttribution(Map)` atribui baixas, `BattleResultCommitter`
   comita no estado da campanha ao final. `BattleAgentBudget` limita spawn.

### Detalhes operacionais
- UDP 4200-4201 no host (modo IP direto); Steam dispensa.
- `Server Instructions.md` manda **desabilitar a DLL do War Sails** — incompatível hoje.
- SubModule depende só dos módulos vanilla 1.4.7.
- Config de dificuldade própria em `mod-config.default.json`.

## O breakthrough para o objetivo do autor

Objetivo: **batalha entre 2 pessoas estilo custom battle** (não campanha co-op).

Insight principal: a Camada 2 é praticamente independente da Camada 1. O que a
batalha consome da campanha é pouco e enumerável:
- um MapEvent com dois lados e rosters       → num custom battle: rosters escolhidos na UI
- seed de terreno + atmosfera                → qualquer inteiro combinado + default
- CoopTroopSupplier (lê rosters do MapEvent) → substituir por supplier de roster estático
- BattleResultCommitter (comita na campanha) → simplesmente NÃO EXISTE em custom battle
- MissionManager/NAT ou SteamMissionBridge   → igual, é agnóstico de campanha

Ou seja: **um "Coop Custom Battle" é a Camada 2 menos a campanha** — launcher tipo
custom battle (o vanilla `CustomBattle` module monta MissionInitializerRecord a
partir da UI), + instância P2P, + dono/fantoche, + damage router. As peças mais
difíceis (netcode de agente, autoridade de dano, reconstrução de míssil, migração
de host) JÁ ESTÃO RESOLVIDAS e decompiladas aqui.

### Caminhos possíveis, do menor para o maior esforço
A. **Usar o mod como está** (campanha co-op) e "batalhar entre amigos" atacando a
   party um do outro dentro da campanha co-op. Zero código, mas: sem conteúdo RF
   (a coop desliga/patcheia centenas de behaviors; nossos mods de campanha não são
   sincronizados; War Sails precisa ser desligado). Serve como EXPERIÊNCIA de
   referência: jogar uma batalha coop vanilla para sentir o netcode.
B. **Custom battle standalone 2P** (o breakthrough): módulo novo que embute a
   arquitetura da Camada 2 com um lobby simples (Steam ou IP). Sem campanha, o
   problema de compatibilidade com RF cai para: os XML de tropas/itens do RF
   precisam estar iguais nos dois lados (validável por hash tipo
   `GameInterface.Services.Modules.Validators`). RF_Magic em batalha exigiria
   sincronizar conjuração (evento de cast → replicar TryCast no fantoche) — o
   transporte (`IBattleNetwork.SendAll`) já é o lugar óbvio.
C. **Campanha co-op RF completa** — não é objetivo agora; escala de anos.

### LICENÇA — VERIFICADO 2026-08-03 (muda o plano!)
Repo: github.com/Bannerlord-Coop-Team/BannerlordCoop (6.800+ commits, ativo HOJE).

- **17/06/2026 (commit `44c15bc00`): licença mudou de MIT para source-available.**
  O NOTICE.md proíbe expressamente copiar/redistribuir/usar "in competing Mount &
  Blade II: Bannerlord multiplayer, co-op, networking, synchronization, or
  derivative mods" sem permissão escrita. Contexto: surgiu um concorrente
  ("Bannerlord Together", Nexus 10426).
- **A camada de batalha madura é TODA pós-corte**: CoopFieldBattleLauncher
  24/06, OwnedAgentReplicator/BattleDamageRouter/PuppetSpawner/BattleSession
  02/07. → NÃO pode ser reutilizada sem permissão escrita.
- **A era MIT (até `827b114aa`, 17/06/2026) continua MIT** e contém a FUNDAÇÃO:
  - `Missions/Services/Network/LiteNetP2PClient.cs` — transporte P2P
  - `AgentMovementHandler`, `MovementPacket`, `AgentDamageHandler`,
    `NetworkDamageAgent`, dano de escudo — sync de agente funcional (era a
    demo de taverna/arena do projeto)
  - **`source/MissionTestMod/`** — A JOIA: mod de teste que põe botões no MENU
    PRINCIPAL ("Join Online Tavern", "Join Online Arena", "Join Online Battle"
    — este último desativado com TODO) e entra numa missão em rede SEM
    campanha. É exatamente o esqueleto do que o autor quer.

### Plano recomendado (3 frentes)
1. **Pedir permissão escrita** aos mantenedores (issue no GitHub / Discord
   deles) para usar a camada de batalha moderna num modo custom-battle do RF.
   Argumento: não compete com o produto deles (campanha co-op); é outro caso de
   uso. Melhor cenário: ganhamos o netcode maduro.
2. **Plano B legal e sólido**: fork do commit MIT `827b114aa`. Base =
   MissionTestMod + LiteNetP2PClient + handlers de agente. Construir por cima o
   que é pós-corte: modelo dono/fantoche, handshake de roster (CustomBattleData),
   deployment, reconstrução de míssil. Conceitos podem ser reimplementados
   (ideias não têm copyright); código pós-corte NÃO pode ser copiado.
3. **Teste de viabilidade** independe de tudo: jogar o mod do workshop como está
   (campanha coop vanilla, 2 pessoas, provocar batalha conjunta) para medir a
   qualidade real do netcode antes de investir.

### Riscos/verificações antes de codar
- O quanto `Missions.dll` referencia `GameInterface` (mensageria/DI): o extrato
  standalone precisa levar `Common` (broker/pacotes) + um recorte de GameInterface
  (ObjectManager vira desnecessário sem campanha; IDs de agente são Guid próprios).
- Testar a estabilidade real do netcode deles ANTES de investir: item A serve para isso.

## A outra metade: como o custom battle vanilla monta uma batalha

Confirmado no vanilla decompilado (MEGA_012, `TaleWorlds.MountAndBlade.CustomBattle`):

```
CustomBattleState (tela) → CustomBattleData (PlayerParty/EnemyParty com rosters,
  SceneId, geral/sargento, estacao, hora)
  → CustomBattleHelper.StartGame(data)
    → BannerlordMissions.OpenCustomBattleMission(...)  (campo)
    → BannerlordMissions.OpenSiegeMissionWithDeployment(...)  (cerco)
```

Sem campanha em lugar nenhum: `CustomBattleData` é autossuficiente. O
`CoopFieldBattleLauncher` do Coop e o `OpenCustomBattleMission` do vanilla montam
QUASE a mesma lista de behaviors — a diferenca e o supplier de tropas (MapEvent vs
roster estatico) e as pecas de rede anexadas.

### Esqueleto do "RF Coop Custom Battle" (2 jogadores)

1. **Lobby** — tela de setup igual a do custom battle + painel host/join
   (Steam tunnel do Coop dispensa port forward; IP direto como alternativa).
2. **Handshake** — o host monta o `CustomBattleData` + seed de terreno e envia ao
   convidado; validacao de modulos por hash (padrao ja existe no Coop:
   `Services.Modules.Validators`). Ambos constroem a MESMA missao localmente.
3. **Missao** — behaviors do custom battle vanilla + as pecas de rede do Coop:
   `LiteNetP2PClient`/túnel Steam, dono/fantoche (`OwnedAgentReplicator`/`PuppetSpawner`),
   `BattleDamageRouter`, `BattleSession` (um dos dois e o host da batalha).
4. **Posse dos agentes** — PvP: jogador A e dono do lado A, jogador B do lado B
   (a IA das tropas de cada lado roda na maquina do dono). Coop-vs-IA: um lado
   dividido entre os dois, o host e dono da IA inimiga. A arquitetura do Coop ja
   suporta jogadores em lados OPOSTOS (varios players num MapEvent com atacante e
   defensor distintos).
5. **Fim** — tela de resultado do custom battle; NAO ha commit de campanha.

### Adaptacoes necessarias (as quatro reais)
- `CoopTroopSupplier` le MapEvent → escrever supplier que le o CustomBattleData
  trocado no handshake (o vanilla `CustomBattleTroopSupplier` e o molde).
- IDs: em batalha os agentes usam Guid proprio (`NetworkAgentRegistry`) e os
  personagens usam StringId de XML — nada disso precisa do ObjectManager de
  campanha; o recorte standalone leva `Common` + `Missions` e deixa o resto.
- O papel do `MissionManager` (servidor de instancias/NAT) colapsa: com 2
  jogadores o proprio host da partida e a instancia.
- RF_Magic: replicar conjuracao como evento (cast → `IBattleNetwork.SendAll` →
  TryCast no fantoche). Sem isso, magia so funciona no lado do dono.

## Mapa de arquivos-chave (no decompilado)
- Entrada/DI: `Coop\CoopMod.cs`, `Missions\MissionModule.cs`
- Servidor de instâncias: `Coop.Core\...\Instances\MissionManager.cs` (NAT punch)
- Início de batalha: `GameInterface\...\MapEvents\Handlers\BattleMissionStartHandler.cs`
- Launchers: `Missions\Missions.Battles\Coop{Field,Siege}BattleLauncher.cs`
- Dono/fantoche: `OwnedAgentReplicator.cs`, `PuppetSpawner.cs`, `NetworkAgentRegistry.cs`
- Dano: `BattleDamageRouter.cs` (+ `Missions.Missiles.*`)
- Transporte: `Missions.Services.Network\LiteNetP2PClient.cs`,
  `Coop.Steam\SteamMissionBridge.cs` (+ lobby Steam completo)
- Host/época: `BattleSession.cs`, `HostEpochPolicy.cs`, `BattleAuthorityMigrator.cs`
