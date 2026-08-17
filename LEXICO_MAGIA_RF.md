# Lexico da magia do RealmsForgotten

Idioma construido do autor. Os NOMES estao no codigo e nos XML; os SIGNIFICADOS so existem aqui.
Fechado em 2026-08-03. Nunca inventar raiz nova sem o autor.

## Raizes

### Do autor
| raiz | significado |
|---|---|
| thyrn | primeira chama (de thyrnan, acender) |
| kaeth | saber pratico, oficio |
| kael | iniciado, controle |
| bael | grande fogo (de bael) |
| vorn / vornir | tecelagem ritual, tecelao |
| vel | alto, ceu, firmamento (de wela) |
| karun | trovao gutural (raiz anaica) |
| aer | luz sagrada, elevado, puro |
| luthien | protecao radiante |
| edra | seiva vital, a vida que corre (de aedre) |
| sul | casca, espinho / excesso, transbordar |
| ulfr | lobo (nordico) |
| hal | o chamado, a cancao da matilha |
| orand | minerio (de ora, extrair) |
| ur | transformar, forjar no calor |
| morg | definhamento, apodrecer (de morgan) |
| syl | o toque silencioso |
| bar | tumulo ancestral, monticulo funerario (de beorg) |
| gulath | erguer, despertar (parente de gul, espectro) |
| dwimm | feiticaria ilusoria, engano (de dwimor) |
| vaen | sombra, corrompido (de faen) |
| elda | os primeiros, os ancestrais |
| sirion | canto da criacao, fluxo do poder puro |
| hring | anel, circulo |
| pyr | perfurar |
| nael | agulha, foco extremo |
| gwaer | dispersao, espalhar |
| thael | preciso, medido / vinculo |
| hael | escudo, absorcao |
| dor | mao firme |
| maer | guardiao |
| saga | historia |
| hald | portador, cantor |
| gaer | forjado no momento |
| lore | saber escrito |
| sael | si mesmo |

### Derivadas por Claude (2026-08-03, pela mesma fonetica, aprovadas pelo autor)
| raiz | significado |
|---|---|
| sir | fluxo, corrente (encurtamento de sirion) |
| gul | espectro (encurtamento de gulath) |
| vaeg | lamina, gume |
| hrom | impacto que arremessa |
| nul | nevoa, exalacao |
| tor | pilar, muralha |
| krath | brasa, cinza ardente |
| orth | esfera, globo |
| krys | gelo, cristal |
| sythra | redemoinho |
| dae | sombra projetada |
| zul | grito, uivo |
| vorth | sangue, seiva tomada |
| orn | arvore |
| roth | raiz |
| khal | pedra |
| mael | ouro |
| varn | corrupcao, mancha |
| glir | brilho, reluzir |
| braek | ruptura |

## Escolas (SotorLores.cs — o LoreId NAO muda, so o Title)

| nome | LoreId | dominio |
|---|---|---|
| Thyrn-Kaeth | MinorMagic | magia menor, entrada |
| Bael-Vornir | LoreOfFire | fogo |
| Vel-Karun | LoreOfHeavens | ceus, astros |
| Aer-Luthien | LoreOfLight | luz |
| Edra-Sul | LoreOfLife | vida |
| Ulfr-Hal | LoreOfBeasts | feras |
| Orand-Ur | LoreOfMetal | metal, pedra |
| Morg-Syl | LoreOfDeath | morte |
| Bar-Gulath | LoreOfNecromancy | necromancia |
| Dwimm-Vaen | DarkMagic | magia sombria |
| Elda-Sirion | HighMagic | alta magia |

## Perks de Spellcraft (SotorPerks.cs)

| nivel | nome | era |
|---|---|---|
| 25 | Thyrn-Kael | Novice Spellcaster |
| 75 | Sael-Vorn | Selfish |
| 75 | Dor-Kael | Well Controlled |
| 125 | Kaeth-Vorn | Adept Spellcaster |
| 175 | Bael-Sul | Overcaster |
| 175 | Thael-Vorn | Efficient Spellcaster |
| 200 | Vorn-Aer | Master Spellcaster |
| 225 | Vel-Gaer | Improvision |
| 225 | Ur-Thyrn | Catalyst |
| 250 | Morg-Hael | Dampener |
| 250 | Elda-Thael | Arcane Link |
| 275 | Lore-Maer | Librarian |
| 275 | Saga-Hald | Storyteller |
| 300 | Archmage | (mantido) |

## Perks de Arcane (RFPerks.cs + str_skills.xml)

Pares exclusivos por nivel: Talisman age no DANO, Staff no RAIO.

| nivel | eixo | nome | fator |
|---|---|---|---|
| 50 | dano | Thael-Pyr | x1.1 |
| 50 | raio | Hring-Kael | x1.15 |
| 100 | dano | Pyr-Dor | x1.2 |
| 100 | raio | Hring-Vorn | x1.3 |
| 150 | dano | Pyr-Nael | x1.3 |
| 150 | raio | Hring-Gwaer | x1.5 |

## Feiticos (97)

O StringID NUNCA muda — e por ele que templates, focos, culturas e efeitos se ligam.

### Thyrn-Kaeth  (MinorMagic)

| nome | StringID |
|---|---|
| Aer-Nael | `RfWhiteParticle` |
| Dôr-Kaeth | `StrengthOfCombat` |
| Gwaer-Nûl | `DustStorm` |
| Hael-Kael | `AuraOfResistance` |
| Hael-Pŷr | `WardOfArrows` |
| Hrôm-Gwaer | `RfKineticForce` |
| Kaeth-Edra | `MinorHeal` |
| Kaeth-Sîr | `RfMysticEnergy` |
| Morg-Nûl | `FoetidCloud` |
| Thyrn-Pŷr | `Dart` |
| Thyrn-Vaeg | `EnchantWeapon` |

### Bael-Vornir  (LoreOfFire)

| nome | StringID |
|---|---|
| Bael-Krath | `CinderBlast` |
| Bael-Orth | `Fireball` |
| Bael-Sythra | `FlameStorm` |
| Bael-Tôr | `RfFlameWall` |
| Bael-Zûl | `BurningHead` |
| Maegôr | `FlamingSwords` |
| Rhûvael | `BoltOfAqshy` |
| Thyrn-Sûl | `Sear` |
| Vorn-Baelth | `CascadingFireCloak` |

### Vel-Karûn  (LoreOfHeavens)

| nome | StringID |
|---|---|
| Aelvindor | `CometOfCasandora` |
| Dae-Karûn | `CurseOfMidnightWind` |
| Kârendil | `Thunderbolt` |
| Karûn-Hrîng | `ChainLightning` |
| Karûn-Pŷr | `LightningBolt` |
| Sîr-Vel | `HarmonicConvergence` |
| Vel-Gûl | `RfCosmicGhost` |
| Vel-Gwaer | `WindBlast` |
| Vel-Krys | `RfIcePurge1` |
| Vel-Krysandel | `RfIcePurge3` |
| Vel-Krysar | `RfIcePurge2` |
| Vel-Sythra | `Blizzard` |

### Aer-Lúthien  (LoreOfLight)

| nome | StringID |
|---|---|
| Aer-Edra | `HealingLight` |
| Aerîs | `PhaProtection` |
| Aer-Sîr | `RadiantGaze` |
| Elyndor | `ShemGaze` |
| Glîr-Nael | `DeathlyShards` |
| Glîr-Tôr | `RadiancePillar` |
| Lôrendil | `BironasTimewarp` |
| Lûthmîr | `NetOfAmyntok` |

### Edra-Sûl  (LoreOfLife)

| nome | StringID |
|---|---|
| Edra-Bael | `SummerHeat` |
| Edra-Roth | `Regrowth` |
| Edra-Sythra | `StormOfRenewal` |
| Edra-Vorth | `DrainLife` |
| Orn-Sîr | `TheGreenEye` |
| Roth-Gûlath | `DwellersBelow` |
| Sûl-Hael | `ShieldOfThorns` |
| Sûl-Orn | `Barkskin` |

### Ulfr-Hâl  (LoreOfBeasts)

| nome | StringID |
|---|---|
| Faen-Gûr | `PannsImpenetrablePelt` |
| Hâl-Gwaer | `FlockOfDoom` |
| Hrôthvaen | `CurseOfAnraheir` |
| Krys-Sythra | `HailStorm` |
| Sûl-Roth | `TanglingThorn` |
| Ulfr-Pŷr | `AmberSpear` |
| Ulfr-Zûl | `TheBeastUnleashed` |

### Ulfr-Hâl (oculto)  (LoreOfBeastsHidden)

| nome | StringID |
|---|---|
| U | `l` |
| A | `m` |

### Orand-Ûr  (LoreOfMetal)

| nome | StringID |
|---|---|
| Khal-Hrôm | `RfEarthRocks` |
| Maelgôr | `GehennasGoldenHounds` |
| Mael-Ûr | `FinalTransmutation` |
| Orand-Hael | `MeteroicIronclad` |
| Orand-Nael | `GleamingArrow` |
| Orvandîl | `GehennasGoldenGlobe` |
| Ûr-Vaeg | `QuicksilverSwords` |
| Varn-Ûr | `PlagueOfRust` |

### Morg-Syl  (LoreOfDeath)

| nome | StringID |
|---|---|
| Dûskara | `FateOfBjuna` |
| Morg-Krath | `AshesAndDust` |
| Morg-Zûl | `RfSkullTerror` |
| Syl-Edra | `Amaranth` |
| Syl-Morgath | `TasteOfDeath` |
| Sylvâeth | `CaressOfLaniph` |
| Syl-Vorth | `SpiritLeech` |
| Zervâel | `PurpleSun` |

### Bar-Gûlath  (LoreOfNecromancy)

| nome | StringID |
|---|---|
| Bar-Gûl | `SummonSkeleton` |
| Bar-Hâl | `GraveCall` |
| Bar-Morgûl | `NagashGaze` |
| Dae-Vorth | `Shadowblood` |
| Gûl-Gwaer | `WindOfDeath` |
| Morg-Sâga | `CurseOfYears` |
| Morkhârn | `Morkharn` |
| Vârghûl | `VanHelsDanseMacabre` |
| Vorth-Gwaer | `RfBloodWave` |

### Dwimm-Vaen  (DarkMagic)

| nome | StringID |
|---|---|
| Dae-Pŷr | `DoomBolt` |
| Dwimm-Sîr | `PowerOfDarkness` |
| Dwimm-Vorth | `SoulStealer` |
| Dwimm-Zûl | `ScreamingSkull` |
| Krys-Vaen | `Chillwind` |
| Vaen-Braek | `DeathSpasm` |
| Vaen-Gwaer | `SoulRain` |
| Vaen-Morgûl | `BlackHorror` |

### Elda-Sîrion  (HighMagic)

| nome | StringID |
|---|---|
| Elda-Aer | `Apotheosis` |
| Elda-Bael | `FieryConvocation` |
| Elda-Dôr | `MindControl` |
| Eldarîon | `CourageOfAenarion` |
| Lúth-Vaenor | `ShieldOfSaphery` |
| Sîrion-Sythra | `Tempest` |
| Sîr-Pŷr | `CurseOfArrowAttraction` |
| Sîr-Vorth | `SoulQuench` |
