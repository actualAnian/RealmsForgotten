# Resource Zones — Plano de Execução

*Ideia #8 do backlog (2026-07-15). Zonas de coleta de recursos estilo Age of Empires:
minas/serrarias capturáveis pelas quais player e AI lutam, com produção real,
caravanas alimentando cidades e receita para o clã dono.*

---

## 1. Decisão de arquitetura: zona = MobileParty estacionária clan-owned

Três modelos foram avaliados:

| Modelo | Prós | Contras | Veredito |
|---|---|---|---|
| **A. Village injetada** (tech settlers pura) | Produção/caravana vanilla de graça | Dono da village = dono do feudo vinculado → **política de clã impossível**; village não é capturável isolada | ❌ |
| **B. Settlement custom** (tech homesteads) | Controle total | O Homesteads precisou de ~40 crash patches para settlement custom sobreviver aos dispatches vanilla | ❌ para MVP |
| **C. Party estacionária clan-owned** (tech settler CAMP) | Capturável NATIVAMENTE (parties são atacáveis; AI em guerra ataca sozinha); dono = `ActualClan` (política de clã de graça); garrison = MemberRoster; estoque = ItemRoster; **zero crash surface de settlement**; visual tenda+bandeira do dono já pronto | Sem nameplate de settlement (aceitável; nome aparece no hover) | ✅ **MVP** |

O modelo C reusa três sistemas provados no nosso código:
- **SettlerCampComponent** (RF_Settlers): party estacionária com `Ai.DisableAi()`, menu de encontro, tenda com bandeira.
- **SettlerCampVisualPatch**: ícone de tenda + bandeira do dono no mapa (patch aplicado TARDE — lição do fold).
- **SettlerCampEncounterPatch**: chegada não-hostil abre game menu em vez de conversa.

Evolução futura (Fase 4+): zonas tier Fortress podem ser promovidas a Settlement
custom (tech homesteads) se quisermos cena walkable — a camada de dados (record)
já é independente da representação.

### Zero patches Harmony novos
O projeto RF_ResourceZones **não adiciona nenhum patch**. Os dois patches dos
settlers foram generalizados pela interface `IRFStationaryCampParty` (definida no
RF_Settlers): qualquer PartyComponent que a implemente ganha tenda no mapa e
redirecionamento para o seu game menu. Disciplina do fold preservada: o patch de
`MobilePartyVisual` continua sendo aplicado uma única vez, tarde, pelo RF_Settlers.

## 2. Projeto novo: RF_ResourceZones

Justificativa: sistema grande e independente (manifesto, economia, captura, AI),
com ciclo de vida e save próprios. Projeto separado = build/iteração isolados e
nenhum risco de regressão nos settlers. Referencia `RF_Settlers` (interface) —
direção de dependência única RF_ResourceZones → RF_Settlers.

```
RF_ResourceZones/
├── RF_ResourceZones.csproj        # padrão RF_Settlers (net472/x64, BANNERLORD_GAME_DIR)
├── SubModule.cs                   # OnGameStart: registra behavior. SEM PatchAll.
├── ResourceZoneDefinition.cs      # modelo do manifesto (id, tipo, âncora, offsets, town)
├── ResourceZoneManifest.cs        # loader de ModuleData/rf_resource_zones.xml (gracioso se ausente)
├── ResourceZoneType.cs            # enum Gold/Iron/Wood/Charcoal/Silver + tabela de produção
├── ResourceZonePartyComponent.cs  # PartyComponent saveable; IRFStationaryCampParty
├── ResourceCaravanPartyComponent.cs # caravana da zona → cidade
├── ResourceZoneRecord.cs          # estado autoritativo saveable (dono, tier, ouro acumulado)
├── ResourceZonesCampaignBehavior.cs # núcleo: spawn/restore, produção, caravanas, captura, menus
├── ResourceZonesSaveDefiner.cs    # base 728850000 (único na solution — auditado)
└── RFZonesConsoleCommands.cs      # rf_zones.mark_zone → imprime linha XML na posição do player
```

## 3. Modelo de dados

**Manifesto (`ModuleData/rf_resource_zones.xml`)** — conteúdo, não save:
```xml
<ResourceZones>
  <!-- Âncora em settlement existente: posição = gate + offset, ajustada para
       navmesh válido via FindReachablePointAroundPosition. Sem coordenadas cruas
       (não dependemos de conhecer o RF_Map de cor) — e o comando de console
       rf_zones.mark_zone imprime a linha pronta com posX/posY absolutos. -->
  <Zone id="rf_zone_iron_01" type="Iron" name="Old Quarry Iron Mine"
        anchorSettlement="town_EN1" offsetX="6.0" offsetY="4.0"
        boundTown="town_EN1" />
  <Zone id="rf_zone_gold_01" type="Gold" name="Sunken Vein Gold Mine"
        posX="412.30" posY="881.70" boundTown="town_EN2" />
</ResourceZones>
```

**Record (save)** — estado autoritativo; a party é uma projeção reconstruível:
- `ZoneId` (string, casa com manifesto), `OwnerClan` (Clan ref), `Tier` (1-3),
- `StoredGold` (int, minas de ouro acumulam denars), `LastCaravanDay` (float),
- `PartyId` (string, relink pós-load).

**Party da zona**: `MemberRoster` = garrison; `ItemRoster` = estoque de produção.
Dono bandido inicial = clã de bandido da cultura mais próxima (looters fallback).

## 4. Mecânicas do MVP (Fase 1)

- **Spawn/restore** (OnSessionLaunched): para cada Zone do manifesto sem record →
  cria record + party bandida na posição resolvida. Com record → garante party
  viva (respawn se destruída fora de captura), relinka componente ↔ record.
- **Produção** (DailyTick): tabela por tipo×tier → `ItemRoster` da party
  (IronOre/HardWood/Charcoal via `DefaultItems`; Silver via id "silver" com
  fallback; Gold → `StoredGold` direto). Garrison de dono AI/bandido regenera devagar.
- **Caravana**: quando o estoque atinge o lote, spawna party pequena
  (`ResourceCaravanPartyComponent`, escolta proporcional ao tier) com a carga,
  AI `SetMoveGoToSettlement(boundTown)`. Ao entrar (OnSettlementEntered):
  itens → mercado da cidade (`Town.Owner.ItemRoster`), receita = Σ preço local →
  ouro pro líder do clã dono (mensagem se player). Caravana destruída no caminho =
  carga perdida (guerra econômica emergente — escoltar/raidar caravanas é gameplay).
- **Captura** (MapEventEnded): zona derrotada → dono vira o clã do líder atacante,
  tier preservado, garrison recomeça pequena, estoque saqueado vai pro vencedor.
  Bandidos vencendo = zona volta a ser neutra-bandida.
- **Menu** (chegada não-hostil): status (tipo/tier/dono/estoque/garrison/ouro),
  `Collect stored gold` (dono), `Donate troops` (`PartyScreenHelper.OpenScreenAsDonateTroops`
  — mesmo mecanismo dos settlers), `Upgrade` (ouro+hardwood → tier↑: +produção,
  +garrison cap, +lote de caravana), `Leave`. Zona hostil nem abre menu — encounter
  de batalha vanilla resolve.
- **Autoria de slots**: comando de console `rf_zones.mark_zone Gold` — cavalga até
  o ponto, roda o comando, cola a linha impressa no manifesto.

## 5. Fases seguintes

- **F2 — Presença física**: paliçada/kitbash por tier no ícone, cena de defesa
  (HomesteadBattleSceneMissionLogic com balista) em vez de field battle para tier ≥2,
  steward companion (Steward=produção, Engineering=defesa, Trade=receita).
- **F3 — AI estratégica**: RF_warsystem passa a pesar zonas como alvos (valor =
  produção×tier); lordes AI capturam e fazem upgrade; raid≠captura (saque queima
  estoque e foge).
- **F4 — Sinergias**: warlords (#3) tomam zonas e vivem delas; eras (#4) corrompem
  minas evil (workers viram refugiados → alimenta #1); War Sails: zonas costeiras
  (pesca/pérola/madeira naval) com caravana marítima e piratas.

## 6. Riscos e mitigação

| Risco | Mitigação |
|---|---|
| Save-compat | Definer base 728850000 (auditado único); records mínimos; party reconstruível |
| Fold/patch cedo | ZERO patch novo; reuso dos patches settlers já aplicados tarde |
| Anian sem o manifesto | Loader gracioso: arquivo ausente = 0 zonas + log, nunca crash; XML shipa no _Module (lição do crash Homesteads) |
| Party estacionária vagando | `Ai.DisableAi()` + re-hold diário (padrão settler camp provado) |
| Economia inflada | Números conservadores (~150-400/dia por zona tier 1 bruto, menos upkeep implícito de guerra); tunáveis em `ResourceZoneType` |
| Posição inválida | Âncora em settlement + `FindReachablePointAroundPosition` |

## 7. Estado da implementação

- [x] Plano
- [x] Interface IRFStationaryCampParty + generalização dos 2 patches settlers
- [x] Projeto RF_ResourceZones na solution
- [x] Núcleo (manifesto, componentes, behavior, definer, console command)
- [x] SubModule.xml (fonte+deploy) + manifesto inicial + build 0 erros
- [x] Slots definitivos: 68 zonas autoradas in-game pelo autor (2026-07-15; manifesto limpo, ids-slug únicos)
- [x] mark_zone v2: escreve no XML deployado + spawna na hora; limpeza de zonas órfãs no load
- [x] Ícones por tipo no mapa (VillageType.MeshName: iron/silver mine, lumberjack, clay kilns) + bandeira do dono
- [x] boundTown com re-rota de guerra (bound hostil ao dono atual → town amigável mais próxima)
- [x] **F3 (parcial) — Ambição das facções** (`ResourceZoneAmbitionBehavior`): lords AI caçam zonas deliberadamente — bandit-held num raio de 18 (limpeza oportunista), inimigas num raio de 45 em guerra; melhor caçador por zona (lord livre, sem exército/cerco, força ≥1.4× a garrison), zonas de tier alto priorizadas, máx. 6 caçadas simultâneas, cooldown 3 dias/zona; nudge diário via SetMoveEngageParty (nunca briga com a iniciativa do engine). AI-owned zones CRESCEM: 18 dias de posse ininterrupta → tier+1 (campo saveable UpgradeProgressDays(8); reseta na captura) com aviso na tela.
- [x] **Reservas e riqueza aleatória** (pedido do autor 2026-07-15): cada mina rola um veio na primeira produção — poor 30% / steady 45% / rich 25% (saveable Richness(9)). Riqueza multiplica o rendimento (0.6×/1×/1.6×) e dimensiona a RESERVA (20/35/55 dias de trabalho tier-1; ReserveUnits(10)). Reserva esgota → produção PARA por cooldown (12/8/5 dias por riqueza; ExhaustedUntilDay(11)) → reabre cheia. Tier alto cava o MESMO veio mais rápido → upgradar mina pobre a esgota mais cedo (decisão real). Menu mostra veio + "X dias de trabalho restantes"/"EXHAUSTED, reabre em N dias"; dono player recebe avisos de exaustão/reabertura. Records antigos rolam riqueza lazy no primeiro tick.
- [ ] Teste in-game (spawn, captura, produção, caravana, menu, caçadas AI, exaustão/reabertura)
- [x] **F3.5 — Cobiça no warsystem**: provider de greed instalado via `RFWarExternalIntentApi.SetResourceGreedProvider` (padrão do contexto externo); `RFWarDecisionPlannerBehavior.GetWarSelectionBias` ganhou `bias += 115f × GetResourceGreedFactor(attacker, defender)`. Greed (0..1) = riqueza ALCANÇÁVEL das zonas do defensor (tier × veio × peso do tipo: ouro 1.5/prata 1.3/ferro 1.0/madeira-carvão 0.8; alcance pleno ≤45 do feudo mais próximo do atacante, decai até 110) × multiplicador have-not (reino pobre em zonas é mais cobiçoso, 0.75–1.25). Cache diário por par. Zonas bandidas não geram motivo de guerra. Provider à prova de falha (exceção → fator 0 e auto-desinstala). Flavor: guerra declarada com greed ≥0.5 → "The mines of X glitter in the eyes of Y — a war of greed begins."
- [ ] F2 (cena de defesa/steward) / F4 (sinergias warlords/eras/costeiras)
