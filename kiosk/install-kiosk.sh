#!/bin/bash
# ==============================================================================
# BuchstabenOS Kiosk Installer für BunsenLabs Linux (Debian)
# Führe dieses Skript auf Moritz' Laptop mit 'sudo bash install-kiosk.sh' aus.
# ==============================================================================

set -e

if [ "$EUID" -ne 0 ]; then
    echo "❌ Bitte als root bzw. mit 'sudo' ausführen!"
    exit 1
fi

echo "🚀 Installiere BuchstabenOS Kiosk..."

# 1. Benötigte Hilfspakete installieren
echo "📦 Installiere Hilfstools (unclutter, pulseaudio-utils, alsa-utils)..."
apt-get update -qq
apt-get install -y -qq unclutter pulseaudio-utils alsa-utils

# 2. Session-Skript installieren
echo "📄 Kopiere Session-Skript nach /usr/local/bin/buchstabenos-session.sh..."
cp "$(dirname "$0")/buchstabenos-session.sh" /usr/local/bin/buchstabenos-session.sh
chmod +x /usr/local/bin/buchstabenos-session.sh

# 3. XSession für LightDM registrieren
echo "📄 Registriere XSession in /usr/share/xsessions/buchstabenos.desktop..."
cp "$(dirname "$0")/buchstabenos.desktop" /usr/share/xsessions/buchstabenos.desktop

# 4. X11 Kiosk-Sicherheit (Kein TTY-Switch)
echo "🔒 Konfiguriere X11 Tastensperren..."
mkdir -p /etc/X11/xorg.conf.d/
cp "$(dirname "$0")/10-kiosk-security.conf" /etc/X11/xorg.conf.d/10-kiosk-security.conf

# 5. Magic SysRq deaktivieren
echo "kernel.sysrq = 0" > /etc/sysctl.d/99-buchstabenos-kiosk.conf
sysctl -p /etc/sysctl.d/99-buchstabenos-kiosk.conf >/dev/null 2>&1 || true

# 6. Programm-Verzeichnis anlegen
mkdir -p /opt/buchstabenos
echo "📂 /opt/buchstabenos vorbereitet. Bitte dorthin die 'dotnet publish' Dateien kopieren."

# 7. Benutzer moritz anlegen, falls noch nicht vorhanden
if ! id "moritz" >/dev/null 2>&1; then
    echo "👤 Erstelle Benutzer 'moritz'..."
    useradd -m -s /bin/bash -G audio,video moritz
    passwd -d moritz # Kein Passwort für Autologin
fi

# 8. LightDM Autologin konfigurieren
LIGHTDM_CONF="/etc/lightdm/lightdm.conf"
if [ -f "$LIGHTDM_CONF" ]; then
    echo "⚙️ Konfiguriere LightDM Autologin für moritz..."
    sed -i 's/^#autologin-user=.*/autologin-user=moritz/' "$LIGHTDM_CONF"
    sed -i 's/^#autologin-user-timeout=.*/autologin-user-timeout=0/' "$LIGHTDM_CONF"
    sed -i 's/^#user-session=.*/user-session=buchstabenos/' "$LIGHTDM_CONF"
fi

echo ""
echo "✅ BuchstabenOS Kiosk wurde erfolgreich installiert!"
echo "   Kopiere die kompilierte BuchstabenOS Binary nach /opt/buchstabenos/."
echo "   Beim nächsten Neustart bootet der Laptop direkt in Moritz' Spiel!"
