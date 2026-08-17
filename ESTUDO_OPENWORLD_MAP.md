# Estudo: substituir o campaign map por travessia open-world em cena de missao

Data: 2026-08-05. Pergunta do autor: e possivel trocar o mapa de campanha por uma
"cena imensa" jogada como mission (estilo open world), com renderizacao
inteligente e o minimo para nao crashar?

Veredito curto: **a versao literal (UMA cena continua e imensa) e impossivel na
API de mod. A versao por CELULAS — o mundo continua sendo o campaign model, mas
voce ANDA nele em cenas de missao costuradas — e possivel, e o vanilla ja
fornece dois dos tres pilares.** Existe um demo de baixo custo que prova o
conceito antes de qualquer aposta grande.

## Por que a versao literal nao passa

1. **Uma Scene por Mission, sem streaming.** `MissionInitializerRecord` recebe UM
   nome de cena; `MapScene` le UM "Main_map" (`_scene.Read("Main_map", ...)`).
   Nao ha API publica de carregar/descartar pedacos de cena em runtime. Streaming
   exigiria o fonte do motor.
2. **Precisao de float.** Posicoes de missao sao Vec3 float em METROS. Alem de
   ~8–16 km da origem, a precisao cai abaixo do centimetro e fisica/animacao
   tremem (limite de IEEE-754, nao de configuracao). Calradia em escala humana
   teria centenas de km. [raciocinio de primeiro principio, nao decompilado]
3. **Escala do Main_map.** O mapa E uma cena (da para abrir como missao — o
   jogador vira um gigante andando entre dioramas), mas e uma MINIATURA:
   assentamentos sao adereços, 1 unidade de mapa nao e 1 metro. Nao serve de
   open world sem refazer o conteudo inteiro.
4. **Custo de conteudo.** Mundo em escala humana = milhares de cenas de autoria.
   Nenhuma "renderizacao inteligente" resolve conteudo que nao existe.

## Os tres pilares da versao possivel (verificados no decompilado)

### 1. Posicao do mapa -> cena, JA EXISTE (map patches)
O mundo e tilado em "patches"; o vanilla escolhe a cena de batalha assim:
```
MapPatchData patch = Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position);
string scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, isNaval);
CampaignMission.OpenBattleMission(scene, ...);
```
(MEGA_010 4513, 149348; modelo em DefaultSceneModel.) Ou seja: "onde estou no
mapa" -> "cena coerente com o terreno local" e infraestrutura pronta. E o
`SceneModel` e substituivel — da para mapear patches para cenas NOSSAS.

### 2. Missao abre qualquer cena
`MissionState.OpenNew(nome, MissionInitializerRecord(cena), behaviors...)` — o
custom battle e a prova. A "cena de travessia" e uma missao normal com os
behaviors que quisermos (sem exercito, com spawns de fauna/bandidos, etc.).

### 3. O tempo de campanha e BOMBEAVEL (o pilar critico)
O relogio do mundo so anda em `MapState.OnMapModeTick`:
```
if (ActiveState == this) { Campaign.Current.RealTick(dt); ...; Campaign.Current.Tick(); }
```
— numa missao, o mundo CONGELA (e por isso "andar no mundo" vanilla nao existe).
MAS: `Campaign.RealTick` (MEGA_010 217794) e autossuficiente — tempo de mapa,
componentes de entidade, tick paralelo de TODAS as parties, cercos. Nao depende
do MapScreen (o Handler e chamado separado). Em principio da para chamar
RealTick+Tick de dentro de uma missao de travessia com guardas:
  - suprimir encounter do jogador enquanto anda (IgnoreByOtherPartiesTill — ja
    usamos no RF_Ambush — e/ou posicao da party "estacionada");
  - menus de campanha nao podem abrir em missao (guardar/adiar eventos de menu);
  - visuais de party do mapa (PartyVisualManager e um CampaignEntityComponent)
    tickam junto — verificar se toleram MapScreen ausente.
RISCO MEDIO-ALTO: e a parte que exige prototipo antes de qualquer promessa.

## Arquitetura proposta: "Calradia a pe" por celulas

1. O CAMPAIGN MODEL continua sendo o mundo (economia, lordes, guerras — nada de
   reescrever a simulacao; e ela que faz Bannerlord ser Bannerlord).
2. Andar = missao de travessia na cena do patch atual. Party principal
   "estacionada" no mapa e movida em espelho conforme o jogador anda na cena
   (razao de escala cena:mapa por patch).
3. Borda da celula = transicao rapida (loading) para a cena do patch vizinho,
   com a party avancada no mapa. Nao e seamless — e Morrowind, nao Skyrim.
4. Tempo: fase A (seguro) — tempo so passa nas transicoes e em "acampar";
   fase B (prototipo do pump) — RealTick/Tick bombeados enquanto anda, mundo
   vivo de verdade.
5. Encontros: party hostil se aproxima da nossa posicao de mapa -> aviso na
   cena -> vira batalha normal (pipeline vanilla intacto) ou spawn dos agentes
   na propria cena de travessia (upgrade posterior).
6. Assentamento no patch = portao na cena -> entra na cena vanilla da cidade.

### Roteiro com custos honestos
- **v0 (dias): "Walkabout"** — tecla no mapa abre missao na cena do patch atual
  (pilares 1+2 puros, tempo congelado como qualquer missao). Prova o pipeline e
  ja e um recurso simpatico ("desmontar e explorar").
- **v1 (semanas): travessia por celulas** — movimento espelhado, transicao de
  borda, entrada em assentamentos, encontros por proximidade com aviso.
- **v2 (meses, risco real): mundo vivo** — pump do RealTick dentro da missao.
  Prototipo primeiro: 1 dia de teste responde se os guardas bastam.
- **Seamless de verdade: nao existe** sem o fonte do motor. Dizer com clareza.

## O mundo visto de dentro da cena (pergunta do autor, 2026-08-05)

**Parties sao visiveis como AVATARES.** A posicao de toda party e dado puro do
campaign model, legivel de dentro da missao. Toda party dentro da celula atual
ganha representacao na cena (espelho invertido do movimento do jogador):
caravana = mulas+guardas na estrada; lorde = coluna de cavaleiros; 100 homens
viram destacamento de 5-10 agentes (mesma convencao de escala do proprio mapa).
Aproximar = encounter normal. Na fase segura (tempo congelado) eles ficam
PARADOS — cenario; na fase viva (tick bombeado) eles se MOVEM — mundo. E o
melhor argumento a favor da fase 2.

**Assentamentos (ideia do autor que zera o custo de autoria):** reusar os
PROPRIOS map icons do campaign map, escalados para proporcao de cena. Verificado:
todo settlement declara `map_icon="<mesh>"` no settlements.xml (inclusive o
RF_Map), e o codigo carrega esses meshes por nome em qualquer cena
(`MetaMesh.GetCopy("map_icon_...")`, MEGA_007 21354). Pipeline 100% data-driven:
settlement -> mesh do icone -> spawn na celula na posicao espelhada, escalado
(~20-50x) -> volume de gatilho no portao -> ENTRAR = a mesma acao do clique no
mapa (settlement encounter vanilla, menu/cena da cidade). Cidade nova do RF
ganha silhueta automaticamente. Ressalva: icones sao low-poly de camera de mapa
— otimos no horizonte, "maquete" de perto; upgrades individuais depois, sem
bloquear. Bussola/marcadores no HUD continuam valendo como complemento barato.

## Riscos alem do tick
- Cenas de batalha vanilla sao ~1–2 km: travessia fica celular demais se usar
  so elas; cenas de travessia proprias (maiores, ate o limite confortavel do
  editor) melhoram a sensacao mas custam autoria.
- Save durante missao de travessia (vanilla salva no mapa): forcar autosave nas
  transicoes resolve o v1.
- Mods de campanha (nossos!) assumem MapState em varios pontos; o pump da fase B
  precisa de auditoria dos NOSSOS behaviors tambem.

## Arquivos-chave (decompilado 1.3.x/War Sails)
- `MapScene` / `GetMapPatchAtPosition`: MEGA_008 75023+, 75462
- `DefaultSceneModel.GetBattleSceneForMapPatch`: MEGA_010 117878
- Uso no encounter: MEGA_010 4513, 4599, 149348
- `MapState.OnMapModeTick` (gate do tempo): MEGA_010 127622
- `Campaign.RealTick` (o que o pump precisa): MEGA_010 217794
- `CampaignTickPartyDataCache.RealTick` (tick paralelo de parties): MEGA_010 232180
