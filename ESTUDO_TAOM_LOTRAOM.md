# Estudo — LOTRAOM / "TAOM" (mod parceiro, 2026-08-18)

Fonte: `F:\Downloads\lotraom-dev\lotraom`. LOTRAOM (Lord of the Rings: Age of Men),
Bannerlord **v1.2.12** (nós estamos em 1.4.8 — portar exige verificação de API), ~1.016
arquivos C# no mod + launcher .NET 8 (Avalonia) + servidor de update ASP.NET + 185
arquivos de teste. Vários sistemas são portes documentados do TAOM original; um credita
o TOW_Core do The Old Realms.

É o projeto de mod com a melhor engenharia que já olhamos: DI própria (IoC), padrão
adapter para testabilidade, TDD com gate de cobertura no build, ADRs, doc por feature
com seção "Why This Exists" **verificada contra o source decompilado**, e testes de
arquitetura que caçam classes de bug (não instâncias).

---

## 1. Candidatos diretos a porte (pequenos, alto valor)

### 1.1 SaveShield — ★ o melhor achado do estudo (310 linhas no total)

Quando um save/load falha, o `ModCulpritAttributor` caminha a stack da exceção até o
**primeiro frame que não é engine** e nomeia o assembly culpado; o `SaveFailureReporter`
loga e mostra um diálogo ao jogador. Duas decisões de design excelentes e documentadas:

- **não engole a exceção**: o TAOM original suprimia; eles inverteram de propósito —
  "save silenciosamente perdido é pior que crash visível". A atribuição é o valor, não a
  supressão;
- o próprio LOTRAOM **não se exclui** da lista de suspeitos.

Para o RF: nosso ecossistema tem 15+ DLLs próprias + RBM + MCM; "save quebrou" é o
pior tipo de report que recebemos (cf. o shadow fix do equipment roster). Porte quase
literal — só trocar os prefixos de engine e o logger. **Recomendo ser o primeiro porte.**

### 1.2 Teste de patch Harmony duplicado

`DuplicateHarmonyPatchTests`: varre o **código-fonte** procurando o mesmo método vanilla
patchado por dois mecanismos (atributo `[HarmonyPatch]` + patcher por reflexão). Motivo
real: um multiplicador 0.9 de cerco virou 0.81 em produção porque o postfix rodava duas
vezes — Harmony **não deduplica**. O RF patcha por atributo, por `PatchAll` em vários
submódulos e por reflexão (NavalPatcher etc.); somos exatamente o perfil de vítima.
Não precisamos de framework de teste: um script PowerShell no build faz o mesmo.

### 1.3 Validate-ModuleData.ps1 (359 linhas, standalone)

Valida integridade referencial de TODO o ModuleData contra o jogo instalado: cada
`Item.x`, `Culture.x`, `NPCCharacter.x`, `SkillSet.x`... que não resolve, IDs duplicados
onde um arquivo sobrescreve outro em silêncio, e `<XmlName path>` sem arquivo. O sintoma
que eles descrevem — "tropa spawna sem o equipamento, acumula em silêncio" — é
literalmente o nosso dia a dia (temos o RF_XmlValidator, mas o deles cobre mais e tem a
precisão *medida*, com regex validada contra 60 mil ocorrências). Vale fundir as ideias.

### 1.4 Índice de decompilado

`Decompile-BannerlordAssemblies.ps1` gera árvore decompilada + `INDEX/types.tsv`
(tipo → arquivo), com re-decompilação incremental por SHA256 quando o jogo atualiza.
Nós grepamos MEGA files; `grep "^NomeDoTipo\t" types.tsv` → coluna 4 é o arquivo. Upgrade
barato e imediato do nosso fluxo de pesquisa.

---

## 2. Sistemas com paralelo direto no RF (comparar antes de construir)

### 2.1 AI Strategic Intelligence ↔ RF_warsystem

Reescreve a escolha de alvo dos lordes com 3 entradas Harmony. Descobertas deles,
verificadas no decompilado, que valem para nós:

- **defesa vanilla é local à facção**: `FindBestTargetAndItsValueForFaction` só passa a
  própria `MapFaction` no modo Defender — **aliado sitiado nunca é candidato a defesa**.
  Se os blocos do RF dependem de aliança (e dependem), sofremos disso hoje;
- **raid é grampeado em 100–250 unidades de mapa** no vanilla — tunado para Calradia.
  Mapa grande (o nosso também) = raiders permanentemente locais;
- o score vanilla não distingue *quem* ataca — sem personalidade de facção.

Isso é exatamente as fases 2/5 do nosso `CampaignWarAIMappingAndPlan.md`. Eles já
pagaram o custo de descobrir os pontos de patch (métodos privados de
`AiMilitaryBehavior`, prefixo que retorna false). Ler `Features/AiStrategicIntelligence/`
antes de implementar a nossa fase 2 economiza semanas.

### 2.2 Troop Weight ↔ RFMonsters

Troll de caverna ocupa 4 vagas de party, elfo de elite 2, soldado comum 1. O truque
comprovado: **inflar a contagem de membros que o jogo lê, não encolher o limite** (eles
documentam que a mecânica invertida foi um bug que já corrigiram — lição grátis). Nossos
monstros/gigantes têm o mesmo problema de balanço e nunca atacamos isso.

### 2.3 Cross-Race Offspring ↔ RF_Races

Vanilla: `DeliverOffSpring` **asserta que os pais têm a mesma raça** e gera aparência
sempre da raça da mãe. Com casamento inter-racial, isso é NRE + filhos com corpo errado.
O RF tem raças e casamentos; não encontrei tratamento disso no nosso código — **suspeito
de bug latente nosso**. A solução deles (escolher a raça do filho, alinhar os pais
temporariamente, devolver ao vanilla) cabe num arquivo. Checar em jogo: casamento
humano×elfo no RF gera filho?

### 2.4 Momentum ↔ roadmap de eras / world-state

Guerra de dois blocos sem paz negociável precisa de placar e condição de fim: cada
batalha/cerco/raid soma contribuição **com tempo de expiração** a um lado; a diferença
alimenta indicador no mapa, buff de moral e o fim da guerra. Encaixa no nosso conceito
de eras ("a era termina quando...") melhor que qualquer coisa que esboçamos.

### 2.5 Messengers ↔ RF_AIDialog

Mensageiro pago viaja N dias, pode se perder, e chega oferecendo **conversa à distância**;
botão "Send Messenger" injetado na enciclopédia. Nosso RF_AIDialog já tem
`MessengerTravelCalculator` — a UX deles (enciclopédia + conversa real na chegada) é o
complemento natural do nosso lado LLM.

### 2.6 Warg ↔ RFMonsters/montarias

Montaria vanilla não tem ataque próprio. A infra deles: behavior tree por agente +
`CustomAttack` (colisão por osso durante janela da animação, callback por acerto) +
estado de fúria (quebra formação, persegue quem bateu, 2-3 mordidas, devolve controle).
Se algum dia dermos mordida/investida a criaturas montadas do RF, o mapa do caminho está
pronto — inclusive o detalhe de manter as mãos do cavaleiro na juba (override de look
direction).

### 2.7 AtmospherePersistence ↔ RFCustomScenes

Prefixo em `InitializeMission`: cena com `forceatmo` no nome mantém a atmosfera autorada
em vez da imposta pela campanha. Creditado ao TOW_Core (The Old Realms). Barato e útil
para as cenas autorais do RF (templos, torres) que hoje herdam clima do mapa.

---

## 3. Práticas de engenharia que valem imitar (custo ~zero)

1. **"Why This Exists" por feature, com honestidade epistemológica** — os docs deles
   distinguem explicitamente *intenção registrada* de *comportamento observado* ("treat
   the description as observed behaviour, not intent"). Nossos ESTUDO_*.md já são bons;
   adotar essa distinção os torna confiáveis a longo prazo.
2. **Regras destiladas de incidentes reais** (do CLAUDE.md deles):
   - *Fail Closed, Never Open* — condição que não dá para representar **desliga a regra**,
     não derruba o filtro (um filtro dropado virou +44% de dano universal em produção);
   - *No Hot-Path Alloc* — patch em método por-frame/por-hit não aloca (sem LINQ/new);
   - *Two Components, One Vocabulary* — dois componentes que precisam concordar num
     conjunto de tokens compartilham UMA fonte de verdade, testada de ponta a ponta;
   - *String IDs em save, nunca enum int* — nós já fazemos, bom ver confirmado.
3. **Feature toggle default-off para sistemas arriscados** ("Disabled by default for
   stability") — SiegeAI e TroopWeight deles nasceram desligados e amadureceram em prod.
4. **RaceBonus indexado** — regras de bônus em JSON compiladas para bitmask no load;
   matching por-hit em 2 fases (bitmask em ns → avaliação completa só nos sobreviventes).
   Se o RF_Races um dia ganhar bônus de combate por raça, é esse o desenho — nunca
   string-compare no caminho do hit.

## 4. O que NÃO vale importar

- **A arquitetura DI/adapter completa** (IoC + 20 adapters + TDD gate): excelente para
  um time com CI e cobertura; para o nosso fluxo (1 autor + assistente, iteração rápida
  in-game) o custo de cerimônia supera o ganho. Adotar as *regras* (seção 3), não a
  *estrutura*.
- **Launcher próprio + servidor de update**: infra pesada (assinatura de código, OAuth
  Discord). Interessante no dia em que o RF precisar de distribuição própria; não agora.
- **Object pooling do damage calc**: resolve um gargalo que eles mediram no caminho
  deles; nós não temos esse hot path (nosso equivalente seria o RF_Magic, que tem regra
  própria de não-mexer).
- Conteúdo LOTR em si (licença split MIT/CC-BY-SA/noncompete — código MIT dá para ler e
  aprender; assets não são nossos).

## 5. Ordem sugerida, se você aprovar

1. ~~SaveShield~~ **FEITO 2026-08-18** — portado como `RealmsForgottenMain/SaveShield/` (3 arquivos, categoria Harmony `RFSaveShield` aplicada cedo em OnSubModuleLoad para cobrir o primeiro Load da sessão; log em `ModLogs/RF_SaveShield.log`); build limpo, aguarda teste;
2. **Checagem de Harmony duplicado** como script no build (algumas horas);
3. **Conferir o bug de offspring inter-racial** em jogo (10 min de teste; se confirmar,
   o porte do fix é 1 arquivo);
4. **Ler AiStrategicIntelligence a fundo** antes da fase 2 do war system (estudo, não
   código);
5. Índice de decompilado (`types.tsv`) para nosso workspace;
6. Momentum/TroopWeight/Atmosphere ficam no backlog de features com o link para o
   código deles.
