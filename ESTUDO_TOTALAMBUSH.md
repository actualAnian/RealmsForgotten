# Estudo: TotalAmbush (Nexus 12376) e o redesenho "emboscada de verdade"

Data: 2026-08-03. Decompilado em `.tmp_ambush_decompiled\` (17 arquivos, DLL 56 KB).
Pedido do autor: a emboscada NAO deveria exigir contato/ataque previo no mapa;
deveria nascer do Scouting do jogador vs o do inimigo, com a opcao aparecendo
sem encostar na party inimiga.

## Como o TotalAmbush funciona hoje

### Lado campanha (a parte fraca — confirmada a critica do autor)
- `AmbushCampaignBehavior`: adiciona a opcao "Try to ambush" ao MENU DE
  ENCOUNTER (`starter.AddGameMenuOption("encounter", ...)`). Ou seja: so existe
  DEPOIS que as duas parties ja se tocaram no mapa e o encounter abriu.
  Restricao: jogador precisa ser DEFENSOR (ou atacante contra caravana).
- `EnemyAmbushCampaignPatches`: prefix em `MenuHelper.EncounterAttackConsequence`
  — quando o jogador clica "atacar", o lider defensor rola a chance de "ter
  preparado uma emboscada". Tambem pos-contato.
- `AmbushChance`: chance = base + dif.Tactics×0.3 (teto ±30) + dif.Scouting×0.15
  (teto ±15), clampada em min/max do MCM. Scouting e o fator MENOR — outra
  inversao em relacao ao que o autor quer.
- `AmbushSession`: flag estatica armada no menu e consumida quando a missao abre.

### Lado missao (a parte BOA — vale reimplementar)
- `AmbushMissionBehavior` (37 KB, jogador embosca): inimigo entra em COLUNA DE
  MARCHA scriptada (velocidade 1.8, alvos suprimidos), com formacoes de marcha
  (vanguarda, escolta de cavalaria, quadrado protegido — `MarchFormations`);
  jogador faz deploy OCULTO; deteccao simulada: cones de visao (cos 0.42
  montado / 0.57 a pe), alcance lateral/traseiro, "parada desconfiada"
  (SuspiciousHalt) antes da deteccao plena; `SpringAmbush` dispara o combate;
  jogador pode girar/deslocar a rota de marcha (Shift+teclas).
- `EnemyAmbushMissionBehavior` (42 KB): o espelho, quando a IA embosca o jogador.
- `BattlefieldAmbushMorale`: choque de moral no lado emboscado.
- `AmbushDeploymentPatches`: restringe zonas de deploy.

## Ganchos vanilla que fazem a versao correta ser natural (verificados)

`MapVisibilityModel` e um GameModel substituivel com exatamente as duas metades:
- `GetPartySpottingRange(party)` — alcance de quem PROCURA: usa o Scouting do
  `party.EffectiveScout`, perks (NightRunner/DayTraveler/VantagePoint/
  MountedScouts/EagleEye), noite corta a base 12→6, terreno.
- `GetPartySpottingDifficulty(spotter, party)` — dificuldade de VER o alvo:
  floresta ja vale 0.3 (mais dificil). O vanilla decide visibilidade com
  `range / difficulty >= 1` (padrao usado nos hideouts).
- `party.StationaryStartTime.ElapsedHoursUntilNow` — tempo parado ja e rastreado.

Ou seja: Scouting vs Scouting sem contato JA E o modelo do jogo — o TotalAmbush
simplesmente nao o usou.

## Redesenho proposto: RF_Ambush

### Fase 1 — a emboscada do jogador
1. **Armar**: acao no mapa ("Preparar emboscada") fora de encounter. A party
   para e entra em stance oculta. Terreno conta: floresta >> colina > campo.
2. **Ocultacao** (override de `GetPartySpottingDifficulty` p/ party emboscada):
   base do terreno × multiplicador da stance (~0.35) × penalidade por tamanho
   (log) × bonus por tempo parado (ate ~3h) — e o Scouting do NOSSO scout
   melhora a camuflagem. Implementar como RFMapVisibilityModel (ou postfix
   Harmony se houver conflito de model com outros mods).
3. **Deteccao**: o vanilla ja faz o teste range/difficulty por party inimiga.
   Se o inimigo nos ve: reage — desvia, ou ataca o acampamento revelado
   (risco real de emboscar). Scouting alto do inimigo = emboscada furada.
4. **Gatilho sem contato**: tick de campanha — inimigo hostil nao-detector a
   distancia de bote → menu "Atacar da emboscada / Deixar passar". O DEIXAR
   PASSAR e essencial: emboscador escolhe a presa (caravana gorda sim, army
   de 800 nao).
5. **Spring**: abre o encounter ja armado como emboscada e entra na missao com
   coluna de marcha inimiga + deploy oculto (reimplementacao propria do
   conceito do TotalAmbush, integrada ao RF_BattleAI).
6. **XP**: Scouting por hora oculto com inimigos proximos; Tactics no spring;
   Scouting pro scout inimigo que nos detecta.

### Fase 2 — a IA embosca
Parties com scout forte (bandidos = natos) armam em chokepoints/floresta na
rota do jogador. Contra-jogo: Scouting alto do jogador revela "sinais de
emboscada" a frente (padrao hideout: IsSpotted). Sem o contra-jogo vira
frustracao — nunca lancar a fase 2 sem ele.

### Balanceamento (aprendido com os numeros do TotalAmbush)
- Chance de sucesso NAO e um d100 unico no menu: e o proprio sistema continuo
  de visibilidade (quem ve quem primeiro). Rolagem so no limiar.
- Custos de esperar: comida normal, moral cai apos ~1 dia, cooldown apos spring.
- Caravanas/aldeoes: crime + relacao, como assalto normal.

### Licenca
Mod fechado de Nexus (sem fonte publicado). REIMPLEMENTAR conceitos com codigo
proprio; nao copiar o decompilado. O lado campanha e trivial de qualquer forma;
o lado missao a gente escreve melhor com o que ja temos no RF_BattleAI
(formacoes, moral, AdaptiveMemory).

## Mapa do decompilado
- Campanha: `AmbushCampaignBehavior.cs`, `EnemyAmbushCampaignPatches.cs`,
  `AmbushChance.cs`, `AmbushSession.cs`
- Missao: `AmbushMissionBehavior.cs` (jogador), `EnemyAmbushMissionBehavior.cs`
  (IA), `MarchFormations.cs`, `BattlefieldAmbushMorale.cs`,
  `AmbushDeploymentPatches.cs`, `AmbushMissionInstaller.cs`
- Config MCM: `AmbushSettings.cs`
