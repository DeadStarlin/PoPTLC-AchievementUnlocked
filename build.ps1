param(
    [Parameter(Mandatory = $true)][string]$GameRoot,
    [switch]$BuildOnly
)
$ErrorActionPreference = 'Stop'
$gameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$interop = Join-Path $gameRoot 'BepInEx\interop'
if (!(Test-Path "$interop\UnityEngine.CoreModule.dll")) {
    throw 'BepInEx interop assemblies are missing. Start the game with a working BepInEx installation first.'
}
$sdk = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdk -notmatch '^(\S+) \[(.+)\]$') { throw 'A .NET SDK is required.' }
$compiler = Join-Path $Matches[2] "$($Matches[1])\Roslyn\bincore\csc.dll"
$output = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $output | Out-Null
$refs = foreach ($dir in @('dotnet', 'BepInEx\core', 'BepInEx\interop')) {
    foreach ($dll in Get-ChildItem (Join-Path $gameRoot $dir) -Filter '*.dll') {
        try { $null = [Reflection.AssemblyName]::GetAssemblyName($dll.FullName) }
        catch { continue }
        '/reference:"' + $dll.FullName + '"'
    }
}
$response = @('/nologo', '/target:library', '/langversion:latest', '/nullable:disable',
    ('/out:"' + (Join-Path $output 'AchievementMenu.dll') + '"')) + $refs + @(
    ('"' + (Join-Path $PSScriptRoot 'Plugin.cs') + '"'),
    ('"' + (Join-Path $PSScriptRoot 'Models.cs') + '"'))
$responsePath = Join-Path $output 'build.rsp'
$response | Set-Content $responsePath -Encoding UTF8
& dotnet $compiler "@$responsePath"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed; no plugin installed.' }
$compiled = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $output 'AchievementMenu.dll'))
if ($compiled.Name -ne 'AchievementMenu') { throw 'Unexpected output assembly; no plugin installed.' }
$checksPath = Join-Path $output 'checks.rsp'
$checksResponse = @('/nologo', '/target:exe', '/langversion:latest',
    ('/out:"' + (Join-Path $output 'Checks.dll') + '"')) + $refs + @(
    ('"' + (Join-Path $PSScriptRoot 'Checks.cs') + '"'),
    ('"' + (Join-Path $PSScriptRoot 'Models.cs') + '"'))
$checksResponse | Set-Content $checksPath -Encoding UTF8
& dotnet $compiler "@$checksPath"
if ($LASTEXITCODE -ne 0) { throw 'Check compilation failed; no plugin installed.' }
'{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"}}}' |
    Set-Content (Join-Path $output 'Checks.runtimeconfig.json') -Encoding UTF8
& dotnet (Join-Path $output 'Checks.dll')
if ($LASTEXITCODE -ne 0) { throw 'Offline checks failed; no plugin installed.' }
$desktopRuntime = Join-Path $env:ProgramFiles 'dotnet\shared\Microsoft.WindowsDesktop.App\6.0.36'
if (!(Test-Path "$desktopRuntime\System.Windows.Forms.dll")) { throw 'The installed .NET 6 Windows Desktop runtime is required.' }
$uiResponse = @('/nologo', '/target:winexe', '/langversion:latest',
    ('/out:"' + (Join-Path $output 'MenuApp.dll') + '"')) + $refs
foreach ($assembly in @('System.Windows.Forms.dll','System.Windows.Forms.Primitives.dll','System.Drawing.Common.dll','Accessibility.dll')) {
    $uiResponse += '/reference:"' + (Join-Path $desktopRuntime $assembly) + '"'
}
$uiResponse += @(('"' + (Join-Path $PSScriptRoot 'MenuApp.cs') + '"'), ('"' + (Join-Path $PSScriptRoot 'Models.cs') + '"'))
$uiResponse | Set-Content (Join-Path $output 'menu.rsp') -Encoding UTF8
& dotnet $compiler "@$(Join-Path $output 'menu.rsp')"
if ($LASTEXITCODE -ne 0) { throw 'Menu compilation failed; no plugin installed.' }
'{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.WindowsDesktop.App","version":"6.0.0"}}}' |
    Set-Content (Join-Path $output 'MenuApp.runtimeconfig.json') -Encoding UTF8
$destination = Join-Path $gameRoot 'BepInEx\plugins\AchievementMenu'
if ($BuildOnly) { Write-Host "Build and offline checks passed. Outputs: $output"; return }
New-Item -ItemType Directory -Force $destination | Out-Null
Copy-Item (Join-Path $output 'AchievementMenu.dll') $destination -Force
Copy-Item (Join-Path $output 'MenuApp.dll'),(Join-Path $output 'MenuApp.runtimeconfig.json') $destination -Force
Write-Host "Installed AchievementMenu.dll. Restart the game, pause, and press F8."
