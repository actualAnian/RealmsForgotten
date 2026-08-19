<#
.SYNOPSIS
    Valida a integridade referencial do ModuleData do RealmsForgotten contra o jogo instalado.

.DESCRIPTION
    Portado do Validate-ModuleData.ps1 do LOTRAOM (codigo MIT), adaptado ao RF em 2026-08-18.

    Nada no repo checa que os IDs referenciados pelos XML do mod existem de verdade. Uma
    referencia pendurada (Item.x, NPCCharacter.x...) nao quebra o build nem o load — aparece
    em jogo como tropa sem equipamento, e por isso acumula em silencio.

    O script monta o universo de IDs definidos a partir de:
      - ModuleData do repo (RealmsForgottenMain/_Module/ModuleData);
      - TODOS os modulos instalados em Modules/ do jogo (o conteudo do RF se espalha por
        RF_Core_II/III, RF_Map, RF_Magic, RF_Races, RFMonsters, RBM_RF, NavalDLC...).
        Trade-off herdado: modulo instalado mas desativado no launcher ainda conta como
        definido — melhor um falso-resolvido raro que centenas de falsos-pendurados.

    E reporta:
      - toda referencia prefixada (Item.x, Culture.x, Faction.x, NPCCharacter.x,
        EquipmentRoster.x, BodyProperty.x, SkillSet.x, PartyTemplate.x) que nao resolve;
      - IDs definidos em duplicidade (um arquivo sobrescreve o outro em silencio);
      - <XmlName path="..."> no SubModule.xml sem arquivo correspondente.

    O gate e por MOVIMENTO (-FailOnNew): o backlog existente nao trava trabalho, mas uma
    mudanca que ADICIONA referencia pendurada trava. Gate permanentemente vermelho e gate
    quebrado — licao herdada do script original.

.PARAMETER FailOnNew       Sai com erro so se houver MAIS pendurados que a baseline.
.PARAMETER UpdateBaseline  Regrava a baseline (avisa se o numero SUBIR).
.PARAMETER LoadedOnly      So arquivos registrados no SubModule.xml (+ convencao de nome).

.EXAMPLE
    powershell -File Scripts\Validate-RFModuleData.ps1
    powershell -File Scripts\Validate-RFModuleData.ps1 -LoadedOnly -FailOnNew
#>

[CmdletBinding()]
param(
    [switch]$FailOnNew,
    [switch]$UpdateBaseline,
    [switch]$LoadedOnly,
    [string]$BaselinePath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$moduleRoot = Join-Path $repoRoot "RealmsForgottenMain\_Module"
$moduleDataRoot = Join-Path $moduleRoot "ModuleData"
$subModuleXml = Join-Path $moduleRoot "SubModule.xml"

if ([string]::IsNullOrEmpty($BaselinePath)) {
    $BaselinePath = Join-Path $PSScriptRoot "rf-moduledata-baseline.json"
}

if (-not $env:BANNERLORD_GAME_DIR) {
    $env:BANNERLORD_GAME_DIR = [System.Environment]::GetEnvironmentVariable("BANNERLORD_GAME_DIR", "User")
}
if (-not $env:BANNERLORD_GAME_DIR -or -not (Test-Path $env:BANNERLORD_GAME_DIR)) {
    Write-Warning "BANNERLORD_GAME_DIR nao definido/ausente; sem os IDs vanilla toda referencia pareceria quebrada. Pulando."
    exit 0
}

Write-Host "`n=== Validacao de ModuleData do RealmsForgotten ===" -ForegroundColor Magenta

# ---------------------------------------------------------------------------
# Quais arquivos do mod a engine realmente carrega?
# ---------------------------------------------------------------------------

[xml]$subModule = Get-Content $subModuleXml
$registered = @{}
foreach ($n in $subModule.SelectNodes("//XmlName")) {
    if ($n.path) { $registered[$n.path] = $true }
}

# Carregados por convencao de nome, sem registro em <Xmls>.
$conventionFiles = @('skins', 'monsters', 'action_sets', 'action_types', 'module_strings',
    'movement_sets', 'item_holsters', 'combat_parameters', 'managed_core_parameters',
    'native_parameters', 'flora_kinds', 'settlements', 'music', 'banner_icons')
foreach ($c in $conventionFiles) { $registered[$c] = $true }

# --- checagem extra: <XmlName path> sem arquivo (arquivo morto registrado) ---
# O repo e parcial: muitos arquivos registrados vivem so na pasta do jogo (assets
# implantados fora do git). Um path so e "morto" se faltar nos DOIS lugares.
$gameModuleData = Join-Path $env:BANNERLORD_GAME_DIR "Modules\RealmsForgotten\ModuleData"
$missingRegistered = @()
foreach ($p in $subModule.SelectNodes("//XmlName")) {
    if (-not $p.path) { continue }
    # .xml OU .xslt: o RF declara paths so-XSLT de proposito (ex.: naval_characters,
    # para transformar o XML do War Sails no load) — isso e legitimo, nao arquivo morto.
    $found = $false
    foreach ($ext in @('.xml', '.xslt')) {
        if ((Test-Path (Join-Path $moduleDataRoot ($p.path + $ext))) -or
            (Test-Path (Join-Path $gameModuleData ($p.path + $ext)))) { $found = $true; break }
    }
    if (-not $found) { $missingRegistered += $p.path }
}

# ---------------------------------------------------------------------------
# Universo de IDs definidos
# ---------------------------------------------------------------------------

$defined = @{}
$definedIn = @{}

function Add-Definition {
    param([string]$Kind, [string]$Id, [string]$File)
    if ([string]::IsNullOrWhiteSpace($Id)) { return }
    $key = "$Kind.$Id"
    if ($defined.ContainsKey($key)) {
        $defined[$key]++
        $definedIn[$key] += $File
    }
    else {
        $defined[$key] = 1
        $definedIn[$key] = @($File)
    }
}

$definitionElements = @{
    'Item'            = 'Item'
    'NPCCharacter'    = 'NPCCharacter'
    'Hero'            = 'Hero'
    'Culture'         = 'Culture'
    'Faction'         = 'Faction'
    'Kingdom'         = 'Kingdom'
    'EquipmentRoster' = 'EquipmentRoster'
    'BodyProperties'  = 'BodyProperty'
    'BodyProperty'    = 'BodyProperty'
    'SkillSet'        = 'SkillSet'
    'PartyTemplate'   = 'PartyTemplate'
    'CraftedItem'     = 'Item'
    'Settlement'      = 'Settlement'
}

function Read-Definitions {
    param([string]$Path, [string]$Label)

    $count = 0
    Get-ChildItem -Path $Path -Filter *.xml -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        $file = $_.FullName
        # backups .bak_* e afins nao sao carregados pela engine
        if ($file -match '\.bak|_backup|\.orig') { return }
        try { [xml]$doc = Get-Content $file -Raw -ErrorAction Stop }
        catch { return }

        foreach ($elementName in $definitionElements.Keys) {
            foreach ($node in $doc.SelectNodes("//$elementName")) {
                $id = $node.getAttribute("id")
                if ($id) {
                    Add-Definition -Kind $definitionElements[$elementName] -Id $id -File $file
                    $count++
                }
            }
        }
    }
    Write-Host ("  {0,-28} {1,7} definicoes" -f $Label, $count) -ForegroundColor Gray
}

Write-Host "`nIndexando IDs definidos..." -ForegroundColor Cyan
Read-Definitions -Path $moduleDataRoot -Label "RealmsForgotten (repo)"

# TODOS os modulos instalados: o ecossistema RF se espalha por muitos, e o custo de
# indexar tudo e so tempo de scan.
$gameModules = Join-Path $env:BANNERLORD_GAME_DIR "Modules"
foreach ($dir in Get-ChildItem -Path $gameModules -Directory) {
    if ($dir.Name -eq 'RealmsForgotten') { continue }
    $dataDir = Join-Path $dir.FullName "ModuleData"
    if (Test-Path $dataDir) {
        Read-Definitions -Path $dataDir -Label $dir.Name
    }
}

# RealmsForgotten do JOGO: o repo e PARCIAL (a maior parte dos rfitems vive so na pasta
# implantada — licao da 1a rodada, que acusou realm_plated_cav_helmet como pendurado
# estando definido la). Entram apenas os arquivos que o repo NAO tem, para o mesmo
# arquivo nao contar duas vezes como "duplicado".
$repoRelative = @{}
Get-ChildItem -Path $moduleDataRoot -Filter *.xml -Recurse | ForEach-Object {
    $repoRelative[$_.FullName.Substring($moduleDataRoot.Length).TrimStart('')] = $true
}
if (Test-Path $gameModuleData) {
    $count = 0
    Get-ChildItem -Path $gameModuleData -Filter *.xml -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        $rel = $_.FullName.Substring($gameModuleData.Length).TrimStart('')
        if ($repoRelative.ContainsKey($rel)) { return }
        if ($_.FullName -match '\.bak|_backup|\.orig') { return }
        try { [xml]$doc = Get-Content $_.FullName -Raw -ErrorAction Stop } catch { return }
        foreach ($elementName in $definitionElements.Keys) {
            foreach ($node in $doc.SelectNodes("//$elementName")) {
                $id = $node.getAttribute("id")
                if ($id) { Add-Definition -Kind $definitionElements[$elementName] -Id $id -File $_.FullName; $count++ }
            }
        }
    }
    Write-Host ("  {0,-28} {1,7} definicoes" -f "RealmsForgotten (jogo, extras)", $count) -ForegroundColor Gray
}

# ---------------------------------------------------------------------------
# Scan de referencias no ModuleData do repo
# ---------------------------------------------------------------------------

Write-Host "`nEscaneando referencias..." -ForegroundColor Cyan

$refPattern = '(?:^|")(Item|NPCCharacter|Culture|Faction|Kingdom|Hero|EquipmentRoster|BodyProperty|SkillSet|PartyTemplate|Settlement)\.([A-Za-z0-9_\-\.]+)'
$broken = @{}
$brokenFiles = @{}
$totalRefs = 0

Get-ChildItem -Path $moduleDataRoot -Filter *.xml -Recurse | ForEach-Object {
    $file = $_.FullName
    if ($file -match '\.bak|_backup|\.orig') { return }
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($file)

    if ($LoadedOnly -and -not $registered.ContainsKey($stem)) { return }

    $text = Get-Content $file -Raw -ErrorAction SilentlyContinue
    if (-not $text) { return }

    foreach ($m in [regex]::Matches($text, $refPattern)) {
        $kind = $m.Groups[1].Value
        $id = $m.Groups[2].Value
        if ($id.Contains('{')) { continue }  # placeholder de template ({{ID}})
        $key = "$kind.$id"
        $totalRefs++
        if (-not $defined.ContainsKey($key)) {
            # Kingdom e Faction se referenciam cruzado nos XML vanilla
            if ($kind -eq 'Kingdom' -and $defined.ContainsKey("Faction.$($m.Groups[2].Value)")) { continue }
            if ($kind -eq 'Faction' -and $defined.ContainsKey("Kingdom.$($m.Groups[2].Value)")) { continue }
            if ($broken.ContainsKey($key)) { $broken[$key]++ } else { $broken[$key] = 1 }
            $rel = $file.Substring($repoRoot.Length + 1)
            if ($brokenFiles.ContainsKey($rel)) { $brokenFiles[$rel]++ } else { $brokenFiles[$rel] = 1 }
        }
    }
}

$brokenTotal = 0
foreach ($v in $broken.Values) { $brokenTotal += $v }

# ---------------------------------------------------------------------------
# Definicoes duplicadas DENTRO do repo (sobrescrita silenciosa)
# ---------------------------------------------------------------------------

$repoDuplicates = @($defined.GetEnumerator() | Where-Object {
    $_.Value -gt 1 -and -not $_.Key.Contains('{') `
        -and (@($definedIn[$_.Key] | Where-Object { $_.StartsWith($moduleDataRoot) })).Count -gt 1
})

# ---------------------------------------------------------------------------
# Relatorio
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host ("Referencias escaneadas : {0}" -f $totalRefs) -ForegroundColor Gray
Write-Host ("Nao resolvidas         : {0} ({1} IDs distintos)" -f $brokenTotal, $broken.Count) `
    -ForegroundColor $(if ($brokenTotal -gt 0) { "Yellow" } else { "Green" })
Write-Host ("IDs duplicados no repo : {0}" -f $repoDuplicates.Count) `
    -ForegroundColor $(if ($repoDuplicates.Count -gt 0) { "Yellow" } else { "Green" })
Write-Host ("XmlName sem arquivo    : {0}" -f $missingRegistered.Count) `
    -ForegroundColor $(if ($missingRegistered.Count -gt 0) { "Red" } else { "Green" })

if ($missingRegistered.Count -gt 0) {
    Write-Host "`nRegistrados no SubModule.xml sem arquivo correspondente:" -ForegroundColor Red
    $missingRegistered | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
}

if ($broken.Count -gt 0) {
    Write-Host "`nTop IDs nao resolvidos:" -ForegroundColor Yellow
    $broken.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 15 | ForEach-Object {
        Write-Host ("  {0,5}x  {1}" -f $_.Value, $_.Key) -ForegroundColor Gray
    }
    Write-Host "`nPiores arquivos:" -ForegroundColor Yellow
    $brokenFiles.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 10 | ForEach-Object {
        Write-Host ("  {0,5}  {1}" -f $_.Value, $_.Key) -ForegroundColor Gray
    }
}

if ($repoDuplicates.Count -gt 0) {
    Write-Host "`nIDs definidos 2+ vezes dentro do repo (ultimo vence em silencio):" -ForegroundColor Yellow
    $repoDuplicates | Sort-Object Value -Descending | Select-Object -First 10 | ForEach-Object {
        Write-Host ("  {0,3}x  {1}" -f $_.Value, $_.Key) -ForegroundColor Gray
    }
}

# ---------------------------------------------------------------------------
# Baseline
# ---------------------------------------------------------------------------

$current = [ordered]@{
    unresolvedTotal    = $brokenTotal
    unresolvedDistinct = $broken.Count
    duplicateIds       = $repoDuplicates.Count
    loadedOnly         = [bool]$LoadedOnly
}

if ($UpdateBaseline) {
    $previous = if (Test-Path $BaselinePath) { Get-Content $BaselinePath -Raw | ConvertFrom-Json } else { $null }
    if ($previous -and $brokenTotal -gt $previous.unresolvedTotal) {
        Write-Warning ("Baseline SUBIRIA de {0} para {1}. So suba de proposito." -f $previous.unresolvedTotal, $brokenTotal)
    }
    $current | ConvertTo-Json | Set-Content -Path $BaselinePath -Encoding UTF8
    Write-Host "`nBaseline gravada: $BaselinePath" -ForegroundColor Green
    exit 0
}

if ($FailOnNew) {
    if (-not (Test-Path $BaselinePath)) {
        Write-Warning "Sem baseline em $BaselinePath. Rode com -UpdateBaseline primeiro."
        exit 0
    }

    $baseline = Get-Content $BaselinePath -Raw | ConvertFrom-Json
    Write-Host ""
    if ($brokenTotal -gt $baseline.unresolvedTotal) {
        Write-Host ("FALHOU: referencias nao resolvidas subiram {0} -> {1}." -f $baseline.unresolvedTotal, $brokenTotal) -ForegroundColor Red
        exit 1
    }
    if ($brokenTotal -lt $baseline.unresolvedTotal) {
        Write-Host ("Melhorou: {0} -> {1}. Rode -UpdateBaseline para travar o ganho." -f $baseline.unresolvedTotal, $brokenTotal) -ForegroundColor Green
    }
    else {
        Write-Host ("Sem referencias novas penduradas (baseline {0})." -f $baseline.unresolvedTotal) -ForegroundColor Green
    }
}

exit 0
