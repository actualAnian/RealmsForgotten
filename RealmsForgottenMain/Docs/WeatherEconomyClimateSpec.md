# Realms Forgotten Weather Economy Climate Spec

This document defines the intended climate mapping and economic design for the custom weather system.

It is a design specification only. It does not change gameplay by itself.

## Goals

- Replace the current culture-only weather flavor system with a climate model that can drive economy.
- Make weather affect:
  - production
  - availability of goods
  - caravan throughput
  - town food pressure
  - market prices through scarcity
- Support special settlement exceptions without requiring exact map coordinates.

## Core Design

Weather should not directly change prices first.

The intended chain is:

1. Climate region determines likely weather.
2. Weather affects production and transport.
3. Production and transport affect market availability.
4. Availability affects prices.

This keeps the economy feeling organic instead of arbitrary.

## Region Model

The system should use:

1. `culture -> default climate region`
2. `settlement_id -> climate override`
3. later, if needed: `village_id -> climate override`

Culture should be the fallback, not the only source of truth.

This is necessary because some factions occupy more than one climate band.

## Climate Regions

### 1. Tropical Rainforest / Monsoon

Used for:
- `giant`

Behavior:
- high rain chance
- flooding possible
- high abundance chance
- low drought chance

Economic identity:
- strong food growth when stable
- strong disruption during floods
- transport disruption in bad weather

### 2. Savannah

Used for:
- `south_realm`

Behavior:
- dry but not true desert
- drought possible
- abundance during favorable periods
- moderate rain

Economic identity:
- good livestock/grain cycle
- weather should swing output more than temperate climates

### 3. Savannah to Temperate Transition

Used for:
- `north_realm`

Behavior:
- warmer and drier than empire
- less extreme than full savannah
- moderate abundance
- occasional dry stress

Economic identity:
- stable mixed production
- better harvests than steppe, but more drought pressure than wet temperate

### 4. Dry Temperate / Continental Transition

Used for:
- `west_realm`

Behavior:
- drier than normal empire
- steppe influence
- lower rain frequency
- moderate drought chance

Economic identity:
- less flood pressure
- more harvest volatility than classic temperate
- suitable for grain and herd economies with occasional shortages

### 5. Full Steppe

Used for:
- `khuzait`

Behavior:
- low rainfall
- dry cycles
- abundance possible in favorable seasons
- low flooding

Economic identity:
- caravan and herd economy
- food volatility tied to dry pressure

### 6. Perma-Winter / Polar Cold

Used for:
- `sturgia`
- `dwarf`
- `urkhai`

Behavior:
- blizzard/snow dominant
- very low drought
- abundance should be rare and modest

Economic identity:
- transport disruption is more important than flooding
- imported food pressure should be higher
- wool, hides, fish, timber become strategically important

### 7. Sacred Balanced Mountain Climate

Used for:
- `elvean`

Behavior:
- all seasons in balance
- very low disaster chance
- high stability
- mild abundance more common than catastrophe

Economic identity:
- low volatility
- stable supply
- premium and refined goods economy makes sense here

### 8. Swamp Temperate

Used for:
- `tharnmar`

Behavior:
- high rain
- flooding possible
- moderate abundance
- almost no drought

Economic identity:
- strong flood/logistics pressure
- swamp roads should feel unreliable

### 9. Central European Temperate

Used for:
- `wulf`

Behavior:
- Germany-like weather
- regular rain
- cool but not polar
- stable agricultural cycle

Economic identity:
- balanced economy
- moderate abundance
- low extremes

### 10. Mild Atlantic / South-France Temperate

Used for:
- `grimwatch`

Behavior:
- mild temperate
- moderate rain
- high harvest stability
- low disaster intensity

Economic identity:
- reliable food production
- modest abundance
- lower scarcity spikes than harsher regions

### 11. Hot Temperate Plains

Used for:
- `nasorian`

Behavior:
- temperate to hot plains
- good spring
- rainy summer
- moderate abundance

Economic identity:
- strong seasonal grain output
- not as dry as steppe
- less harsh than desert

### 12. Harsh Desert

Used for:
- `aserai`
- `aqarun`

Behavior:
- drought dominant
- abundance rare
- almost no flooding except via explicit override zones

Economic identity:
- high food scarcity pressure
- pack animals, trade, and wells/oases become important

### 13. Semi-Arid Coastal / Mediterranean

Used for:
- default `nord`

Behavior:
- semi-arid coast
- Mediterranean influence
- mild rain cycle
- moderate abundance

Economic identity:
- more stable than desert
- drier than classic temperate
- coastal trade identity

### 14. Winter Nord Exception

Used for:
- `town_Nord4`
- `town_Nord5`
- `town_Nord6`
- `castle_Nord5`
- `castle_Nord6`
- `castle_Nord7`

Behavior:
- Norway-like winter climate
- should behave much closer to the perma-winter group than to default nord coast

Economic identity:
- cold transport pressure
- higher winter scarcity

## Culture Default Mapping

- `giant` -> `tropical_rainforest`
- `south_realm` -> `savannah`
- `north_realm` -> `savannah_temperate_transition`
- `west_realm` -> `dry_temperate_transition`
- `khuzait` -> `steppe`
- `sturgia` -> `perma_winter`
- `dwarf` -> `perma_winter`
- `urkhai` -> `perma_winter`
- `elvean` -> `sacred_balanced_mountain`
- `tharnmar` -> `swamp_temperate`
- `wulf` -> `central_european_temperate`
- `grimwatch` -> `mild_temperate_atlantic`
- `nasorian` -> `hot_temperate_plains`
- `aserai` -> `harsh_desert`
- `aqarun` -> `harsh_desert`
- `nord` -> `semiarid_mediterranean_coast`

## Settlement Overrides

The following settlements should override default `nord` climate:

- `town_Nord4` -> `winter_nord`
- `town_Nord5` -> `winter_nord`
- `town_Nord6` -> `winter_nord`
- `castle_Nord5` -> `winter_nord`
- `castle_Nord6` -> `winter_nord`
- `castle_Nord7` -> `winter_nord`

Further overrides can be added later for:
- northern `north_realm` fiefs
- steppe-side `west_realm` fiefs
- river/coastal special cases

## Suggested Weather Weights By Region

These are design targets, not code values.

### tropical_rainforest
- Clear: low
- Rainy: high
- Flooding: medium
- Abundance: medium-high
- Drought: almost none
- Blizzard: none

### savannah
- Clear: medium
- Rainy: medium
- Flooding: low
- Abundance: medium
- Drought: medium
- Blizzard: none

### savannah_temperate_transition
- Clear: medium
- Rainy: medium
- Flooding: low
- Abundance: medium
- Drought: low-medium
- Blizzard: none

### dry_temperate_transition
- Clear: medium
- Rainy: low-medium
- Flooding: low
- Abundance: medium
- Drought: low-medium
- Blizzard: low

### steppe
- Clear: medium
- Rainy: low
- Flooding: very low
- Abundance: medium
- Drought: medium
- Blizzard: low in winter-like cycles only if later seasonal depth is added

### perma_winter
- Clear: low-medium
- Rainy: very low
- Flooding: very low
- Abundance: low
- Drought: none
- Blizzard: high

### sacred_balanced_mountain
- Clear: high
- Rainy: low-medium
- Flooding: low
- Abundance: medium
- Drought: very low
- Blizzard: very low

### swamp_temperate
- Clear: low-medium
- Rainy: high
- Flooding: medium-high
- Abundance: medium
- Drought: almost none
- Blizzard: low

### central_european_temperate
- Clear: medium
- Rainy: medium
- Flooding: low-medium
- Abundance: medium
- Drought: low
- Blizzard: low

### mild_temperate_atlantic
- Clear: medium
- Rainy: medium
- Flooding: low
- Abundance: medium-high
- Drought: low
- Blizzard: very low

### hot_temperate_plains
- Clear: medium
- Rainy: medium
- Flooding: low
- Abundance: medium-high
- Drought: low-medium
- Blizzard: none

### harsh_desert
- Clear: medium
- Rainy: very low
- Flooding: almost none
- Abundance: low
- Drought: high
- Blizzard: none

### semiarid_mediterranean_coast
- Clear: medium
- Rainy: low-medium
- Flooding: low
- Abundance: medium
- Drought: low-medium
- Blizzard: none

### winter_nord
- Clear: low-medium
- Rainy: very low
- Flooding: very low
- Abundance: low
- Drought: none
- Blizzard: high

## Economic Design

### Weather should first affect production

Primary layer:
- village production multiplier by village type
- village hearth pressure
- town food pressure
- caravan throughput / route friction

### Then it should affect market availability

Examples:
- drought reduces grain, flax, grapes, dates
- flooding reduces clay, olive, grain, village access
- blizzard reduces transport reliability and imported food flow
- abundance improves farm output and lowers scarcity

### Then price should react to scarcity

Price changes should be strongest in:
- food
- animals / pack animals
- raw materials
- construction goods

Luxury goods should be less weather-sensitive.

## Suggested Village-Type Sensitivity

Examples only:

- `grain_farm`
  - bad: drought, flooding, blizzard
  - good: abundance

- `flax_plant`
  - bad: drought
  - mixed risk: flooding
  - good: abundance

- `olive_trees`
  - bad: flooding
  - mixed: drought
  - good: abundance

- `vineyard`
  - bad: drought, flooding
  - good: abundance

- `cattle_farm`, `sheep_farm`, `hog_farm`
  - bad: drought, blizzard
  - moderate risk: flooding

- `clay_mine`
  - bad: flooding
  - mostly neutral: drought

- `iron_mine`, `silver_mine`
  - largely weather-resistant production
  - but transport should still suffer in flood/blizzard

- `fishery`
  - bad: blizzard
  - mixed: flooding
  - stable in temperate wet climates

## Duration Design

Weather should also have duration/intensity logic:

- short duration:
  - mostly morale and travel effects
- medium duration:
  - stock pressure and village output effects
- long duration:
  - prosperity, hearth, and major scarcity effects

This prevents every climate event from feeling like an instant economic shock.

## Known Problems In Current System

Current implementation limitations:

- Weather is tied mostly to culture, not geography.
- `OnSettlementTick` currently exits on `!settlement.IsFortification`, which prevents village weather effects from executing.
- Current weather effects mostly touch:
  - morale
  - prosperity
  - loyalty
- Current trade model does not use weather in price calculations.
- Current settlement economy model does not use weather for general demand/availability.

## Recommended Implementation Order

### Phase 1
- fix village weather application path
- add climate region defaults + settlement overrides

### Phase 2
- add weather -> village type production multipliers
- add weather -> caravan throughput penalties

### Phase 3
- add weather -> town stock / food pressure
- add scarcity-aware trade price adjustments

### Phase 4
- add duration and intensity
- add more refined regional overrides

## Final Principle

The system should feel like:

weather changes the land,
the land changes production,
production changes availability,
availability changes price.

That is the intended Realms Forgotten weather-economy model.
