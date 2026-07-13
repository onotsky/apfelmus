#!/usr/bin/env bash
#
# release.sh - baut alle vier Plattform-Artefakte, setzt den Tag, erstellt das GitHub-
# Release und raeumt ueberholte Releases auf. Version kommt zentral aus Directory.Build.props.
#
# Voraussetzungen: macOS (fuer die .app-Bundles: sips/iconutil/codesign), dotnet-SDK,
# gh (GitHub CLI), git. Token-Datei mit einem GitHub-Token (repo-Scope) fuer Push + gh.
#
# Aufruf:
#   ./release.sh                      # baut + released die Version aus Directory.Build.props
#   ./release.sh -n notes.md          # Release-Notes aus Datei (sonst Minimal-Notiz)
#   ./release.sh --dry-run            # nur bauen, NICHT taggen/releasen/aufraeumen
#
# Umgebungsvariablen (mit Defaults):
#   AJ_TOKEN_FILE  Pfad zur Token-Datei            (Default: ~/aj_token)
#   AJ_REPO        GitHub-Repo owner/name          (Default: onotsky/apfelmus)
#   AJ_KEEP        Tags, die beim Aufraeumen bleiben (Default: "v5.4.0" - Fallback-Release)
#
set -euo pipefail

# --- Konfiguration ----------------------------------------------------------
HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$HERE/Apfelmus.Avalonia/Apfelmus.Avalonia.csproj"
TOKEN_FILE="${AJ_TOKEN_FILE:-$HOME/aj_token}"
REPO="${AJ_REPO:-onotsky/apfelmus}"
KEEP="${AJ_KEEP:-v5.4.0}"        # leerzeichengetrennte Liste geschuetzter Tags
NOTES_FILE=""
DRY_RUN=0

# Avalonia-Telemetrie-Build-Task scheitert in Sandboxes am Schreiben in ~/Library/... .
export AVALONIA_TELEMETRY_OPTOUT=1

while [ $# -gt 0 ]; do
  case "$1" in
    -n|--notes) NOTES_FILE="$2"; shift 2 ;;
    --dry-run)  DRY_RUN=1; shift ;;
    *) echo "Unbekanntes Argument: $1" >&2; exit 2 ;;
  esac
done

VER="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$HERE/Directory.Build.props" | head -1)"
[ -n "$VER" ] || { echo "FEHLER: Version nicht aus Directory.Build.props lesbar." >&2; exit 1; }
TAG="v$VER"
echo "== Apfelmus Release $TAG (Repo $REPO) =="

# --- macOS-.app-Bundle bauen, bereinigen, signieren, zippen -----------------
# build-macos-app.sh signiert selbst, scheitert aber am com.apple.provenance-xattr
# (macOS haengt es nach dem Signieren wieder an). Deshalb hier: xattr -cr -> neu
# signieren -> ERST DANN zippen, damit die Ad-hoc-Signatur wirklich im Zip landet.
build_mac() {
  local rid="$1"
  echo "== macOS $rid =="
  "$HERE/Apfelmus.Avalonia/build-macos-app.sh" "$rid" >/dev/null
  local dir="$HERE/Apfelmus.Avalonia/bin/macos-app/$rid"
  local app="$dir/Apfelmus.app"
  xattr -cr "$app"
  codesign --force --deep --sign - "$app" >/dev/null 2>&1 || echo "  WARN: codesign"
  local zip="$dir/Apfelmus.Avalonia-$VER-$rid-app.zip"
  rm -f "$zip"
  ( cd "$dir" && ditto -c -k --keepParent Apfelmus.app "Apfelmus.Avalonia-$VER-$rid-app.zip" )
  echo "$zip"
}

# --- Windows/Linux: self-contained publish + zip ----------------------------
build_selfcontained() {
  local rid="$1"
  echo "== $rid =="
  local base="$HERE/Apfelmus.Avalonia/bin/release/$rid"
  local out="$base/Apfelmus.Avalonia"
  rm -rf "$base"
  dotnet publish "$PROJ" -c Release -r "$rid" --self-contained true -o "$out" >/dev/null
  local zip="$base/Apfelmus.Avalonia-$VER-$rid.zip"
  ( cd "$base" && zip -qr "Apfelmus.Avalonia-$VER-$rid.zip" "Apfelmus.Avalonia" )
  echo "$zip"
}

A_ARM="$(build_mac osx-arm64 | tail -1)"
A_X64="$(build_mac osx-x64  | tail -1)"
A_WIN="$(build_selfcontained win-x64   | tail -1)"
A_LIN="$(build_selfcontained linux-x64 | tail -1)"

echo "== Artefakte =="
for f in "$A_WIN" "$A_LIN" "$A_ARM" "$A_X64"; do
  [ -f "$f" ] || { echo "FEHLER: Artefakt fehlt: $f" >&2; exit 1; }
  echo "  $f"
done

if [ "$DRY_RUN" -eq 1 ]; then
  echo "== --dry-run: kein Tag/Release/Cleanup =="
  exit 0
fi

# --- Token laden ------------------------------------------------------------
[ -f "$TOKEN_FILE" ] || { echo "FEHLER: Token-Datei $TOKEN_FILE fehlt." >&2; exit 1; }
TOKEN="$(cat "$TOKEN_FILE")"
export GH_TOKEN="$TOKEN"
PUSH_URL="https://x-access-token:${TOKEN}@github.com/${REPO}.git"

# --- Tag auf HEAD setzen und pushen -----------------------------------------
echo "== Tag $TAG -> HEAD ($(git -C "$HERE" rev-parse --short HEAD)) =="
git -C "$HERE" tag -f "$TAG"
git -C "$HERE" push -f "$PUSH_URL" "refs/tags/$TAG" 2>&1 | grep -vi x-access-token || true

# --- Release-Notes ----------------------------------------------------------
if [ -n "$NOTES_FILE" ] && [ -f "$NOTES_FILE" ]; then
  NOTES="$(cat "$NOTES_FILE")"
else
  NOTES="Apfelmus $VER

Artefakte:
- win-x64 / linux-x64: self-contained, entpacken und Apfelmus.Avalonia starten.
- osx-arm64 / osx-x64: .app-Bundle (ad-hoc signiert). Beim ersten Start Rechtsklick -> Oeffnen."
fi

# --- Release erstellen (ersetzt ein evtl. vorhandenes gleiches Tag-Release) --
if gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
  echo "== Release $TAG existiert -> ersetzen =="
  gh release delete "$TAG" --repo "$REPO" --yes >/dev/null 2>&1 || true
fi
echo "== Release $TAG erstellen =="
gh release create "$TAG" --repo "$REPO" --title "Apfelmus $VER" --notes "$NOTES" \
  "$A_WIN" "$A_LIN" "$A_ARM" "$A_X64"

# --- Ueberholte Releases aufraeumen (KEEP + aktuelles Tag bleiben) -----------
echo "== Aufraeumen (behalte: $KEEP $TAG) =="
for old in $(gh release list --repo "$REPO" --json tagName -q '.[].tagName' 2>/dev/null); do
  keep=0
  [ "$old" = "$TAG" ] && keep=1
  for k in $KEEP; do [ "$old" = "$k" ] && keep=1; done
  if [ "$keep" -eq 0 ]; then
    echo "  loesche $old"
    gh release delete "$old" --repo "$REPO" --yes --cleanup-tag >/dev/null 2>&1 || true
  fi
done

echo "== Fertig: https://github.com/$REPO/releases/tag/$TAG =="
gh release list --repo "$REPO" 2>/dev/null
