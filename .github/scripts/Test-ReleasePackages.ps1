param(
    [Parameter(Mandatory = $true)][string] $Directory,
    [Parameter(Mandatory = $true)][string] $Version,
    [Parameter(Mandatory = $true)][string] $AssemblyVersion
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$packages = @(Get-ChildItem -LiteralPath $Directory -Filter '*.nupkg' -File)
$symbols = @(Get-ChildItem -LiteralPath $Directory -Filter '*.snupkg' -File)
if ($packages.Count -ne 1 -or $symbols.Count -ne 1) { throw 'Expected one main package and one symbol package.' }
$inspectionFile = Join-Path ([IO.Path]::GetTempPath()) ('svg-package-' + [Guid]::NewGuid().ToString('N') + '.dll')
try {
    foreach ($package in @($packages[0], $symbols[0])) {
        $archive = [IO.Compression.ZipFile]::OpenRead($package.FullName)
        try {
            $spec = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
            if ($spec.Count -ne 1) { throw 'Expected one package manifest.' }
            $reader = [IO.StreamReader]::new($spec[0].Open())
            try { [xml] $manifest = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $metadata = $manifest.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
            if ($metadata.id -ne 'DotNetProjects.SVGImage' -or $metadata.version -ne $Version) {
                throw "Unexpected identity/version in $($package.Name): $($metadata.id) $($metadata.version)."
            }
            if ($package.Extension -eq '.nupkg') {
                $assemblies = @($archive.Entries | Where-Object { $_.FullName -match '^lib/[^/]+/DotNetProjects\.SVGImage\.dll$' })
                if ($assemblies.Count -ne 9) { throw 'The main package must contain all nine framework assemblies.' }
                foreach ($entry in $assemblies) {
                    $inputStream = $entry.Open()
                    $outputStream = [IO.File]::Create($inspectionFile)
                    try { $inputStream.CopyTo($outputStream) } finally { $inputStream.Dispose(); $outputStream.Dispose() }
                    $actualVersion = [Reflection.AssemblyName]::GetAssemblyName($inspectionFile).Version.ToString()
                    if ($actualVersion -ne $AssemblyVersion) { throw "Incorrect assembly version $actualVersion in $($entry.FullName)." }
                }
            } else {
                if (@($archive.Entries | Where-Object { $_.FullName.EndsWith('.pdb') }).Count -ne 9) {
                    throw 'The symbol package must contain symbols for all nine frameworks.'
                }
            }
        } finally { $archive.Dispose() }
    }
} finally { if ([IO.File]::Exists($inspectionFile)) { [IO.File]::Delete($inspectionFile) } }
Write-Output "Validated release packages: $Version ($AssemblyVersion), nine frameworks and symbols."
