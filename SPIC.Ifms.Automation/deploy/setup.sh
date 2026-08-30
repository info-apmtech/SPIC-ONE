#!/usr/bin/env bash
#
# One-time preparation of a host for the SPIC IFMS automation.
# Safe to re-run: every step checks before it acts.
#
#   sudo ./setup.sh
#
set -euo pipefail

APP_DIR=/opt/spic-ifms
SERVICE=spic-ifms

say() { printf '\n\033[1;34m==>\033[0m %s\n' "$1"; }
warn() { printf '\033[1;33m warning:\033[0m %s\n' "$1"; }

if [ "$(id -u)" -ne 0 ]; then
  echo "Run this with sudo." >&2
  exit 1
fi

say "Checking the .NET runtime"
if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is not on PATH. Install the ASP.NET Core 10 runtime first:" >&2
  echo "  https://learn.microsoft.com/dotnet/core/install/linux-ubuntu" >&2
  exit 1
fi
dotnet --list-runtimes | grep -q 'Microsoft.NETCore.App 10\.' \
  || warn "No .NET 10 runtime found. The service will not start without it."

say "Checking the time zone database"
# The schedule resolves Asia/Kolkata by name. Without tzdata it silently falls
# back to the machine's local zone, and a UTC server then runs 5.5 hours early.
if [ ! -e /usr/share/zoneinfo/Asia/Kolkata ]; then
  warn "tzdata is missing; installing it."
  apt-get update -qq && apt-get install -y -qq tzdata
fi

say "Creating $APP_DIR"
mkdir -p "$APP_DIR"/{downloads,diagnostics}

say "Creating the secrets file"
if [ ! -f "$APP_DIR/secrets.env" ]; then
  cat > "$APP_DIR/secrets.env" <<'EOF'
# Mode 0600. Never commit this file.
# Portal passwords are NOT here - they live encrypted in the database.

ConnectionStrings__DefaultConnection=Host=localhost;Port=30001;Database=spicone;Username=postgres;Password=CHANGE_ME;Include Error Detail=true

# Must match IfmsAutomation:AutomationKey in SpicAPI.
Alerts__Push__ApiKey=

# SMTP, once you have the details.
Alerts__Email__Password=
EOF
  chmod 600 "$APP_DIR/secrets.env"
  echo "  created $APP_DIR/secrets.env - edit it before starting the service"
else
  echo "  already exists, left alone"
fi

say "Installing the systemd unit"
install -m 644 "$(dirname "$0")/spic-ifms.service" /etc/systemd/system/$SERVICE.service
systemctl daemon-reload

say "Done"
cat <<EOF

Next:
  1. Edit $APP_DIR/secrets.env
  2. ./publish.sh          build and install the application
  3. dotnet $APP_DIR/SPIC.Ifms.Automation.dll set-credentials spic <user> <pass> SPIC
  4. systemctl enable --now $SERVICE

EOF
