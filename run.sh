#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

PROJECT="HeatMapStreak.Web"
CONFIG="Debug"
URLS="${URLS:-http://localhost:5161}"

# Everything this script prints is also kept in a log file for later debugging.
LOG_DIR="${LOG_DIR:-$HOME/magnus/heatmap-streak/logs}"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/run-$(date +%Y%m%d-%H%M%S).log"
exec > >(tee -a "$LOG_FILE") 2>&1

echo "==> Log file: $LOG_FILE"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "error: 'dotnet' not found on PATH. Install the .NET 8 SDK: https://dot.net" >&2
    exit 1
fi

sdk_major="$(dotnet --version | cut -d. -f1)"
if [ -z "$sdk_major" ] || [ "$sdk_major" -lt 8 ]; then
    echo "error: .NET SDK '$(dotnet --version)' is too old, this project targets net8.0." >&2
    exit 1
fi

echo "==> Restoring $PROJECT"
dotnet restore "$PROJECT/$PROJECT.csproj"

echo "==> Building ($CONFIG)"
dotnet build "$PROJECT/$PROJECT.csproj" -c "$CONFIG" --no-restore

echo "==> Starting HeatMapStreak at $URLS (Ctrl-C to stop)"
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="$URLS" \
exec dotnet run --project "$PROJECT" -c "$CONFIG" --no-build --no-launch-profile
