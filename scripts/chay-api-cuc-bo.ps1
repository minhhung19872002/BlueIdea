<#
.SYNOPSIS
    Chạy API ở môi trường phát triển cục bộ (không dùng Docker cho API).

.DESCRIPTION
    Dừng tiến trình API đang chạy, build lại và khởi động API trên cổng chỉ định.
    PostgreSQL vẫn chạy trong Docker (xem deploy/docker-compose.yml).

.EXAMPLE
    ./scripts/chay-api-cuc-bo.ps1 -Cong 5299 -CongPostgres 55432
#>
param(
    [int]$Cong = 5299,
    [int]$CongPostgres = 5432,
    [switch]$NapLaiDuLieuMau
)

$ErrorActionPreference = 'Stop'
$goc = Split-Path -Parent $PSScriptRoot
Set-Location $goc

Write-Host "==> Dừng tiến trình API đang chạy (nếu có)..." -ForegroundColor Cyan
Get-Process -Name BlueIdea.Api -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

if ($NapLaiDuLieuMau) {
    Write-Host "==> Xoá và tạo lại schema public..." -ForegroundColor Yellow
    docker exec blueidea-postgres psql -U blueidea -d blueidea `
        -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;" | Out-Null
}

Write-Host "==> Build..." -ForegroundColor Cyan
dotnet build src/BlueIdea.Api -v q --nologo

$env:ConnectionStrings__Postgres =
    "Host=localhost;Port=$CongPostgres;Database=blueidea;Username=blueidea;Password=blueidea"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://localhost:$Cong"
$env:EfCore__GhiLogChiTiet = 'false'

Write-Host "==> Khởi động API tại http://localhost:$Cong (Swagger: /swagger)" -ForegroundColor Green
dotnet run --project src/BlueIdea.Api --no-launch-profile --no-build
