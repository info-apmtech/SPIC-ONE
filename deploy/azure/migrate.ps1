<#
.SYNOPSIS
  Applies pending EF Core migrations to the environment's Azure PostgreSQL database.

.DESCRIPTION
  Reads the connection string from Key Vault, opens the PostgreSQL firewall for this PC
  for the duration of the run, and executes `dotnet ef database update` with
  Spic.Infrastructure as the migrations project and SpicAPI as the startup project.
  The connection string is passed through an environment variable, never on the command line.

.EXAMPLE
  .\deploy\azure\migrate.ps1 -Environment staging
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment
)

. (Join-Path $PSScriptRoot 'common.ps1')

$names = Get-EnvNames -Environment $Environment
$null = Assert-AzLogin
$outputs = Get-Outputs -Names $names
$vault = Find-KeyVault -ResourceGroup $names.ResourceGroup
$conn = Get-KeyVaultSecretValue -VaultName $vault -Name 'db-connection'
if (-not $conn) { throw 'db-connection secret not readable. Are you signed in as the deployment user?' }

$ip = Get-MyPublicIp
$rule = "migrate-$((Get-Date).ToString('yyyyMMddHHmmss'))"
Write-Host "Opening PostgreSQL firewall for $ip ($rule) ..." -ForegroundColor DarkGray
Invoke-Az postgres flexible-server firewall-rule create --resource-group $names.ResourceGroup --name $outputs.postgresServerName `
    --rule-name $rule --start-ip-address $ip --end-ip-address $ip | Out-Null

try {
    Push-Location $script:RepoRoot
    dotnet tool restore | Out-Null
    $env:ConnectionStrings__DefaultConnection = $conn
    Write-Host 'Applying migrations ...' -ForegroundColor Cyan
    dotnet ef database update --project Spic.Infrastructure --startup-project SpicAPI
    if ($LASTEXITCODE -ne 0) { throw 'dotnet ef database update failed.' }
}
finally {
    Remove-Item Env:\ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    Pop-Location
    Invoke-Az postgres flexible-server firewall-rule delete --resource-group $names.ResourceGroup --name $outputs.postgresServerName --rule-name $rule --yes | Out-Null
}
Write-Host 'Migrations applied.' -ForegroundColor Green
