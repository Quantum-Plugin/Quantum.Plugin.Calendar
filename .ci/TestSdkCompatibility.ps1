param([string[]]$SdkVersions = @('0.3.0-rc.3'))
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = Split-Path $PSScriptRoot -Parent
$testName = 'Quantum.CalendarPlugin.Tests'
$pluginName = 'Quantum.CalendarPlugin.dll'
$testOutput = Join-Path $root "tests/$testName/bin/Release/net10.0"
if (!(Test-Path (Join-Path $testOutput "$testName.dll"))) { throw 'Build the Release tests before running SDK compatibility checks.' }
$originalHash = (Get-FileHash (Join-Path $testOutput $pluginName) -Algorithm SHA256).Hash
$previousVersion = $env:QUANTUM_TEST_SDK_VERSION
try {
    foreach ($version in $SdkVersions) {
        if ($version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') { throw 'Expected a concrete SDK package version.' }
        $matrixRoot = Join-Path $root "artifacts/sdk-compatibility/$version"
        New-Item -ItemType Directory -Force -Path $matrixRoot | Out-Null
        $restoreProject = Join-Path $matrixRoot 'Restore.csproj'
        $escapedVersion = [Security.SecurityElement]::Escape($version)
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally></PropertyGroup>
  <ItemGroup><PackageReference Include="Quantum.Plugin.Abstraction" Version="[$escapedVersion]" /></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $restoreProject -Encoding utf8NoBOM
        $packages = Join-Path $matrixRoot 'packages'
        dotnet restore $restoreProject --packages $packages --source https://api.nuget.org/v3/index.json --verbosity quiet
        $runtimeSdk = Join-Path $packages "quantum.plugin.abstraction/$version/lib/net10.0/Quantum.Plugin.Abstraction.dll"
        if (!(Test-Path -LiteralPath $runtimeSdk)) { throw 'The published SDK runtime was not restored.' }
        $runtimeOutput = Join-Path $matrixRoot 'runtime'
        New-Item -ItemType Directory -Force -Path $runtimeOutput | Out-Null
        Get-ChildItem -LiteralPath $testOutput | Copy-Item -Destination $runtimeOutput -Recurse -Force
        Copy-Item -LiteralPath $runtimeSdk -Destination (Join-Path $runtimeOutput 'Quantum.Plugin.Abstraction.dll') -Force
        if ((Get-FileHash (Join-Path $runtimeOutput $pluginName) -Algorithm SHA256).Hash -ne $originalHash) { throw 'SDK compatibility tests must run the unchanged plugin binary.' }
        $env:QUANTUM_TEST_SDK_VERSION = $version
        dotnet vstest (Join-Path $runtimeOutput "$testName.dll")
    }
} finally {
    $env:QUANTUM_TEST_SDK_VERSION = $previousVersion
}
