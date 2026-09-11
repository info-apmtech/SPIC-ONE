#!/usr/bin/env bash

# publish.sh is run as spicops (sudo limited to the spic-ifms unit) or as
# root; only the former needs the prefix. -n so a missing rule fails loudly
# instead of prompting inside a pipe.
if [ "$(id -u)" -eq 0 ]; then SUDO=""; else SUDO="sudo -n"; fi
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
$SUDO systemctl stop $SERVICE 2>/dev/null || true

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
  # Never run the app here: the host does not stop at --help, it starts the
  # worker as an orphan outside systemd (seen 2026-09-03). The browsers live in
  # PLAYWRIGHT_BROWSERS_PATH and survive a redeploy; if they are missing, say so.
  BROWSERS="${PLAYWRIGHT_BROWSERS_PATH:-$APP_DIR/browsers}"
  if ls -d "$BROWSERS"/chromium* >/dev/null 2>&1; then
    echo "  Chromium already present in $BROWSERS"
  else
    echo "  Chromium MISSING in $BROWSERS - run: pwsh $APP_DIR/playwright.ps1 install --with-deps chromium"
  fi
fi

say "Tesseract native libraries"
# The Tesseract NuGet package ships Windows DLLs only. On Linux the wrapper
# dlopens three names that Ubuntu does not provide under those names:
#
#   libleptonica-1.82.0.so  -> liblept.so.5      (package liblept5)
#   libtesseract50.so       -> libtesseract.so.5 (package libtesseract5)
#   libdl.so                -> libdl.so.2        (glibc >= 2.34 dropped the bare name)
#
# Symlinks beside the app bridge the gap without touching /usr/lib, and this
# block re-creates them on every deploy so a fresh host does not silently lose
# OCR. Install the packages first:
#   sudo apt-get install -y tesseract-ocr libtesseract-dev libleptonica-dev
LIBDIR=/usr/lib/x86_64-linux-gnu
mkdir -p "$APP_DIR/x64"
for pair in   "libleptonica-1.82.0.so:liblept.so.5"   "libtesseract50.so:libtesseract.so.5"   "libdl.so:libdl.so.2"
do
  want=${pair%%:*}; have=${pair##*:}
  real=$(readlink -f "$LIBDIR/$have" 2>/dev/null || true)
  if [ -n "$real" ]; then
    ln -sfn "$real" "$APP_DIR/x64/$want"
    [ "$want" = "libdl.so" ] && ln -sfn "$real" "$APP_DIR/$want"
    echo "  $want -> $real"
  else
    echo "  WARNING: $have not found; OCR will fail. Install libtesseract-dev libleptonica-dev."
  fi
done

if ! grep -q '^LD_LIBRARY_PATH=' "$APP_DIR/secrets.env" 2>/dev/null; then
  printf '
LD_LIBRARY_PATH=%s/x64:%s
' "$APP_DIR" "$APP_DIR" >> "$APP_DIR/secrets.env"
  echo "  LD_LIBRARY_PATH added to secrets.env"
fi

say "Checking the OCR language data"
# The repo ships the 4 MB "fast" eng.traineddata; the 15 MB tessdata_best
# model reads these CAPTCHAs noticeably better. rsync just put the fast one
# back, so restore the best one if it was downloaded before, else fetch it.
TD="$APP_DIR/tessdata"
if [ -s "$TD/eng.best.traineddata" ] && [ "$(stat -c %s "$TD/eng.best.traineddata")" -gt 10000000 ]; then
  cp "$TD/eng.best.traineddata" "$TD/eng.traineddata"; echo "  best model restored"
else
  if curl -sSL -m 300 -o "$TD/eng.best.traineddata" "https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata"      && [ "$(stat -c %s "$TD/eng.best.traineddata")" -gt 10000000 ]; then
    cp "$TD/eng.best.traineddata" "$TD/eng.traineddata"; echo "  best model downloaded and installed"
  else
    echo "  could not fetch tessdata_best; the fast model stays"
  fi
fi
if [ ! -f "$APP_DIR/tessdata/eng.traineddata" ]; then
  echo "  missing; downloading"
  mkdir -p "$APP_DIR/tessdata"
  curl -fsSL -o "$APP_DIR/tessdata/eng.traineddata" \
    https://github.com/tesseract-ocr/tessdata_fast/raw/main/eng.traineddata
fi

say "Starting $SERVICE"
$SUDO systemctl start $SERVICE
sleep 2
$SUDO systemctl --no-pager --lines=15 status $SERVICE || true

say "Done. Follow the log with: journalctl -u $SERVICE -f"
