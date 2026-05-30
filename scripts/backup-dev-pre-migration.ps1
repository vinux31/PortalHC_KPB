<#
.SYNOPSIS
    SQL Server Database backup pre-migration. Phase 338 REST-05.

.DESCRIPTION
    Standalone PowerShell script untuk IT jalankan SEBELUM migration / redeploy.
    Bertujuan systematize backup workflow supaya Cilacap-style data loss (Phase 336
    ROOT_CAUSE.md) tidak terulang. Script TIDAK hardcode credential — semua via
    param mandatory + Windows Auth (sqlcmd -E flag).

.PARAMETER Server
    SQL Server instance hostname atau named instance.
    Contoh: "10.55.3.3", "(localdb)\MSSQLLocalDB", "localhost\SQLEXPRESS"

.PARAMETER Database
    Nama database target.
    Contoh: "HcPortalDB_Dev", "HcPortalDB"

.PARAMETER OutputPath
    Full path output file .bak. Folder parent harus exist (script TIDAK auto-create).

.EXAMPLE
    .\backup-dev-pre-migration.ps1 -Server "10.55.3.3" -Database "HcPortalDB_Dev" -OutputPath "C:\Backup\HcPortalDB_Dev_pre_20260530_143000.bak"

.NOTES
    Phase 338 REST-05 (D-06 A+ Enhanced existing DB_HANDOFF_IT workflow).
    Ikut systematize workflow doc-only sebelumnya (handoff doc 2026-05-13 + 2026-05-26).

    Requirements:
    - sqlcmd di PATH (SQL Server Command Line Utilities)
    - Windows Auth user punya BACKUP DATABASE permission
    - Folder OutputPath exist
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="SQL Server instance hostname")]
    [string]$Server,

    [Parameter(Mandatory=$true, HelpMessage="Database name")]
    [string]$Database,

    [Parameter(Mandatory=$true, HelpMessage="Full path output .bak file")]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

# Step 1: Validate output folder exists
$folder = Split-Path -Parent $OutputPath
if ([string]::IsNullOrEmpty($folder)) {
    Write-Error "OutputPath harus full path (e.g., C:\Backup\file.bak), bukan filename saja."
    exit 1
}
if (-not (Test-Path $folder)) {
    Write-Error "Output folder tidak ada: $folder. Create folder dulu lalu retry: mkdir -Path '$folder'"
    exit 1
}

# Step 2: Validate sqlcmd available
$sqlcmdInfo = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmdInfo) {
    Write-Error "sqlcmd tidak ditemukan di PATH. Install 'SQL Server Command Line Utilities' (msodbcsql + mssql-tools)."
    exit 1
}

# Step 3: Compose BACKUP DATABASE T-SQL
$query = @"
BACKUP DATABASE [$Database]
TO DISK = N'$OutputPath'
WITH FORMAT, INIT, COMPRESSION,
     NAME = N'$Database Pre-Migration Full Backup',
     STATS = 10;
"@

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " Phase 338 REST-05 — Pre-Migration Backup" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "Server   : $Server"
Write-Host "Database : $Database"
Write-Host "Output   : $OutputPath"
Write-Host "Time     : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "----------------------------------------------------------------"

# Step 4: Execute backup via sqlcmd
& sqlcmd -S $Server -E -Q $query

if ($LASTEXITCODE -ne 0) {
    Write-Error "Backup gagal (sqlcmd exit code $LASTEXITCODE). Check sqlcmd output di atas untuk error detail."
    exit $LASTEXITCODE
}

# Step 5: Verify file created
if (Test-Path $OutputPath) {
    $sizeMb = [Math]::Round((Get-Item $OutputPath).Length / 1MB, 1)
    Write-Host "----------------------------------------------------------------"
    Write-Host ("Backup SUKSES: {0} MB di {1}" -f $sizeMb, $OutputPath) -ForegroundColor Green
    Write-Host "================================================================" -ForegroundColor Cyan

    if ($sizeMb -lt 1) {
        Write-Warning "Backup file size SUSPICIOUSLY KECIL ($sizeMb MB). Bisa jadi DB kosong atau backup corrupt — verify sebelum migration."
    }
    exit 0
} else {
    Write-Error "Backup file tidak terbuat di $OutputPath. Cek SQL Server log + service account permission."
    exit 1
}
