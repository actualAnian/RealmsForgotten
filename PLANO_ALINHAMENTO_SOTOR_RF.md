# Alinhamento SOTOR ↔ RF — plano de execução

Escrito em 2026-07-30, depois do checkpoint bom
(`_CHECKPOINT_BOM_2026-07-29_bandeiras_ok/`).

## 0. O que este plano precisa impedir

Na tentativa anterior eu apliquei ~10 mudanças no lado RF de uma vez. As
bandeiras de ordem quebraram, e por dois dias não houve como saber qual mudança
foi — porque nenhuma delas tinha sido testada isolada. O bug só sarou quando o
lado RF **inteiro** voltou ao pré-magia.

Conclusão que sustenta este plano: **o culpado está no conjunto revertido**, e
esse conjunto é finito e conhecido. Ele não é um mistério — é uma lista.

## 1. A lei

> **1 mudança = 1 canário. Sem exceção, sem lote.**

**Canário (3 min)** — cobre os dois defeitos que já apareceram:

1. Entrar em batalha de campanha
2. Selecionar tropa com `1`, arrastar `F1` → **as bandeiras estão inteiras?**
3. Ganhar/terminar a batalha, pegar loot e prisioneiros
4. **Voltar ao mapa de campanha** — carrega, ou trava?

Passo 4 entrou porque o travamento do mapa (shaders de terreno do `RF_Map`
faltando) mostrou que a transição batalha→mapa é frágil e não estava coberta.

**Dispensa canário:** mudança só de UI — prefab, brush, sprite, SpriteData, ou
VM da tela mexida. Não toca cena, mesh, material, item nem modelo de combate.

**Ao fim de cada fase aprovada:** atualizar o checkpoint. Voltar atrás passa a
ser uma cópia de pasta, nunca arqueologia.

## 2. Fase 1 — achar o culpado reaplicando o que é necessário

O conjunto revertido, decomposto em passos atômicos. A ordem é por
dependência; os dois primeiros são **neutros por construção** (não mudam
comportamento) e por isso compartilham um canário.

| # | passo | risco | canário |
|---|---|---|---|
| 1a | criar `RFLegacyMagic.cs` com `Enabled = true` | nenhum (só andaime) | junto com 1b |
| 1b | ligar os 10 pontos do RF ao flag, ainda `true` | nenhum (caminho idêntico) | 1 canário |
| 1c | virar `Enabled = false` — magia legada off | **alto** | próprio |
| 1d | `GetCartridgeSkillPatch` + `RFCombatXpModel`: Cartridge→Arcane | médio | próprio |
| 1e | `RFPerks`: perks de Arcane (dano/raio) | médio | próprio |
| 1f | `enchanteditems.xml`: 22 cajados Musket→Polearm | **o maior suspeito** | próprio, em 2 lotes |

Sobre **1c**: é onde mora o requisito que você fixou — *NecromancerStaff e
GandalfStaff mantêm invocar undead e curar*, e o meteoro da Winged Witch
continua vivo (`MeteorMissionLogic` fica fora do gate; conteúdo de quest nunca
entra em gate de sistema).

Sobre **1f**: é o suspeito nº 1 porque é a única mudança que altera **itens que
as tropas carregam em batalha de campanha** — exatamente o contexto onde o bug
vivia, e o único que custom battle não reproduzia. Vai em dois lotes (11 + 11)
para estreitar rápido se quebrar.

**Se algum passo quebrar o canário:** para ali. Aquele passo é o culpado dos
dois dias. Reverto só ele, isolo a causa dentro dele (é uma mudança pequena) e
volto com um conserto — não com a mudança inteira.

**Se nenhum passo quebrar:** o defeito era interação entre dois deles, ou algo
que a restauração corrigiu de lado (ex.: os 17 XMLs de template que
sobrescreviam o jogo base, já removidos). Nesse caso o registro fica e seguimos —
sem fingir que a pergunta foi respondida.

## 3. Fases 2+ — o alinhamento propriamente dito

Só começa com a Fase 1 fechada e aprovada por você. Toda lógica nova vive em
`SOTOR.RFIntegration`; arquivo do SOTOR só recebe **gancho de uma linha**
(categoria `[RF-B]` do [PLANO_RECONSTRUCAO_RF_MAGIC.md](PLANO_RECONSTRUCAO_RF_MAGIC.md)).

| fase | conteúdo | canário |
|---|---|---|
| **2** | Skill unificada: `SotorSkills.Spellcraft` resolve para a skill `arcane` do RF (ponto único, 13 call sites) | sim |
| **3** | Foco arcano: sem cajado empunhado não conjura; cajado define pool de Mana | sim |
| **4** | Cultura define escolas (`rf_culture_lores.xml`, as 15 culturas que você ditou) | sim |
| **5** | Afinidade do cajado: bônus/penalidade em custo e cooldown | sim |
| **6** | Tropas conjuram: pool de Mana por tropa + IA de conjuração | sim |
| **7** | Migrar os feitiços RF distintivos para o motor SOTOR | sim |
| **8** | UI: "Winds of Magic" → **Mana** (via `Languages`, não literal em C#), ícones | não |

Fase 6 é a de maior superfície (toca agentes em batalha) — vai subdividida em
passos próprios quando chegarmos nela.

## 4. Proibições permanentes

Vindas de erro real cometido, não de precaução teórica:

- **Nunca** Harmony patch em `OrderTroopPlacer`, `MissionScreen` ou MissionView
  do jogo base. O RF_Magic não tem motivo para tocar nisso.
- **Nunca** `Material.GetFromResource` confiando em `null` (devolve placeholder),
  nem `GetFirstMesh().SetMaterial()` (escreve no prefab **compartilhado** e
  envenena a sessão inteira).
- **Nunca** mexer no cache de shaders (`ProgramData\...\Shaders`). Ele contém
  shaders de terreno **compilados por cena**, inclusive do `RF_Map`; apagar
  causou o travamento do mapa e a regeneração voltou incompleta.
- **Nunca** criar módulo pelo template do jogo sem apagar os XMLs de nome de
  jogo-base que ele gera (`physics_materials`, `collision_infos`,
  `native_parameters`, `skins`, `action_sets`…). São stubs vazios que
  **sobrescrevem** as tabelas do jogo.
- **Nunca** deixar classe de diagnóstico no projeto. Entra, mede, sai.
- **Nunca** editar arquivo do SOTOR com lógica RF inline. Só `[RF-A]`
  (artefato de decompilação), `[RF-B]` (gancho de 1 linha), `[RF-C]` (UI),
  `[RF-D]` (identidade do módulo).

## 5. Higiene de processo

- **Fechar jogo e launcher antes de buildar.** Com eles abertos o PostBuild não
  copia o DLL (`MSB3021` / "locked by BannerlordLauncher").
- Cada fase aprovada vira commit numa branch de trabalho — **com autorização
  explícita sua**, nunca por iniciativa minha.
- Antes de pedir um teste, esgotar o que é verificável por leitura. Cada `F5`
  seu é caro; foi por não respeitar isso que dois dias se perderam.
