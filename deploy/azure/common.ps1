# Shared helpers for the SPIC ONE Azure deployment scripts. Dot-source this file.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$script:InfraDir = Join-Path $script:RepoRoot 'infra\azure'
$script:Az = $null

function Get-AzCli {
    if ($script:Az) { return $script:Az }
    $candidates = @(
        (Get-Command az -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source),
        'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd',
        'C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
    ) | Where-Object { $_ -and (Test-Path $_) }
    if (-not $candidates) { throw 'Azure CLI not found. Install with: winget install Microsoft.AzureCLI' }
    $script:Az = $candidates[0]
    return $script:Az
}

function Invoke-Az {
    # Runs az with the given arguments and returns stdout as text. Throws on a non-zero exit.
    # Windows PowerShell 5.1 turns every stderr line of a native command into an ErrorRecord,
    # which is fatal under ErrorActionPreference=Stop even for a plain WARNING. So stderr is
    # collected with the preference relaxed and only the exit code decides success.
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)
    $az = Get-AzCli
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $az @Arguments 2>&1
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previous
    }
    $stdout = @($out | Where-Object { $_ -is [string] })
    $stderr = @($out | Where-Object { $_ -isnot [string] } | ForEach-Object { $_.ToString() })
    if ($code -ne 0) {
        throw "az $($Arguments -join ' ') failed (exit $code):`n$(($stdout + $stderr) -join "`n")"
    }
    $stderr | Where-Object { $_ -match 'WARNING' -and $_ -notmatch 'Bicep release|preview|under development' } | ForEach-Object { Write-Warning $_ }
    return $stdout -join "`n"
}

function Invoke-AzJson {
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)
    $text = Invoke-Az @Arguments --output json
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    return $text | ConvertFrom-Json
}

function Get-EnvNames {
    param([Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment)
    return @{
        ResourceGroup  = "rg-spicone-$Environment"
        ParametersFile = Join-Path $script:InfraDir "$Environment.parameters.json"
        TemplateFile   = Join-Path $script:InfraDir 'main.bicep'
        OutputsFile    = Join-Path $PSScriptRoot ".outputs.$Environment.json"
    }
}

function Assert-AzLogin {
    $acct = Invoke-AzJson account show
    if (-not $acct) { throw 'Not signed in. Run: az login --use-device-code' }
    Write-Host "Azure: $($acct.user.name) on subscription '$($acct.name)' ($($acct.id))" -ForegroundColor DarkGray
    return $acct
}

function Find-KeyVault {
    # The vault created by main.bicep carries tag app=spicone. Returns its name or $null.
    param([Parameter(Mandatory)][string]$ResourceGroup)
    $vaults = Invoke-AzJson keyvault list --resource-group $ResourceGroup --query "[?tags.app=='spicone'].name"
    if ($vaults) { return @($vaults)[0] }
    return $null
}

function Get-KeyVaultSecretValue {
    param([Parameter(Mandatory)][string]$VaultName, [Parameter(Mandatory)][string]$Name)
    $az = Get-AzCli
    $out = & $az keyvault secret show --vault-name $VaultName --name $Name --query value --output tsv 2>$null
    if ($LASTEXITCODE -ne 0) { return $null }
    return ($out | Where-Object { $_ -is [string] }) -join ''
}

function New-SecretParametersFile {
    # Reads the secrets that main.bicep manages back out of Key Vault (if the vault exists)
    # and writes them to a temporary ARM parameters file, so every deployment passes the
    # SAME values instead of letting newGuid() rotate them. Caller must delete the file.
    param(
        [string]$VaultName,
        [hashtable]$Overrides = @{}
    )
    $map = @{
        postgresAdminPassword = 'postgres-admin-password'
        jwtKey                = 'jwt-key'
        ifmsDeviceKey         = 'ifms-device-key'
        ifmsAutomationKey     = 'ifms-automation-key'
        ifmsConnectionString  = 'ifms-connection'
    }
    $parameters = @{}
    foreach ($param in $map.Keys) {
        $value = $null
        if ($Overrides.ContainsKey($param) -and $Overrides[$param]) { $value = $Overrides[$param] }
        elseif ($VaultName) { $value = Get-KeyVaultSecretValue -VaultName $VaultName -Name $map[$param] }
        if ($value) { $parameters[$param] = @{ value = $value } }
    }
    $file = Join-Path ([IO.Path]::GetTempPath()) ("spicone-secrets-" + [guid]::NewGuid().ToString('N') + '.json')
    @{
        '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters     = $parameters
    } | ConvertTo-Json -Depth 5 | Set-Content -Path $file -Encoding utf8
    return $file
}

function Invoke-PlatformDeployment {
    # Runs main.bicep against the environment's resource group and returns the outputs object.
    param(
        [Parameter(Mandatory)][hashtable]$Names,
        [Parameter(Mandatory)][string]$SecretsFile,
        [Parameter(Mandatory)][string[]]$ExtraParameters,
        [string]$Label = 'platform'
    )
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $args = @(
        'deployment', 'group', 'create',
        '--name', "spicone-$Label-$stamp",
        '--resource-group', $Names.ResourceGroup,
        '--template-file', $Names.TemplateFile,
        '--parameters', "@$($Names.ParametersFile)",
        '--parameters', "@$SecretsFile",
        '--parameters'
    ) + $ExtraParameters + @('--query', 'properties.outputs')
    Write-Host "Deploying $Label ($($Names.ResourceGroup)) ..." -ForegroundColor Cyan
    $outputs = Invoke-AzJson @args
    $flat = @{}
    foreach ($p in $outputs.PSObject.Properties) { $flat[$p.Name] = $p.Value.value }
    $flat | ConvertTo-Json | Set-Content -Path $Names.OutputsFile -Encoding utf8
    return $flat
}

function Get-Outputs {
    param([Parameter(Mandatory)][hashtable]$Names)
    if (-not (Test-Path $Names.OutputsFile)) {
        throw "No outputs for this environment yet. Run provision.ps1 -Environment <env> first."
    }
    $obj = Get-Content $Names.OutputsFile -Raw | ConvertFrom-Json
    $flat = @{}
    foreach ($p in $obj.PSObject.Properties) { $flat[$p.Name] = $p.Value }
    return $flat
}

function Wait-Healthy {
    param([Parameter(Mandatory)][string]$Url, [int]$Attempts = 18, [int]$DelaySeconds = 10)
    for ($i = 1; $i -le $Attempts; $i++) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 15
            if ($r.StatusCode -eq 200) { Write-Host "  OK  $Url" -ForegroundColor Green; return $true }
        } catch { }
        Write-Host "  waiting for $Url ($i/$Attempts)" -ForegroundColor DarkGray
        Start-Sleep -Seconds $DelaySeconds
    }
    Write-Warning "$Url did not return 200 in time."
    return $false
}

function Get-MyPublicIp {
    try { return (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 10).Trim() } catch { return $null }
}
