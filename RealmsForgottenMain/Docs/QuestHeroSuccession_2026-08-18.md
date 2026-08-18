# Sucessão de heróis de missão (2026-08-18)

## O problema relatado

Um jogador avançou na missão até a parte de falar com o **rei dos Dugrast** e o rei não
existia mais — tinha morrido na campanha. A missão travou sem aviso.

Causa: o diálogo era travado num id fixo.

```csharp
// SeventhQuest.cs, antes
bool heroMatch = Hero.OneToOneConversationHero?.StringId == "lord_dugrast_faction_1";
```

`lord_dugrast_faction_1` é o Borgul Tharn, dono do clã `dugrast_faction_1` e **rei do
`dwarf_kingdom`** (RF_Core_III/spkingdoms.xml). Ele é um lorde normal: envelhece, vai à
guerra e morre. Quando morre, aquela condição nunca mais é verdadeira e não existe
caminho alternativo — a missão fica sem saída.

O mesmo padrão aparecia em mais quatro lugares, todos com o mesmo destino:

| Onde | Id fixo | Sintoma se morresse |
|---|---|---|
| `SeventhQuest` (2x) | `lord_dugrast_faction_1` | diálogo do rei anão nunca aparece |
| `QuestLibrary.AnoritLord`, `AnoritFindRelicsQuest` | `lord_WE9_l` | inquérito "Error initializing the quest hero AnoritLord" |
| `SixthQuest.Lord2_1` | `lord_2_1` | `null` silencioso (usava `AllAliveHeroes`) |
| `MercenaryOfferBehavior` | `lord_3_1` | "Initialization failed: lord_3_1 or his kingdom is null" |
| `QuestLibrary.QuestQueen`, `AnoritFindRelicsQuest` | `Kingdom "empire" → Leader.Spouse` | exceção no `First(...)` ou inquérito de erro |

## A solução

`Quest/QuestHeroSuccession.cs` troca "id de herói" por **papel de missão**, e o papel tem
linha de sucessão. Ordem, do mais para o menos legítimo:

1. **substituto já escolhido** antes (e ainda vivo) — o papel não fica pulando de dono;
2. **o herói original**, se estiver vivo (caso normal, custo de um `GetObject` + `IsAlive`);
3. **quem ocupa o trono** do reino do papel — para papel de rei este é o herdeiro certo,
   e a sucessão vanilla já escolheu alguém quando o rei morreu;
4. **o clã do original**: o líder atual, ou o nobre mais velho que sobrou no clã;
5. **mesma cultura**: nobre vivo, preferindo reis, depois líderes de clã, depois clã de
   maior tier/renome — foi a ideia levantada no feedback.

A escolha é persistida (`QuestHeroSuccessionBehavior`, `Dictionary<string, Hero>` no
`QuestTypeDefiner`), então o papel não troca de dono a cada carregamento. Quando alguém
herda, o jogador recebe uma mensagem: *"X is dead. Y now answers as King of the Dugrast."*

### Papéis registrados

| Papel | Original | Trono | Cultura | Herança por cultura |
|---|---|---|---|---|
| `dwarf_king` | `lord_dugrast_faction_1` | `dwarf_kingdom` | `dwarf` | sim |
| `anorit_lord` | `lord_WE9_l` | — | `west_realm` | sim |
| `dread_king` | `lord_2_1` | — | (do herói) | sim |
| `mercenary_lord_3_1` | `lord_3_1` | — | (do herói) | sim |
| `the_owl` | `rf_the_owl` | — | — | **não** |

O Owl é personagem único de história: não faz sentido um nobre qualquer da mesma cultura
"herdar" o papel dele, então esse papel morre com o dono e quem chama continua tratando
`null` como já tratava.

### Uso

```csharp
// condição de diálogo
.Condition(() => QuestHeroes.IsInConversation(QuestHeroes.DwarfKing))

// obter o herói do papel (pode ser null)
Hero lord = QuestHeroes.Resolve(QuestHeroes.AnoritLord);

// consorte do trono ("a rainha"), sem estourar se o reino sumiu
Hero queen = QuestHeroes.ResolveRulerConsort("empire");
```

Para registrar um papel novo é só acrescentar um `QuestHeroRole` na lista em
`QuestHeroes.Roles`.

## Estado

Build limpo, DLL já em `Modules/RealmsForgotten`. **Não testado em jogo.** Roteiro de
teste sugerido:

1. Campanha nova: falar com o rei anão normalmente — nada deve mudar.
2. Save existente com o rei vivo: idem (o papel resolve para o original).
3. Matar o rei via cheat (`campaign.kill_hero lord_dugrast_faction_1` ou equivalente) e
   voltar a falar com quem herdou o trono anão: o diálogo da missão deve aparecer nele, e
   a mensagem de herança deve ter saído no log.
4. Salvar/carregar depois da troca: o papel deve continuar no mesmo herdeiro.

## O que ficou de fora

Não impedi que esses heróis morram (marcá-los como imortais mudaria o comportamento de
campanha e escondiria o problema em vez de resolvê-lo). Se em algum momento fizer sentido
proteger só os de missão principal, dá para fazer por cima disso — mas a sucessão continua
sendo necessária para os saves onde o herói **já** morreu.
