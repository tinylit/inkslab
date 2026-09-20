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
