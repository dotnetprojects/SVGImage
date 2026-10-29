$ErrorActionPreference = 'Stop'
$parser = Join-Path $PSScriptRoot '../.github/scripts/Get-ReleaseVersion.ps1'
$valid = @{
    '6.0.0' = '6.0.0'
    'v6.1.2' = '6.1.2'
    'V6.1.2-preview.1' = '6.1.2-preview.1'
    '1.2.3-rc.2+build.7' = '1.2.3-rc.2+build.7'
    '0.0.0' = '0.0.0'
}
foreach ($tag in $valid.Keys) {
    $result = & $parser -Tag $tag
    if ($result.Version -ne $valid[$tag]) { throw "Incorrect package version for $tag." }
    $core = ($valid[$tag] -split '[-+]')[0]
    if ($result.AssemblyVersion -ne "$core.0") { throw "Incorrect assembly version for $tag." }
}
foreach ($tag in @('release-6.0.0', '1.2', '1.2.3.4', '1.02.3', '1.2.3-', '1.2.3-01', '65535.0.0', '999999999999.0.0', '1.2.3;whoami', "1.2.3`nextra", "1.2.3`n", "1.2.3`r`n")) {
    $rejected = $false
    try { $null = & $parser -Tag $tag } catch { $rejected = $true }
    if (-not $rejected) { throw "Invalid release tag was accepted: $tag." }
}
$outputFile = Join-Path ([IO.Path]::GetTempPath()) ('svg-version-' + [Guid]::NewGuid().ToString('N') + '.txt')
try {
    $null = & $parser -Tag 'v6.2.3-beta.4' -GitHubOutput $outputFile
    $outputs = [IO.File]::ReadAllText($outputFile)
    if ($outputs -ne "version=6.2.3-beta.4`nassembly-version=6.2.3.0`n") { throw 'Incorrect GitHub Actions step outputs.' }
} finally { if ([IO.File]::Exists($outputFile)) { [IO.File]::Delete($outputFile) } }
Write-Output 'Release version and GitHub output checks passed.'
