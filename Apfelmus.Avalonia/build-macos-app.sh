#!/usr/bin/env bash
#
# Baut aus dem Avalonia-Projekt ein macOS-.app-Bundle (self-contained, osx-arm64).
# Enthaelt Info.plist mit ajfsp://-URL-Scheme und ein aus Apple-icon.png generiertes .icns
# (behebt zugleich das fehlende Dock-Icon). Ergebnis: bin/macos-app/Apfelmus.app (+ .zip).
#
# Voraussetzung: .NET-SDK (dotnet im PATH), macOS (sips/iconutil).
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$HERE/Apfelmus.Avalonia.csproj"
# Version zentral aus Directory.Build.props ziehen (nicht mehr hartkodiert -> nie veraltet).
VER="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$HERE/../Directory.Build.props" | head -1)"
VER="${VER:-0.0.0}"
# Zielarchitektur als 1. Argument: osx-arm64 (Apple Silicon, Standard) oder osx-x64 (Intel).
RID="${1:-osx-arm64}"
OUT="$HERE/bin/macos-app/$RID"
APP="$OUT/Apfelmus.app"
PNG="$HERE/Assets/Images/Apple-icon.png"
EXE="Apfelmus.Avalonia"

echo "== Publish ($RID) =="
rm -rf "$OUT"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
dotnet publish "$PROJ" -c Release -r "$RID" --self-contained true -o "$APP/Contents/MacOS" | tail -1

echo "== Icon (.icns) =="
ICONSET="$OUT/Apfelmus.iconset"
mkdir -p "$ICONSET"
gen() { sips -z "$2" "$2" "$PNG" --out "$ICONSET/$1" >/dev/null 2>&1 || true; }
gen icon_16x16.png 16
gen icon_16x16@2x.png 32
gen icon_32x32.png 32
gen icon_32x32@2x.png 64
gen icon_128x128.png 128
gen icon_128x128@2x.png 256
gen icon_256x256.png 256
gen icon_256x256@2x.png 512
gen icon_512x512.png 512
gen icon_512x512@2x.png 1024
if iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/Apfelmus.icns"; then
  echo "  Apfelmus.icns erstellt"
else
  echo "  WARNUNG: iconutil fehlgeschlagen - Bundle ohne Icon"
fi
rm -rf "$ICONSET"

echo "== Info.plist =="
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>Apfelmus</string>
    <key>CFBundleDisplayName</key><string>Apfelmus</string>
    <key>CFBundleIdentifier</key><string>cc.applejuicenet.apfelmus</string>
    <key>CFBundleVersion</key><string>${VER}</string>
    <key>CFBundleShortVersionString</key><string>${VER}</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleExecutable</key><string>${EXE}</string>
    <key>CFBundleIconFile</key><string>Apfelmus.icns</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>LSMinimumSystemVersion</key><string>11.0</string>
    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleURLName</key><string>appleJuice Link</string>
            <key>CFBundleURLSchemes</key>
            <array><string>ajfsp</string></array>
        </dict>
    </array>
</dict>
</plist>
PLIST

chmod +x "$APP/Contents/MacOS/${EXE}"

echo "== Codesign (ad-hoc, komplettes Bundle) =="
# Ohne Signatur ueber das GANZE Bundle meldet spctl "code has no resources but signature
# indicates they must be present" -> macOS haelt die App fuer beschaedigt. Eine Ad-hoc-Signatur
# (-s -) ueber alle Bestandteile (--deep) behebt das. Keine Notarisierung (kein Apple-Account),
# daher bleibt beim ersten Start der Rechtsklick->Oeffnen / das Entfernen der Quarantaene noetig.
codesign --force --deep --sign - "$APP" && echo "  signiert (ad-hoc)" || echo "  WARNUNG: codesign fehlgeschlagen"

echo "== Zip =="
ZIP="$OUT/Apfelmus.Avalonia-${VER}-${RID}-app.zip"
( cd "$OUT" && ditto -c -k --keepParent "Apfelmus.app" "$ZIP" )

echo "Fertig:"
echo "  $APP"
echo "  $ZIP"
echo
echo "Hinweis: ajfsp://-Verknuepfung wird von macOS registriert, sobald das Bundle"
echo "bekannt ist - z.B. nach /Applications kopieren oder einmal:"
echo "  /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f \"$APP\""
