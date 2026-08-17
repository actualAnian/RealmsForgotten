# RF Coop Warsails

Modo cooperativo próprio para o Realms Forgotten, construído do zero (sem reusar
código do BannerlordCoop). Objetivo de longo prazo: batalha entre 2 jogadores,
com War Sails e conteúdo RF. Construído por marcos testáveis.

## Base legal

- **Nenhum código do BannerlordCoop é usado.** O transporte de rede é a
  biblioteca **LiteNetLib** (MIT, github.com/RevenantX/LiteNetLib), obtida
  direto do NuGet — biblioteca independente, sem relação com o coop.
- Os *conceitos* de coop (host/join, dono/fantoche, pacotes de movimento) não
  têm copyright e podem ser reimplementados livremente. O que é protegido é o
  *código-fonte* pós-17/06/2026 do coop, que não tocamos.
- Referência de leitura da era MIT do coop ficou só no scratchpad da sessão,
  fora do repositório.

## Marco 1 (ATUAL) — transporte P2P

Prova que dois PCs se conectam e trocam dados, sem nada de campanha/batalha.

Arquivos:
- `CoopNet.cs` — wrapper LiteNetLib: host abre UDP, client conecta, handshake +
  heartbeat, connection key para não conectar versões diferentes.
- `CoopController.cs` — mantém a sessão viva, manda heartbeat com contador ~1/s,
  reporta o contador recebido.
- `SubModule.cs` — 3 opções no menu principal (Hospedar / Conectar / Encerrar).
- `CoopLog.cs` — log em `Modules/RF_CoopWarsails/rf_coop_warsails.log` + mensagens
  na tela.

### Como testar (2 PCs, ou 2 cópias no mesmo PC)

1. Ative **RF Coop Warsails** no lançador nos dois lados (ordem: depois de
   Native/SandBox). Não precisa do RF nem do Coop para este teste.
2. **PC A (host)**: no menu principal, clique **"RF Coop: Hospedar"**.
   → mensagem verde "hospedando em UDP 4300; aguardando...".
3. **PC B (cliente)**: clique **"RF Coop: Conectar"**, digite o IP do PC A.
   - Mesmo PC / teste local: `127.0.0.1`.
   - Mesma rede (LAN): o IPv4 do PC A (`ipconfig` → algo como `192.168.x.x`).
   - Internet: exige encaminhar a porta **UDP 4300** no roteador do host, ou
     usar uma VPN de LAN (Radmin/Hamachi/ZeroTier). NAT punch-through vem num
     marco futuro.
4. Os dois devem mostrar **"CONECTADO"** e, em seguida, linhas periódicas
   "recebendo de 'NOME-DO-PC': tick #N" com N subindo → **rede bidirecional OK**.
5. **"RF Coop: Encerrar sessao"** fecha a conexão.

### Diagnóstico
- Nada acontece ao conectar → firewall do Windows bloqueando UDP 4300 (libere o
  Bannerlord na primeira vez, ou abra a porta), ou IP errado.
- `rf_coop_warsails.log` na pasta do módulo registra cada passo.

## Marco 2 (EM TESTE) — sincronização de movimento

Cada jogador vê o avatar do outro se mover na mesma cena. Arquivos:
- `AgentStateSnapshot.cs` — estado do agente (posição/direção/olhar/input/vida) +
  lógica de `Apply` (teleporta se distante). **Adaptado do BannerlordCoop** com
  permissão (ver `THIRD_PARTY_NOTICES.md`); serialização própria via LiteNetLib.
- `CoopMissionSync.cs` — MissionBehavior anexado a toda missão: envia `Agent.Main`
  ~30 Hz e spawna/atualiza um "fantoche" para o outro jogador.
- `CoopNet` ganhou o pacote de estado de agente (canal Sequenced, latest-wins).

### Como testar o marco 2 (2 PCs)

Este marco NÃO tem lançador de cena automático ainda — vocês entram na mesma
missão manualmente. Precisa de um save de campanha (para ter personagem/teams).

1. Conectem no menu principal (marco 1): um **Hospedar**, outro **Conectar**.
   Confirmem "CONECTADO" + ticks subindo.
2. **Sem desconectar**, os dois carregam um save e iniciam a MESMA situação de
   missão (ex.: os dois entram numa arena da mesma cidade, ou provocam a mesma
   batalha). O importante é os dois estarem numa missão ao mesmo tempo.
3. Cada lado deve ver a mensagem "fantoche do outro jogador spawnado" e um
   segundo personagem se movendo conforme o outro jogador anda.

### O que ainda NÃO funciona no marco 2 (esperado)
- Aparência do fantoche = personagem do jogador local (sync de aparência é marco
  futuro).
- Sem sincronização de ataque/dano/morte/equipamento/montaria (marcos 3-4).
- Sem lançador de cena compartilhada automático (host escolher cena + mandar
  para o cliente) — é o próximo endurecimento.
- Provável necessidade de ajuste após playtest (spawn timing, teams). Reporte o
  que aparece no `rf_coop_warsails.log`.

## Próximos marcos (planejados)

2b. **Lançador de cena compartilhada** — host escolhe uma cena (custom battle) +
   seed, envia ao cliente, os dois abrem a MESMA missão automaticamente.
3. **Dono/fantoche completo** — cada jogador é dono dos próprios agentes; os do outro são
   fantoches locais alimentados por pacotes. Ações e morte replicadas.
4. **Dano com autoridade** — dano em fantoche vai ao dono, que aplica e replica.
5. **Batalha custom 2P** — montar a batalha a partir de uma tela tipo custom
   battle (rosters escolhidos), com as peças de rede acopladas.
6. **War Sails / naval** — a batalha naval como diferencial, depois que a
   batalha terrestre estiver estável.
7. **Conteúdo RF** — validação de que tropas/itens/cenas do RF são idênticos nos
   dois lados (hash de módulos) e magia/etc. em batalha.

Cada marco é testável isoladamente antes do próximo.
