# Export all beatmap check metadata to JSON.
# Run from repository root: .\scripts\export-checks-metadata.ps1 [output-path]
# If output-path is omitted, writes to scripts/checks-metadata.json.

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $RepoRoot "MapsetVerifier.sln"))) {
    $RepoRoot = Get-Location
}

$outPath = $args[0]
if (-not $outPath) { $outPath = "scripts/checks-metadata.json" }
$projectPath = Join-Path $RepoRoot "MapsetVerifier.Exports\MapsetVerifier.Exports.csproj"

Push-Location $RepoRoot
try {
    dotnet build $projectPath -c Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet run --project $projectPath -c Release --no-build -- $outPath
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
