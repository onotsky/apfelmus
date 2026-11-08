#!/usr/bin/env bash
# Baut ajcore-logd für alle Plattformen (jeweils x64 und ARM64) nach dist/. Braucht nur Go (>= 1.21).
# Die Dateien aus dist/ hängt release.sh automatisch ans GitHub-Release.
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p dist
for t in linux/amd64 linux/arm64 darwin/amd64 darwin/arm64 windows/amd64 windows/arm64; do
  os=${t%/*}; arch=${t#*/}; ext=""; [ "$os" = windows ] && ext=".exe"
  CGO_ENABLED=0 GOOS=$os GOARCH=$arch go build -trimpath -ldflags "-s -w" -o "dist/ajcore-logd-$os-$arch$ext" .
  echo "dist/ajcore-logd-$os-$arch$ext"
done
