param([string] $PackageDirectory = '.artifacts/packages', [string] $Version = '1.2.25-validation')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$frameworks = 'net461', 'netstandard2.1', 'net6.0', 'net8.0', 'net10.0'
$packagesToInspect = @()
foreach ($name in @('Inkslab', 'Inkslab.Config', 'Inkslab.Json', 'Inkslab.Map', 'Inkslab.DI', 'Inkslab.Net')) {
    $packagePath = Join-Path $PackageDirectory "$name.$Version.nupkg"
    $packagesToInspect += (Resolve-Path -LiteralPath $packagePath).Path
    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $packagePath))
    try {
        $entries = $archive.Entries.FullName
        foreach ($framework in $frameworks) {
            foreach ($extension in @('dll', 'xml')) {
                if ($entries -notcontains "lib/$framework/$name.$extension") {
                    throw "$packagePath missing lib/$framework/$name.$extension"
                }
            }
        }
        $nuspec = $archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') }
        $reader = [IO.StreamReader]::new($nuspec.Open())
        try { [xml] $metadata = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($metadata.package.metadata.version -ne $Version) { throw "Wrong version in $packagePath" }
        $groups = @($metadata.package.metadata.dependencies.group | ForEach-Object { $_.targetFramework })
        if ($groups.Count -ne $frameworks.Count) { throw "$packagePath does not contain $($frameworks.Count) dependency groups: $groups" }
        Write-Host "${name}: $($frameworks.Count) DLL/XML targets and dependency groups verified."
    }
    finally { $archive.Dispose() }
}
& dotnet run --project (Join-Path $PSScriptRoot '../tests/PackageMetadata/PackageMetadata.csproj') -c Release --no-restore -- @packagesToInspect
if ($LASTEXITCODE -ne 0) { throw 'Packaged assembly friend-attribute verification failed.' }
