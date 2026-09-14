#!/bin/bash
# ==============================================================================
# BuchstabenOS: Automatischer Installer für Offline-TTS (Piper & deutsche Stimme)
# Installiert Piper TTS und die deutsche 'Thorsten'-Stimme nach ~/.local/
# Funktioniert sowohl auf Arch Linux als auch auf BunsenLabs / Debian.
# ==============================================================================

set -e

BIN_DIR="$HOME/.local/bin"
VOICE_DIR="$HOME/.local/share/piper"

mkdir -p "$BIN_DIR"
mkdir -p "$VOICE_DIR"

echo "🎙️ Installiere Offline Text-to-Speech für BuchstabenOS..."

# 1. Piper Binary herunterladen (Standalone Linux x86_64)
if [ ! -f "$BIN_DIR/piper" ]; then
    echo "⬇️ Lade Piper TTS Binary herunter..."
    TEMP_DIR=$(mktemp -d)
    PIPER_URL="https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_linux_x86_64.tar.gz"
    
    curl -sL "$PIPER_URL" -o "$TEMP_DIR/piper.tar.gz"
    tar -xzf "$TEMP_DIR/piper.tar.gz" -C "$TEMP_DIR"
    
    cp -r "$TEMP_DIR/piper/"* "$BIN_DIR/"
    chmod +x "$BIN_DIR/piper"
    rm -rf "$TEMP_DIR"
    echo "✅ Piper installiert in $BIN_DIR/piper"
else
    echo "✅ Piper ist bereits in $BIN_DIR/piper vorhanden."
fi

# 2. Deutsche Vorlesestimme (Thorsten - Medium) herunterladen
ONNX_FILE="$VOICE_DIR/de_DE-thorsten-medium.onnx"
JSON_FILE="$VOICE_DIR/de_DE-thorsten-medium.onnx.json"

if [ ! -f "$ONNX_FILE" ]; then
    echo "⬇️ Lade deutsche Stimme 'Thorsten' (~63 MB)..."
    curl -L "https://huggingface.co/rhasspy/piper-voices/resolve/main/de/de_DE/thorsten/medium/de_DE-thorsten-medium.onnx" -o "$ONNX_FILE"
fi

if [ ! -f "$JSON_FILE" ]; then
    echo "⬇️ Lade Stimmenkonfiguration (~5 KB)..."
    curl -L "https://huggingface.co/rhasspy/piper-voices/resolve/main/de/de_DE/thorsten/medium/de_DE-thorsten-medium.onnx.json" -o "$JSON_FILE"
fi

echo "✅ Deutsche Stimme bereit in $VOICE_DIR"

# 3. Funktionstest
echo ""
echo "🔊 Teste Sprachausgabe..."
if command -v paplay >/dev/null 2>&1; then
    echo "Hallo Moritz, Buchstaben OS ist bereit!" | "$BIN_DIR/piper" --model "$ONNX_FILE" --output-raw | paplay --raw --rate=22050 --channels=1
elif command -v aplay >/dev/null 2>&1; then
    echo "Hallo Moritz, Buchstaben OS ist bereit!" | "$BIN_DIR/piper" --model "$ONNX_FILE" --output-raw | aplay -r 22050 -f S16_LE -t raw -c 1
fi

echo "🎉 Fertig! Die deutsche Offline-Sprachausgabe ist einsatzbereit."
