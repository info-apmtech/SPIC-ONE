<#
.SYNOPSIS
  Builds the API and Web images on this PC, pushes them to the environment's registry and
  rolls them out to Azure Container Apps, one app at a time, with health checks.

.DESCRIPTION
  No CI service is involved: this is the SPIC ONE equivalent of the push-to-deploy kit used
  for the other APM products. Images are tagged with the git commit and a timestamp.

  Default mode re-runs infra/azure/main.bicep with deployApps=true so the app definitions
  (replicas, secrets, mounts) stay in sync with the repository.
  -Quick only swaps the image on the existing apps (faster, no template run).
  -RemoteBuild builds the images inside Azure Container Registry (ACR Tasks) instead of
  local Docker: the source tree is uploaded and built there. Use it when Docker Desktop is
  unavailable or unreliable; it costs a few rupees per build and needs no Docker on the PC.

.EXAMPLE
  .\deploy\azure\deploy.ps1 -Environment staging
  .\deploy\azure\deploy.ps1 -Environment prod -Quick
  .\deploy\azure\deploy.ps1 -Environment staging -SkipBuild -Tag 3f2a1c9-202609091530
  .\deploy\azure\deploy.ps1 -Environment staging -RemoteBuild
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('staging', 'prod')][string]$Environment,
    [switch]$Quick,
    [switch]$SkipBuild,
    [switch]$RemoteBuild,
    [string]$Tag,
    [string]$WebApiBaseUrl = ''
)

. (Join-Path $PSScriptRoot 'common.ps1')

$names = Get-EnvNames -Environment $Environment
$null = Assert-AzLogin
$outputs = Get-Outputs -Names $names
$acr = $outputs.acrLoginServer

if (-not $Tag) {
    Push-Location $script:RepoRoot
    try {
        $sha = (git rev-parse --short HEAD).Trim()
        $dirty = if ((git status --porcelain) -ne $null) { '-dirty' } else { '' }
    } finally { Pop-Location }
    $Tag = "$sha$dirty-" + (Get-Date -Format 'yyyyMMddHHmm')
}
$apiImage = "$acr/spicone-api:$Tag"
$webImage = "$acr/spicone-web:$Tag"

if (-not $SkipBuild -and $RemoteBuild) {
    # ACR Tasks: the repository (minus .dockerignore exclusions) is uploaded and built in Azure.
    $az = Get-AzCli
    Push-Location $script:RepoRoot
    try {
        foreach ($b in @(
            @{ Name = 'spicone-api'; File = 'SpicAPI/Dockerfile' },
            @{ Name = 'spicone-web'; File = 'SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/Dockerfile' })) {
            Write-Host "Building $($b.Name):$Tag in registry $($outputs.acrName) ..." -ForegroundColor Cyan
            & $az acr build --registry $outputs.acrName --image "$($b.Name):$Tag" --image "$($b.Name):latest" `
                --file $b.File --platform linux/amd64 .
            if ($LASTEXITCODE -ne 0) { throw "Remote build failed: $($b.Name)" }
        }
    } finally { Pop-Location }
}
elseif (-not $SkipBuild) {
    Write-Host "Building images with tag $Tag ..." -ForegroundColor Cyan
    Push-Location $script:RepoRoot
    try {
        docker build -f SpicAPI/Dockerfile -t $apiImage -t "$acr/spicone-api:latest" .
        if ($LASTEXITCODE -ne 0) { throw 'API image build failed.' }
        docker build -f SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/Dockerfile -t $webImage -t "$acr/spicone-web:latest" .
        if ($LASTEXITCODE -ne 0) { throw 'Web image build failed.' }
    } finally { Pop-Location }

    Write-Host "Pushing to $acr ..." -ForegroundColor Cyan
    Invoke-Az acr login --name $outputs.acrName | Out-Null
    foreach ($img in @($apiImage, "$acr/spicone-api:latest", $webImage, "$acr/spicone-web:latest")) {
        docker push $img
        if ($LASTEXITCODE -ne 0) { throw "Push failed: $img" }
    }
}

if ($Quick) {
    # Image swap only. Apps must already exist (first deploy must use the default mode).
    foreach ($app in @(@{ Name = $outputs.apiAppName; Image = $apiImage }, @{ Name = $outputs.webAppName; Image = $webImage })) {
        Write-Host "Updating $($app.Name) -> $($app.Image)" -ForegroundColor Cyan
        Invoke-Az containerapp update --name $app.Name --resource-group $names.ResourceGroup --image $app.Image | Out-Null
        $fqdn = Invoke-Az containerapp show --name $app.Name --resource-group $names.ResourceGroup --query properties.configuration.ingress.fqdn --output tsv
        if (-not (Wait-Healthy -Url "https://$fqdn/health")) { throw "$($app.Name) is not healthy; stopping before the next app." }
    }
    Write-Host 'Done.' -ForegroundColor Green
    return
}

$vault = Find-KeyVault -ResourceGroup $names.ResourceGroup
if (-not $vault) { throw 'Key Vault not found; run provision.ps1 first.' }
$deployer = Invoke-AzJson ad signed-in-user show --query id
$secretsFile = New-SecretParametersFile -VaultName $vault
try {
    $extra = @(
        'deployApps=true',
        "apiImage=$apiImage",
        "webImage=$webImage",
        "deployerObjectId=$deployer",
        "webApiBaseUrl=$WebApiBaseUrl"
    )
    $outputs = Invoke-PlatformDeployment -Names $names -SecretsFile $secretsFile -ExtraParameters $extra -Label "$Environment-apps"
}
finally {
    Remove-Item $secretsFile -Force -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host 'Health checks:' -ForegroundColor Cyan
$apiOk = Wait-Healthy -Url "https://$($outputs.apiFqdn)/health"
$webOk = Wait-Healthy -Url "https://$($outputs.webFqdn)/health"

Write-Host ''
Write-Host "API : https://$($outputs.apiFqdn)/" -ForegroundColor Green
Write-Host "Web : https://$($outputs.webFqdn)/" -ForegroundColor Green
Write-Host "Tag : $Tag"
if (-not ($apiOk -and $webOk)) { throw 'Deployment finished but a health check failed. See: az containerapp logs show' }
