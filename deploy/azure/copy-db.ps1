<#
.SYNOPSIS
  Copies the SPIC ONE PostgreSQL database from the current VPS into the Azure environment.

.DESCRIPTION
  Uses pg_dump / pg_restore from the official postgres Docker image, so nothing needs to be
  installed on this PC. Passwords are read from files and handed to the containers through a
  temporary env file; they never appear on a command line.
  The Azure firewall is opened for this PC only for the duration of the copy.

.EXAMPLE
  .\deploy\azure\copy-db.ps1 -Environment staging -SourcePasswordFile C:\secure\vps-postgres.txt
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment,
    [string]$SourceHost = '103.14.121.144',
    [int]$SourcePort = 30001,
    [string]$SourceUser = 'postgres',
    [string]$SourceDatabase = 'spicone',
    [Parameter(Mandatory)][string]$SourcePasswordFile,
    [string]$PostgresImage = 'postgres:16-alpine',
    [switch]$KeepDump
)

. (Join-Path $PSScriptRoot 'common.ps1')

$names = Get-EnvNames -Environment $Environment
$null = Assert-AzLogin
$outputs = Get-Outputs -Names $names
$vault = Find-KeyVault -ResourceGroup $names.ResourceGroup
$targetPassword = Get-KeyVaultSecretValue -VaultName $vault -Name 'postgres-admin-password'
if (-not $targetPassword) { throw 'postgres-admin-password not readable from Key Vault.' }
$sourcePassword = (Get-Content $SourcePasswordFile -Raw).Trim()

$work = Join-Path ([IO.Path]::GetTempPath()) "spicone-dbcopy-$((Get-Date).ToString('yyyyMMddHHmmss'))"
New-Item -ItemType Directory -Path $work | Out-Null
$srcEnv = Join-Path $work 'src.env'; "PGPASSWORD=$sourcePassword" | Set-Content $srcEnv -Encoding ascii
$dstEnv = Join-Path $work 'dst.env'; "PGPASSWORD=$targetPassword" | Set-Content $dstEnv -Encoding ascii
$dumpDir = Join-Path $work 'dump'; New-Item -ItemType Directory -Path $dumpDir | Out-Null

$ip = Get-MyPublicIp
$rule = "dbcopy-$((Get-Date).ToString('yyyyMMddHHmmss'))"
Invoke-Az postgres flexible-server firewall-rule create --resource-group $names.ResourceGroup --name $outputs.postgresServerName `
    --rule-name $rule --start-ip-address $ip --end-ip-address $ip | Out-Null

try {
    Write-Host "Dumping $SourceDatabase from $SourceHost:$SourcePort ..." -ForegroundColor Cyan
    docker run --rm --env-file $srcEnv -v "${dumpDir}:/dump" $PostgresImage `
        pg_dump -h $SourceHost -p $SourcePort -U $SourceUser -d $SourceDatabase -Fc --no-owner --no-privileges -f /dump/spicone.dump
    if ($LASTEXITCODE -ne 0) { throw 'pg_dump failed.' }

    Write-Host "Restoring into $($outputs.postgresFqdn)/$($outputs.postgresDatabase) ..." -ForegroundColor Cyan
    docker run --rm --env-file $dstEnv -v "${dumpDir}:/dump" $PostgresImage `
        pg_restore -h $outputs.postgresFqdn -p 5432 -U $outputs.postgresAdminLogin -d $outputs.postgresDatabase `
        --no-owner --no-privileges --clean --if-exists --exit-on-error /dump/spicone.dump
    if ($LASTEXITCODE -ne 0) { throw 'pg_restore failed.' }
    Write-Host 'Database copied.' -ForegroundColor Green
}
finally {
    Invoke-Az postgres flexible-server firewall-rule delete --resource-group $names.ResourceGroup --name $outputs.postgresServerName --rule-name $rule --yes | Out-Null
    Remove-Item $srcEnv, $dstEnv -Force -ErrorAction SilentlyContinue
    if (-not $KeepDump) { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue } else { Write-Host "Dump kept at $dumpDir" }
}
