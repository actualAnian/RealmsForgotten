# O que entregar aos testers (coop RF sem você presente)

Regra de ouro do Coop: **host e TODOS os clientes precisam ter módulos idênticos
— mesmos Ids e mesmas versões.** Qualquer divergência = conexão recusada. O jeito
mais seguro de garantir isso é distribuir as **pastas de módulo exatas** (arquivos
idênticos), não só "a mesma versão".

Para dois testers rodarem entre si, UM deles é o host (seu PC não precisa estar
ligado). Cada um precisa ter, na pasta `...\Mount & Blade II Bannerlord\Modules`:

## A) O que CADA tester obtém por conta própria (você NÃO distribui)

1. **Bannerlord + DLC War Sails** — cada um precisa **possuir** o jogo e o DLC
   (o War Sails é pago; não dá para distribuir). Mesma versão do jogo (1.4.8).
2. **Coop** — assinar o mesmo item no Steam Workshop e atualizar todos juntos,
   para a versão bater. (Alternativa: você congela a versão distribuindo a pasta
   `Coop` também — mas aí todos usam a sua cópia.)
3. **Mods de dependência**, nas MESMAS versões: Bannerlord.Harmony, ButterLib,
   UIExtenderEx, MCM (MBOptionScreen). Se houver risco de versões diferentes,
   melhor você incluir essas pastas no pacote (ver B) para garantir.

## B) O que VOCÊ entrega (zip com as pastas de módulo)

Estas não estão no Workshop / são suas — copie as **pastas inteiras** de
`Modules\` para um zip:

- **Todos os módulos do Realms Forgotten**, exatamente como você usa:
  `RealmsForgotten`, `RF_Magic`, `RF_Map`, `RF_Races`, `RF_Core_II`, `RF_Core_III`,
  `RF_Extension`, `RFMonsters`, `RF_DualWield`, e quaisquer outros RF/RBM que você
  tenha ativos.
- **RF_CoopCompat** — a pasta `Modules\RF_CoopCompat` inteira (o `.dll`, o
  `SubModule.xml` e o `rf_coop_compat.cfg`). Essencial: é o que destrava o War
  Sails no Coop e mantém o RF estável. **Não existe no Workshop.**
- (Opcional, para travar versões) as pastas de dependência do item A3, e/ou a
  pasta `Coop`.

> NÃO inclua `RF_CoopWarsails` — é o modo batalha standalone, não faz parte do
> coop de campanha.

## C) O que mais vai no pacote (texto)

1. **Lista exata de módulos ATIVOS e a ORDEM**, para todos replicarem igual. A
   regra que não pode furar: **RF_CoopCompat é o ÚLTIMO** da lista; War Sails
   LIGADO; Coop antes do RF_CoopCompat.
2. **Instrução de desbloquear DLLs** (Windows bloqueia DLLs baixadas):
   no PowerShell como admin, sobre a pasta `Modules`:
   `Get-ChildItem "CAMINHO\Modules" -Recurse | Unblock-File`
3. **Como conectar** (resumido): instalar Radmin VPN nos dois; um cria a rede, o
   outro entra; o host cria save (Sandbox → salvar) e escolhe "Host Coop
   Campaign"; o outro conecta pelo IP da Radmin. War Sails LIGADO nos dois.
4. **O que reportar** se falhar: a mensagem de erro exata da conexão e o arquivo
   `Modules\RF_CoopCompat\rf_coop_compat.log` dos dois lados (procurar
   `coop services resolved`, `coop session STARTED`, `dlc-block: neutralizado`).

## Armadilha nº 1 (quase sempre é isso)

"Wrong version of module 'X'" ou "module 'X' required" = as listas de módulos não
batem. Solução: garantir que os DOIS têm exatamente as mesmas pastas/versões. Por
isso distribuir as pastas reais (não só pedir "instale tal mod") é o mais seguro.

## Aviso honesto

Isto ainda é build de teste, não testado ponta-a-ponta. Espere que a primeira
tentativa pare em algum ponto (versões, conexão, ou um crash de sistema RF ainda
não sincronizado). Os logs acima são o que permite diagnosticar e ajustar.
