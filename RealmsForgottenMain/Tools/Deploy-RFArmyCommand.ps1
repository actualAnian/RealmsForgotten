$ErrorActionPreference = "Stop"

$DryRun = $args -contains "-DryRun"

$workspaceRoot = "C:\Users\gupol\Documents\Bannerlord_Warsails_AI"
$projectRoot = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain"
$localModuleRoot = Join-Path $projectRoot "_Module"
$gameFolder = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord"
$installedModuleRoot = Join-Path $gameFolder "Modules\RealmsForgotten"

$localBinRoot = Join-Path $localModuleRoot "bin\Win64_Shipping_Client"
$installedBinCandidates = @(
    (Join-Path $installedModuleRoot "bin\Win64_Shipping_Client"),
    (Join-Path $installedModuleRoot "bin\Gaming.Desktop.x64_Shipping_Client")
)

$assetMap = @(
    @{ Source = Join-Path $localModuleRoot "SubModule.xml"; Target = Join-Path $installedModuleRoot "SubModule.xml" },
    @{ Source = Join-Path $localModuleRoot "GUI\Prefabs\Map\RFArmyOverlayWindow.xml"; Target = Join-Path $installedModuleRoot "GUI\Prefabs\Map\RFArmyOverlayWindow.xml" },
    @{ Source = Join-Path $localModuleRoot "GUI\Prefabs\Extensions\RFArmyManagementWidgets.xml"; Target = Join-Path $installedModuleRoot "GUI\Prefabs\Extensions\RFArmyManagementWidgets.xml" },
    @{ Source = Join-Path $localModuleRoot "GUI\Brushes\ArmyCommanderBrushes.xml"; Target = Join-Path $installedModuleRoot "GUI\Brushes\ArmyCommanderBrushes.xml" }
)

function Copy-FileWithRetries {
    param(
        [string]$Source,
        [string]$Target,
        [int]$RetryCount = 5,
        [int]$RetryDelayMs = 1000
    )

    if (-not (Test-Path $Source)) {
        throw "Source file not found: $Source"
    }

    $targetDirectory = Split-Path -Path $Target -Parent
    if (-not (Test-Path $targetDirectory)) {
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    }

    if ($DryRun) {
        Write-Host "[DRYRUN] Would copy $Source -> $Target"
        return
    }

    for ($attempt = 1; $attempt -le $RetryCount; $attempt++) {
        try {
            Copy-Item -LiteralPath $Source -Destination $Target -Force
            Write-Host "[OK] Copied $Source -> $Target"
            return
        }
        catch {
            if ($attempt -eq $RetryCount) {
                throw
            }

            Start-Sleep -Milliseconds $RetryDelayMs
        }
    }
}

Write-Host "== Deploy RFArmyCommand =="
if ($DryRun) {
    Write-Host "[INFO] Dry run enabled. No files will be copied."
}

$localDll = Join-Path $localBinRoot "RealmsForgotten.dll"
$localPdb = Join-Path $localBinRoot "RealmsForgotten.pdb"

foreach ($installedBin in $installedBinCandidates) {
    if (-not (Test-Path $installedBin)) {
        continue
    }

    Copy-FileWithRetries $localDll (Join-Path $installedBin "RealmsForgotten.dll")
    if (Test-Path $localPdb) {
        Copy-FileWithRetries $localPdb (Join-Path $installedBin "RealmsForgotten.pdb")
    }
}

foreach ($asset in $assetMap) {
    Copy-FileWithRetries $asset.Source $asset.Target
}

Write-Host "== Deploy RFArmyCommand finished successfully =="
