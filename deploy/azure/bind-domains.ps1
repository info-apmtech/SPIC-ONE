<#
.SYNOPSIS
  Shows the DNS records the environment needs, and binds the public hostnames with
  Azure-managed certificates once DNS is in place.

.DESCRIPTION
  Without -Bind: prints the environment's static IP, the domain verification id and the exact
  DNS records to create. Send that to whoever manages the DNS zone.
  With -Bind: checks DNS from a public resolver, then binds each hostname to its container app
  and waits for the managed certificate. Apex domains are validated over HTTP, sub-domains over
  CNAME. Re-runnable; already-bound hostnames are skipped.

.EXAMPLE
  .\deploy\azure\bind-domains.ps1 -Environment prod
  .\deploy\azure\bind-domains.ps1 -Environment prod -Bind
  .\deploy\azure\bind-domains.ps1 -Environment prod -WebHost spicone.in -ApiHost api.spicone.in -Bind
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment,
    [string]$WebHost = 'spicone.in',
    [string]$ApiHost = 'api.spicone.in',
    [switch]$Bind
)

. (Join-Path $PSScriptRoot 'common.ps1')

$names = Get-EnvNames -Environment $Environment
$null = Assert-AzLogin
$outputs = Get-Outputs -Names $names
$rg = $names.ResourceGroup

$staticIp = Invoke-Az containerapp env show -n $outputs.environmentName -g $rg --query properties.staticIp --output tsv
$verifyId = Invoke-Az containerapp show -n $outputs.webAppName -g $rg --query properties.customDomainVerificationId --output tsv
$apiFqdn = Invoke-Az containerapp show -n $outputs.apiAppName -g $rg --query properties.configuration.ingress.fqdn --output tsv

function Get-Label([string]$hostName, [string]$zone) {
    if ($hostName -eq $zone) { return '@' }
    return $hostName.Substring(0, $hostName.Length - $zone.Length - 1)
}
$zone = ($WebHost -split '\.')[-2..-1] -join '.'
$webLabel = Get-Label $WebHost $zone
$apiLabel = Get-Label $ApiHost $zone
$webIsApex = ($webLabel -eq '@')

Write-Host ''
Write-Host "DNS records for zone $zone" -ForegroundColor Cyan
Write-Host ('  {0,-6} {1,-14} {2}' -f 'Type', 'Name', 'Value')
if ($webIsApex) {
    Write-Host ('  {0,-6} {1,-14} {2}' -f 'A', '@', $staticIp)
    Write-Host ('  {0,-6} {1,-14} {2}' -f 'TXT', 'asuid', $verifyId)
} else {
    Write-Host ('  {0,-6} {1,-14} {2}' -f 'CNAME', $webLabel, $outputs.webFqdn)
    Write-Host ('  {0,-6} {1,-14} {2}' -f 'TXT', "asuid.$webLabel", $verifyId)
}
Write-Host ('  {0,-6} {1,-14} {2}' -f 'CNAME', $apiLabel, $apiFqdn)
Write-Host ('  {0,-6} {1,-14} {2}' -f 'TXT', "asuid.$apiLabel", $verifyId)
Write-Host ''

if (-not $Bind) {
    Write-Host 'Create the records above, then run again with -Bind.' -ForegroundColor Yellow
    return
}

function Test-Dns([string]$name, [string]$type, [string]$expected) {
    try {
        $r = Resolve-DnsName -Name $name -Type $type -Server 8.8.8.8 -ErrorAction Stop | Where-Object { $_.Type -eq $type }
        $values = foreach ($x in $r) { if ($type -eq 'A') { $x.IPAddress } elseif ($type -eq 'CNAME') { $x.NameHost.TrimEnd('.') } else { ($x.Strings -join '').Trim() } }
        return ($values -contains $expected)
    } catch { return $false }
}

$checks = @(
    @{ Name = "asuid.$ApiHost"; Type = 'TXT'; Expected = $verifyId }
    @{ Name = $ApiHost; Type = 'CNAME'; Expected = $apiFqdn }
)
if ($webIsApex) {
    $checks += @{ Name = $WebHost; Type = 'A'; Expected = $staticIp }
    $checks += @{ Name = "asuid.$WebHost"; Type = 'TXT'; Expected = $verifyId }
} else {
    $checks += @{ Name = $WebHost; Type = 'CNAME'; Expected = $outputs.webFqdn }
    $checks += @{ Name = "asuid.$WebHost"; Type = 'TXT'; Expected = $verifyId }
}
$missing = @()
foreach ($c in $checks) {
    $ok = Test-Dns $c.Name $c.Type $c.Expected
    Write-Host ('  {0} {1,-6} {2}' -f ($(if ($ok) { 'OK ' } else { '-- ' })), $c.Type, $c.Name) -ForegroundColor $(if ($ok) { 'Green' } else { 'Yellow' })
    if (-not $ok) { $missing += $c.Name }
}
if ($missing) { throw "DNS not ready yet for: $($missing -join ', '). Wait for propagation and run again." }

function Bind-Host([string]$app, [string]$hostName, [string]$method) {
    $bound = Invoke-AzJson containerapp hostname list -n $app -g $rg --query "[?name=='$hostName'].bindingType | [0]"
    if ($bound -eq 'SniEnabled') { Write-Host "  $hostName already bound to $app" -ForegroundColor DarkGray; return }
    Write-Host "  binding $hostName to $app ($method validation) ..." -ForegroundColor Cyan
    $az = Get-AzCli
    # Native stderr (progress spinner) must not become a terminating error under PS 5.1.
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $az containerapp hostname add -n $app -g $rg --hostname $hostName --only-show-errors -o none 2>&1 | Out-Null
        & $az containerapp hostname bind -n $app -g $rg --hostname $hostName --environment $outputs.environmentName `
            --validation-method $method --only-show-errors -o none 2>&1 | Where-Object { "$_" -match 'ERROR' } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        $code = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previous }
    if ($code -ne 0) { throw "Binding $hostName failed. Check: az containerapp env certificate list -g $rg -n $($outputs.environmentName)" }
}

Bind-Host $outputs.apiAppName $ApiHost 'CNAME'
Bind-Host $outputs.webAppName $WebHost $(if ($webIsApex) { 'HTTP' } else { 'CNAME' })

Write-Host ''
foreach ($u in "https://$ApiHost/health", "https://$WebHost/health") { Wait-Healthy -Url $u -Attempts 12 -DelaySeconds 10 | Out-Null }
Write-Host "Web : https://$WebHost/" -ForegroundColor Green
Write-Host "API : https://$ApiHost/" -ForegroundColor Green
