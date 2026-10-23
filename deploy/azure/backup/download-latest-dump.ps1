<#
.SYNOPSIS
  Downloads the newest nightly SPIC ONE database dump from the Azure backup storage
  account to a local folder. Meant to run unattended from Windows Task Scheduler
  (monthly, or any schedule) on a PC at the customer's site.

.DESCRIPTION
  The Container Apps job caj-spicone-dbbackup-prd writes a pg_dump copy every night to
  container "pg-dumps" in the backup storage account (path <yyyy>/<mm>/spicone-<stamp>.dump).
  This script signs in with a service principal that holds "Storage Blob Data Reader" on
  that account only, picks the newest blob, downloads it to <Destination>\<yyyy>-<mm>\,
  checks the archive opens (if pg_restore is installed), and deletes local copies older
  than <KeepMonths>. Shared-key access is disabled on the account, so Entra sign-in is
  the only way in; no storage keys or database passwords are needed on the PC.

.EXAMPLE
  .\download-latest-dump.ps1 -StorageAccount stspiconebkprdxxxxxx -TenantId <guid> `
      -ClientId <guid> -ClientSecretFile C:\secure\spic-backup-sp.txt -Destination D:\SPIC-DB-Backups

.NOTES
  Requires Azure CLI (winget install Microsoft.AzureCLI). pg_restore is optional
  (PostgreSQL 16 client tools) and only used to verify the archive.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $StorageAccount,
    [Parameter(Mandatory)] [string] $TenantId,
    [Parameter(Mandatory)] [string] $ClientId,
    [Parameter(Mandatory)] [string] $ClientSecretFile,
    [string] $Container = 'pg-dumps',
    [string] $Destination = 'D:\SPIC-DB-Backups',
    [int]    $KeepMonths = 12
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$log = Join-Path $Destination 'download.log'
function Log([string] $msg) {
    $line = "{0:yyyy-MM-dd HH:mm:ss}  {1}" -f (Get-Date), $msg
    Write-Host $line
    Add-Content -Path $log -Value $line -Encoding utf8
}

try {
    if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw 'Azure CLI (az) is not installed.' }
    if (-not (Test-Path $ClientSecretFile)) { throw "Secret file not found: $ClientSecretFile" }
    $secret = (Get-Content -Path $ClientSecretFile -Raw).Trim()

    Log "Signing in as service principal $ClientId"
    az login --service-principal --username $ClientId --password $secret --tenant $TenantId --output none
    if ($LASTEXITCODE -ne 0) { throw 'az login failed.' }

    Log "Listing blobs in $StorageAccount/$Container"
    $json = az storage blob list --auth-mode login --account-name $StorageAccount --container-name $Container `
                --query '[].{name:name, size:properties.contentLength, modified:properties.lastModified}' --output json
    if ($LASTEXITCODE -ne 0) { throw 'az storage blob list failed.' }
    $blobs = $json | ConvertFrom-Json
    if (-not $blobs) { throw 'No dump files found in the container.' }

    $latest = $blobs | Where-Object { $_.name -like '*.dump' } | Sort-Object modified -Descending | Select-Object -First 1
    if (-not $latest) { throw 'No .dump blobs found.' }
    $ageDays = ((Get-Date) - [datetime]$latest.modified).TotalDays
    Log ("Newest dump: {0} ({1:N1} MB, {2:N1} days old)" -f $latest.name, ($latest.size / 1MB), $ageDays)
    if ($ageDays -gt 3) { Log 'WARNING: newest dump is more than 3 days old; check the nightly job in Azure.' }

    $monthDir = Join-Path $Destination ((Get-Date).ToString('yyyy-MM'))
    New-Item -ItemType Directory -Force -Path $monthDir | Out-Null
    $target = Join-Path $monthDir (Split-Path $latest.name -Leaf)
    if (Test-Path $target) {
        Log "Already downloaded: $target"
    } else {
        $tmp = "$target.part"
        az storage blob download --auth-mode login --account-name $StorageAccount --container-name $Container `
            --name $latest.name --file $tmp --output none
        if ($LASTEXITCODE -ne 0) { throw 'az storage blob download failed.' }
        $len = (Get-Item $tmp).Length
        if ($len -ne [int64]$latest.size) { throw "Size mismatch: expected $($latest.size) bytes, got $len." }
        Move-Item -Path $tmp -Destination $target
        Log "Downloaded to $target"
    }

    if (Get-Command pg_restore -ErrorAction SilentlyContinue) {
        & pg_restore --list $target 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'pg_restore could not read the archive; the file may be corrupt.' }
        Log 'Archive verified with pg_restore --list.'
    } else {
        Log 'pg_restore not installed; skipped archive verification.'
    }

    $cutoff = (Get-Date).AddMonths(-$KeepMonths)
    Get-ChildItem -Path $Destination -Directory | Where-Object {
        $_.Name -match '^\d{4}-\d{2}$' -and [datetime]::ParseExact($_.Name, 'yyyy-MM', $null) -lt $cutoff
    } | ForEach-Object {
        Log "Deleting old copy folder $($_.FullName)"
        Remove-Item -Path $_.FullName -Recurse -Force
    }

    Log 'Done.'
    exit 0
}
catch {
    Log "FAILED: $($_.Exception.Message)"
    exit 1
}
finally {
    az logout --output none 2>$null
}
