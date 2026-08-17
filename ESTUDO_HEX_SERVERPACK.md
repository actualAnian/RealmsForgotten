# Estudo: Hex Server Pack — como outro dev integrou um mod ao Coop

Data: 2026-08-15. Fonte: `F:\Downloads\HexServerPack1` (decompilado com ilspycmd,
só para aprender a ABORDAGEM — não para copiar código). "Hex's Tool" está na
lista de projetos autorizados do time do Coop.

## O que o pack contém

Dois módulos:

1. **CoopRealmServer** (`Id=CoopRealmServer`, v0.1.2) — é o **Coop repackado**:
   mesmo `Coop.dll` / `Coop.CoopMod` / mesmo conjunto de DLLs, só com Id/Nome
   próprios. Versão **v0.1.2**, uma acima da v0.1.1 do workshop. Não traz exe de
   servidor dedicado — só `Coop.CrashReporter.exe` + `mod-config.default.json`.
   → **Confirma: mesmo o "server pack" hospeda dentro do jogo, não há servidor
   headless.** Bate com o que passamos sobre hospedar no próprio PC.

2. **HexServerPack** (v1.4.30) — conteúdo + comportamentos coop-aware + a ponte:
   - Carrega **antes** do Coop (`ModulesToLoadAfterThis` = Coop/CoopRealmServer).
   - Conteúdo próprio via XmlNode (culturas `hex_bosses_culture`, clãs, tropas
     `hex_bandit_king`/`hex_looter_troops`, skill sets) + `HexDeferredContentLoader`
     como rede de segurança que recarrega o conteúdo no início da campanha.
   - Behaviors PRÓPRIOS já pensados para coop: `BanditPartyRuntime`,
     `HexSettlementDefenseBehavior`, `HexStartingGoldBehavior`, `QuestGiverBehavior`,
     `RepeatableLordQuestBehavior`, `MercTownRecruitBehavior`, `HexCompanionEnsureBehavior`.
   - DLLs: `HexServerPack.dll`, `HexServerPack.CoopBridge.dll` (a ponte),
     `HexServerPack.McmBridge.dll`, `HexServerPack.BannerEditor.dll`.

## A técnica central (HexServerPack.CoopBridge) — o ouro

Como sincronizar estado de um mod DENTRO do Coop, sem AutoSync:

1. **Pega os serviços do Coop pelo container DI**, via `GameInterface.ContainerProvider.TryResolve<T>(ref x)`:
   `IMessageBroker`, `INetwork`, `IPlayerManager`, `IControllerIdProvider`,
   `IObjectManager`, `ISerializableTypeMapper`. (Resolve tanto por referência
   direta quanto por reflexão `FindType("GameInterface.ContainerProvider")` como
   fallback — degrada de boa se o Coop não estiver presente.)

2. **Detecta a sessão por POLLING**, não por patch: `Initialize()` roda todo tick
   (`OnApplicationTick`) e fica tentando resolver o container até o Coop subir a
   DI (que só existe quando a sessão inicia). Quando resolve, constrói um
   `Transport`; se o container troca (nova sessão), reconstrói. → **Mais robusto
   que o meu hook em PatchAll.**

3. **Registra tipos de mensagem PRÓPRIOS** na rede do Coop:
   `ISerializableTypeMapper.AddTypes([...])` com structs/classes `Bridge*Request` /
   `Bridge*Message`. A partir daí essas mensagens viajam pelo canal confiável do
   Coop como qualquer mensagem nativa.

4. **Padrão de sync**:
   - **Servidor → clientes**: `Publish(...)` / `PublishForOwner(ownerHeroId, ...)`
     manda valores autoritativos (multiplicador de velocidade de party, mínimos/
     máximos de bandidos, taxa de spawn, multiplicadores de smithing, battle size,
     ofertas de quest, cooldown de quest de comércio). Clientes leem via getters
     locais (`GetClientPartySpeedMultiplier()`, etc.).
   - **Cliente → servidor**: `RequestAccept/Claim/DeliverGrain/Cancel`,
     `RequestMercHire`, `ApplyPlayerBanner`, `SendChat`, `RequestDeleteCharacter`.
     Cliente pede, servidor processa autoritativo e replica.

5. **Integra com os patches do PRÓPRIO Coop**: chega a chamar
   `GameInterface.Services.MobileParties.Patches.RecruitmentCampaignBehaviorPatch.UpdateVolunteersOfNotablesInSettlementPostfix`
   — ou seja, reaproveita a sincronização que o Coop já faz, em vez de brigar com ela.

## O que isso muda no NOSSO projeto (RF coop)

Comparado ao `RF_CoopCompat` atual (que só **desliga** RF em coop = estável mas
dormente), o Hex mostra o caminho para os recursos **funcionarem** em coop:

- **Adotar o polling de `ContainerProvider.TryResolve`** para detecção de sessão
  e para obter `IMessageBroker`/`INetwork`/`ISerializableTypeMapper` — substitui/
  complementa o meu hook em PatchAll e já entrega o canal de sync de graça.
- **Sincronizar estado do RF pelo padrão do Hex**: por sistema, registrar tipos
  de mensagem e publicar servidor→cliente / pedir cliente→servidor.
- **Precedente direto** para sistemas que o RF também tem: bandidos (RF tem spawn
  de bandidos), velocidade de party (RFPartySpeedModel), smithing (RFSmithing),
  quests, contratação de mercenários. O Hex já resolveu análogos.
- **Ordem de carregamento**: conteúdo/compat carrega ANTES do Coop
  (`ModulesToLoadAfterThis`). Vale revisar se o RF_CoopCompat deveria também.

## Ressalvas

- **É código de outro dev.** Aqui aprendi a ABORDAGEM (arquitetura/técnica), não
  copiei o código. Se formos reusar código do Hex de fato, pedir OK a ele.
- **Versão**: o pack usa Coop **v0.1.2** (via CoopRealmServer); o instalado é
  v0.1.1. Host e clientes têm que bater. Se adotarmos o CoopRealmServer, os dois
  lados usam ele.
- **War Sails**: o pack do Hex não parece exigir War Sails (é conteúdo de bosses/
  bandidos). Nosso caso ainda precisa do `DlcBlockNeutralizer` para o naval.
