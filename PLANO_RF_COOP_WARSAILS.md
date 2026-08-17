# RF Coop Warsails — nosso modo multiplayer próprio (base MIT)

Data: 2026-08-15. Continuação de `PLANO_RF_COOP_COMPAT.md` (compat com o Coop
oficial) e do `ESTUDO_COOP_BATTLE.md` (que definiu este caminho como "Plano B").

## A decisão e o porquê

Pedir permissão aos mantenedores do Coop está travado (ocupados). Portar o
código **atual** deles é proibido pela licença source-available (17/06/2026) e
colocaria o RF em risco de takedown. Mas:

- **Todo o histórico até o commit `827b114aa51392e34073c87890f80ee05634501d`
  (17/06/2026, "Fixed launch config") é MIT** — verificado: é o pai direto do
  commit "License Update" (`44c15bc00d…`, 18/06/2026). Fork legal, sem pedir
  nada a ninguém, com atribuição preservada (arquivo LICENSE MIT junto).
- Num mod de rede NOSSO, o bloqueio de War Sails não existe — aquela regra é do
  validador do Coop, não do jogo. **War Sails pode ficar ligado.**
- A era MIT já continha uma demo funcional de missão em rede SEM campanha:
  `MissionTestMod` (botões "Join Online Tavern/Arena/Battle" no menu principal)
  + sync de agentes (movimento, dano, equipamento, mísseis) + LiteNetLib P2P.

## O que NÃO vamos fazer

- Copiar/portar qualquer código pós-17/06/2026 do repositório do Coop
  (BattleDamageRouter, OwnedAgentReplicator, launchers de batalha etc.).
  Conceitos (dono/fantoche, autoridade de dano no dono, seed de terreno
  combinada) são ideias — reimplementáveis do zero; código, não.
- Prometer "campanha coop completa". Isso é escala de anos (o Coop oficial tem
  ~400 serviços de sync). Nosso alvo é **batalha/missão multiplayer** com
  conteúdo RF e War Sails, crescendo por marcos.

## Base extraída (era