param(
    [int]$Port = 5087,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "HastaneRandevuSistemi\HastaneRandevuSistemi.csproj"

if (-not (Test-Path $projectPath)) {
    throw "Proje dosyasi bulunamadi: $projectPath"
}

$existing = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -eq $Port } |
    Select-Object -First 1

if ($existing) {
    Write-Host "Port $Port kullanimda, eski surec durduruluyor (PID: $($existing.OwningProcess))."
    Stop-Process -Id $existing.OwningProcess -Force
    Start-Sleep -Milliseconds 400
}

$env:ASPNETCORE_ENVIRONMENT = "Development"

$args = @(
    "run",
    "--project", $projectPath,
    "--launch-profile", "http"
)

if ($NoBuild) {
    $args += "--no-build"
}

Write-Host "Uygulama baslatiliyor: http://localhost:$Port"
Write-Host "Kapatmak icin Ctrl+C."
dotnet @args
