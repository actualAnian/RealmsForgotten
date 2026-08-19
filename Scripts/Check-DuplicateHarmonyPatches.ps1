<#
.SYNOPSIS
    Caca patch Harmony aplicado duas vezes na solution do RF.

.DESCRIPTION
    Harmony NAO deduplica: o mesmo patch aplicado por dois mecanismos roda duas vezes, e um
    multiplicador duplicado e elevado ao quadrado em silencio (licao do LOTRAOM, onde um 0.9
    de cerco virou 0.81 em producao). Ideia portada do DuplicateHarmonyPatchTests deles,
    adaptada aos mecanismos do RF:

      ERRO 1  a mesma assembly coberta por DOIS mecanismos de varredura completa
              (PatchAll duas vezes, ou PatchAll + sweep de nao-categorizados como o
              ApplyUncategorizedHarmonyPatchesSafely do SubModule principal);
      ERRO 2  a mesma categoria aplicada em mais de um call site (PatchCategory 2x);
      ERRO 3  o mesmo metodo de patch (classe.metodo) referenciado por harmony.Patch manual
              E por atributo [HarmonyPatch] na mesma classe — dupla aplicacao literal;
      AVISO   o mesmo alvo vanilla (Tipo.Metodo) patchado por atributo em mais de um modulo
              do RF — legitimo quando sao efeitos diferentes, mas e onde multiplicadores
              empilham; lista para revisao humana.

    Estatico, por fonte (regex), sem precisar do jogo aberto. Roda standalone:
        powershell -File Scripts\Check-DuplicateHarmonyPatches.ps1
    Sai com codigo 1 se houver ERRO (para poder entrar num build/CI depois).
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# Projetos da solution (exclui decompilados, checkpoints, backups e pacotes).
$excludeDirs = '\\obj\\|\\bin\\|_checkpoints|_old_gpt_backup|\\packages\\|\\Decompiled|decompiled_'
$files = Get-ChildItem -Path $root -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch $excludeDirs }

Write-Host "Analisando $($files.Count) arquivos .cs em $root"

# Assembly ~ pasta de projeto de primeiro nivel.
function Get-Project([string]$path) {
    $rel = $path.Substring($root.Length).TrimStart('\')
    return $rel.Split('\')[0]
}

$sources = @{}
foreach ($f in $files) { $sources[$f.FullName] = Get-Content $f.FullName -Raw }

$errors = @()
$warnings = @()

# ---------------------------------------------------------------- coleta por arquivo
# Regexes toleram espacos; linhas comentadas (// ...) sao descartadas por linha.
function Get-ActiveLines([string]$text) {
    ($text -split "`n") | Where-Object { $_ -notmatch '^\s*//' }
}

$patchAllByProject = @{}     # projeto -> call sites de PatchAll/sweep
$categoryApplies = @{}       # categoria -> call sites de PatchCategory
$attributeTargets = @{}      # "Tipo::Metodo" -> lista "projeto (arquivo)"
$manualPatchSites = @()      # objetos {Project,File,TargetType,TargetMethod,PatchClass}

foreach ($kvp in $sources.GetEnumerator()) {
    $file = $kvp.Key; $text = $kvp.Value
    $proj = Get-Project $file
    $shortFile = Split-Path $file -Leaf

    foreach ($line in (Get-ActiveLines $text)) {

        # PatchAll de Harmony: o receptor precisa parecer uma instancia Harmony
        # (exclui metodos custom como QuestPatches.PatchAll).
        if ($line -match '(?i)(harmony[A-Za-z0-9_]*|new\s+Harmony\s*\([^)]*\))\s*\.\s*PatchAll(Uncategorized)?\s*\(') {
            if (-not $patchAllByProject.ContainsKey($proj)) { $patchAllByProject[$proj] = @() }
            $patchAllByProject[$proj] += "${shortFile}: $($line.Trim())"
        }

        # PatchCategory("X") / PatchCategory(asm, "X")
        if ($line -match '\.PatchCategory\s*\(') {
            $cat = $null
            if ($line -match '\.PatchCategory\s*\([^,)]*,\s*"?([A-Za-z0-9_.]+)"?\s*\)') { $cat = $Matches[1] }
            elseif ($line -match '\.PatchCategory\s*\(\s*"([A-Za-z0-9_.]+)"\s*\)') { $cat = $Matches[1] }
            elseif ($line -match '\.PatchCategory\s*\(\s*([A-Za-z0-9_.]+)\s*\)') { $cat = $Matches[1] }
            if ($cat) {
                if (-not $categoryApplies.ContainsKey($cat)) { $categoryApplies[$cat] = @() }
                $categoryApplies[$cat] += "$proj/${shortFile}: $($line.Trim())"
            }
        }

        # harmony.Patch(AccessTools.Method(typeof(X), "Y") ...)
        if ($line -match '\.Patch\s*\(\s*AccessTools\.Method\s*\(\s*typeof\(\s*([A-Za-z0-9_.]+)\s*\)\s*,\s*"([A-Za-z0-9_]+)"') {
            $manualPatchSites += [pscustomobject]@{
                Project = $proj; File = $shortFile
                TargetType = ($Matches[1].Split('.') | Select-Object -Last 1)
                TargetMethod = $Matches[2]
                Line = $line.Trim()
            }
        }
    }

    # Sweeps via CreateClassProcessor: um loop que FILTRA POR uma categoria especifica e
    # aplicacao daquela categoria (conta junto com PatchCategory); um loop que EXCLUI
    # categorizados e uma varredura completa de nao-categorizados (conta como PatchAll).
    if ($text -match 'CreateClassProcessor\s*\(') {
        $scopedCategory = $null
        # ordem importa: $Matches fica com o resultado do ULTIMO -match
        if ($text -match 'ConstructorArguments\[0\]' -and
            $text -match 'PatchCategoryName\s*=\s*"([A-Za-z0-9_.]+)"') {
            $scopedCategory = $Matches[1]
        }
        if ($scopedCategory) {
            if (-not $categoryApplies.ContainsKey($scopedCategory)) { $categoryApplies[$scopedCategory] = @() }
            $categoryApplies[$scopedCategory] += "$proj/${shortFile}: sweep escopado (CreateClassProcessor)"
        } else {
            if (-not $patchAllByProject.ContainsKey($proj)) { $patchAllByProject[$proj] = @() }
            $patchAllByProject[$proj] += "${shortFile}: sweep de nao-categorizados (CreateClassProcessor)"
        }
    }

    # Atributos [HarmonyPatch(typeof(X), "Y")] — multiline-safe no texto inteiro.
    foreach ($m in [regex]::Matches($text, '\[HarmonyPatch\s*\(\s*typeof\(\s*([A-Za-z0-9_.]+)\s*\)\s*,\s*"([A-Za-z0-9_]+)"')) {
        $key = ($m.Groups[1].Value.Split('.') | Select-Object -Last 1) + '::' + $m.Groups[2].Value
        if (-not $attributeTargets.ContainsKey($key)) { $attributeTargets[$key] = @() }
        $attributeTargets[$key] += "$proj ($shortFile)"
    }
}

# --------------------------------------------------- ERRO 1: assembly varrida duas vezes
foreach ($proj in $patchAllByProject.Keys) {
    $sites = $patchAllByProject[$proj] | Sort-Object -Unique
    # CreateClassProcessor em loop conta como UM mecanismo (sweep); PatchAll e outro.
    $sweeps = @($sites | Where-Object { $_ -match 'CreateClassProcessor' })
    $alls   = @($sites | Where-Object { $_ -match 'PatchAll' })
    $mechanisms = $sweeps.Count + $alls.Count
    if ($mechanisms -gt 1) {
        $errors += "ERRO [$proj]: $mechanisms mecanismos de varredura completa na mesma assembly:`n    " + ($sites -join "`n    ")
    }
}

# --------------------------------------------------- ERRO 2: categoria aplicada 2x
foreach ($cat in $categoryApplies.Keys) {
    $sites = $categoryApplies[$cat] | Sort-Object -Unique
    if ($sites.Count -gt 1) {
        $errors += "ERRO [categoria $cat]: aplicada em $($sites.Count) call sites:`n    " + ($sites -join "`n    ")
    }
}

# ------------------- ERRO 3: alvo manual identico a alvo de atributo NO MESMO projeto
# (mesmo alvo em projetos diferentes vira AVISO; no mesmo projeto os dois mecanismos
# rodam com certeza — e quase sempre a mesma intencao aplicada duas vezes)
foreach ($site in $manualPatchSites) {
    $key = $site.TargetType + '::' + $site.TargetMethod
    if ($attributeTargets.ContainsKey($key)) {
        $sameProj = @($attributeTargets[$key] | Where-Object { $_ -like "$($site.Project) *" })
        if ($sameProj.Count -gt 0) {
            $errors += "ERRO [$($site.Project)]: $key patchado por harmony.Patch manual ($($site.File)) E por atributo em: " + (($sameProj | Sort-Object -Unique) -join ', ')
        } else {
            $warnings += "AVISO: $key patchado manualmente em $($site.Project)/$($site.File) e por atributo em: " + (($attributeTargets[$key] | Sort-Object -Unique) -join ', ')
        }
    }
}

# --------------------------- AVISO: mesmo alvo vanilla em mais de um modulo (atributos)
foreach ($key in $attributeTargets.Keys) {
    $projs = @($attributeTargets[$key] | ForEach-Object { ($_ -split ' ')[0] } | Sort-Object -Unique)
    if ($projs.Count -gt 1) {
        $warnings += "AVISO: $key patchado por atributo em $($projs.Count) modulos: " + ($projs -join ', ')
    }
}

# ------------------------------------------------------------------------- relatorio
Write-Host ""
if ($errors.Count -eq 0) {
    Write-Host "OK: nenhum patch aplicado em duplicidade." -ForegroundColor Green
} else {
    foreach ($e in $errors) { Write-Host $e -ForegroundColor Red; Write-Host "" }
}

if ($warnings.Count -gt 0) {
    Write-Host "--- Sobreposicoes para revisao humana ($($warnings.Count)) ---" -ForegroundColor Yellow
    foreach ($w in ($warnings | Sort-Object -Unique)) { Write-Host $w -ForegroundColor Yellow }
}

Write-Host ""
Write-Host "Resumo: $($errors.Count) erro(s), $($warnings.Count) aviso(s)."
if ($errors.Count -gt 0) { exit 1 }
exit 0
