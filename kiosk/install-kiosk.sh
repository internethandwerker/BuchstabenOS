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

# 6. Benutzer moritz anlegen, falls noch nicht vorhanden
if ! id "moritz" >/dev/null 2>&1; then
    echo "👤 Erstelle Benutzer 'moritz'..."
    useradd -m -s /bin/bash -G audio,video,input moritz
    passwd -d moritz # Kein Passwort für Autologin
fi

# 7. Programm-Verzeichnis anlegen & Rechte für Moritz vergeben
# (Erlaubt dem 1-Klick In-App Updater, die Binärdateien ohne Root-Passwort zu aktualisieren!)
mkdir -p /opt/buchstabenos
chown -R moritz:moritz /opt/buchstabenos
chmod 755 /opt/buchstabenos

# 8. LightDM Autologin konfigurieren
LIGHTDM_CONF="/etc/lightdm/lightdm.conf"
if [ -f "$LIGHTDM_CONF" ]; then
    echo "⚙️ Konfiguriere LightDM Autologin für moritz..."
    sed -i 's/^#autologin-user=.*/autologin-user=moritz/' "$LIGHTDM_CONF"
    sed -i 's/^#autologin-user-timeout=.*/autologin-user-timeout=0/' "$LIGHTDM_CONF"
    sed -i 's/^#user-session=.*/user-session=buchstabenos/' "$LIGHTDM_CONF"
fi

# 9. Offline TTS (Piper & Thorsten-Stimme) für moritz installieren
TTS_SCRIPT="$(dirname "$0")/../scripts/install-tts.sh"
if [ -f "$TTS_SCRIPT" ]; then
    echo "🎙️ Installiere deutsche Offline-Sprachausgabe für 'moritz'..."
    su - moritz -c "bash \"$TTS_SCRIPT\"" || echo "⚠️ TTS-Installation übersprungen oder fehlgeschlagen (kann manuell nachgeholt werden)."
fi

# 10. Falls bereits ein Build in dist/ vorliegt: Direkt nach /opt/buchstabenos installieren!
DIST_DIR="$(dirname "$0")/../dist"
if [ -d "$DIST_DIR" ] && [ -f "$DIST_DIR/BuchstabenOS.UI.Desktop" ]; then
    echo "📦 Kopiere BuchstabenOS aus dist/ nach /opt/buchstabenos/..."
    cp -r "$DIST_DIR/"* /opt/buchstabenos/
    chown -R moritz:moritz /opt/buchstabenos
    chmod +x /opt/buchstabenos/BuchstabenOS.UI.Desktop
    echo "✅ BuchstabenOS wurde direkt nach /opt/buchstabenos kopiert und aktiviert."
fi

echo ""
echo "🎉 BuchstabenOS Kiosk wurde erfolgreich eingerichtet!"
echo "   Beim nächsten Neustart (oder 'systemctl restart lightdm') startet der Laptop direkt in Moritz' Spiel!"
