# Update the ranking criteria snapshot from ppy/osu-wiki and re-generate the catalogue.
# Run from repository root: .\scripts\update-ranking-criteria.ps1 [update|reparse] [--commit <sha>]
# See docs/RANKING_CRITERIA.md.

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $RepoRoot "MapsetVerifier.slnx"))) {
    $RepoRoot = Get-Location
}

$toolArgs = @($args)
if ($toolArgs.Count -eq 0 -or $toolArgs[0].StartsWith("--")) {
    $toolArgs = @("update") + $toolArgs
}
$projectPath = Join-Path $RepoRoot "MapsetVerifier.RankingCriteria.Tool\MapsetVerifier.RankingCriteria.Tool.csproj"

Push-Location $RepoRoot
try {
    # Build output goes to the host so the pipeline only carries the change report.
    dotnet build $projectPath -c Release --verbosity quiet | Out-Host
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    dotnet run --project $projectPath -c Release --no-build -- @toolArgs
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
