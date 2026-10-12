param([Parameter(Mandatory = $true)][string]$GameRoot)
$ErrorActionPreference = 'Stop'
$targetRoot = (Resolve-Path -LiteralPath $GameRoot).Path
if (!(Test-Path (Join-Path $targetRoot 'TheLostCrown.exe')) -or
    !(Test-Path (Join-Path $targetRoot 'BepInEx\core\BepInEx.Unity.IL2CPP.dll'))) {
    throw 'Select the Lost Crown game folder with BepInEx 6 IL2CPP installed.'
}
$sourceDir = Join-Path $PSScriptRoot 'release\BepInEx\plugins\AchievementMenu'
$pluginFiles = @('AchievementMenu.dll','MenuApp.dll','MenuApp.runtimeconfig.json')
foreach ($pluginFile in $pluginFiles) {
    if (!(Test-Path (Join-Path $sourceDir $pluginFile))) { throw "Missing release file: $pluginFile" }
}
$destinationDir = Join-Path $targetRoot 'BepInEx\plugins\AchievementMenu'
New-Item -ItemType Directory -Force $destinationDir | Out-Null
foreach ($pluginFile in $pluginFiles) {
    Copy-Item -LiteralPath (Join-Path $sourceDir $pluginFile) -Destination $destinationDir -Force
}
Write-Host 'Installed. Launch through Steam, sign into Ubisoft Connect, and press F8.'
