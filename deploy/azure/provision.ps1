<#
.SYNOPSIS
  Creates (or updates) the SPIC ONE Azure platform for one environment, then deploys the apps.

.DESCRIPTION
  Pass 1 deploys infra/azure/main.bicep with deployApps=false: log workspace, VNet,
  storage + file shares, container registry, managed identity, Key Vault, PostgreSQL,
  Container Apps environment. Generated secrets land in Key Vault; on later runs the
  same values are read back so nothing rotates by accident.
  Then the sample download templates are seeded into the uploads share, and unless
  -PlatformOnly is given, deploy.ps1 builds and deploys the two apps.

.EXAMPLE
  .\deploy\azure\provision.ps1 -Environment staging -AllowMyIp
  .\deploy\azure\provision.ps1 -Environment prod -IfmsConnectionStringFile C:\secure\ifms.txt
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment,
    [string]$Subscription = '',
    [string]$Location = 'centralindia',
    [switch]$PlatformOnly,
    [switch]$AllowMyIp,
    [string]$IfmsConnectionStringFile,
    [string]$IfmsDeviceKeyFile,
    [string]$IfmsAutomationKeyFile,
    [string]$DatabaseConnectionStringFile,
    [switch]$SecretsFromAppSettings,
    [switch]$ClearDatabaseOverride,
    [string]$WebApiBaseUrl = ''
)

. (Join-Path $PSScriptRoot 'common.ps1')

$names = Get-EnvNames -Environment $Environment
if ($Subscription) { Invoke-Az account set --subscription $Subscription | Out-Null }
$account = Assert-AzLogin
Invoke-Az config set bicep.check_version=false --only-show-errors | Out-Null

# Resource providers are registered once per subscription; harmless when already registered.
foreach ($provider in 'Microsoft.App', 'Microsoft.OperationalInsights', 'Microsoft.DBforPostgreSQL',
                      'Microsoft.ContainerRegistry', 'Microsoft.KeyVault', 'Microsoft.Storage',
                      'Microsoft.ManagedIdentity', 'Microsoft.Insights', 'Microsoft.Network') {
    $state = Invoke-Az provider show -n $provider --query registrationState --output tsv
    if ($state -ne 'Registered') {
        Write-Host "Registering provider $provider ..." -ForegroundColor DarkGray
        Invoke-Az provider register -n $provider --wait | Out-Null
    }
}

Write-Host "Resource group $($names.ResourceGroup) in $Location" -ForegroundColor Cyan
Invoke-Az group create --name $names.ResourceGroup --location $Location --tags app=spicone env=$Environment client=SPIC | Out-Null

$deployer = Invoke-AzJson ad signed-in-user show --query id
$clientIp = if ($AllowMyIp) { Get-MyPublicIp } else { '' }
if ($AllowMyIp -and -not $clientIp) { Write-Warning 'Could not determine public IP; no admin firewall rule added.' }

# Secret overrides come from files so values never appear on a command line.
$overrides = @{}
if ($IfmsConnectionStringFile) { $overrides.ifmsConnectionString = (Get-Content $IfmsConnectionStringFile -Raw).Trim() }
if ($IfmsDeviceKeyFile)        { $overrides.ifmsDeviceKey        = (Get-Content $IfmsDeviceKeyFile -Raw).Trim() }
if ($IfmsAutomationKeyFile)    { $overrides.ifmsAutomationKey    = (Get-Content $IfmsAutomationKeyFile -Raw).Trim() }

if ($DatabaseConnectionStringFile) { $overrides.databaseConnectionString = (Get-Content $DatabaseConnectionStringFile -Raw).Trim() }

# -SecretsFromAppSettings takes the connection strings and IFMS keys the VPS deployment runs with
# straight from SpicAPI/appsettings.json (comments stripped), so the Azure API can use the same
# database and IFMS setup without anyone typing a secret. Values are never printed.
if ($SecretsFromAppSettings) {
    $raw = Get-Content (Join-Path $script:RepoRoot 'SpicAPIppsettings.json') -Raw
    $raw = ($raw -split "`n" | Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
    $cfg = $raw | ConvertFrom-Json
    if ($cfg.ConnectionStrings.DefaultConnection) { $overrides.databaseConnectionString = $cfg.ConnectionStrings.DefaultConnection }
    if ($cfg.ConnectionStrings.IfmsConnection)    { $overrides.ifmsConnectionString     = $cfg.ConnectionStrings.IfmsConnection }
    if ($cfg.IfmsAutomation.DeviceKey)            { $overrides.ifmsDeviceKey            = $cfg.IfmsAutomation.DeviceKey }
    if ($cfg.IfmsAutomation.AutomationKey)        { $overrides.ifmsAutomationKey        = $cfg.IfmsAutomation.AutomationKey }
    Write-Host "Secrets taken from appsettings.json: $($overrides.Keys -join ', ')" -ForegroundColor DarkGray
}

$vault = Find-KeyVault -ResourceGroup $names.ResourceGroup
if ($vault) { Write-Host "Existing Key Vault $vault found; reusing its secrets." -ForegroundColor DarkGray }
if ($vault -and $ClearDatabaseOverride) {
    Write-Host 'Clearing db-connection-override: the API will use the Azure PostgreSQL server.' -ForegroundColor Yellow
    $az = Get-AzCli
    & $az keyvault secret delete --vault-name $vault --name db-connection-override --only-show-errors -o none 2>$null
    & $az keyvault secret purge  --vault-name $vault --name db-connection-override --only-show-errors -o none 2>$null
    $overrides.Remove('databaseConnectionString')
}
$secretsFile = New-SecretParametersFile -VaultName $vault -Overrides $overrides
try {
    $extra = @(
        'deployApps=false',
        "deployerObjectId=$deployer",
        "clientIp=$clientIp",
        "webApiBaseUrl=$WebApiBaseUrl"
    )
    $outputs = Invoke-PlatformDeployment -Names $names -SecretsFile $secretsFile -ExtraParameters $extra -Label "$Environment-platform"
}
finally {
    Remove-Item $secretsFile -Force -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host 'Platform ready:' -ForegroundColor Green
$outputs.GetEnumerator() | Sort-Object Name | ForEach-Object { '  {0,-22} {1}' -f $_.Name, $_.Value }

# Seed the sample templates that the API serves from Uploads/Downloads. The Azure Files
# share is mounted over /app/Uploads, so anything baked into the image is hidden.
$downloads = Join-Path $script:RepoRoot 'SpicAPI\Uploads\Downloads'
if (Test-Path $downloads) {
    Write-Host 'Seeding Uploads/Downloads into the api-uploads share ...' -ForegroundColor Cyan
    $key = Invoke-AzJson storage account keys list --resource-group $names.ResourceGroup --account-name $outputs.storageAccountName --query '[0].value'
    Invoke-Az storage file upload-batch --account-name $outputs.storageAccountName --account-key $key `
        --destination 'api-uploads' --destination-path 'Downloads' --source $downloads --no-progress | Out-Null
}

if ($PlatformOnly) {
    Write-Host 'Platform only. Run deploy.ps1 to build and deploy the apps.' -ForegroundColor Yellow
    return
}

& (Join-Path $PSScriptRoot 'deploy.ps1') -Environment $Environment -WebApiBaseUrl $WebApiBaseUrl
