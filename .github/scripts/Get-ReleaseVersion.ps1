param(
    [Parameter(Mandatory = $true)][string] $Tag,
    [string] $GitHubOutput
)

$ErrorActionPreference = 'Stop'
$version = $Tag -replace '^[vV]', ''
$pattern = '^(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<pre>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z'
$match = [regex]::Match($version, $pattern)
if (-not $match.Success) { throw "Release tag '$Tag' must be a semantic version such as 6.0.0 or v6.0.0-preview.1." }
foreach ($identifier in $match.Groups['pre'].Value.Split('.')) {
    if ($identifier -match '^0[0-9]+$') { throw 'Numeric prerelease identifiers cannot contain leading zeros.' }
}
$numbers = foreach ($part in @('major', 'minor', 'patch')) {
    $number = 0
    if (-not [int]::TryParse($match.Groups[$part].Value, [ref] $number) -or $number -gt 65534) {
        throw 'Release version components must fit the .NET assembly-version range (0..65534).'
    }
    $number
}
$assemblyVersion = ($numbers -join '.') + '.0'
if ($GitHubOutput) {
    [IO.File]::AppendAllText($GitHubOutput, "version=$version`nassembly-version=$assemblyVersion`n", [Text.UTF8Encoding]::new($false))
}
[pscustomobject]@{ Version = $version; AssemblyVersion = $assemblyVersion }
