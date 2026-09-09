# SPIC ONE on Azure — deployment runbook

Everything is driven from a developer PC with PowerShell, Docker Desktop and the Azure CLI.
There is no CI service; releases are built here and pushed, the same way the other APM
products deploy.

## What gets deployed

| Azure resource | Purpose |
|---|---|
| Container Apps environment `cae-spicone-<env>` | Runs the two apps, load-balances across replicas and zones, scales on request load |
| Container app `ca-spicone-api-<env>` | SpicAPI, 8080, public HTTPS, 1–2 replicas (staging) / 2–4 (prod) |
| Container app `ca-spicone-web-<env>` | Blazor Server web portal, sticky sessions, same scaling |
| PostgreSQL Flexible Server `pg-spicone-<env>-xxxxxx` | Database `spicone`, automated backups, optional zone-redundant standby |
| Storage account, Azure Files shares | `api-uploads` (Uploads), `api-webuploads` (wwwroot/uploads), `api-keys`, `web-keys` |
| Container Registry `crspicone<env>xxxxxx` | Images `spicone-api`, `spicone-web` |
| Key Vault `kv-spicone-<env>-xxxxxx` | DB password, connection string, JWT key, IFMS keys |
| Managed identity `id-spicone-<env>` | Apps pull images and read secrets; no passwords in app config |
| Log Analytics `log-spicone-<env>` | Console logs, metrics |

Not deployed: `SPIC.Ifms.Automation` (stays on the VPS), `SPICBlazorApp`, `SPIC.Worker`, the MAUI app.

Only one environment is used: **prod** (`rg-spicone-prod`). The `staging` parameter file is kept
for a future test environment but nothing is provisioned for it.

## One-time prerequisites on the PC

1. Docker Desktop running (Linux containers).
2. Azure CLI: `winget install Microsoft.AzureCLI`, then `az bicep install`.
3. Sign in as the deployment user (never the tenant admin):
   `az login --tenant spicsupportapmtechnologies.onmicrosoft.com --use-device-code`
4. Providers registered once per subscription: `Microsoft.App`, `Microsoft.DBforPostgreSQL`,
   `Microsoft.ContainerRegistry`, `Microsoft.KeyVault`, `Microsoft.Storage`,
   `Microsoft.OperationalInsights`, `Microsoft.ManagedIdentity`.

## First deployment of an environment

```powershell
# Platform + apps. -AllowMyIp opens the PostgreSQL firewall for this PC (migrations / copy).
.\deploy\azure\provision.ps1 -Environment staging -AllowMyIp
```

Then:

```powershell
# Copy the current database from the VPS (password in a file, never typed)
.\deploy\azure\copy-db.ps1 -Environment staging -SourcePasswordFile C:\secure\vps-postgres.txt

# Or, for an empty database, create the schema
.\deploy\azure\migrate.ps1 -Environment staging
```

The IFMS automation keeps writing to the VPS database. To let the API read it, put the
IFMS connection string in a file and pass it once:

```powershell
.\deploy\azure\provision.ps1 -Environment staging -PlatformOnly -IfmsConnectionStringFile C:\secure\ifms-conn.txt `
    -IfmsDeviceKeyFile C:\secure\ifms-device.txt -IfmsAutomationKeyFile C:\secure\ifms-automation.txt
.\deploy\azure\deploy.ps1 -Environment staging
```

Values land in Key Vault and are reused on every later run.

## Every release

```powershell
.\deploy\azure\deploy.ps1 -Environment staging          # build, push, roll out, health check
.\deploy\azure\deploy.ps1 -Environment prod             # same for production
.\deploy\azure\deploy.ps1 -Environment prod -Quick      # image swap only, no template run
.\deploy\azure\deploy.ps1 -Environment staging -RemoteBuild   # build inside the registry, no local Docker
```

`-RemoteBuild` uploads the source tree to Azure Container Registry and builds there (ACR Tasks,
a few rupees per build). Use it when Docker Desktop is not available or keeps hanging.

Images are tagged `<git-sha>-<timestamp>`; `-dirty` is appended when the working tree has
uncommitted changes. To roll back, redeploy an earlier tag:
`.\deploy\azure\deploy.ps1 -Environment prod -Quick -SkipBuild -Tag <old tag>`.

Migrations are not applied automatically. Run `migrate.ps1` before deploying a build that
adds one.

## How the apps are configured

All configuration is environment variables set by `infra/azure/main.bicep`; nothing is read
from `appsettings.json` in Azure except defaults.

| Variable | API | Web |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Key Vault `db-connection` | |
| `ConnectionStrings__IfmsConnection` | Key Vault `ifms-connection` (if set) | |
| `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` | Key Vault `jwt-key` + parameters | |
| `IfmsAutomation__DeviceKey` / `__AutomationKey` | Key Vault (if set) | |
| `DataProtection__KeysPath` | `/app/keys` (Azure Files) | `/app/keys` (Azure Files) |
| `ApiBaseUrl` | | API app FQDN, or `-WebApiBaseUrl` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | true (TLS terminates at the ingress) | true |

Health endpoint: `/health` on both apps. Container Apps uses it for startup, readiness and
liveness probes; a replica that fails it is replaced and receives no traffic.

## Sizes and cost

| | staging | prod |
|---|---|---|
| API / Web replicas | 1–2 each, 0.5 vCPU 1 GiB | 2–4 each, 1 vCPU 2 GiB |
| PostgreSQL | B1ms, 32 GB, 7-day backup | D2ds_v4, 128 GB, 14-day backup |
| Zone redundancy | off | Container Apps on; PostgreSQL standby optional (`postgresHaMode`) |

Edit `infra/azure/<env>.parameters.json` to change sizes; the next `deploy.ps1` applies them.

## Custom domains

Bind `api.<domain>` and `app.<domain>` to the container apps from the portal (Container App →
Custom domains → Add, managed certificate). Then redeploy with
`-WebApiBaseUrl https://api.<domain>/` so browser links to files use the public name.

## Useful commands

```powershell
az containerapp logs show -n ca-spicone-api-stg -g rg-spicone-staging --follow
az containerapp revision list -n ca-spicone-web-stg -g rg-spicone-staging -o table
az postgres flexible-server show -g rg-spicone-staging -n <server>
```

## Known follow-ups

- The repository's `SpicAPI/appsettings.json` still contains a database password, JWT key and
  IFMS keys. Azure ignores them, but rotate all three once Azure is live and remove them from
  the file.
- `SpicAPI/Uploads/DealerRegistration` in git contains real dealer documents. They are not
  copied into the image, but they should be removed from the repository history.
- PostgreSQL is reachable from Azure services and the admin PC only. Moving it to a private
  endpoint inside the VNet is the next hardening step for production.
- Tesseract OCR on Linux uses the distro libraries linked in the Dockerfile. Verify a dealer PDF
  upload on staging before relying on it.
