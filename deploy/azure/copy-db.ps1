<#
.SYNOPSIS
  Copies the SPIC ONE PostgreSQL database from the current VPS into the Azure environment.

.DESCRIPTION
  Uses pg_dump / pg_restore from the official postgres Docker image, so nothing needs to be
  installed on this PC. Passwords are read from files and handed to the containers through a
  temporary env file; they never appear on a command line.
  The Azure firewall is opened for this PC only for the duration of the copy.

  With -DumpFile the source step is skipped and an existing custom-format dump (a nightly copy
  downloaded from the backup storage account, or one kept with -KeepDump) is restored instead.

.EXAMPLE
  .\deploy\azure\copy-db.ps1 -Environment staging -SourcePasswordFile C:\secure\vps-postgres.txt
  .\deploy\azure\copy-db.ps1 -Environment prod -DumpFile C:\restore\spicone-20260918T203000Z.dump
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment,
    [string]$SourceHost = '103.14.121.144',
    [int]$SourcePort = 30001,
    [string]$SourceUser = 'postgres',
    [string]$SourceDatabase = 'spicone',
    [string]$SourcePasswordFile,
    [string]$SourceConnectionStringFile,   # alternative: a Npgsql connection string (or an appsettings JSON line) holding host/port/db/user/password
    [string]$DumpFile,                     # restore this pg_dump custom-format file instead of dumping a source server
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
$sourcePassword = ''
if ($DumpFile) {
    if (-not (Test-Path $DumpFile)) { throw "Dump file not found: $DumpFile" }
    Write-Host "Source: dump file $DumpFile" -ForegroundColor DarkGray
} elseif ($SourceConnectionStringFile) {
    $cs = (Get-Content $SourceConnectionStringFile -Raw).Trim()
    if ($cs -match '^\s*"[^"]+"\s*:\s*"(.*)"\s*,?\s*$') { $cs = $Matches[1] }
    $kv = @{}
    foreach ($part in ($cs -split ';')) { if ($part -match '^\s*([^=]+)=(.*)$') { $kv[$Matches[1].Trim().ToLower()] = $Matches[2].Trim() } }
    if ($kv['host'])     { $SourceHost = $kv['host'] }
    if ($kv['port'])     { $SourcePort = [int]$kv['port'] }
    if ($kv['database']) { $SourceDatabase = $kv['database'] }
    if ($kv['username']) { $SourceUser = $kv['username'] }
    $sourcePassword = $kv['password']
    if (-not $sourcePassword) { throw 'No Password= in the connection string file.' }
} elseif ($SourcePasswordFile) {
    $sourcePassword = (Get-Content $SourcePasswordFile -Raw).Trim()
} else { throw 'Give -SourcePasswordFile, -SourceConnectionStringFile or -DumpFile.' }
if (-not $DumpFile) { Write-Host "Source: ${SourceHost}:${SourcePort}/${SourceDatabase} as ${SourceUser}" -ForegroundColor DarkGray }

$work = Join-Path ([IO.Path]::GetTempPath()) "spicone-dbcopy-$((Get-Date).ToString('yyyyMMddHHmmss'))"
New-Item -ItemType Directory -Path $work | Out-Null
$srcEnv = Join-Path $work 'src.env'; "PGPASSWORD=$sourcePassword" | Set-Content $srcEnv -Encoding ascii
$dstEnv = Join-Path $work 'dst.env'; "PGPASSWORD=$targetPassword" | Set-Content $dstEnv -Encoding ascii
$dumpDir = Join-Path $work 'dump'; New-Item -ItemType Directory -Path $dumpDir | Out-Null

$ip = Get-MyPublicIp
$rule = "dbcopy-$((Get-Date).ToString('yyyyMMddHHmmss'))"
Invoke-Az postgres flexible-server firewall-rule create --resource-group $names.ResourceGroup --server-name $outputs.postgresServerName `
    --name $rule --start-ip-address $ip --end-ip-address $ip | Out-Null

try {
    if ($DumpFile) {
        Copy-Item $DumpFile (Join-Path $dumpDir 'spicone.dump')
    } else {
        Write-Host "Dumping $SourceDatabase from ${SourceHost}:${SourcePort} ..." -ForegroundColor Cyan
        docker run --rm --env-file $srcEnv -v "${dumpDir}:/dump" $PostgresImage `
            pg_dump -h $SourceHost -p $SourcePort -U $SourceUser -d $SourceDatabase -Fc --no-owner --no-privileges -f /dump/spicone.dump
        if ($LASTEXITCODE -ne 0) { throw 'pg_dump failed.' }
    }

    Write-Host "Restoring into $($outputs.postgresFqdn)/$($outputs.postgresDatabase) ..." -ForegroundColor Cyan
    docker run --rm --env-file $dstEnv -v "${dumpDir}:/dump" $PostgresImage `
        pg_restore -h $outputs.postgresFqdn -p 5432 -U $outputs.postgresAdminLogin -d $outputs.postgresDatabase `
        --no-owner --no-privileges --clean --if-exists --exit-on-error /dump/spicone.dump
    if ($LASTEXITCODE -ne 0) { throw 'pg_restore failed.' }
    Write-Host 'Database copied.' -ForegroundColor Green
}
finally {
    Invoke-Az postgres flexible-server firewall-rule delete --resource-group $names.ResourceGroup --server-name $outputs.postgresServerName --name $rule --yes | Out-Null
    Remove-Item $srcEnv, $dstEnv -Force -ErrorAction SilentlyContinue
    if (-not $KeepDump) { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue } else { Write-Host "Dump kept at $dumpDir" }
}
