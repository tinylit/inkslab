[CmdletBinding(PositionalBinding = $false)]
param(
    [bool] $CreatePackages = $true,
    [switch] $SkipRestore,
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
$projectsToBuild = 'Inkslab', 'Inkslab.Config', 'Inkslab.Json', 'Inkslab.Map', 'Inkslab.DI', 'Inkslab.Net'
$packageOutputFolder = Join-Path $PSScriptRoot '.nupkgs'

function Invoke-CheckedDotnet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE."
    }
}

Push-Location $PSScriptRoot
try {
    if (-not $SkipRestore) {
        Invoke-CheckedDotnet restore Inkslab.sln
    }
    Invoke-CheckedDotnet build Inkslab.sln -c Release --no-restore
    if (-not $SkipTests) {
        $testProjects = Get-ChildItem -Path (Join-Path $PSScriptRoot 'tests') -Filter '*.csproj' -Recurse |
            Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match 'Microsoft.NET.Test.Sdk' }
        foreach ($testProject in $testProjects) {
            Invoke-CheckedDotnet test $testProject.FullName -c Release --no-build --no-restore
        }
        # The certificate policy tests exercise internal helpers available only in Debug.
        Invoke-CheckedDotnet test tests/Inkslab.Net.Tests/Inkslab.Net.Tests.csproj -c Debug --no-restore --filter 'FullyQualifiedName~CertificateValidationTests'
        Invoke-CheckedDotnet test tests/Inkslab.Net.Tests/Inkslab.Net.Tests.csproj -c Release -f net10.0 --no-restore '-p:InkslabNetAssetFramework=netstandard2.1' --filter 'FullyQualifiedName~TransferPerformanceRegressionTests'
        # Exercise the compatibility asset, whose DI package also supports keyed services.
        Invoke-CheckedDotnet test tests/Inkslab.DI.UnitTests/Inkslab.DI.UnitTests.csproj -c Release -f net10.0 --no-restore '-p:UseNetstandardDependencyInjection=true'
    }
    if ($CreatePackages) {
        foreach ($project in $projectsToBuild) {
            Invoke-CheckedDotnet pack "src/$project/$project.csproj" -c Release --no-build --no-restore -o $packageOutputFolder
        }
        $packageVersion = & dotnet msbuild 'src/Inkslab/Inkslab.csproj' -nologo -getProperty:Version
        if ($LASTEXITCODE -ne 0) { throw 'Cannot determine package version.' }
        & (Join-Path $PSScriptRoot 'tools/Test-Packages.ps1') -PackageDirectory $packageOutputFolder -Version $packageVersion.Trim()
    }
}
finally {
    Pop-Location
}
