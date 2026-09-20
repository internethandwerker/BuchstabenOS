#!/bin/bash
# ==============================================================================
# BuchstabenOS Release Packager
# Baut ein Standalone Linux-x64 Release, schnürt ein .tar.gz Archiv und
# erzeugt SHA-256 Checksummen für GitHub Releases.
#
# Verwendung:
#   bash scripts/build-release.sh [version] [--publish]
#   Beispiel: bash scripts/build-release.sh v1.0.0
# ==============================================================================

set -e

VERSION="${1:-v1.0.0}"
PUBLISH_FLAG="$2"
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
DIST_DIR="$ROOT_DIR/dist"
RELEASE_DIR="$ROOT_DIR/releases"

echo "🔨 Baue BuchstabenOS Release ($VERSION)..."
cd "$ROOT_DIR"

SEMVER="${VERSION#v}"

# 1. Self-contained Release kompilieren
dotnet publish src/BuchstabenOS.UI.Desktop/BuchstabenOS.UI.Desktop.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version="$SEMVER" \
    -p:AssemblyVersion="$SEMVER.0" \
    -p:FileVersion="$SEMVER.0" \
    -p:InformationalVersion="$SEMVER" \
    -o "$DIST_DIR"

# 2. Release-Verzeichnis vorbereiten
mkdir -p "$RELEASE_DIR"
ARCHIVE_NAME="buchstabenos-linux-x64.tar.gz"
ARCHIVE_PATH="$RELEASE_DIR/$ARCHIVE_NAME"

echo "📦 Erstelle Release-Archiv $ARCHIVE_NAME..."
rm -f "$ARCHIVE_PATH"
tar -czf "$ARCHIVE_PATH" -C "$DIST_DIR" .

# 3. SHA-256 Checksumme generieren
cd "$RELEASE_DIR"
sha256sum "$ARCHIVE_NAME" > "$ARCHIVE_NAME.sha256"

echo "✅ Release-Archiv erstellt:"
echo "   Archiv:    $ARCHIVE_PATH ($(du -h "$ARCHIVE_PATH" | cut -f1))"
echo "   Checksum:  $RELEASE_DIR/$ARCHIVE_NAME.sha256"

# 4. Optional: Direkt zu GitHub hochladen (falls gh installiert und authentifiziert ist)
if [ "$PUBLISH_FLAG" == "--publish" ]; then
    if command -v gh >/dev/null 2>&1; then
        echo "🚀 Veröffentliche Release $VERSION auf GitHub..."
        gh release create "$VERSION" "$ARCHIVE_PATH" "$ARCHIVE_NAME.sha256" \
            --title "BuchstabenOS $VERSION" \
            --notes "Offizielles BuchstabenOS Release $VERSION für Linux (x86_64)."
        echo "🎉 Release $VERSION erfolgreich auf GitHub veröffentlicht!"
    else
        echo "⚠️ 'gh' CLI nicht gefunden. Das Archiv kann manuell auf GitHub hochgeladen werden."
    fi
fi
