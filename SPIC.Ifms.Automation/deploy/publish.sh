#!/usr/bin/env bash
#
# Build the automation from the checked-out repository and install it into
# /opt/spic-ifms, then restart the service.
#
#   ./publish.sh [path-to-repo]
#
# Re-run this for every update. It stops the service, replaces the binaries,
# and starts it again - downloads, diagnostics and secrets are left alone.
#
set -euo pipefail

# This script lives at <repo>/SPIC.Ifms.Automation/deploy/, so the repository is
# two levels up - not three. Ask git first, since that is exact whatever the
# script is invoked from.
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO="${1:-$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel 2>/dev/null || cd "$SCRIPT_DIR/../.." && pwd)}"
PROJECT="$REPO/SPIC.Ifms.Automation/SPIC.Ifms.Automation.csproj"
APP_DIR=/opt/spic-ifms
SERVICE=spic-ifms

say() { printf '\n\033[1;34m==>\033[0m %s\n' "$1"; }

if [ ! -f "$PROJECT" ]; then
  echo "Cannot find $PROJECT" >&2
  echo "Pass the repository root explicitly:  sudo ./publish.sh /opt/spic-src" >&2
  exit 1
fi

say "Publishing from $REPO"
dotnet publish "$PROJECT" -c Release -o /tmp/spic-ifms-publish --nologo

say "Stopping $SERVICE"
systemctl stop $SERVICE 2>/dev/null || true

say "Installing to $APP_DIR"
# Deliberately not --delete: downloads/, diagnostics/ and secrets.env live here
# and must survive an update.
rsync -a /tmp/spic-ifms-publish/ "$APP_DIR/"
rm -rf /tmp/spic-ifms-publish

say "Installing Chromium for Playwright"
# Idempotent, and a no-op once the browser is already present.
PW="$APP_DIR/playwright.sh"
if [ -f "$PW" ]; then
  chmod +x "$PW"
  "$PW" install --with-deps chromium
else
  echo "  playwright.sh not found in the publish output; installing via dotnet"
  ( cd "$APP_DIR" && dotnet SPIC.Ifms.Automation.dll --help >/dev/null 2>&1 || true )
  echo "  run: pwsh $APP_DIR/playwright.ps1 install --with-deps chromium"
fi

say "Checking the OCR language data"
if [ ! -f "$APP_DIR/tessdata/eng.traineddata" ]; then
  echo "  missing; downloading"
  mkdir -p "$APP_DIR/tessdata"
  curl -fsSL -o "$APP_DIR/tessdata/eng.traineddata" \
    https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata
fi

say "Starting $SERVICE"
systemctl start $SERVICE
sleep 2
systemctl --no-pager --lines=15 status $SERVICE || true

say "Done. Follow the log with: journalctl -u $SERVICE -f"
