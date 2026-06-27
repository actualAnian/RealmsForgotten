$ErrorActionPreference = "Stop"

$workspaceRoot = "C:\Users\gupol\Documents\Bannerlord_Warsails_AI"
$projectPath = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\RealmsForgotten.csproj"
$verifyScript = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\Tools\Verify-RFArmyCommand.ps1"
$startupVerifyScript = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\Tools\Verify-StartupPatchSweep.ps1"
$deployScript = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\Tools\Deploy-RFArmyCommand.ps1"

$SkipDeployDryRun = $args -contains "-SkipDeployDryRun"
$DeployInstalled = $args -contains "-DeployInstalled"

Write-Host "== Build and Check RFArmyCommand =="

dotnet build $projectPath -c Debug -p:Platform=x64 -p:DisableModuleCopy=true -p:BuildForWindows=false -p:BuildForWindowsStore=false /clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) {
    throw "RFArmyCommand build failed."
}

& $verifyScript -SkipBuild -SkipInstalledHash
& $startupVerifyScript -SkipBuild

if ($DeployInstalled) {
    & $deployScript
    & $verifyScript -SkipBuild
}
elseif (-not $SkipDeployDryRun) {
    & $deployScript -DryRun
}

Write-Host "== Build and Check RFArmyCommand finished successfully =="
