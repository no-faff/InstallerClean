#!/usr/bin/env bash
# Rebuild InstallerClean-cli.exe from launcher.c.
# Run on Linux with mingw-w64 installed (Fedora: sudo dnf install mingw64-gcc).
#
# Output is committed alongside the source so the Inno Setup installer can
# always pick up a fresh binary without a build-on-Windows step.
set -eu

cd "$(dirname "$0")"
x86_64-w64-mingw32-gcc \
    -O2 -s -municode \
    -Wall -Wextra -Werror \
    -static -static-libgcc \
    -o InstallerClean-cli.exe \
    launcher.c

echo "Built InstallerClean-cli.exe"
ls -l InstallerClean-cli.exe
sha256sum InstallerClean-cli.exe
