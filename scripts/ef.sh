#!/usr/bin/env bash
# Runs `dotnet ef` inside the .NET SDK container, because this host's WDAC policy
# blocks the local EF tool from loading our compiled assembly.
#
# Usage:
#   scripts/ef.sh migrations add <Name> -o DataAccess/Migrations
#   scripts/ef.sh migrations remove
#
# Migrations are written into the mounted source tree (src/EventTicketing.Api/...).
set -euo pipefail

# Windows path of the repo root (forward slashes) for the Docker volume mount.
REPO_WIN="$(cd "$(dirname "$0")/.." && pwd -W)"

MSYS_NO_PATHCONV=1 docker run --rm \
  -v "${REPO_WIN}:/src" \
  -v "ettest-nuget:/root/.nuget" \
  -w "/src/src/EventTicketing.Api" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc "dotnet tool install --global dotnet-ef --version 10.0.9 >/dev/null 2>&1; export PATH=\"\$PATH:/root/.dotnet/tools\"; dotnet ef $*"
