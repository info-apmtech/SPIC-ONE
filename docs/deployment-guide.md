# SPIC ONE — deployment guide for developers

How to release SPIC ONE (API + web) to Azure and the iFMS automation to cam.server, by hand,
from any developer's PC. Every step is a command you run yourself; nothing here depends on a
particular person or tool. The longer reference is `docs/azure-deployment.md`.

Two independent systems:

| System | Runs on | What you deploy | How |
|---|---|---|---|
| SPIC ONE API + web (`SpicAPI`, `SPIC.MauiBlazorApp.Web`) | Azure Container Apps in SPIC's subscription, https://spicone.in and https://api.spicone.in | two container images | `deploy\azure\deploy.ps1` from your PC |
| iFMS automation (`SPIC.Ifms.Automation`) | cam.server 103.14.121.144, Linux service `spic-ifms` | a .NET publish folder | `publish.sh` on the server over SSH |

Secrets are never typed into a shell or pasted into chat or mail. Passwords live in Key Vault
(Azure) or in `secrets.env` on the server, and scripts read them from files.

---

## Part A — SPIC ONE on Azure

### A1. Access you need (one time)

1. **Azure.** Ask SPIC IT (spic1support@greenstar.net.in) to invite your Microsoft account as a
   guest into their directory (SOUTHERN PETROCHEMICAL, `southernpetrochemical.onmicrosoft.com`)
   and to give it the **Owner** role on the subscription **"Azure subscription 1"**
   (Subscriptions → Access control (IAM) → Add role assignment → Privileged administrator roles
   → Owner). Contributor is not enough because the template assigns roles.
2. **Code.** GitHub `info-apmtech/SPIC-ONE`. Releases are built from branch **`azure-deploy`**.
   Development happens on `Satham` (and feature branches); merge into `azure-deploy` before a
   release. Never force-push either branch.
3. **Key Vault** (only if you need the database password): the **Key Vault Secrets User** role on
   the vault `kv-spicone-prd-…` in resource group `rg-spicone-prod`.

### A2. Your PC (one time)

```powershell
winget install Git.Git
winget install Docker.DockerDesktop          # Linux containers; start it and wait until it says "running"
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.AzureCLI
az bicep install
git clone https://github.com/info-apmtech/SPIC-ONE.git D:\GIT\SPIC-ONE
```

Sign in to SPIC's directory (a browser window opens; pick the invited account; device-code
sign-in is blocked by their security defaults):

```powershell
az login --tenant southernpetrochemical.onmicrosoft.com
az account set --subscription "Azure subscription 1"
az account show --query "{name:name, id:id}" -o table
```

The first release from a new PC rebuilds the local outputs file (`.outputs.prod.json`, ignored by
git) from the resource group by itself. Nothing else to configure.

### A3. A normal release (10–15 minutes)

```powershell
cd D:\GIT\SPIC-ONE
git checkout azure-deploy
git pull
git merge Satham            # or the branch that carries the change; resolve conflicts, build, commit
git push origin azure-deploy

.\deploy\azure\deploy.ps1 -Environment prod -Quick
```

What the script does, in order: `dotnet`-free Docker builds of the API and web images (and the
backup job image), pushes them to the registry `crspiconeprdp657hv`, updates both container apps
with the new image (a new revision starts, passes `/health`, then takes traffic; the old revision
is stopped, so users see no downtime), and finally checks `https://api.spicone.in/health` and
`https://spicone.in/health`. It prints the image tag at the end, for example
`a248f2f-202609181615`. **Note the tag** — it is how you roll back.

Variants:

```powershell
.\deploy\azure\deploy.ps1 -Environment prod -Quick -RemoteBuild        # no Docker Desktop: build inside Azure (ACR Tasks, a few rupees)
.\deploy\azure\deploy.ps1 -Environment prod                            # full run including the infrastructure template; only when infra/azure/*.bicep or prod.parameters.json changed
```

If the change adds an EF Core migration, run it **before** deploying the build that needs it:

```powershell
.\deploy\azure\migrate.ps1 -Environment prod          # opens the DB firewall for your PC, applies migrations, closes it
```

### A4. Verify

```powershell
curl https://api.spicone.in/health
curl https://spicone.in/health
az containerapp revision list -n ca-spicone-api-prd -g rg-spicone-prod -o table   # the new revision should be Active with traffic 100
az containerapp revision list -n ca-spicone-web-prd -g rg-spicone-prod -o table
az containerapp logs show -n ca-spicone-api-prd -g rg-spicone-prod --tail 100      # recent API log lines
```

Then open https://spicone.in in a private window and log in once.

### A5. Roll back (2 minutes)

Redeploy the previous tag; no rebuild:

```powershell
az acr repository show-tags -n crspiconeprdp657hv --repository spicone-api --orderby time_desc -o table   # list tags, newest first
.\deploy\azure\deploy.ps1 -Environment prod -Quick -SkipBuild -Tag <previous tag>
```

### A6. If the script cannot run: the same release by hand

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

Or in the portal: Container Apps → `ca-spicone-api-prd` → Revisions and replicas → Create new
revision → change the image tag → Create. Same for `ca-spicone-web-prd`.

### A7. Database access

```powershell
.\deploy\azure\db-access.ps1 -Environment prod                  # opens the PostgreSQL firewall for this PC, prints host/db/user
.\deploy\azure\db-access.ps1 -Environment prod -ShowPassword    # also prints the admin password (needs the Key Vault role)
.\deploy\azure\db-access.ps1 -Environment prod -Remove          # closes the firewall again when done
```

pgAdmin / DBeaver / psql: host as printed, port 5432, database `spicone`, user `spicadmin`,
SSL mode `require`. For day-to-day queries and small fixes, prefer the **Data Explorer** page in
the admin panel (Admin / SpecialAdmin login, then the page's own password), which needs no
firewall change and keeps an audit log.

### A8. Names you will see in the portal

| Thing | Name |
|---|---|
| Resource group | `rg-spicone-prod` |
| Container Apps environment | `cae-spicone-prd` (static IP 4.186.192.97) |
| Container apps | `ca-spicone-api-prd`, `ca-spicone-web-prd` |
| Registry | `crspiconeprdp657hv` |
| PostgreSQL | `pg-spicone-prd-p657hv`, database `spicone` |
| Key Vault | `kv-spicone-prd-p657hv` |
| Storage (uploads, keys, backups) | `stspiconeprdp657hv` |

Sizing is in `infra/azure/prod.parameters.json` (API 2–4 replicas, web 2, 1 vCPU / 2 GiB each).
Changing it means editing that file and running `deploy.ps1 -Environment prod` **without** `-Quick`.

### A9. Do not

- Do not run `provision.ps1` for a normal release; it is for first-time setup and infrastructure changes.
- Do not run two releases at the same time.
- Do not put secrets in `appsettings.json`, the parameters file, commits, chat or mail.
- Do not deploy from a dirty working tree if you can avoid it (the tag gets a `-dirty` suffix and cannot be traced to a commit).

---

## Part B — iFMS automation on cam.server

### B1. Access

- SSH to `103.14.121.144`, port **2244**, user **`spicops`**, key-based only. Ask Satham for the
  private key (`spic_ifms_deploy`) or for your own public key to be added to the server. Put it
  at `~/.ssh/spic_ifms_deploy` and test:

```bash
ssh -i ~/.ssh/spic_ifms_deploy -p 2244 spicops@103.14.121.144 "hostname; systemctl is-active spic-ifms"
```

- `spicops` may `sudo` only for the `spic-ifms` unit; it cannot write to `/opt` other than
  `/opt/spic-ifms` and `/opt/spic-src`.

### B2. Layout on the server

| Path | What |
|---|---|
| `/opt/spic-src` | git checkout of the repository (branch `Satham`) used for builds |
| `/opt/spic-ifms` | the running service: binaries, `appsettings.json`, `secrets.env`, `downloads/<date>/`, `diagnostics/<date>/`, `browsers/` (Chromium), `tessdata/` |
| `/opt/spic-ifms/secrets.env` | connection string, API keys, `Upload__Enabled`, `Upload__ApiBaseUrl` (https://api.spicone.in) — edit with care, never commit |
| `/home/spicops/spic-ifms-backfill` | a second, independent build used only for the one-time historical download (`backfill` command); its `downloads/backfill/` holds the history, `progress.jsonl` the resumable state |

The service signs in to the iFMS portal at **04:05 IST** every day, downloads the configured
reports for Greenstar (and SPIC once that configuration is deployed) and stores them under
`downloads/<date>/`. Each sign-in needs a CAPTCHA (read by OCR) and an OTP SMS forwarded by the
**SPIC IFMS Relay** app on the registered phone; if the phone or the portal's SMS is down, the
run fails and retries twice (04:25, 04:45).

### B3. Deploy a new version of the automation (3 minutes, restarts the service)

Do this outside 04:00–05:30 IST.

```bash
ssh -i ~/.ssh/spic_ifms_deploy -p 2244 spicops@103.14.121.144
cd /opt/spic-src && git pull --ff-only && git log --oneline -1
bash SPIC.Ifms.Automation/deploy/publish.sh /opt/spic-src
```

`publish.sh` builds with `dotnet publish`, stops the service, copies the build into
`/opt/spic-ifms` (downloads, diagnostics and `secrets.env` are left alone), restores the
Tesseract data, restarts the service and prints its status. Watch it start:

```bash
sudo journalctl -u spic-ifms -n 40 --no-pager       # if your user is not allowed to read the journal, use the next line
cd /opt/spic-ifms && set -a && . ./secrets.env && set +a && dotnet SPIC.Ifms.Automation.dll list-credentials
```

The `list-credentials` output proves the build runs and reaches the database. Passwords are
never shown.

### B4. Test one report without waiting for 04:05

```bash
cd /opt/spic-ifms && set -a && . ./secrets.env && set +a
dotnet SPIC.Ifms.Automation.dll test-job company-sales-greenstar          # signs in (one OTP on the phone), runs that job, file under downloads/<today>/
dotnet SPIC.Ifms.Automation.dll run-now                                   # or queue a full run for the service to pick up within 20 s
```

Job keys are in `SPIC.Ifms.Automation/appsettings.json` under `ReportJobs:Jobs`
(`retail-stocks-greenstar`, `company-sales-greenstar`, `wholesale-sales-greenstar`,
`sales-and-receipt-greenstar`, `wholesale-stock-today-greenstar`, `state-global-stock-greenstar`,
`warehouse-global-stock-greenstar`, and the same with `-spic`).

### B5. Portal logins (user ID / password) and the phone

```bash
dotnet SPIC.Ifms.Automation.dll list-credentials                          # who is stored, when the password expires
dotnet SPIC.Ifms.Automation.dll set-credentials greenstar 1000249825 Greenstar   # prompts for the password twice; nothing in history
```

Account keys: `greenstar` (nightly) and `greenstar-apm`, `greenstar-satham`, `greenstar-spicone`,
`greenstar-support` (backfill); `spic` (nightly) and `spic-apm`, `spic-satham`, `spic-support`
(backfill). All OTPs go to the mobile registered on the portal for these user IDs; the phone
runs the relay app and must stay paired (open the app, tap **Pair**).

The portal allows **one session per user ID**; a second sign-in ends the first. Never run two
things under the same account key at the same time.

### B6. The historical backfill (one-time)

Runs from `/home/spicops/spic-ifms-backfill`, one process per account key, and never uploads.

```bash
cd ~/spic-ifms-backfill && set -a && . /opt/spic-ifms/secrets.env && set +a
dotnet SPIC.Ifms.Automation.dll backfill retail-stocks-greenstar from=2024-04-01 dry=true      # what it would do
bash start-nine.sh                                                                             # (re)start all nine runs; each resumes where it stopped
cut -d, -f2,5 downloads/backfill/progress.jsonl | sort | uniq -c                               # progress
pgrep -af "dll backfill" | cut -c1-90                                                          # what is running
pkill -f "^dotnet SPIC.Ifms.Automation.dll backfil[l]"                                         # stop all (the [l] keeps pkill from killing your own shell)
```

To rebuild that folder after a code change (does not touch the service):

```bash
cd /opt/spic-src && git pull --ff-only
dotnet publish SPIC.Ifms.Automation/SPIC.Ifms.Automation.csproj -c Release -o ~/spic-ifms-backfill
cp -a /opt/spic-ifms/tessdata/. ~/spic-ifms-backfill/tessdata/
```

Sign-ins of the backfill runs are serialised by `login.lock`; they pause 04:00–06:09 for the
nightly job. Both the pause window and the chunking (`step=`) are command-line options; see the
header of `SPIC.Ifms.Automation/Reports/BackfillCommand.cs`.

### B7. When the morning run failed

1. `ls /opt/spic-ifms/downloads/$(date +%F)` — empty means nothing was downloaded.
2. `ls /opt/spic-ifms/diagnostics/$(date +%F)` — the file names say where it stopped
   (`login-failed`, `otp-timeout`, `step-waitFor …`); open the `.png` for a screenshot.
3. `otp-timeout` = the phone did not forward the OTP (relay app closed, phone offline) or the
   portal did not send it. Fix the phone, then `run-now`.
4. Portal down (`ERR_CONNECTION_REFUSED`): wait; the portal has maintenance windows around
   01:00 IST.
