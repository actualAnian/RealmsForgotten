# Dissecação — cena de cerco vanilla (`sturgia_castle_siege_001`)

Objetivo: gabarito real para **gerar uma cena de cerco por código** (terreno clonado do
base game + prefabs vanilla + arranjo vindo de uma imagem do autor). Fontes: o
`scene.xscene` da cena (2,25 MB, 4.999 entidades) cruzado com a doc oficial
(moddocs.bannerlord.com/authoring-mission-scenes/sieges). 2026-08-18.

## 1. Anatomia da pasta

| Arquivo | Tamanho | Formato | Quem faz |
|---|---|---|---|
| `scene.xscene` | 2,25 MB | **XML** | **código (nós)** |
| `atmosphere.xml` | 5 KB | **XML** | **código (nós)** |
| `terrain.bin` | 6,3 MB | binário | clonado da doadora |
| `navmesh.bin` | 4,5 MB | binário | **bake no editor** + pintura de IDs |
| `flora.bin` | 343 KB | binário | doadora/editor (opcional) |
| `ShaderCache/` | — | — | gerado sozinho |

## 2. Sistema de níveis (máscaras de visibilidade)

```xml
<levels>
  <level name="base" mask="1"/>      <level name="level_1" mask="2"/>
  <level name="level_2" mask="4"/>   <level name="level_3" mask="8"/>
  <level name="siege" mask="16"/>    <level name="civilian" mask="32"/>
</levels>
```
Cada entidade pertence a combinações (muralha nível 2 = base+level_2+siege...). É assim
que UMA cena serve aos 3 níveis de castelo da campanha. `north_angle` (42.6° aqui) define
o norte; `scene_scale_multiplier` 1.5.

## 3. Censo de scripts (o que uma cena de cerco realmente contém)

| Script | Qtde | Papel |
|---|---|---|
| `StrategicArea` | 131 | pontos de arqueiro individuais (ver §9) |
| `barrier_builder` | 99 | barreiras de IA por path (ver §11) |
| `DestructableComponent` | 89 | merlões/destrutíveis |
| `AnimationPoint` | 76 | figuração ambiente |
| `AmbientSoundEmitter` | 56 | som |
| `TacticalPosition` | 54 | posições de formação p/ IA |
| `LightCycle` | 30 | luzes dia/noite |
| `WallSegment` | 22 | muralhas assaltáveis (§4) |
| `MangonelSpawner` | 15 | catapultas (2 lados × níveis) |
| `DeploymentPoint` | 14 | slots de engenho no deployment |
| `SiegeLadderSpawner` | 12 | escadas (2 lados × 3 níveis × 2) |
| `BallistaSpawner` | 12 | balistas |
| `SiegeTowerSpawner` | 6 | torres (2 lados × 3 níveis) |
| `CastleGate` | 6 | portões (externo+interno × 3 níveis) |
| `TrebuchetSpawner` | 4 | trebuchets (atacante) |
| `BatteringRamSpawner` | 3 | aríete (1 × nível) |
| `StonePile`/`VolumeBox`/`StandingPoint*` | 3+3+17 | pilhas de pedra arremessável |
| `path_converger` | 1 | convergência de paths |

## 4. Muralhas assaltáveis (o coração)

Hierarquia real extraída (bloco verbatim no fim do doc):

```
game_entity  "sturgia_castle_wall_a_l3_broken_right"   ← PAI, tag right_wall_lvl3
 ├─ script WallSegment (9 navmesh IDs + SideTag)
 ├─ child "..._solid"   tag solid_child   (com wait_pos/middle_pos)
 └─ child "..._broken"  tag broken_child  (com attacker_wait_pos; merlões
      DestructableComponent com DestructionStates="debris_sturgia_l3")
```

As 9 variáveis de navmesh do `WallSegment` (valores reais da muralha direita lvl3):

| Variável | Valor | Significado |
|---|---|---|
| `_properGroundOutsideNavmeshID` | 328 | chão externo íntegro |
| `_properGroundInsideNavmeshID` | 391 | chão interno íntegro |
| `_underDebrisOutsideNavmeshID` | 342 | sob escombros, fora |
| `_underDebrisInsideNavmeshID` | -1 | (não usado = -1) |
| `_overDebrisOutsideNavmeshID` | 371 | sobre a brecha, fora |
| `_overDebrisInsideNavmeshID` | 381 | sobre a brecha, dentro |
| `_underDebrisGenericNavmeshID` | 341 | genérico sob |
| `_overDebrisGenericNavmeshID` | 361 | genérico sobre |
| `_onSolidWallGenericNavmeshID` | 351 | adarve da muralha íntegra |
| `SideTag` | right | esquerda/direita/meio |

Tags de alvo por nível: `left_wall_lvl1..3`, `right_wall_lvl1..3` (1 muralha
atacável por lado por nível — 6 alvos no total; os outros 16 WallSegments são as peças
filhas com IDs -1).

## 5. Portões — 6 no total (externo+interno × 3 níveis)

Tags `outer_gate` / `inner_gate` (3 de cada). Script `CastleGate` com só 2 variáveis:
`NavigationMeshId` (502 externo / 521 interno) e `NavigationMeshIdToDisableOnOpen`
(512 / 531). HPs vêm do prefab por nível (doc: 12000/15000/18000 externo, 1000 interno).
Família Vlandia: prefabs `European_castle_gate_outer_l1/l2` etc.

## 6. Escadas — 12 (`SiegeLadderSpawner`)

Variáveis reais de uma escada esquerda lvl2:
- `SideTag`=left, `TargetWallSegmentTag`=left_wall_lvl2, `OnWallNavMeshId`=203
- `UpperStateRotationDegree`=-27.2, `DownStateRotationDegree`=89.74 (encaixe)
- `TacticalPositionWidth`=4.0
- `BarrierTagToRemove`=left_ladder_barrier_a (a barreira de IA que some quando a escada
  levanta)
- `IndestructibleMerlonsTag`=merlon_solid_left (merlões que nunca quebram, onde a escada
  encosta — tags `merlon_solid_left/right`, 12 de cada na cena)
- 4 transforms embutidos: `fork_holder`, `initial_wait_pos`, `use_push`, `distance_holder`

IDs de navmesh das escadas observados: **202/203 (esquerda), 302/303 (direita)**.

## 7. Torres de cerco — 6 (`SiegeTowerSpawner`)

- `TargetWallSegmentTag` (ex.: right_wall_lvl2), `PathEntityName` (ex.:
  `siege_tower_path_right` — ver §10), `RampRotationDegree` (~91-92°),
  `BarrierLength`, `ai_barrier_l/r` (transforms), `BarrierTagToRemove`
  (`tower_barrier_lvlN_side`), `wait_pos_ground` (transform).
- Merlões removidos na chegada: tag `tower_merlon` (6 na cena).

## 8. Aríete — 3 (`BatteringRamSpawner`, um por nível)

`SideTag`=middle, `GateTag`=outer_gate, `PathEntityName`=`ram_path`/`ram_path_lvl1`/
`ram_path_lvl2`, `wait_pos_ground`.

## 9. Arqueiros e táticas

- **131 × `strategic_archer_point`** (prefab), script `StrategicArea`:
  `_side`=Defender/Attacker, `_distanceToCheck`=40 (def)/60 (atk), `_width`/`_depth`
  (1×1 = 1 tropa), `_ignoreHeight`=true.
- **9 × `defender_archer_position` + 2 × `attacker_archer_position`** (posições grandes,
  tags `archer_position`/`archer_position_attacker`) — cada grande precisa ter pequenas
  no raio.
- **54 × `TacticalPosition`**: `_width` em metros, `_tacticalPositionType` (Regional...),
  usados em wait_pos/middle_pos.
- Coberturas de balista defensora: tags `ballista_cover_defender_l{1..3}_{a..d}`.

## 10. Paths (ferramenta de caminhos) — a espinha logística

Nomes reais: `siege_tower_path_left|right` (+ variantes `_lvl1`/`_lvl3`), `ram_path`
(+ `_lvl1`/`_lvl2`), `murder_hole_l1..3`, e **~90 paths `barrier_*`** que alimentam os
99 `barrier_builder` (barreiras de IA por nível: `barrier_lvl2_a`...,
`barrier_gatehouse_lvl1_c`, `barrier_left_lvl2_aa`...). Barreira em cerco não é exceção,
é o grosso do trabalho de contenção da IA.

## 11. Pilhas de pedra, munição e câmeras

- 3 × `throwable_rock_pile`: `StonePile` (8 pontos) + `throw_pos` + `volume_box`
  conectados por tag (`volumebox`, 3×); tags `ammopickup` (11) e `throwing` (6).
- Câmeras estratégicas: entidades com tags `strategycameraattacker` /
  `strategycameradefender` (1 de cada).

## 12. Esquema de navmesh IDs observado (a "tabela de pintura")

| Faixa | Uso |
|---|---|
| 202-203 / 302-303 | escadas esquerda / direita (por nível) |
| 228-291 | estados de muralha (esq.) |
| 328-391 | estados de muralha (dir.): chão 328/391, brecha 341-381, adarve 351 |
| 502/512 | portão externo / faixa desativada ao abrir |
| 521/531 | portão interno / idem |

Na nossa cena gerada, EU defino esse esquema no XML e entrego a tabela "pinte a face X
com ID Y" — o trabalho de editor vira pintar-por-números + bake.

## 13. ⚠️ Discrepâncias doc oficial × cena real (importante!)

A doc manda; a cena shipped **não tem**:
- `sp_battle_set` (16 spawns por lado/tropa/fase): **zero** na cena;
- limites de deployment (`walk_area_vertex`/`deployment_castle_boundary_N`): **zero**;
- linhas de fuga (`Flee_line_attacker/defender`): **zero**.

Leitura: o runtime tem fallback (deployment computado das câmeras estratégicas +
geometria) e a cena vanilla confia nele. Para a NOSSA cena: **seguir a doc e incluir**
os três — custo baixo em XML e elimina a aposta no fallback. Se o fallback bastar,
remove-se depois.

## 14. Receita para a cena RF (Vlandia, arranjo de imagem do autor)

1. **Doadora**: clonar pasta de cena vanilla com terreno compatível com o esboço
   (`terrain.bin`, `flora.bin`; `navmesh.bin` será refeito).
2. **Eu gero `scene.xscene`**: muralhas família Vlandia (`european_castle_wall_*`,
   portões `European_castle_gate_*`) no traçado da imagem; 6 alvos de muralha
   (esq/dir × 3 níveis) com hierarquia solid/broken; 6 portões; 12 escadas, 6 torres,
   3 aríetes com paths; mangonels/balistas/trebuchets + 14 DeploymentPoints;
   archer positions grandes+pequenas; TacticalPositions; barreiras por path; pilhas de
   pedra; câmeras; spawns e boundaries (§13); esquema de navmesh IDs pré-preenchido.
3. **Eu gero `atmosphere.xml`** (formato §15 — trivial).
4. **Autor no editor**: bake do navmesh, pintura de IDs pela tabela, prints → eu itero
   coordenadas.

## 15. atmosphere.xml (formato)

`<atmosphere><values>` com `time_of_day`, `season`, `snow_density`, `color_grade_name`,
`skybox_background_texture_name`... + blocos `global_ambient`, `fog`, sol. Texto puro,
sem segredo.

---
*Bloco verbatim de referência (muralha lvl3 direita) preservado no histórico da
dissecação; o padrão completo de qualquer elemento pode ser re-extraído do
`sturgia_castle_siege_001/scene.xscene` com os greps deste estudo.*
