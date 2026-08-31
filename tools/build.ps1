# Build MCG only. Dependencies are supplied by the caller and are never packaged.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDirectory,
    [Parameter(Mandatory = $true)][string]$UnityEditorPath,
    [Parameter(Mandatory = $true)][string]$BauiDll
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$game = (Resolve-Path -LiteralPath $GameDirectory).Path
$editor = (Resolve-Path -LiteralPath $UnityEditorPath).Path
$baui = (Resolve-Path -LiteralPath $BauiDll).Path
$editorData = Join-Path (Split-Path $editor -Parent) 'Data'
$managed = Join-Path $game 'Big Ambitions_Data/Managed'
foreach ($binary in @($editor, (Join-Path $game 'UnityPlayer.dll'))) {
    $version = (Get-Item -LiteralPath $binary).VersionInfo.ProductVersion
    if ($version -notlike '2022.3.62f2*7670c08855a9*') { throw 'Unity Editor and game player must use 2022.3.62f2 (7670c08855a9).' }
}
if (!(Test-Path -LiteralPath (Join-Path $game 'MonoBleedingEdge'))) { throw 'A Mono game installation is required; IL2CPP is unsupported.' }
foreach ($name in @('mscorlib.dll', 'BigAmbitions.dll', 'BigAmbitions.ModAPI.dll', 'ArcadeMachines.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $managed $name))) { throw "Missing required game assembly: $name" }
}
$dotnet = Join-Path $editorData 'NetCoreRuntime/dotnet.exe'
$compiler = Join-Path $editorData 'DotNetSdkRoslyn/csc.dll'
$cecil = Join-Path $editorData 'il2cpp/build/deploy/Mono.Cecil.dll'
foreach ($tool in @($dotnet, $compiler, $cecil)) { if (!(Test-Path -LiteralPath $tool)) { throw 'Matching Unity compiler tools are missing.' } }
if (!('Mono.Cecil.AssemblyDefinition' -as [type])) { Add-Type -LiteralPath $cecil }
$dependency = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($baui)
try {
    if ($dependency.Name.Name -ne 'LIB_BaUnifiedUI') { throw 'BauiDll must be the standalone LIB_BaUnifiedUI assembly.' }
    if ($dependency.MainModule.AssemblyReferences.Name -contains 'netstandard') { throw 'BAUI must be built for the game Mono player profile.' }
    $versionType = $dependency.MainModule.GetType('Capisoft.Lib.BaUnifiedUI.Core.BaUiVersion')
    $versionField = $versionType.Fields | Where-Object Name -eq 'Version'
    if (!$versionField -or [version]$versionField.Constant -lt [version]'1.0.2') { throw 'BAUI 1.0.2 or later is required.' }
}
finally { $dependency.Dispose() }

# Unique output per invocation: no recursive deletion or implicit installation.
$buildRoot = Join-Path $repo ('artifacts/build-' + (Get-Date -Format 'yyyyMMdd-HHmmssfff') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
$package = Join-Path $buildRoot 'LIB_BA_MoreComputerGames'
$referenceRoot = Join-Path $buildRoot 'private-references'
New-Item -ItemType Directory -Path $package, $referenceRoot -Force | Out-Null

# The game strips unused Unity wrappers. Compile against the matching full Unity
# modules, retargeted from netstandard to the game's mscorlib profile in private copies.
$unityModules = Join-Path $editorData 'Managed/UnityEngine'
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory($unityModules)
$reader = New-Object Mono.Cecil.ReaderParameters
$reader.AssemblyResolver = $resolver
try {
    foreach ($module in Get-ChildItem -LiteralPath $unityModules -Filter 'UnityEngine*.dll' -File) {
        if ($module.Name -eq 'UnityEngine.UnityWebRequestModule.dll') { continue }
        $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($module.FullName, $reader)
        try {
            foreach ($reference in $assembly.MainModule.AssemblyReferences | Where-Object Name -eq 'netstandard') {
                $reference.Name = 'mscorlib'; $reference.Version = [version]'4.0.0.0'; $reference.Culture = $null
                $reference.PublicKeyToken = [byte[]]@(0xb7,0x7a,0x5c,0x56,0x19,0x34,0xe0,0x89)
            }
            $assembly.Write((Join-Path $referenceRoot $module.Name))
        }
        finally { $assembly.Dispose() }
    }
}
finally { $resolver.Dispose() }
$references = @(
    Get-ChildItem -LiteralPath $managed -Filter '*.dll' -File | Where-Object {
        $_.Name -notlike 'UnityEngine*.dll' -or $_.Name -in @('UnityEngine.UI.dll','UnityEngine.UnityWebRequestModule.dll')
    }
    Get-ChildItem -LiteralPath $referenceRoot -Filter '*.dll' -File
    Get-Item -LiteralPath $baui
) | Sort-Object Name -Unique
$sources = @(Get-ChildItem -LiteralPath (Join-Path $repo 'Scripts') -Filter '*.cs' -File -Recurse | Sort-Object FullName)
if (!$sources.Count) { throw 'No MCG sources found.' }
$dll = Join-Path $package 'LIB_BaComputerGames.dll'
$response = Join-Path $buildRoot 'private-build.rsp'
$compilerArgs = @('/target:library','/optimize+','/debug-','/deterministic+','/langversion:latest','/define:BA_GAME_DLLS_IMPORTED')
$compilerArgs += '/pathmap:"' + $repo.Replace('\','/') + '=/_/MCG"'
$compilerArgs += '/out:"' + $dll.Replace('\','/') + '"'
$compilerArgs += @($references | ForEach-Object { '/reference:"' + $_.FullName.Replace('\','/') + '"' })
$compilerArgs += @($sources | ForEach-Object { '"' + $_.FullName.Replace('\','/') + '"' })
[IO.File]::WriteAllLines($response, $compilerArgs, (New-Object Text.UTF8Encoding($false)))
& $dotnet exec $compiler /noconfig /nostdlib ("@" + $response)
if ($LASTEXITCODE -ne 0) { throw 'MCG compilation failed; private build files remain under ignored artifacts.' }

$built = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
try {
    $manifestVersion = [regex]::Match([IO.File]::ReadAllText((Join-Path $repo 'ModManifest.asset')), '(?m)^\s{2}Version:\s*(\d+\.\d+\.\d+)\s*$').Groups[1].Value
    $apiVersion = $built.MainModule.GetType('Capisoft.Lib.BaComputerGames.ComputerGames').Fields | Where-Object Name -eq 'ApiVersion'
    if (!$manifestVersion -or $apiVersion.Constant -ne $manifestVersion -or $built.Name.Version -ne [version]($manifestVersion + '.0')) { throw 'Manifest, API and assembly versions must agree.' }
    foreach ($attributeName in @('System.Reflection.AssemblyFileVersionAttribute','System.Reflection.AssemblyInformationalVersionAttribute')) {
        $attribute = $built.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq $attributeName }
        if (!$attribute -or $attribute.ConstructorArguments[0].Value -ne $manifestVersion) { throw 'DLL release metadata must match the manifest.' }
    }
    $assemblyRefs = @($built.MainModule.AssemblyReferences.Name)
    if ($assemblyRefs -contains 'netstandard' -or $assemblyRefs -notcontains 'mscorlib') { throw 'Wrong player runtime profile.' }
    if ($assemblyRefs -contains 'FlappyAmbition' -or $assemblyRefs -contains 'ComputerGameHighScore') { throw 'MCG must remain independent of game/leaderboard mods.' }
    if ($built.MainModule.HasDebugHeader) {
        foreach ($entry in $built.MainModule.GetDebugHeader().Entries) {
            if ([string]$entry.Directory.Type -in @('CodeView','EmbeddedPortablePdb')) { throw 'Debug symbols or a PDB reference must not be shipped.' }
        }
    }
}
finally { $built.Dispose() }
$bytes = [IO.File]::ReadAllBytes($dll)
# A UTF-16 string can begin on either byte alignment inside a PE file.
$decodedViews = @(
    [Text.Encoding]::UTF8.GetString($bytes)
    [Text.Encoding]::Unicode.GetString($bytes)
    [Text.Encoding]::Unicode.GetString($bytes, 1, $bytes.Length - 1)
)
foreach ($text in $decodedViews) {
    foreach ($privatePath in @($repo, $game, $editorData, $env:USERPROFILE)) {
        if (!$privatePath) { continue }
        foreach ($variant in @($privatePath, $privatePath.Replace('\','/'))) {
            if ($text.IndexOf($variant, [StringComparison]::OrdinalIgnoreCase) -ge 0) { throw 'Private absolute path detected in the compiled assembly.' }
        }
    }
}
foreach ($name in @('README.md','API.md','REQUIRED_MODS.md','VERIFICATION.md','CHANGELOG.md','LICENSE','ModManifest.asset','Thumbnail.jpg')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination (Join-Path $package $name)
}
foreach ($folder in @('Locales','docs')) {
    $target = Join-Path $package $folder
    New-Item -ItemType Directory -Path $target | Out-Null
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repo $folder) -File | Where-Object Extension -in @('.json','.md')) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $target $file.Name)
    }
}
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repo 'releases') -File -Recurse | Where-Object Extension -in @('.md','.txt')) {
    $target = Join-Path $package $file.FullName.Substring($repo.Length + 1)
    New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $target
}
if (@(Get-ChildItem -LiteralPath $package -Recurse -Filter '*.dll').Count -ne 1) { throw 'Package contains dependency DLLs.' }
if (@(Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object Extension -in @('.pdb','.mdb','.rsp','.log')).Count) { throw 'Package contains private build artifacts.' }
Write-Host ('MCG-only package: ' + $package)
Write-Host ('SHA256: ' + (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash)
Write-Host 'Nothing installed or published. Only share the LIB_BA_MoreComputerGames package, never its parent build directory.'
