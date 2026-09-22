<#
.SYNOPSIS
  Opens the Azure PostgreSQL firewall for this PC and prints the connection details for
  pgAdmin / DBeaver / psql. The password is read from Key Vault, never typed.

.DESCRIPTION
  Adds a firewall rule named after this computer for the current public IP. Run again after
  the IP changes. -Remove deletes the rule when access is no longer needed.
  Requires: Azure CLI signed in to SPIC's directory with the Key Vault Secrets User role
  (granted to the deployer by provision.ps1; others get it via Access control (IAM)).

.EXAMPLE
  .\deploy\azure\db-access.ps1 -Environment prod
  .\deploy\azure\db-access.ps1 -Environment prod -ShowPassword
  .\deploy\azure\db-access.ps1 -Environment prod -Remove
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment,
    [switch]$ShowPassword,
    [switch]$Remove
)

. (Join-Path $PSScriptRoot 'common.ps1')

$names = Get-EnvNames -Environment $Environment
$null = Assert-AzLogin
$outputs = Get-Outputs -Names $names
$rule = ('dev-' + $env:COMPUTERNAME).ToLower() -replace '[^a-z0-9-]', '-'

if ($Remove) {
    Invoke-Az postgres flexible-server firewall-rule delete --resource-group $names.ResourceGroup --server-name $outputs.postgresServerName --name $rule --yes | Out-Null
    Write-Host "Firewall rule $rule removed." -ForegroundColor Green
    return
}

$ip = Get-MyPublicIp
if (-not $ip) { throw 'Could not determine the public IP of this PC.' }
Invoke-Az postgres flexible-server firewall-rule create --resource-group $names.ResourceGroup --server-name $outputs.postgresServerName `
    --name $rule --start-ip-address $ip --end-ip-address $ip | Out-Null

Write-Host ''
Write-Host "PostgreSQL access from this PC ($ip) is open. Connection details:" -ForegroundColor Cyan
Write-Host ('  {0,-10} {1}' -f 'Host',     $outputs.postgresFqdn)
Write-Host ('  {0,-10} {1}' -f 'Port',     '5432')
Write-Host ('  {0,-10} {1}' -f 'Database', $outputs.postgresDatabase)
Write-Host ('  {0,-10} {1}' -f 'Username', $outputs.postgresAdminLogin)
Write-Host ('  {0,-10} {1}' -f 'SSL',      'required (SSL mode: require)')
if ($ShowPassword) {
    $vault = Find-KeyVault -ResourceGroup $names.ResourceGroup
    $pw = Get-KeyVaultSecretValue -VaultName $vault -Name 'postgres-admin-password'
    if (-not $pw) { throw 'Password not readable: ask for the Key Vault Secrets User role on the vault.' }
    Write-Host ('  {0,-10} {1}' -f 'Password', $pw)
} else {
    Write-Host '  Password  run again with -ShowPassword, or read Key Vault secret postgres-admin-password in the portal'
}
Write-Host ''
Write-Host "Remove access later with: .\deploy\azure\db-access.ps1 -Environment $Environment -Remove" -ForegroundColor DarkGray
