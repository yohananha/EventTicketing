#!/usr/bin/env bash
# Runs the test suite inside the .NET SDK container.
# Tests can't run on this host directly: the WDAC policy blocks the signed test host
# from loading our unsigned test/app assemblies. The Linux container has no such policy.
set -euo pipefail

REPO_WIN="$(cd "$(dirname "$0")/.." && pwd -W)"

MSYS_NO_PATHCONV=1 docker run --rm \
  -v "${REPO_WIN}:/src" \
  -v "ettest-nuget:/root/.nuget" \
  -w "/src" \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc "dotnet test tests/EventTicketing.Tests --nologo"
