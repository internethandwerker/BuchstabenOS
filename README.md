# BuchstabenOS 🎈

> **"Ein Linux-Betriebssystem, das mit jedem Tastendruck spricht und wächst."**  
> Entwickelt mit Liebe für Moritz – ein offline-fähiges, minimalistisches und robustes Lernspiel für Vorschulkinder auf Linux.

---

[![Build & Test](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Target Framework](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![UI Framework](https://img.shields.io/badge/Avalonia-11.3-blue.svg)](https://avaloniaui.net/)
[![Architecture](https://img.shields.io/badge/Architecture-DDD%20%2F%20Clean%20Code-orange.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Linux%20First%20(X11%20%2F%20Wayland)-green.svg)]()

---

## 🌟 Unsere Vision

Vorschulkinder lieben Tastaturen. Aber normale Betriebssysteme sind voller Fallen: Fenster schließen sich versehentlich, Tastenkombinationen zerstören den Workflow, und bunte Apps überfluten die Sinne mit blinkender Werbung und Reizüberflutung.

**BuchstabenOS geht den radikal handwerklichen Weg:**
*   **Ablenkungsfrei & Augenschonend:** Tiefschwarzer Hintergrund ("Midnight Chalkboard"), warmweiße, riesige Schrift – kein Menü-Wirrwarr, kein blinkender Schnickschnack.
*   **Pädagogisch fundiert:** Standardmäßig wird **lautiert** (wie in Montessori-Schulen: [m], [b], [t]), anstelle von verwirrenden Buchstabennamen ("Em", "Be", "Te").
*   **100% Offline & Datensicher:** Läuft komplett auf einem ausrangierten Laptop (z. B. altes Lenovo ThinkPad mit BunsenLabs Linux). Keine Cloud, keine Accounts, kein Internet nötig.
*   **Magisches Feedback:** Der erste Buchstabe thront riesig im Zentrum des Bildschirms. Jeder weitere Buchstabe lässt das Wort harmonisch schrumpfen, bis es perfekt in die Zeile passt.
*   **Worterkennung & Feier:** Erkennt deutsche Kinderwörter (`MAMA`, `AUTO`, `BAGGER`, `MORITZ`) in Echtzeit und feiert den Erfolg!

---

## 🚀 Hauptfeatures

### 1. Dynamische Schriftgrößen-Engine & Zentrierte Bühne
*   **1. Buchstabe:** Bis zu `240 pt` groß im Zentrum des Bildschirms.
*   **Wortwachstum:** Schrumpft proportional mit jedem Zeichen, damit das Wort als optische Einheit erfasst werden kann.
*   **Schwebende Historie:** Bei Zeilenumbruch oder `Enter` bleibt der aktive Schreibfokus **immer vertikal in der Bildschirmmitte**. Ältere Zeilen gleiten sanft nach oben und verblassen schrittweise ins Tiefschwarz (`Opacity = 0.55 -> 0.28 -> 0.12 -> 0.05`), bis sie am oberen Bildschirmrand verschwinden.

### 2. Polyphone Multitrack-Audio-Pipeline
*   Kinder tippen wild und ungeduldig. Mehrfaches Drücken der Leertaste oder schnelles Hämmern auf Tasten schneidet laufende Ausgaben nicht ab.
*   Tastentöne/Laute und Sprachausgaben laufen über zwei unabhängige, parallele Audio-Tracks via PipeWire (`pw-play`) bzw. PulseAudio (`paplay`).

### 3. Vorschulgerechte Sprachausgabe
*   **Modus "Lautieren" (Default):** Tastenanschläge spielen sofort den phonetischen Laut ab (`[b]`, `[d]`, `[m]`, `[s]`).
*   **Modus "Alphabet":** Klassisches Abc-Lied ("Be", "De", "Ka").
*   **DIY-Soundpacks:** Eigene Sprach-Samples (z. B. Papas oder Mamas Stimme) können einfach als `.wav`, `.ogg` oder `.mp3` unter `~/.config/buchstabenos/sounds/laute/` abgelegt werden und haben sofort Vorrang!

### 4. Lokale neuronale Sprachsynthese (Piper TTS)
*   Sobald die **Leertaste** gedrückt wird, liest die integrierte neuronale TTS ([Piper TTS](https://github.com/rhasspy/piper)) das entstandene Wort vor – selbst wenn es herrlicher Quatsch-Buchstabensalat ist!
*   Verwendet offline das hochwertige deutsche Modell `de_DE-thorsten-medium`.
*   Zukunftssicher: Kann per Fine-Tuning mit der eigenen "Papa-Stimme" trainiert werden!

### 5. Unsichtbares Elternmenü mit Kiosk-Exit
*   **Geheimer Shortcut:** `Strg + Alt + Shift + P`
*   **Sicherheits-PIN:** Geschützt durch SHA-256 (Standard: `1337`).
*   **Funktionen:** Lautstärke regeln, Sprachmodus umschalten, Kinderwörterbuch live bearbeiten, Laptop herunterfahren oder mit Returncode 42 direkt auf den Linux-Desktop zurückkehren.

---

## 🏛️ Architektur & Code-Qualität

Das Projekt ist konsequent nach **Clean Architecture** und **Domain-Driven Design (DDD)** strukturiert:

```
BuchstabenOS/
├── src/
│   ├── BuchstabenOS.Domain/          # Reiner C# Kern ohne externe Abhängigkeiten
│   │   ├── Model/Typing/             # TextStage, FontScaleCalculator, TrieNode
│   │   ├── Model/Words/              # WordDetector, WordMatch
│   │   ├── Model/Games/              # IGameModule, GameMetadata, PedagogicalSkill
│   │   ├── Model/Security/           # ParentPin, Hashing
│   │   └── Events/                   # LetterTypedEvent, WordRecognizedEvent, ...
│   ├── BuchstabenOS.Application/     # Use Cases, Ports & Orchestrierung
│   │   ├── Coordinates/              # GameCoordinator, InMemoryGameRegistry
│   │   └── Ports/                    # IAudioPlayer, ITtsEngine, IWordDictionaryRepository
│   ├── BuchstabenOS.Infrastructure/  # Konkrete Linux- und Datei-Implementierungen
│   │   ├── Audio/                    # LinuxAudioSamplePlayer (Multitrack), PiperTtsEngine
│   │   ├── Dictionary/               # JsonWordDictionaryRepository (Persistenz)
│   │   ├── Storage/                  # JsonSettingsRepository
│   │   └── System/                   # LinuxSystemControl (wpctl, amixer, systemctl)
│   └── BuchstabenOS.UI.Desktop/      # Avalonia 11 MVVM Oberfläche mit Skia-Rendering
├── tests/
│   └── BuchstabenOS.Domain.Tests/    # Automatisierte Unit-Tests (xUnit)
├── docs/                             # Vollständiges Systemhandbuch (Architektur, Kiosk, Audio)
├── kiosk/                            # BunsenLabs/Debian X11-Kiosk-Session Skripte
├── scripts/                          # Automatisierter TTS-Installer & Sample-Generatoren
└── assets/                           # Audio-Samples (Laute, Alphabet, Jingles) & Fonts
```

### Zukunftsfähiges Plugin-System (`IGameModule`)
BuchstabenOS ist nicht auf das freie Tippen beschränkt. Jedes Spielmodul implementiert `IGameModule` und liefert semantische Metadaten:
*   `PedagogicalSkill`: Welche Fähigkeit wird trainiert (z. B. `PhonologicalAwareness`, `LetterRecognition`, `Arithmetic`).
*   `SkillPrerequisite`: Welche Fähigkeiten werden vorausgesetzt?
*   `AgeRecommendation`: Empfohlenes Alter (z. B. 4–6 Jahre).
*   `KnowledgeScore`: Ermöglicht späteren KI-Agenten oder Eltern-Dashboards die adaptive Auswahl von Lern-Playlists.

---

## 💻 Schnellstart & Installation

### Voraussetzungen
*   .NET 10 SDK (zur Entwicklung / zum Kompilieren)
*   Linux (X11 oder Wayland mit PipeWire/PulseAudio)

### 1. Im Fenstermodus starten (Entwickler-Modus)
```bash
git clone git@github.com:internethandwerker/BuchstabenOS.git
cd BuchstabenOS

# Sprachausgabe (Piper TTS) und Thorsten-Modell installieren:
./scripts/install-tts.sh

# Starten im Fenstermodus:
dotnet run --project src/BuchstabenOS.UI.Desktop -- --windowed
```

### 2. Standalone-Binary bauen (für den Kinder-Laptop)
```bash
dotnet publish src/BuchstabenOS.UI.Desktop/BuchstabenOS.UI.Desktop.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -o dist/
```
Erzeugt eine einzige ausführbare Datei unter `dist/BuchstabenOS.UI.Desktop`. Auf dem Kinder-Laptop muss **kein .NET SDK** installiert werden!

### 3. Kiosk-Modus auf dem Kinder-Laptop (BunsenLabs / Debian)
```bash
sudo bash kiosk/install-kiosk.sh
```
Richtet eine isolierte X11-Session ein:
*   Kein Fenstermanager, kein störender Desktop.
*   Mauszeiger wird nach 1 Sekunde Inaktivität unsichtbar (`unclutter`).
*   Bildschirmschoner und Energiesparmodi sind deaktiviert.
*   Sicherer Returncode 42 führt zum normalen Eltern-Desktop.

---

## 🗺️ Roadmap & Meilensteine

*   [x] **Phase 1: MVP (v1.0)** – Vollständig funktionierende Tastatur-Bühne, Multitrack-Audio, Trie-Wörterbuch, Piper-TTS, Elternmenü und Kiosk-Session.
*   [ ] **Phase 1.5: Die Papa-Stimme (v1.1)** – Eigene 30 Studio-Samples für Anlaute ([b], [d], [m]) + Fine-Tuning einer persönlichen Piper-TTS-Stimme.
*   [ ] **Phase 2: Farben & Belohnungen (v1.2)** – Montessori-Färbung (Vokale blau/rot, Konsonanten warmweiß), Partikel/Konfetti und Erfolgs-Jingles.
*   [ ] **Phase 3: Bild-Wörterbuch & Belohnungs-Videos (v2.0)** – Freundliche Tier- und Alltagsfotos bei erkannten Wörtern, 15-Sekunden-Clips bei 5 Sternen.
*   [ ] **Phase 4: Neue Lernmodule (v3.0)** – "Mathe-Zwerg" (1+1=?), "Buchstaben-Detektiv" (Finde das B wie Bär).

Ausführliche Dokumentation findest du im Ordner [`docs/`](docs/).

---

## 📄 Lizenz & Autorenschaft

Entwickelt von **Alex** ([@internethandwerker](https://github.com/internethandwerker)) und **Antigravity**.  
*Solide Handwerkskunst für neugierige Kinder.* 🛠️❤️
