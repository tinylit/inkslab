param([string] $Version = '1.2.25-validation.2')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$output = Join-Path $PSScriptRoot '../.artifacts/standard-feed'
[IO.Directory]::CreateDirectory($output) | Out-Null
$testVersion = "$Version-standard"
foreach ($name in @('Inkslab','Inkslab.Config','Inkslab.DI','Inkslab.Json','Inkslab.Map','Inkslab.Net')) {
    $inputPath = Join-Path $PSScriptRoot "../.artifacts/packages/$name.$Version.nupkg"
    $destination = Join-Path $output "$name.$testVersion.nupkg"
    if (Test-Path -LiteralPath $destination) { throw "Choose a new validation version; already exists: $destination" }
    $inputArchive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $inputPath))
    $outputArchive = [IO.Compression.ZipFile]::Open($destination, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $inputArchive.Entries) {
            if ($entry.FullName -match '^lib/' -and $entry.FullName -notmatch '^lib/netstandard2.1/') { continue }
            $newEntry = $outputArchive.CreateEntry($entry.FullName)
            $newStream = $newEntry.Open()
            try {
                if ($entry.FullName.EndsWith('.nuspec')) {
                    $reader = [IO.StreamReader]::new($entry.Open())
                    try { [xml] $spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
                    $spec.package.metadata.version = $testVersion
                    foreach ($group in @($spec.package.metadata.dependencies.group)) {
                        if ($group.targetFramework -ne '.NETStandard2.1') { [void]$group.ParentNode.RemoveChild($group) }
                    }
                    foreach ($dependency in @($spec.package.metadata.dependencies.group.dependency)) {
                        if ($dependency.id -like 'Inkslab*') { $dependency.version = $testVersion }
                    }
                    foreach ($nodeName in @('frameworkReferences','frameworkAssemblies')) {
                        $node = $spec.package.metadata.$nodeName
                        if ($node -is [Xml.XmlNode]) { [void]$node.ParentNode.RemoveChild($node) }
                    }
                    $writer = [IO.StreamWriter]::new($newStream, [Text.UTF8Encoding]::new($false), 1024, $true)
                    try { $writer.Write($spec.OuterXml) } finally { $writer.Dispose() }
                }
                else {
                    $originalStream = $entry.Open()
                    try { $originalStream.CopyTo($newStream) } finally { $originalStream.Dispose() }
                }
            }
            finally { $newStream.Dispose() }
        }
    }
    finally { $inputArchive.Dispose(); $outputArchive.Dispose() }
}
Write-Host "Test-only netstandard2.1 feed: $output; version $testVersion. DLLs are copied unchanged. Do not publish."
