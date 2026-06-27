$SkipBuild = $false
if ($args -contains "-SkipBuild") {
    $SkipBuild = $true
}

$ErrorActionPreference = "Stop"

$workspaceRoot = "C:\Users\gupol\Documents\Bannerlord_Warsails_AI"
$projectPath = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\RealmsForgotten.csproj"
$moduleRoot = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\_Module"
$realmsForgottenDll = Join-Path $moduleRoot "bin\Win64_Shipping_Client\RealmsForgotten.dll"
$gameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
$gameBinRoot = Join-Path $gameRoot "bin\Win64_Shipping_Client"
$nativeBinRoot = Join-Path $gameRoot "Modules\Native\bin\Win64_Shipping_Client"
$sandBoxBinRoot = Join-Path $gameRoot "Modules\SandBox\bin\Win64_Shipping_Client"
$sandBoxCoreBinRoot = Join-Path $gameRoot "Modules\SandBoxCore\bin\Win64_Shipping_Client"
$storyModeBinRoot = Join-Path $gameRoot "Modules\StoryMode\bin\Win64_Shipping_Client"
$customBattleBinRoot = Join-Path $gameRoot "Modules\CustomBattle\bin\Win64_Shipping_Client"
$bannerlordHarmonyBinRoot = Join-Path $gameRoot "Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client"
$uiExtenderBinRoot = Join-Path $gameRoot "Modules\Bannerlord.UIExtenderEx\bin\Win64_Shipping_Client"
$moduleBinCandidates = @(
    (Join-Path $gameRoot "Modules\RealmsForgotten\bin\Win64_Shipping_Client"),
    (Join-Path $gameRoot "Modules\RealmsForgotten\bin\Gaming.Desktop.x64_Shipping_Client")
)

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }

    Write-Host "[OK] $Message"
}

function Load-AssemblySafe {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    try {
        return [System.Reflection.Assembly]::LoadFrom($Path)
    }
    catch {
        return $null
    }
}

function Load-AssembliesFromFolder {
    param(
        [string]$Folder,
        [string[]]$ExcludeFileNames = @()
    )

    if (-not (Test-Path $Folder)) {
        return
    }

    Get-ChildItem -Path $Folder -Filter "*.dll" -File | ForEach-Object {
        if ($ExcludeFileNames -contains $_.Name) {
            return
        }

        $null = Load-AssemblySafe $_.FullName
    }
}

function Get-AssemblyTypesSafe {
    param([System.Reflection.Assembly]$Assembly)

    try {
        return @($Assembly.GetTypes())
    }
    catch [System.Reflection.ReflectionTypeLoadException] {
        foreach ($loaderException in $_.Exception.LoaderExceptions) {
            if ($loaderException -ne $null) {
                Write-Host "[INFO] LoaderException: $($loaderException.Message)"
            }
        }

        return @($_.Exception.Types | Where-Object { $_ -ne $null })
    }
}

Write-Host "== Verify startup harmony sweep =="

if (-not $SkipBuild) {
    Write-Host "-- Build"
    & dotnet build $projectPath -c Debug -p:Platform=x64 -p:DisableModuleCopy=true -p:BuildForWindows=false -p:BuildForWindowsStore=false /clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) {
        throw "RealmsForgotten build failed."
    }
}
else {
    Write-Host "-- Build"
    Write-Host "[INFO] Build step skipped by request."
}

Write-Host "-- Load assemblies"
Load-AssembliesFromFolder $gameBinRoot
Load-AssembliesFromFolder $nativeBinRoot
Load-AssembliesFromFolder $sandBoxBinRoot
Load-AssembliesFromFolder $sandBoxCoreBinRoot
Load-AssembliesFromFolder $storyModeBinRoot
Load-AssembliesFromFolder $customBattleBinRoot
Load-AssembliesFromFolder $bannerlordHarmonyBinRoot
Load-AssembliesFromFolder $uiExtenderBinRoot
foreach ($candidate in $moduleBinCandidates) {
    Load-AssembliesFromFolder $candidate @("RealmsForgotten.dll")
}

$realmsForgottenAssembly = Load-AssemblySafe $realmsForgottenDll
Assert-True ($null -ne $realmsForgottenAssembly) "RealmsForgotten.dll loaded for startup patch verification"

$harmonyAssembly = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "0Harmony" } | Select-Object -First 1
Assert-True ($null -ne $harmonyAssembly) "Harmony assembly is loaded"

$harmonyType = $harmonyAssembly.GetType("HarmonyLib.Harmony")
Assert-True ($null -ne $harmonyType) "Harmony type exists"

Write-Host "-- Collect uncategorized patch types"
$patchTypes = Get-AssemblyTypesSafe $realmsForgottenAssembly |
    Where-Object {
        $_.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq "HarmonyLib.HarmonyPatch" }
    } |
    Where-Object {
        -not ($_.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq "HarmonyLib.HarmonyPatchCategory" })
    } |
    Sort-Object FullName

Assert-True ($patchTypes.Count -gt 0) "Uncategorized harmony patch types found in RealmsForgotten.dll"
Write-Host "[INFO] Uncategorized patch types: $($patchTypes.Count)"

Write-Host "-- Harmony dry run for uncategorized patches"
$failedPatchTypes = @()
$harmony = [Activator]::CreateInstance($harmonyType, @("rf.startup.verify"))

foreach ($patchType in $patchTypes) {
    try {
        $processor = $harmony.CreateClassProcessor($patchType)
        $null = $processor.Patch()
        Write-Host "[OK] Patched $($patchType.FullName)"
    }
    catch {
        $failedPatchTypes += [PSCustomObject]@{
            Type = $patchType.FullName
            Error = $_.Exception.ToString()
        }
    }
}

if ($failedPatchTypes.Count -gt 0) {
    $failedPatchTypes | Format-Table -AutoSize | Out-Host
    throw "Startup harmony sweep found patch failures."
}

Write-Host "[OK] Startup harmony sweep reported no patch failures."
Write-Host "== Verify startup harmony sweep finished successfully =="
