#!/bin/bash
# ==============================================================================
# BuchstabenOS Kiosk Session Script für BunsenLabs Linux (Debian)
# Startet eine isolierte X11-Session ohne Fenstermanager exklusiv für Moritz.
# ==============================================================================

# 1. Bildschirmschoner, Display-Sleep und DPMS komplett abschalten
xset s off
xset -dpms
xset s noblank

# 2. Mauszeiger nach 2 Sekunden Inaktivität automatisch ausblenden
if command -v unclutter >/dev/null 2>&1; then
    unclutter -idle 2 -root &
fi

# 3. Audio-Server sicherstellen (PulseAudio / PipeWire)
if command -v start-pulseaudio-x11 >/dev/null 2>&1; then
    start-pulseaudio-x11 &
fi

# Soundkarte nicht schlafen legen (verhindert Knacken bei alten Intel-Chips)
if [ -d /sys/module/snd_hda_intel/parameters ]; then
    echo 0 | sudo tee /sys/module/snd_hda_intel/parameters/power_save >/dev/null 2>&1
fi

# 4. Installations-Pfad von BuchstabenOS
BIN_PATH="/opt/buchstabenos/BuchstabenOS.UI.Desktop"

# Fallback für lokale Entwicklung / Ausführung aus dem Benutzerverzeichnis
if [ ! -f "$BIN_PATH" ]; then
    BIN_PATH="$HOME/buchstabenos/BuchstabenOS.UI.Desktop"
fi

# 5. Kiosk-Schleife (Selbstheilung bei unbeabsichtigtem Crash)
while true; do
    "$BIN_PATH" --kiosk
    EXIT_CODE=$?

    # Exit-Code 42: Papa hat im Elternmenü "Desktop freigeben" gewählt
    if [ $EXIT_CODE -eq 42 ]; then
        echo "Eltern-Freigabe empfangen. Beende Kiosk-Session."
        break
    fi

    # Kurze Pause vor möglichem Neustart
    sleep 1
done
