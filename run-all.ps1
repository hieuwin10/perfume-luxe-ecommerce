param(
    [ValidateSet("menu", "dotnet", "docker", "stop")]
    [string]$Mode = "menu",
    [switch]$Background
)

$ErrorActionPreference = "Continue"
$RootDir = $PSScriptRoot
Set-Location $RootDir
$Port = 5000

function Write-Header([string]$Title) {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "  PERFUME LUXE - BO KHOI DONG HE THONG ($Title)" -ForegroundColor Yellow
    Write-Host "======================================================================" -ForegroundColor Cyan
}

function Ensure-EnvFile {
    $envPath = Join-Path $RootDir ".env"
    if (-not (Test-Path $envPath)) {
        @"
MSSQL_SA_PASSWORD=PerfumePass@2026!
INITIAL_ADMIN_EMAIL=admin@perfumeluxe.vn
INITIAL_ADMIN_PASSWORD=AdminPassword123!
JWT_ISSUER=PerfumeLuxe
JWT_AUDIENCE=PerfumeLuxe.Clients
JWT_SIGNING_KEY=SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!
DOCKER_WEB_PORT=5000
DOCKER_SQL_PORT=1433
"@ | Set-Content -Path $envPath -Encoding UTF8
    }
}

function Stop-DotnetProcess {
    $procs = Get-Process -Name "Perfume.Web" -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "[STOP] Dang tat tien trinh Dotnet Thuan (Perfume.Web)..." -ForegroundColor Yellow
        $procs | Stop-Process -Force
        Start-Sleep -Seconds 1
    }
}

function Stop-DockerContainers {
    Write-Host "[STOP] Dang tat toan bo Docker Compose containers..." -ForegroundColor Yellow
    docker compose -f docker-compose.yml down 2>$null
    docker compose -f docker-compose.infra.yml down 2>$null
}

function Stop-All {
    Write-Header "TAT TOAN BO HE THONG (DOTNET & DOCKER)"
    Stop-DotnetProcess
    Stop-DockerContainers
    Write-Host "[DONE] Da tat sach ca Dotnet Thuan va Docker!" -ForegroundColor Green
}

function Start-DotnetOnly([bool]$InBackground = $false) {
    Write-Header "CHE DO 1: CHAY DOTNET THUAN (HTTP://LOCALHOST:$Port)"
    Write-Host "[LOCK] Chi chay 1 trong 2: Dang kiem tra va tat Docker (neu dang chay)..." -ForegroundColor DarkCyan
    Stop-DockerContainers
    Stop-DotnetProcess

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=PerfumeDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
    $env:Storage__RootPath = Join-Path $RootDir "src\Perfume.Web\App_Data"
    $env:InitialAdmin__Email = "admin@perfumeluxe.vn"
    $env:InitialAdmin__Password = "AdminPassword123!"
    $env:JWT_SIGNING_KEY = "SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!"
    $env:Jwt__SigningKey = "SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!"
    $env:JWT_ISSUER = "PerfumeLuxe"
    $env:JWT_AUDIENCE = "PerfumeLuxe.Clients"

    if ($InBackground) {
        Write-Host "[START] Dang khoi dong Dotnet Thuan (Background) tai http://localhost:$Port ..." -ForegroundColor Green
        Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Perfume.Web/Perfume.Web.csproj --no-launch-profile --urls http://localhost:$Port" -WorkingDirectory $RootDir -WindowStyle Hidden
        Write-Host "        -> Web Storefront : http://localhost:$Port" -ForegroundColor Yellow
        Write-Host "        -> API Swagger    : http://localhost:$Port/swagger" -ForegroundColor Yellow
    } else {
        Write-Host "[START] Dang chay Dotnet Thuan tai http://localhost:$Port (Nhan Ctrl+C de dung)..." -ForegroundColor Green
        Write-Host "        -> Web Storefront : http://localhost:$Port" -ForegroundColor Yellow
        Write-Host "        -> API Swagger    : http://localhost:$Port/swagger" -ForegroundColor Yellow
        dotnet run --project src/Perfume.Web/Perfume.Web.csproj --no-launch-profile --urls "http://localhost:$Port"
    }
}

function Start-DockerOnly {
    Write-Header "CHE DO 2: CHAY DOCKER FULL-STACK (HTTP://LOCALHOST:$Port)"
    Write-Host "[LOCK] Chi chay 1 trong 2: Dang kiem tra va tat Dotnet Thuan (neu dang chay)..." -ForegroundColor DarkCyan
    Stop-DotnetProcess

    Ensure-EnvFile
    $env:DOCKER_WEB_PORT = "$Port"

    Write-Host "[START] Dang build va khoi dong Docker (Web:$Port + SQL Server:1433 + Redis:6379 + Mailpit:8025)..." -ForegroundColor Cyan
    docker compose -f docker-compose.yml up -d --build

    Write-Host "[SUCCESS] Docker Full-Stack da duoc kich hoat!" -ForegroundColor Green
    Write-Host "          -> Web Storefront : http://localhost:$Port" -ForegroundColor Yellow
    Write-Host "          -> API Swagger    : http://localhost:$Port/swagger" -ForegroundColor Yellow
    Write-Host "          -> Mailpit UI     : http://localhost:8025" -ForegroundColor Yellow
}

if ($Mode -ne "menu") {
    switch ($Mode) {
        "dotnet" { Start-DotnetOnly -InBackground:$Background.IsPresent }
        "docker" { Start-DockerOnly }
        "stop"   { Stop-All }
    }
    exit 0
}

while ($true) {
    Write-Header "CHON 1 TRONG 2 HUONG CHAY (HOAC TAT HE THONG)"
    Write-Host "  [1] Chay DOTNET THUAN (Tu dong tat Docker -> Chay .NET + LocalDB tai http://localhost:5000)" -ForegroundColor Green
    Write-Host "  [2] Chay DOCKER       (Tu dong tat Dotnet -> Chay Docker Compose tai http://localhost:5000)" -ForegroundColor Cyan
    Write-Host "  [3] NUT TAT TAT CA    (Dung ngay lap tuc ca Dotnet lan Docker)" -ForegroundColor Red
    Write-Host "  [0] Thoat" -ForegroundColor DarkGray
    Write-Host ""
    $choice = Read-Host "Nhap lua chon cua ban (1 / 2 / 3 / 0)"

    switch ($choice) {
        "1" { Start-DotnetOnly -InBackground:$false; break }
        "2" { Start-DockerOnly; break }
        "3" { Stop-All; break }
        "0" { Write-Host "Da thoat." -ForegroundColor Gray; exit 0 }
        default { Write-Host "[!] Vui long chi chon 1, 2, 3 hoac 0." -ForegroundColor Red }
    }
}
