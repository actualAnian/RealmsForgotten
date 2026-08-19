# Audit de performance — processos periódicos do RF (2026-08-18)

Motivo: queda forte de FPS relatada em jogo. Este audit varreu **tudo que roda por
frame ou por tick** na solution (`OnApplicationTick`, `CampaignEvents.TickEvent`,
`HourlyTick(Party)`, `DailyTick`, `OnMissionTick`, patches por frame) e classificou por
custo real. Onde havia ganho seguro, já foi corrigido.

## O que foi corrigido agora

| Onde | Problema | Correção |
|---|---|---|
| `CaravanTradePactBehavior` (RealmsForgottenMain) | Cada caravana, a cada hora de jogo, refazia `Town.AllTowns.Where(...).OrderByDescending(...)` — ~100 caravanas × ~100 cidades com sort, toda hora, mesmo sem nenhum pacto ativo | Cache por facção renovado 1x por hora de campanha (`FindBestPactTown`); o resultado nulo ("sem pacto") também é cacheado, que era o caso comum |

Build limpo, DLL implantada. Comportamento idêntico: a caravana continua desviando para
a melhor cidade com pacto — só não recalcula a mesma resposta cem vezes por hora.

## Suspeito pesado que NÃO é custo vivo

- `RFCampaignAITraceBehavior` — por lorde/hora varria `MobileParty.All` (~1000) +
  `Settlement.All` (~600) **antes** de checar se o log estava ligado. Seria o item nº 1
  deste audit, mas está **comentado** no `AiSubModule.cs:94`. ⚠️ Se um dia for reativado
  para depurar IA de campanha, mover a checagem `RFLogSwitchboard.IsEnabled` para o topo
  do `OnHourlyTickParty` antes do `CaptureState`.

## Auditado e saudável (não mexer)

**Por frame no mapa/app (`OnApplicationTick` / `TickEvent`)** — todos com early-exit barato:
- `RealmsForgotten.SubModule` → BannerWorks hotkey + `RFPoliticalMapManager` (o renderer
  interno se auto-limita: checagem de dono a cada 2 s, rebuild só quando o fingerprint muda);
- `RFReligions` (leitura de tecla R), `RF_AIDialog` (drena fila de ações da thread de
  áudio — vazia quase sempre), `RF_Homesteads` (flags pendentes + HUD que só faz trabalho
  com homestead em movimento), `RF_CoopCompat`/`RF_CoopWarsails` (inertes sem sessão coop);
- os 16 listeners de `CampaignEvents.TickEvent` (quests, Enlistment, Promoted, Ambush,
  Faith, Religion...) são todos `if (flag) return;` — custo desprezível.

**Por party/hora (`HourlyTickPartyEvent`)**:
- `AIBreakInBehavior` — já otimizado em ciclo anterior (só olha `SiegeEvents`, com
  early-exit quando não há cerco);
- `BanditIncrease`, `ArmyCommand`, `MercenaryFactionContract` — lookups baratos.

**Missões/batalha (`OnMissionTick`)**:
- Quests (Seventh/Eighth/Ninth etc.) — só flags;
- `InfectionManager` — fila com orçamento de 8 por tick;
- RF_AliveScenes — varredura a cada 7 s (cidade) / 22 s (batalha) / 42 s (naval), nunca
  por frame;
- RF_BattleAI — `Tick()` estáticos já limitados (auditados em ciclo anterior).

## Custos reais restantes, em ordem — e qual botão aliviar

Nenhum destes é bug; é feature paga em FPS. Num laptop fraco, é aqui que se ganha:

1. **Agentes extras em cena** (o maior custo em cidade, disparado). Cada agente custa
   animação+cloth+IA. Multidão do RF_AliveScenes: **MCM → RF Alive Scenes → Crowds** —
   baixar `Hard cap per scene` de 40 → 10–15, ou desligar. Se a queda for em cidade, o
   primeiro teste é esse.
2. **RF_Magic `StatusEffectMissionLogic`** — itera todos os agentes por frame
   (`GetComponent` + verificação de aura de capitão). Aceitável em batalha média, sente-se
   em batalha de 500+. Otimização possível (tick de efeitos a cada 0,1 s em vez de por
   frame), mas RF_Magic está sob regra "não tocar sem necessidade" — fica anotado, não mexido.
3. **RF_PartyVisuals** — reposiciona figuras decorativas por frame no mapa (culled por
   distância). **MCM → RF Party Visuals**: `View distance` 45 → 25 e
   `Extra troop figures` 6 → 3 em laptop fraco.
4. **Batalha: tamanho da batalha do próprio jogo** — RBM_RF + RF_Magic + AliveScenes
   somam por agente; reduzir o battle size nas opções do jogo multiplica o alívio de tudo.

## Checklist do lado do laptop

Antes de culpar o mod, vale confirmar (a "situação atual do laptop" que você citou):
1. **Térmico**: notebook estrangulando por calor derruba FPS de forma idêntica a mod
   pesado. HWMonitor/Afterburner: CPU acima de ~95 °C sob jogo = throttling.
2. **Energia**: plano do Windows em "Balanced" no cabo limita clock — usar High
   Performance; conferir se o jogo está na GPU dedicada (Configurações > Vídeo > Preferência de GPU).
3. **No jogo**: battle size é o multiplicador dominante; sombras e cloth simulation logo atrás.
4. Se o FPS caiu *de repente* sem mudança de mod, suspeitar de driver novo da GPU ou de
   outro processo (antivírus indexando, Windows Update).

## Como medir de verdade (próximo passo se persistir)

Teste A/B por módulo, 2 minutos cada, mesma cena (uma cidade grande + uma batalha 500):
1. baseline com tudo ligado (anotar FPS médio);
2. desligar Crowds no MCM → mesma cena;
3. `View distance` do PartyVisuals em 25 → mapa;
4. se sobrar suspeita, desmarcar RBM_RF e comparar batalha.
O que mais devolver FPS é onde investir a próxima rodada de otimização.
