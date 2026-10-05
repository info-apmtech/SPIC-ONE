# SPIC ONE — Azure deployment guide

Step-by-step procedure to release SPIC ONE (API and web) to Azure from a developer's PC.
Every step is a command you run yourself. The longer reference, including first-time
provisioning and the cut-over history, is `docs/azure-deployment.md`.

What is deployed:

| App | Public address | Azure resource |
|---|---|---|
| SpicAPI (`SpicAPI/`) | https://api.spicone.in | Container App `ca-spicone-api-prd` |
| Web (`SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/`) | https://spicone.in | Container App `ca-spicone-web-prd` |

Both run as Docker images in SPIC's Azure subscription, resource group `rg-spicone-prod`, with
2 replicas each (the API scales to 4 under load). Secrets are in Key Vault and are never typed
into a shell, pasted into chat or mail, or committed.

## 1. Access (one time)

1. **Azure.** Ask SPIC IT (spic1support@greenstar.net.in) to invite your Microsoft account as a
   guest into their directory, SOUTHERN PETROCHEMICAL (`southernpetrochemical.onmicrosoft.com`),
   and to give it the **Owner** role on the subscription **"Azure subscription 1"**:
   Subscriptions → Access control (IAM) → Add role assignment → Privileged administrator roles
   → Owner. Contributor is not enough, because the deployment template assigns roles.
2. **Code.** GitHub repository `info-apmtech/SPIC-ONE`. Releases are built from branch
   **`azure-deploy`**. Development happens on `Satham` and feature branches; merge into
   `azure-deploy` before a release. Never force-push either branch.
3. **Key Vault** (only if you need the database password): the **Key Vault Secrets User** role
   on the vault `kv-spicone-prd-p657hv`.

## 2. Prepare your PC (one time)

Install the tools:

```powershell
winget install Git.Git
winget install Docker.DockerDesktop
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.AzureCLI
az bicep install
```

Start Docker Desktop (Linux containers) and wait until it reports "running". Then clone the
repository and sign in to SPIC's directory. A browser window opens; pick the invited account.
Device-code sign-in is blocked by SPIC's security defaults.

```powershell
git clone https://github.com/info-apmtech/SPIC-ONE.git D:\GIT\SPIC-ONE
az login --tenant southernpetrochemical.onmicrosoft.com
az account set --subscription "Azure subscription 1"
az account show --query "{name:name, id:id}" -o table
```

The first release from a new PC rebuilds the local outputs file (`.outputs.prod.json`, ignored
by git) from the resource group by itself. Nothing else needs configuring.

## 3. Release (10–15 minutes)

Step 1 — bring the release branch up to date:

```powershell
cd D:\GIT\SPIC-ONE
git checkout azure-deploy
git pull
git merge Satham
git push origin azure-deploy
```

Resolve conflicts if any, build locally (`dotnet build SPIC-ONE.sln`), and commit before pushing.

Step 2 — if the change adds an EF Core migration, apply it first:

```powershell
.\deploy\azure\migrate.ps1 -Environment prod
```

This opens the PostgreSQL firewall for your PC, applies the migrations, and closes it again.

Step 3 — build, push and roll out:

```powershell
.\deploy\azure\deploy.ps1 -Environment prod -Quick
```

The script builds the API and web images with Docker, pushes them to the registry
`crspiconeprdp657hv`, and updates both container apps. Each app starts a new revision, waits for
it to pass `/health`, moves traffic to it, and stops the old one, so users see no downtime.
Finally it checks `https://api.spicone.in/health` and `https://spicone.in/health` and prints
the image tag, for example `a248f2f-202609181615`. **Write the tag down**; it is how you roll
back.

Variants:

| Command | When |
|---|---|
| `.\deploy\azure\deploy.ps1 -Environment prod -Quick -RemoteBuild` | Docker Desktop is unavailable; the images are built inside Azure Container Registry (a few rupees per build) |
| `.\deploy\azure\deploy.ps1 -Environment prod` | infrastructure changed (`infra/azure/*.bicep` or `prod.parameters.json`); runs the template as well |

## 4. Verify

```powershell
curl https://api.spicone.in/health
curl https://spicone.in/health
az containerapp revision list -n ca-spicone-api-prd -g rg-spicone-prod -o table
az containerapp revision list -n ca-spicone-web-prd -g rg-spicone-prod -o table
az containerapp logs show -n ca-spicone-api-prd -g rg-spicone-prod --tail 100
```

The newest revision should be Active with 100 % traffic. Then open https://spicone.in in a
private window and log in once.

## 5. Roll back (2 minutes)

Redeploy the previous image tag, no rebuild:

```powershell
az acr repository show-tags -n crspiconeprdp657hv --repository spicone-api --orderby time_desc -o table
.\deploy\azure\deploy.ps1 -Environment prod -Quick -SkipBuild -Tag <previous tag>
```

## 6. If the script cannot run: the same release by hand

```powershell
az acr login -n crspiconeprdp657hv
$tag = "$(git rev-parse --short HEAD)-$(Get-Date -Format yyyyMMddHHmm)"
docker build -f SpicAPI/Dockerfile -t crspiconeprdp657hv.azurecr.io/spicone-api:$tag .
docker build -f SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/Dockerfile -t crspiconeprdp657hv.azurecr.io/spicone-web:$tag .
docker push crspiconeprdp657hv.azurecr.io/spicone-api:$tag
docker push crspiconeprdp657hv.azurecr.io/spicone-web:$tag
az containerapp update -n ca-spicone-api-prd -g rg-spicone-prod --image crspiconeprdp657hv.azurecr.io/spicone-api:$tag
az containerapp update -n ca-spicone-web-prd -g rg-spicone-prod --image crspiconeprdp657hv.azurecr.io/spicone-web:$tag
```

In the Azure portal instead: Container Apps → `ca-spicone-api-prd` → Revisions and replicas →
Create new revision → change the image tag → Create. Then the same for `ca-spicone-web-prd`.

## 7. Database access

```powershell
.\deploy\azure\db-access.ps1 -Environment prod                  # opens the firewall for this PC, prints host, database, user
.\deploy\azure\db-access.ps1 -Environment prod -ShowPassword    # also prints the admin password (needs the Key Vault role)
.\deploy\azure\db-access.ps1 -Environment prod -Remove          # closes the firewall when done
```

pgAdmin, DBeaver or psql: host as printed, port 5432, database `spicone`, user `spicadmin`,
SSL mode `require`. For everyday queries and small corrections, use the **Data Explorer** page in
the admin panel instead (Admin or SpecialAdmin login, then the page's own password). It needs no
firewall change and keeps an audit log of every change.

## 8. Uploads, backups and logs

- **Uploaded files** (dealer documents, web uploads) are on Azure Files shares in the storage
  account `stspiconeprdp657hv`, mounted into the containers, so they survive every release.
- **Database backups**: PostgreSQL keeps 7 days of automatic backups; a nightly `pg_dump` job
  also writes copies to the storage account with 35-day retention (see `deploy/azure/backup`).
- **Logs**: `az containerapp logs show … --follow`, or in the portal under the container app →
  Log stream, or Log Analytics for history.

## 9. Names in the portal

| Thing | Name |
|---|---|
| Resource group | `rg-spicone-prod` |
| Container Apps environment | `cae-spicone-prd` (static IP 4.186.192.97) |
| Container apps | `ca-spicone-api-prd`, `ca-spicone-web-prd` |
| Registry | `crspiconeprdp657hv` |
| PostgreSQL | `pg-spicone-prd-p657hv`, database `spicone` |
| Key Vault | `kv-spicone-prd-p657hv` |
| Storage | `stspiconeprdp657hv` |

Sizing lives in `infra/azure/prod.parameters.json` (API 2–4 replicas, web 2, 1 vCPU / 2 GiB
each). To change it, edit that file and run `deploy.ps1 -Environment prod` **without** `-Quick`.

## 10. Do not

- Do not run `provision.ps1` for a normal release; it is for first-time setup and infrastructure changes.
- Do not run two releases at the same time.
- Do not put secrets in `appsettings.json`, the parameters file, commits, chat or mail.
- Do not deploy from a dirty working tree if you can avoid it; the tag gets a `-dirty` suffix and cannot be traced to a commit.
- Do not force-push `azure-deploy` or `Satham`.
