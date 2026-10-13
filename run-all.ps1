param(
    [ValidateSet("menu", "dotnet", "docker", "sequential", "parallel", "hybrid", "test", "status", "stop")]
    [string]$Mode = "menu",
    [int]$DotnetPort = 5000,
    [int]$DockerPort = 5000,
    [switch]$UseDockerSqlForDotnet,
    [switch]$Background
)

$ErrorActionPreference = "Continue"
$RootDir = $PSScriptRoot
Set-Location $RootDir

function Write-Header([string]$Title) {
    Write-Host ""
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "  PERFUME LUXE - ALL-IN-ONE LAUNCHER ($Title)" -ForegroundColor Yellow
    Write-Host "======================================================================" -ForegroundColor Cyan
}

function Ensure-EnvFile([int]$WebPort = 5000) {
    $envPath = Join-Path $RootDir ".env"
    @"
MSSQL_SA_PASSWORD=PerfumePass@2026!
INITIAL_ADMIN_EMAIL=admin@perfumeluxe.vn
INITIAL_ADMIN_PASSWORD=AdminPassword123!
JWT_ISSUER=PerfumeLuxe
JWT_AUDIENCE=PerfumeLuxe.Clients
JWT_SIGNING_KEY=SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!
DOCKER_WEB_PORT=$WebPort
DOCKER_SQL_PORT=1433
"@ | Set-Content -Path $envPath -Encoding UTF8
}

function Set-DotnetEnv([bool]$UseDockerSql = $false) {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    if ($UseDockerSql) {
        $env:ConnectionStrings__DefaultConnection = "Server=localhost,1433;Database=PerfumeDb;User Id=sa;Password=PerfumePass@2026!;TrustServerCertificate=True;MultipleActiveResultSets=True;"
        Write-Host "[DOTNET] Database: SQL Server Docker (localhost,1433 -> PerfumeDb)" -ForegroundColor Green
    } else {
        $env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=PerfumeDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
        Write-Host "[DOTNET] Database: SQL Server LocalDB ((localdb)\MSSQLLocalDB -> PerfumeDb)" -ForegroundColor Green
    }
    $env:Storage__RootPath = Join-Path $RootDir "src\Perfume.Web\App_Data"
    $env:InitialAdmin__Email = "admin@perfumeluxe.vn"
    $env:InitialAdmin__Password = "AdminPassword123!"
    $env:JWT_SIGNING_KEY = "SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!"
    $env:Jwt__SigningKey = "SuperSecretPerfumeLuxeKeyWithAtLeast32BytesLength!"
    $env:JWT_ISSUER = "PerfumeLuxe"
    $env:JWT_AUDIENCE = "PerfumeLuxe.Clients"
}

function Test-HttpEndpoint([string]$Url, [int]$Retries = 20, [int]$DelaySec = 2) {
    for ($i = 1; $i -le $Retries; $i++) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 4 -ErrorAction Stop
            if ($resp.StatusCode -eq 200) {
                return $true
            }
        } catch {
            Start-Sleep -Seconds $DelaySec
        }
    }
    return $false
}

function Stop-DotnetProcess {
    $procs = Get-Process -Name "Perfume.Web" -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "[STOP] Dang dung tien trinh Dotnet (Perfume.Web)..." -ForegroundColor Yellow
        $procs | Stop-Process -Force
        Start-Sleep -Seconds 1
    }
}

function Stop-DockerContainers {
    Write-Host "[STOP] Dang dung Docker Compose containers..." -ForegroundColor Yellow
    docker compose -f docker-compose.yml down 2>$null
    docker compose -f docker-compose.infra.yml down 2>$null
}

function Start-DockerMode([int]$TargetPort = 5000, [bool]$StopDotnetFirst = $true) {
    Write-Header "HUONG 1: CHAY FULL DOCKER STACK (PORT $TargetPort)"
    if ($StopDotnetFirst) {
        Stop-DotnetProcess
    }
    Ensure-EnvFile -WebPort $TargetPort
    $env:DOCKER_WEB_PORT = "$TargetPort"

    Write-Host "[DOCKER] Dang build va khoi dong cac container (web:$TargetPort, sqlserver:1433, redis:6379, mailpit:8025)..." -ForegroundColor Cyan
    docker compose -f docker-compose.yml up -d --build

    Write-Host "[WAIT] Dang cho Docker Web App khoi dong va migrate CSDL tai http://localhost:$TargetPort ..." -ForegroundColor Cyan
    if (Test-HttpEndpoint -Url "http://localhost:$TargetPort" -Retries 30 -DelaySec 2) {
        $homeResp = Invoke-WebRequest -Uri "http://localhost:$TargetPort" -UseBasicParsing
        $swagResp = Invoke-WebRequest -Uri "http://localhost:$TargetPort/swagger" -UseBasicParsing
        $boxResp  = Invoke-WebRequest -Uri "http://localhost:$TargetPort/discovery-box" -UseBasicParsing
        Write-Host "[SUCCESS] Full Docker Stack da ONLINE 100%!" -ForegroundColor Green
        Write-Host "          -> Trang chu Docker  : http://localhost:$TargetPort (HTTP $($homeResp.StatusCode))" -ForegroundColor Yellow
        Write-Host "          -> Swagger Docker    : http://localhost:$TargetPort/swagger (HTTP $($swagResp.StatusCode))" -ForegroundColor Yellow
        Write-Host "          -> Discovery Box 3B  : http://localhost:$TargetPort/discovery-box (HTTP $($boxResp.StatusCode))" -ForegroundColor Yellow
        Write-Host "          -> Mailpit Web UI    : http://localhost:8025" -ForegroundColor Yellow
    } else {
        Write-Host "[WARN] Container dang khoi tao SQL Server. Xem log: docker logs perfume_web_app" -ForegroundColor Yellow
        docker logs --tail 25 perfume_web_app
    }
}

function Start-DotnetMode([bool]$InBackground = $false, [bool]$UseDockerSql = $false, [bool]$StopDockerFirst = $true) {
    Write-Header "HUONG 2: CHAY DOTNET THUAN (PORT $DotnetPort)"
    if ($StopDockerFirst -and -not $UseDockerSql) {
        $runningDocker = docker ps --filter "name=perfume_web_app" -q 2>$null
        if ($runningDocker) {
            Stop-DockerContainers
        }
    }
    Set-DotnetEnv -UseDockerSql:$UseDockerSql

    if (Test-HttpEndpoint -Url "http://localhost:$DotnetPort" -Retries 1 -DelaySec 1) {
        Write-Host "[OK] Dotnet Thuan dang chay san tai http://localhost:$DotnetPort" -ForegroundColor Green
        return
    }

    Write-Host "[BUILD] Dang kiem tra build src/Perfume.Web..." -ForegroundColor Cyan
    dotnet build src/Perfume.Web/Perfume.Web.csproj --verbosity quiet

    if ($InBackground) {
        Write-Host "[START] Dang khoi dong Kestrel (Background) tren http://localhost:$DotnetPort ..." -ForegroundColor Cyan
        Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Perfume.Web/Perfume.Web.csproj --no-build --no-launch-profile --urls http://localhost:$DotnetPort" -WorkingDirectory $RootDir -WindowStyle Hidden
        if (Test-HttpEndpoint -Url "http://localhost:$DotnetPort" -Retries 15 -DelaySec 1) {
            $homeResp = Invoke-WebRequest -Uri "http://localhost:$DotnetPort" -UseBasicParsing
            $swagResp = Invoke-WebRequest -Uri "http://localhost:$DotnetPort/swagger" -UseBasicParsing
            $boxResp  = Invoke-WebRequest -Uri "http://localhost:$DotnetPort/discovery-box" -UseBasicParsing
            Write-Host "[SUCCESS] Dotnet Thuan da ONLINE 100%!" -ForegroundColor Green
            Write-Host "          -> Trang chu Dotnet  : http://localhost:$DotnetPort (HTTP $($homeResp.StatusCode))" -ForegroundColor Yellow
            Write-Host "          -> Swagger Dotnet    : http://localhost:$DotnetPort/swagger (HTTP $($swagResp.StatusCode))" -ForegroundColor Yellow
            Write-Host "          -> Discovery Box 3B  : http://localhost:$DotnetPort/discovery-box (HTTP $($boxResp.StatusCode))" -ForegroundColor Yellow
        } else {
            Write-Host "[WARN] Server dang khoi dong, vui long kiem tra http://localhost:$DotnetPort sau vai giay." -ForegroundColor Yellow
        }
    } else {
        Write-Host "[START] Dang chay Dotnet Thuan tai http://localhost:$DotnetPort (Nhan Ctrl+C de dung)..." -ForegroundColor Green
        Write-Host "        -> Swagger UI: http://localhost:$DotnetPort/swagger" -ForegroundColor Yellow
        dotnet run --project src/Perfume.Web/Perfume.Web.csproj --no-build --no-launch-profile --urls "http://localhost:$DotnetPort"
    }
}

function Start-SequentialVerification {
    Write-Header "CHAY TUAN TU: (1) TEST DOCKER -> TAT DOCKER -> (2) CHAY DOTNET THUAN"
    # Buoc 1: Chay Docker
    Start-DockerMode -TargetPort 5000 -StopDotnetFirst:$true

    # Buoc 2: Tat Docker
    Write-Host "`n[STEP 2] Da kiem tra xong Docker! Dang tat Docker de chuyen sang Dotnet Thuan..." -ForegroundColor Cyan
    Stop-DockerContainers

    # Buoc 3: Chay Dotnet Thuan
    Start-DotnetMode -InBackground:$true -UseDockerSql:$false -StopDockerFirst:$false
}

function Start-ParallelMode {
    Write-Header "CHAY SONG SONG CA 2: DOTNET THUAN (:5000) + DOCKER FULL-STACK (:5080)"
    Start-DotnetMode -InBackground:$true -UseDockerSql:$false -StopDockerFirst:$false
    Start-DockerMode -TargetPort 5080 -StopDotnetFirst:$false
}

function Show-Status {
    Write-Header "KIEM TRA TRANG THAI HE THONG"
    foreach ($p in @(5000, 5080, 8025)) {
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:$p" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            Write-Host "  [ONLINE]  http://localhost:$p => HTTP $($r.StatusCode)" -ForegroundColor Green
        } catch {
            Write-Host "  [OFFLINE] http://localhost:$p" -ForegroundColor DarkGray
        }
    }
    Write-Host "`n  [DOCKER CONTAINERS]:" -ForegroundColor Cyan
    docker ps --filter "name=perfume_" --format "  - {{.Names}} | {{.Status}} | {{.Ports}}" 2>$null
}

if ($Mode -ne "menu") {
    switch ($Mode) {
        "docker"     { Start-DockerMode -TargetPort $DockerPort -StopDotnetFirst:$true }
        "dotnet"     { Start-DotnetMode -InBackground:$Background.IsPresent -UseDockerSql:$UseDockerSqlForDotnet.IsPresent -StopDockerFirst:$true }
        "sequential" { Start-SequentialVerification }
        "parallel"   { Start-ParallelMode }
        "hybrid"     {
            Ensure-EnvFile -WebPort 5080
            docker compose -f docker-compose.infra.yml up -d
            Start-Sleep -Seconds 5
            Start-DotnetMode -InBackground:$Background.IsPresent -UseDockerSql:$true -StopDockerFirst:$false
        }
        "test"       { dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --verbosity minimal }
        "status"     { Show-Status }
        "stop"       { Stop-DotnetProcess; Stop-DockerContainers }
    }
    exit 0
}

Write-Header "MENU DIEU KHIEN ALL-IN-ONE (DOCKER & DOTNET THUAN)"
Write-Host "  [1] Chay DOTNET THUAN (Native .NET 8 + LocalDB)            -> http://localhost:5000" -ForegroundColor Green
Write-Host "  [2] Chay DOCKER FULL-STACK (Web + SQL 2022 + Redis + Mail) -> http://localhost:5000" -ForegroundColor Cyan
Write-Host "  [3] Chay TUAN TU (Bat Docker test -> Tat Docker -> Dotnet) -> http://localhost:5000" -ForegroundColor Yellow
Write-Host "  [4] Chay SONG SONG (Dotnet :5000 + Docker :5080 cung luc)" -ForegroundColor Magenta
Write-Host "  [5] Chay Full 362 Bai Test xUnit (Team 1 + Team 2 + Team 3)" -ForegroundColor White
Write-Host "  [6] Kiem tra trang thai (Status Check)" -ForegroundColor Gray
Write-Host "  [7] Dung tat ca (Stop Dotnet & Docker)" -ForegroundColor Red
Write-Host "  [0] Thoat" -ForegroundColor DarkGray
Write-Host ""
$choice = Read-Host "Chon che do chay (1-7)"

switch ($choice) {
    "1" { Start-DotnetMode -InBackground:$false -UseDockerSql:$false -StopDockerFirst:$true }
    "2" { Start-DockerMode -TargetPort 5000 -StopDotnetFirst:$true }
    "3" { Start-SequentialVerification }
    "4" { Start-ParallelMode }
    "5" { dotnet test tests/Perfume.Tests/Perfume.Tests.csproj --verbosity minimal }
    "6" { Show-Status }
    "7" { Stop-DotnetProcess; Stop-DockerContainers }
    default { Write-Host "Da thoat menu." -ForegroundColor Gray }
}
