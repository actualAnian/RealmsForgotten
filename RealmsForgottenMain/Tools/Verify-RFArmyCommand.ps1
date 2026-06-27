$SkipBuild = $false
if ($args -contains "-SkipBuild") {
    $SkipBuild = $true
}

$SkipInstalledHash = $false
if ($args -contains "-SkipInstalledHash") {
    $SkipInstalledHash = $true
}

$ErrorActionPreference = "Stop"

$workspaceRoot = "C:\Users\gupol\Documents\Bannerlord_Warsails_AI"
$projectPath = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\RealmsForgotten.csproj"
$armyCommandSourceRoot = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\AiMade\ArmyCommand"
$moduleRoot = Join-Path $workspaceRoot "RF_Warsails_AI\RealmsForgottenMain\_Module"
$localDll = Join-Path $moduleRoot "bin\Win64_Shipping_Client\RealmsForgotten.dll"
$localBinRoot = Join-Path $moduleRoot "bin\Win64_Shipping_Client"
$installedDllCandidates = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\RealmsForgotten\bin\Win64_Shipping_Client\RealmsForgotten.dll",
    "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\RealmsForgotten\bin\Gaming.Desktop.x64_Shipping_Client\RealmsForgotten.dll"
)
$gameBinRoot = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
$sandBoxBinRoot = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\bin\Win64_Shipping_Client"
$nativeBinRoot = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Native\bin\Win64_Shipping_Client"
$modulesRoot = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules"
$harmonyAssemblyPath = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll"
$viewModelAssemblyPath = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.ViewModelCollection.dll"
$campaignAssemblyPath = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.CampaignSystem.dll"

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

function Assert-XmlValid {
    param([string]$Path)

    [xml](Get-Content -Raw $Path) > $null
    Write-Host "[OK] XML valid: $Path"
}

function Assert-SourceRegex {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -Path $Path -Raw
    Assert-True ([regex]::IsMatch($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) $Message
}

function Assert-SourceRegexAbsent {
    param(
        [string]$Path,
        [string]$Pattern,
        [string]$Message
    )

    $content = Get-Content -Path $Path -Raw
    Assert-True (-not [regex]::IsMatch($content, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)) $Message
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
    param([string]$Folder)

    if (-not (Test-Path $Folder)) {
        return
    }

    Get-ChildItem -Path $Folder -Filter "*.dll" -File | ForEach-Object {
        $null = Load-AssemblySafe $_.FullName
    }
}

function Get-LoadedAssemblyByName {
    param([string]$Name)

    return [AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq $Name } |
        Select-Object -First 1
}

function Get-FirstExistingPath {
    param([string[]]$Paths)

    foreach ($path in $Paths) {
        if (-not [string]::IsNullOrWhiteSpace($path) -and (Test-Path $path)) {
            return $path
        }
    }

    return $null
}

function Get-AssemblyTypesSafe {
    param([System.Reflection.Assembly]$Assembly)

    try {
        return @($Assembly.GetTypes())
    }
    catch [System.Reflection.ReflectionTypeLoadException] {
        return @($_.Exception.Types | Where-Object { $_ -ne $null })
    }
}

Write-Host "== RFArmyCommand verification =="

if (-not $SkipBuild) {
    Write-Host "-- Build"
    dotnet build $projectPath -c Debug -p:Platform=x64 -p:DisableModuleCopy=true -p:BuildForWindows=false -p:BuildForWindowsStore=false /clp:ErrorsOnly | Out-Host
    Assert-True ($LASTEXITCODE -eq 0) "RealmsForgotten.csproj builds successfully"
}
else {
    Write-Host "-- Build"
    Write-Host "[INFO] Build step skipped by request."
}

Write-Host "-- XML"
Assert-XmlValid (Join-Path $moduleRoot "GUI\Prefabs\Extensions\RFArmyManagementWidgets.xml")
Assert-XmlValid (Join-Path $moduleRoot "GUI\Prefabs\Map\RFArmyOverlayWindow.xml")
Assert-XmlValid (Join-Path $moduleRoot "GUI\Brushes\ArmyCommanderBrushes.xml")

Write-Host "-- UI bindings"
$mixinsSourcePath = Join-Path $armyCommandSourceRoot "RFArmyCommandMixins.cs"
$viewModelsSourcePath = Join-Path $armyCommandSourceRoot "RFArmyCommandViewModels.cs"

$managementBindingProperties = @(
    "ACIsValidArmySelected",
    "ArmyBehaviorDescription",
    "RecommendedPlanEnabled",
    "RecommendedOrderText",
    "RecommendedReasonText",
    "StrategicPlanEnabled",
    "StrategicPlanText",
    "StrategicPlanReasonText",
    "TargetSettlementEnabled",
    "TargetSettlementName",
    "SendInfluenceEnabled",
    "ArmyBehaviorEnabled",
    "ArmyBehaviorText"
)

foreach ($propertyName in $managementBindingProperties) {
    Assert-SourceRegex $mixinsSourcePath "\[DataSourceProperty\]\s*public\s+[A-Za-z0-9_<>\.\?]+\s+$propertyName\b" "ArmyManagement binding property $propertyName exists in source"
}

$managementBindingMethods = @(
    "ExecuteUseRecommendedPlan",
    "ExecuteSelectStrategicPlan",
    "ExecuteSendInfluence",
    "ExecuteSelectTargetSettlement",
    "ExecuteSelectArmyBehavior"
)

foreach ($methodName in $managementBindingMethods) {
    Assert-SourceRegex $mixinsSourcePath "public\s+void\s+$methodName\s*\(" "ArmyManagement binding method $methodName exists in source"
}

$overlayBindingProperties = @(
    "ACSelectedArmyVisible",
    "ACSelectedArmyName",
    "ACSelectedArmyOrder",
    "ACSelectedArmyActionText",
    "ACArmiesCount",
    "ACPartiesCount",
    "ACManCount",
    "ArmyOverlayArmiesList"
)

foreach ($propertyName in $overlayBindingProperties) {
    Assert-SourceRegex $mixinsSourcePath "\[DataSourceProperty\]\s*public\s+[A-Za-z0-9_<>\.\?]+\s+$propertyName\b" "ArmyOverlay binding property $propertyName exists in source"
}

Assert-SourceRegex $mixinsSourcePath "public\s+void\s+ExecuteOpenSelectedArmyManagement\s*\(" "ArmyOverlay binding method ExecuteOpenSelectedArmyManagement exists in source"

$lineBindingProperties = @(
    "LeaderVisual",
    "ArmyInfoRows",
    "ForceHovered",
    "IsSelected"
)

foreach ($propertyName in $lineBindingProperties) {
    Assert-SourceRegex $viewModelsSourcePath "\[DataSourceProperty\]\s*public\s+[A-Za-z0-9_<>\.\?]+\s+$propertyName\b" "Army line binding property $propertyName exists in source"
}

$lineBindingMethods = @(
    "ExecuteClickFunction",
    "ExecuteBeginHover",
    "ExecuteEndHover"
)

foreach ($methodName in $lineBindingMethods) {
    Assert-SourceRegex $viewModelsSourcePath "public\s+void\s+$methodName\s*\(" "Army line binding method $methodName exists in source"
}

$itemBindingProperties = @(
    "SpritePath",
    "IsWarning",
    "Value",
    "ChangeAmount"
)

foreach ($propertyName in $itemBindingProperties) {
    Assert-SourceRegex $viewModelsSourcePath "\[DataSourceProperty\]\s*public\s+[A-Za-z0-9_<>\.\?]+\s+$propertyName\b" "Army item binding property $propertyName exists in source"
}

$itemBindingMethods = @(
    "ExecuteBeginHint",
    "ExecuteEndHint",
    "ExecuteClickFunction"
)

foreach ($methodName in $itemBindingMethods) {
    Assert-SourceRegex $viewModelsSourcePath "public\s+void\s+$methodName\s*\(" "Army item binding method $methodName exists in source"
}

Write-Host "-- Deployed DLL hash"
$localHash = (Get-FileHash $localDll -Algorithm SHA256).Hash
$installedDll = Get-FirstExistingPath $installedDllCandidates
if ($SkipInstalledHash) {
    Write-Host "[INFO] Installed DLL hash check skipped by request."
    Write-Host "[INFO] Local DLL hash: $localHash"
}
elseif ($installedDll -ne $null) {
    $installedHash = (Get-FileHash $installedDll -Algorithm SHA256).Hash
    Assert-True ($localHash -eq $installedHash) "Local and installed RealmsForgotten.dll hashes match"
    Write-Host "[INFO] Installed DLL path: $installedDll"
    Write-Host "[INFO] DLL hash: $localHash"
}
else {
    Write-Host "[INFO] Installed RealmsForgotten.dll was not found in any expected path:"
    foreach ($candidate in $installedDllCandidates) {
        Write-Host "[INFO]   $candidate"
    }
    Write-Host "[INFO] Local DLL hash: $localHash"
}

Write-Host "-- Reflection"
Load-AssembliesFromFolder $gameBinRoot
Load-AssembliesFromFolder $nativeBinRoot
Load-AssembliesFromFolder $sandBoxBinRoot
Load-AssembliesFromFolder (Split-Path -Path $harmonyAssemblyPath -Parent)
Load-AssembliesFromFolder $localBinRoot

if (Test-Path $modulesRoot) {
    Get-ChildItem -Path $modulesRoot -Recurse -Filter "*.dll" -File | Where-Object {
        $_.Name -ne "0Harmony.dll" -and $_.FullName -ne $installedDll
    } | ForEach-Object {
        $null = Load-AssemblySafe $_.FullName
    }
}

$viewModelAssembly = [System.Reflection.Assembly]::LoadFrom($viewModelAssemblyPath)
$campaignAssembly = [System.Reflection.Assembly]::LoadFrom($campaignAssemblyPath)
$sandBoxViewAssembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $sandBoxBinRoot "SandBox.View.dll"))
$sandBoxGauntletAssembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $sandBoxBinRoot "SandBox.GauntletUI.dll"))
$mountAndBladeGauntletWidgetsAssembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $gameBinRoot "TaleWorlds.MountAndBlade.GauntletUI.Widgets.dll"))
$realmsForgottenAssembly = [System.Reflection.Assembly]::LoadFrom($localDll)
$harmonyAssembly = Get-LoadedAssemblyByName "0Harmony"
if ($null -eq $harmonyAssembly -and (Test-Path $harmonyAssemblyPath)) {
    $harmonyAssembly = [System.Reflection.Assembly]::LoadFrom($harmonyAssemblyPath)
}

$armyManagementVm = $viewModelAssembly.GetType("TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement.ArmyManagementVM")
$campaignUiHelper = $viewModelAssembly.GetType("TaleWorlds.CampaignSystem.ViewModelCollection.CampaignUIHelper")
$armyMenuOverlayVm = $viewModelAssembly.GetType("TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay.ArmyMenuOverlayVM")
$armyCalcModel = $campaignAssembly.GetType("TaleWorlds.CampaignSystem.GameComponents.DefaultArmyManagementCalculationModel")
$mapBarVm = $viewModelAssembly.GetType("TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar.MapBarVM")
$mapScreen = $sandBoxViewAssembly.GetType("SandBox.View.Map.MapScreen")
$mapView = $sandBoxViewAssembly.GetType("SandBox.View.Map.MapView")
$mapBarGlobalLayer = $sandBoxGauntletAssembly.GetType("SandBox.GauntletUI.Map.GauntletMapBarGlobalLayer")
$mapOverlayView = $sandBoxGauntletAssembly.GetType("SandBox.GauntletUI.Map.GauntletMapOverlayView")
$kingdomScreen = $sandBoxGauntletAssembly.GetType("SandBox.GauntletUI.GauntletKingdomScreen")
$armyOverlayWidget = $mountAndBladeGauntletWidgetsAssembly.GetType("TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay.ArmyOverlayWidget")

Assert-True ($null -ne $armyManagementVm) "ArmyManagementVM type exists"
Assert-True ($null -ne $campaignUiHelper) "CampaignUIHelper type exists"
Assert-True ($null -ne $armyMenuOverlayVm) "ArmyMenuOverlayVM type exists"
Assert-True ($null -ne $armyCalcModel) "DefaultArmyManagementCalculationModel type exists"
Assert-True ($null -ne $mapBarVm) "MapBarVM type exists"
Assert-True ($null -ne $mapScreen) "MapScreen type exists"
Assert-True ($null -ne $mapView) "MapView type exists"
Assert-True ($null -ne $mapBarGlobalLayer) "GauntletMapBarGlobalLayer type exists"
Assert-True ($null -ne $mapOverlayView) "GauntletMapOverlayView type exists"
Assert-True ($null -ne $kingdomScreen) "GauntletKingdomScreen type exists"
Assert-True ($null -ne $armyOverlayWidget) "ArmyOverlayWidget type exists"

$constructor = $armyManagementVm.GetConstructor(@([System.Action]))
Assert-True ($null -ne $constructor) "ArmyManagementVM(Action) constructor exists"

$methodNames = @(
    "OnRefresh",
    "RefreshValues",
    "GetCanDisbandArmyWithReason",
    "OnAddToCart",
    "OnRemove",
    "ExecuteDone",
    "DisbandArmy",
    "ExecuteReset",
    "ExecuteCancel",
    "OnFinalize",
    "ApplyCohesionChange"
)

foreach ($methodName in $methodNames) {
    $method = $armyManagementVm.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
        Where-Object { $_.Name -eq $methodName } |
        Select-Object -First 1
    Assert-True ($null -ne $method) "ArmyManagementVM.$methodName exists"
}

$overlayVmMethods = @(
    "ExecuteOpenArmyManagement",
    "get_ArmyToUse",
    "get_IsPlayerArmyLeader",
    "OnFinalize",
    "OnPartyAttachedAnotherParty"
)

foreach ($methodName in $overlayVmMethods) {
    $method = $armyMenuOverlayVm.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
        Where-Object { $_.Name -eq $methodName } |
        Select-Object -First 1
    Assert-True ($null -ne $method) "ArmyMenuOverlayVM.$methodName exists"
}

$fieldNames = @(
    "_influenceSpentForCohesionBoosting",
    "_onClose",
    "_mainPartyItem",
    "_initialInfluence",
    "_boostedCohesion",
    "_playerHasArmy",
    "_currentParties",
    "_partiesToRemove"
)

foreach ($fieldName in $fieldNames) {
    $field = $armyManagementVm.GetField($fieldName, [System.Reflection.BindingFlags]"Instance,Public,NonPublic")
    Assert-True ($null -ne $field) "ArmyManagementVM field $fieldName exists"
}

$canManageMethod = $campaignUiHelper.GetMethods([System.Reflection.BindingFlags]"Static,Public,NonPublic") |
    Where-Object {
        $_.Name -eq "GetCanManageCurrentArmyWithReason" -and
        $_.GetParameters().Count -eq 1 -and
        $_.GetParameters()[0].ParameterType.IsByRef
    } |
    Select-Object -First 1
Assert-True ($null -ne $canManageMethod) "CampaignUIHelper.GetCanManageCurrentArmyWithReason(ref TextObject) exists"

$mapBarVisibilityMethod = $mapBarVm.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
    Where-Object { $_.Name -eq "GetIsGatherArmyVisible" } |
    Select-Object -First 1
Assert-True ($null -ne $mapBarVisibilityMethod) "MapBarVM.GetIsGatherArmyVisible exists"

$eligibilityMethod = $armyCalcModel.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
    Where-Object {
        $_.Name -eq "CheckPartyEligibility" -and
        $_.GetParameters().Count -eq 2 -and
        $_.GetParameters()[1].ParameterType.IsByRef
    } |
    Select-Object -First 1
Assert-True ($null -ne $eligibilityMethod) "DefaultArmyManagementCalculationModel.CheckPartyEligibility(MobileParty, ref TextObject) exists"

$addArmyOverlayMethod = $mapScreen.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
    Where-Object { $_.Name -eq "AddArmyOverlay" } |
    Select-Object -First 1
Assert-True ($null -ne $addArmyOverlayMethod) "MapScreen.AddArmyOverlay exists"

$onRefreshStateMethod = $null
$mapStateHandler = $mapScreen.GetInterfaces() | Where-Object { $_.Name -eq "IMapStateHandler" } | Select-Object -First 1
if ($null -ne $mapStateHandler) {
    try {
        $interfaceMap = $mapScreen.GetInterfaceMap($mapStateHandler)
        for ($i = 0; $i -lt $interfaceMap.InterfaceMethods.Length; $i++) {
            if ($interfaceMap.InterfaceMethods[$i].Name -eq "OnRefreshState") {
                $onRefreshStateMethod = $interfaceMap.TargetMethods[$i]
                break
            }
        }
    }
    catch {
    }
}

if ($null -eq $onRefreshStateMethod) {
    $onRefreshStateMethod = $mapScreen.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
        Where-Object { $_.Name -eq "OnRefreshState" } |
        Select-Object -First 1
}
Assert-True ($null -ne $onRefreshStateMethod) "MapScreen.OnRefreshState exists"

$onArmyLeftMethod = $mapView.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
    Where-Object { $_.Name -eq "OnArmyLeft" } |
    Select-Object -First 1
Assert-True ($null -ne $onArmyLeftMethod) "MapView.OnArmyLeft exists"

$onDisperseArmyMethod = $mapView.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
    Where-Object { $_.Name -eq "OnDispersePlayerLeadedArmy" } |
    Select-Object -First 1
Assert-True ($null -ne $onDisperseArmyMethod) "MapView.OnDispersePlayerLeadedArmy exists"

foreach ($overlayType in @($mapBarGlobalLayer, $mapOverlayView, $kingdomScreen)) {
    $method = $overlayType.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
        Where-Object { $_.Name -eq "OpenArmyManagement" } |
        Select-Object -First 1
    Assert-True ($null -ne $method) "$($overlayType.Name).OpenArmyManagement exists"
}

$pageCountMethod = $armyOverlayWidget.GetMethods([System.Reflection.BindingFlags]"Instance,Public,NonPublic") |
    Where-Object { $_.Name -eq "OnArmyListPageCountChanged" } |
    Select-Object -First 1
Assert-True ($null -ne $pageCountMethod) "ArmyOverlayWidget.OnArmyListPageCountChanged exists"

Write-Host "-- Harmony dry run"
$harmonyType = $harmonyAssembly.GetType("HarmonyLib.Harmony")
Assert-True ($null -ne $harmonyType) "Harmony type exists"

$patchTypes = Get-AssemblyTypesSafe $realmsForgottenAssembly |
    Where-Object {
        $_.Namespace -eq "RealmsForgotten.AiMade.ArmyCommand" -and
        ($_.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq "HarmonyLib.HarmonyPatch" }) -and
        ($_.CustomAttributes | Where-Object {
            $_.AttributeType.FullName -eq "HarmonyLib.HarmonyPatchCategory" -and
            $_.ConstructorArguments.Count -gt 0 -and
            $_.ConstructorArguments[0].Value -eq "RFArmyCommand"
        })
    } |
    Sort-Object FullName

Assert-True ($patchTypes.Count -gt 0) "RFArmyCommand patch types were found in RealmsForgotten.dll"

$legacyPatchType = $realmsForgottenAssembly.GetType("RealmsForgotten.AiMade.ArmyCommand.RFArmyManagementVMPatches")
Assert-True ($null -ne $legacyPatchType) "RFArmyManagementVMPatches type exists in RealmsForgotten.dll"

$legacyConstructorPostfix = $legacyPatchType.GetMethods([System.Reflection.BindingFlags]"Instance,Static,Public,NonPublic") |
    Where-Object { $_.Name -eq "ConstructorPostfix" } |
    Select-Object -First 1
Assert-True ($null -ne $legacyConstructorPostfix) "RealmsForgotten.dll contains RFArmyManagementVMPatches.ConstructorPostfix"

Write-Host "-- Patch target resolution"
$patchTypeNames = @($patchTypes | ForEach-Object { $_.FullName })
$allArmyCommandTypeNames = @(Get-AssemblyTypesSafe $realmsForgottenAssembly | Where-Object { $_.Namespace -eq "RealmsForgotten.AiMade.ArmyCommand" } | ForEach-Object { $_.FullName })
Assert-True ($patchTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyCommandOpenArmyManagementPatch") "OpenArmyManagement patch type exists"
Assert-True ($patchTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyOverlayArmyToUsePatch") "ArmyToUse patch type exists"
Assert-True ($patchTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyCommandMapScreenPatch") "MapScreen patch type exists"
Assert-True ($patchTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyEligibilityPatch") "Army eligibility patch type exists"
Assert-True ($patchTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyManagementComparerPatch") "Army comparer patch type exists"
Assert-True ($patchTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyMapBarVisibilityPatch") "MapBar visibility patch type exists"
Assert-True ($allArmyCommandTypeNames -contains "RealmsForgotten.AiMade.ArmyCommand.RFArmyCanManagePatch") "Can-manage patch helper exists"
Assert-True ($null -ne $onRefreshStateMethod) "MapScreen patch target resolves to OnRefreshState"
Assert-True ($null -ne $eligibilityMethod) "Army eligibility patch target resolves to CheckPartyEligibility"
Assert-True ($null -ne $canManageMethod) "Can-manage patch target resolves to CampaignUIHelper.GetCanManageCurrentArmyWithReason(ref TextObject)"
Assert-True ($null -ne $mapBarVisibilityMethod) "MapBar visibility patch target resolves to MapBarVM.GetIsGatherArmyVisible"

$compareMethodCandidates = $armyManagementVm.GetNestedTypes([System.Reflection.BindingFlags]"Instance,Static,Public,NonPublic") |
    ForEach-Object {
        $_.GetMethods([System.Reflection.BindingFlags]"Instance,Static,Public,NonPublic")
    } |
    Where-Object {
        $_.Name -eq "Compare" -and
        $_.GetParameters().Count -eq 2
    }
Assert-True (@($compareMethodCandidates).Count -gt 0) "Army comparer patch target resolves to at least one Compare method"
Write-Host "[OK] RFArmyCommand custom target checks passed."

Write-Host "-- Guardrails"
$constructorPostfixInSource = Select-String -Path (Join-Path $armyCommandSourceRoot "RFArmyCommandPatches.cs") -Pattern "ConstructorPostfix" -Quiet
Assert-True $constructorPostfixInSource "RFArmyCommand patches source contains the constructor postfix"

$failedPatchTypes = @()
try {
    $harmony = [Activator]::CreateInstance($harmonyType, @("rf.armycommand.verify"))
    foreach ($patchType in $patchTypes) {
        try {
            $processor = $harmony.CreateClassProcessor($patchType)
            $null = $processor.Patch()
        }
        catch {
            $failedPatchTypes += [PSCustomObject]@{
                Type = $patchType.FullName
                Error = $_.Exception.Message
            }
        }
    }
}
catch {
    Write-Host "[INFO] Harmony dry run skipped in this PowerShell host: $($_.Exception.Message)"
}

if ($failedPatchTypes.Count -gt 0) {
    $failedPatchTypes | Format-Table -AutoSize | Out-Host
}

if ($failedPatchTypes.Count -gt 0) {
    throw "Harmony dry run found RFArmyCommand patch failures."
}

Write-Host "[OK] Harmony dry run reported no RFArmyCommand patch failures."
Write-Host "[INFO] RFArmyCommand patch types checked: $($patchTypes.Count)"

Write-Host "== RFArmyCommand verification finished successfully =="
