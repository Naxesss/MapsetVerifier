# Run the full dev stack without building (Electron/Vite + local API).
# Also creates a shortcut that uses the app icon.

param(
    [switch]$CreateShortcut
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $RepoRoot "MapsetVerifier.sln"))) {
    $RepoRoot = Get-Location
}

$ScriptPath = $PSCommandPath
$ShortcutPath = Join-Path $PSScriptRoot "Mapset Verifier (Development).lnk"
$IconPath = Join-Path $RepoRoot "assets\icons\dev\icon.ico"

function New-DevAllShortcut {
    if (-not (Test-Path $IconPath)) {
        throw "Icon file not found: $IconPath"
    }

    $wshShell = New-Object -ComObject WScript.Shell
    $shortcut = $wshShell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = "powershell.exe"
    $shortcut.Arguments = "-ExecutionPolicy Bypass -File `"$ScriptPath`""
    $shortcut.WorkingDirectory = $RepoRoot
    $shortcut.IconLocation = $IconPath
    $shortcut.Save()
}

if ($CreateShortcut) {
    New-DevAllShortcut
    Write-Host "Created shortcut: $ShortcutPath"
    exit 0
}

if (-not (Test-Path $ShortcutPath)) {
    try {
        New-DevAllShortcut
        Write-Host "Created shortcut with app icon: $ShortcutPath"
    } catch {
        Write-Warning "Could not create shortcut icon: $($_.Exception.Message)"
    }
}

Push-Location $RepoRoot
try {
    npm run dev:all
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
