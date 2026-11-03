<#
.SYNOPSIS
    Script deploy cap toc tu may local len Somee hosting khi GitHub Actions gap su co.
.DESCRIPTION
    Tu dong:
    1. Publish du an Perfume.Web sang thu muc ./publish
    2. Upload file app_offline.htm de mo khoa tap tin DLL tren IIS cua Somee
    3. Cho 10 giay de IIS giai phong hoan toan tien trinh w3wp.exe
    4. Dong bo cac tap tin DLL, Views, wwwroot len FTP Somee
    5. Xoa app_offline.htm va kiem tra HTTP 200
#>

param (
    [string]$FtpHost = "perfumeluxe.somee.com",
    [string]$FtpUser = "",
    [string]$FtpPass = "",
    [string]$RemoteDir = "/www.perfumeluxe.somee.com",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "     PERFUME LUXE - SOMEE FAST DEPLOYMENT ENGINE         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Kiem tra tai khoan FTP
if (-not $FtpUser) {
    $FtpUser = Read-Host "Nhap FTP Username (mac dinh: perfumeluxe)"
    if (-not $FtpUser) { $FtpUser = "perfumeluxe" }
}
if (-not $FtpPass) {
    $securePass = Read-Host "Nhap FTP Password" -AsSecureString
    $FtpPass = [System.Net.NetworkCredential]::new("", $securePass).Password
}

# 2. Build & Publish Release
$publishDir = Join-Path $PSScriptRoot "..\publish"
if (-not $SkipBuild) {
    Write-Host "`n[1/5] Dang bien dich va publish ASP.NET Core 8 Web App..." -ForegroundColor Yellow
    dotnet publish src/Perfume.Web/Perfume.Web.csproj -c Release -o $publishDir /p:UseAppHost=false
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Bien dich that bai! Kiem tra lai ma nguon."
        exit 1
    }
    Write-Host "-> Publish thanh cong vao: $publishDir" -ForegroundColor Green
} else {
    Write-Host "`n[1/5] Bo qua buoc bien dich (su dung thu muc publish hien co)..." -ForegroundColor DarkGray
}

# 3. Dat website offline de mo khoa DLL
Write-Host "`n[2/5] Dat website offline (Tao app_offline.htm de giai phong DLL)..." -ForegroundColor Yellow
$offlineHtml = "<html><body><h2>Deploying updates... Please refresh in a moment.</h2></body></html>"
$tempOffline = Join-Path $env:TEMP "app_offline.htm"
[System.IO.File]::WriteAllText($tempOffline, $offlineHtml)

$offlineUri = "ftp://$FtpHost$RemoteDir/app_offline.htm"
try {
    $req = [System.Net.FtpWebRequest]::Create($offlineUri)
    $req.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.UseBinary = $true
    $req.KeepAlive = $false
    $req.Timeout = 15000
    $bytes = [System.IO.File]::ReadAllBytes($tempOffline)
    $req.ContentLength = $bytes.Length
    $stream = $req.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    Write-Host "-> Da upload app_offline.htm thanh cong." -ForegroundColor Green
} catch {
    Write-Warning "Khong the upload app_offline.htm qua FTPWebRequest (co the IIS dang ban). Thu voi curl..."
    curl.exe --retry 3 --max-time 15 --ftp-create-dirs -T $tempOffline -u "$($FtpUser):$($FtpPass)" $offlineUri
}

Write-Host "-> Cho 10 giay de IIS Somee go bo tien trinh va giai phong file DLL..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

# 4. Upload cac tap tin
Write-Host "`n[3/5] Dang dong bo du lieu len Somee FTP..." -ForegroundColor Yellow
$files = Get-ChildItem -Path $publishDir -Recurse -File
$count = 0
$total = $files.Count

foreach ($file in $files) {
    $count++
    $relPath = $file.FullName.Substring($publishDir.Length).Replace("\", "/")
    $targetUri = "ftp://$FtpHost$RemoteDir$relPath"
    
    # Chi hien thi tien do cho cac file quan trong (dll, json, cshtml, css, js)
    if ($file.Extension -match "\.(dll|json|cshtml|css|js)$") {
        Write-Host "[$count/$total] Uploading: $relPath" -ForegroundColor DarkGray
    }

    $uploaded = $false
    $retries = 3
    while (-not $uploaded -and $retries -gt 0) {
        try {
            curl.exe --silent --show-error --retry 2 --max-time 30 --ftp-create-dirs -T $file.FullName -u "$($FtpUser):$($FtpPass)" $targetUri
            $uploaded = $true
        } catch {
            $retries--
            if ($retries -gt 0) {
                Write-Warning "Loi upload file $relPath, thu lai sau 2 giay (con $retries lan)..."
                Start-Sleep -Seconds 2
            } else {
                Write-Warning "Bo qua file do loi lien tiep: $relPath"
            }
        }
    }
}

# 5. Xoa app_offline.htm de dua website online
Write-Host "`n[4/5] Dua website tro lai online (Xoa app_offline.htm)..." -ForegroundColor Yellow
try {
    $delReq = [System.Net.FtpWebRequest]::Create($offlineUri)
    $delReq.Credentials = New-Object System.Net.NetworkCredential($FtpUser, $FtpPass)
    $delReq.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
    $delReq.GetResponse().Close()
    Write-Host "-> Da dua website online thanh cong!" -ForegroundColor Green
} catch {
    curl.exe --silent -u "$($FtpUser):$($FtpPass)" "ftp://$FtpHost/" -Q "DELE $RemoteDir/app_offline.htm"
}

# 6. Kiem tra HTTP status
Write-Host "`n[5/5] Kiem tra phan hoi tu live storefront https://perfumeluxe.somee.com..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
    $resp = Invoke-WebRequest -Uri "https://perfumeluxe.somee.com" -TimeoutSec 15 -UseBasicParsing
    Write-Host "-> Website dang hoat dong! HTTP $($resp.StatusCode)" -ForegroundColor Green
} catch {
    Write-Warning "Website dang khoi dong hoac mat nhieu thoi gian hon de phan hoi: $($_.Exception.Message)"
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "       DEPLOY SOMEE HOAN TAT VA SAN SANG SU DUNG!         " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

