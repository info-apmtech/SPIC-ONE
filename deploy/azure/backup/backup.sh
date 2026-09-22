#!/bin/sh
# pg_dump of the SPIC ONE database into the backup storage account.
# Custom format with the same flags as copy-db.ps1, so a copy restores with
#   copy-db.ps1 -Environment <env> -DumpFile <downloaded file>
# Blob name: <container>/<yyyy>/<mm>/<database>-<utc stamp>.dump
set -eu
for v in PGHOST PGUSER PGPASSWORD PGDATABASE STORAGE_ACCOUNT CONTAINER AZURE_CLIENT_ID; do
  eval "test -n \"\${$v:-}\"" || { echo "$v is not set" >&2; exit 2; }
done
: "${PGSSLMODE:=require}"; export PGSSLMODE   # Azure requires SSL; a local test may override
stamp=$(date -u +%Y%m%dT%H%M%SZ)
name="${PGDATABASE}-${stamp}.dump"
file="/tmp/${name}"
blob="$(date -u +%Y/%m)/${name}"

echo "Dumping ${PGDATABASE} from ${PGHOST} ..."
pg_dump --format=custom --compress=6 --no-owner --no-privileges --file "$file"
pg_restore --list "$file" > /dev/null      # the archive opens and has a table of contents
echo "Dump ${name}: $(stat -c %s "$file") bytes"

echo "Uploading to ${STORAGE_ACCOUNT}/${CONTAINER}/${blob} ..."
az login --identity --client-id "$AZURE_CLIENT_ID" --output none
az storage blob upload --auth-mode login --account-name "$STORAGE_ACCOUNT" --container-name "$CONTAINER" \
  --name "$blob" --file "$file" --tier Cool --output none
rm -f "$file"
echo "Backup done: ${CONTAINER}/${blob}"
