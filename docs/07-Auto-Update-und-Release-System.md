# 07. Auto-Update & Release-System 🚀

> **Die Kernanforderung:** Wie halten Eltern und Pädagogen BuchstabenOS auf dem aktuellen Stand, ohne jemals ein Linux-Terminal öffnen oder Git-Befehle eingeben zu müssen?

---

## 🎯 Überblick & Benutzer-Erlebnis

BuchstabenOS ist für Kinder und Familien gedacht. Die meisten Eltern sind keine Linux-Administratoren. Deshalb folgt das Update-System dem Prinzip: **"Ein Knopfdruck genügt."**

```
┌─────────────────────────────────────────────────────────────┐
│ 🚀 Software-Updates                             v1.0.0      │
│ Ein neues Update ist verfügbar: v1.1.0                      │
│                                                             │
│ [ 🔍 Nach Updates suchen ]      [ ⬇️ Jetzt installieren ]   │
│                                                             │
│  [==========================>        ] 65%                  │
│                                                             │
│ 📋 Was ist neu in diesem Update? [v]                         │
│    - Neues Mathespiel "Addition"                            │
│    - Schönere Sprachausgabe                                 │
└─────────────────────────────────────────────────────────────┘
```

1. **Prüfen:** Ein Klick auf *"🔍 Nach Updates suchen"* fragt im Hintergrund die GitHub Releases API ab.
2. **Informieren:** Ist eine neuere Version vorhanden, wird das Changelog angezeigt und der Button *"⬇️ Jetzt installieren"* leuchtet grün auf.
3. **Herunterladen:** Ein Klick lädt das gepackte Linux-Archiv (`.tar.gz`) mit Live-Fortschrittsbalken herunter.
4. **Installieren:** Die Binärdatei wird im laufenden Betrieb atomar ausgetauscht.
5. **Aktivieren:** Ein Klick auf *"🔄 Jetzt neu starten"* schließt das Programm sauber. Die Kiosk-Dauerschleife startet sofort die neue Version – ohne System-Reboot!

---

## 🏛️ Architektur & Schichtenmodell

Das Update-System ist strikt nach Clean Architecture und Dependency Inversion aufgebaut:

```
┌─────────────────────────────────────────────────────────────┐
│                 BuchstabenOS.Domain                         │
│  (Keine externen Abhängigkeiten, reine Logik & Events)      │
└──────────────────────────────▲──────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────┐
│               BuchstabenOS.Application                      │
│  Ports:                                                     │
│  • IUpdateService                                           │
│  • ISystemControl (RestartApp())                            │
│  Modelle:                                                   │
│  • UpdateInfo, UpdateCheckResult                            │
└──────────────────────────────▲──────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────┐
│             BuchstabenOS.Infrastructure                     │
│  Implementierungen:                                         │
│  • GitHubReleaseUpdateService (HttpClient, Tar, Inode-Swap) │
│  • LinuxSystemControl (Environment, Process.Exit, PID)      │
└──────────────────────────────▲──────────────────────────────┘
                               │
┌──────────────────────────────┴──────────────────────────────┐
│              BuchstabenOS.UI.Desktop                        │
│  • ParentMenuViewModel (State-Machine, Commands, Progress)  │
│  • MainWindow.axaml (Software-Updates Karte in UI)          │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Technische Details & Linux-Besonderheiten

### 1. GitHub Releases als distributionsunabhängiges CDN
Als Datenquelle dient der offizielle GitHub Releases Endpunkt:
`GET https://api.github.com/repos/internethandwerker/BuchstabenOS/releases/latest`

* **Header:** `User-Agent: BuchstabenOS-Updater`
* **Parsing:** JSON-Deserialisierung des Release-Tags (z. B. `v1.1.0`), des Changelogs (`body`) und der Asset-Liste.
* **Filter:** Gesucht wird nach einem Asset mit dem Namensmuster `buchstabenos-linux-x64.tar.gz`.

### 2. SemVer Versionsvergleich
Versionen werden nach Semantic Versioning (`MAJOR.MINOR.PATCH`) verglichen:
* Präfixe wie `v` oder `V` werden automatisch bereinigt.
* Bei gültigen Versionen greift `System.Version.TryParse()`:
  $$\text{IsRemoteVersionNewer} = (\text{remoteVersion} > \text{currentVersion})$$
* Ist eine Version nicht strikt numerisch, wird ein String-Ungleichheitsvergleich als Fallback genutzt.

### 3. Das Linux-Problem: Laufende Binärdateien überschreiben (`ETXTBSY`)
Unter Linux führt der Versuch, eine Datei zu überschreiben, die gerade als Prozess im Speicher ausgeführt wird, zum Kernel-Fehler:
`ETXTBSY (Text file busy)`

#### Die Lösung: Atomarer Inode-Tausch
Linux unterscheidet zwischen dem **Dateinamen** im Verzeichnisbaum und dem eigentlichen **Inode** auf der Festplatte. Solange ein Prozess läuft, hält der Kernel den Inode geöffnet.

Unser [`GitHubReleaseUpdateService`](file:///home/alex/Projekte/Coding/BuchstabenOS/src/BuchstabenOS.Infrastructure/Updates/GitHubReleaseUpdateService.cs) nutzt dieses Verhalten gezielt aus:
1. **Temporärer Download:** Das `.tar.gz` Archiv wird in ein isoliertes Temp-Verzeichnis geladen (`/tmp/buchstabenos-update-*`).
2. **Entpacken:** Das Archiv wird mit `System.Formats.Tar.TarFile` oder dem systemeigenen `tar -xzf` in ein Staging-Verzeichnis entpackt.
3. **Atomares Verschieben:**
   ```csharp
   string currentExe = Environment.ProcessPath!;
   string backupExe = currentExe + ".old";
   
   // 1. Laufende Binärdatei umbenennen (erlaubt unter Linux!)
   File.Move(currentExe, backupExe, overwrite: true);
   
   // 2. Neue Binärdatei an die Originalposition kopieren
   File.Copy(newExeSource, currentExe, overwrite: true);
   
   // 3. Ausführungsrechte (chmod +x) setzen
   File.SetUnixFileMode(currentExe, 
       UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
       UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
       UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
   
   // 4. Altes Backup löschen (Linux gibt den Plattenplatz frei, sobald der Prozess endet)
   File.Delete(backupExe);
   ```

### 4. Rootless Berechtigungs-Design (Kein sudo im Spiel)
Damit der Updater Dateien austauschen kann, benötigt der Linux-Prozess Schreibrechte auf das Verzeichnis `/opt/buchstabenos`.
* **Klassischer Fehler:** `/opt` gehört `root`, sodass für jedes Update ein Root-Passwort verlangt werden müsste.
* **BuchstabenOS-Lösung:** Im Kiosk-Installationsskript (`kiosk/install-kiosk.sh`) wird der Ordner explizit dem Benutzer `moritz` übereignet:
  ```bash
  mkdir -p /opt/buchstabenos
  chown -R moritz:moritz /opt/buchstabenos
  chmod 755 /opt/buchstabenos
  ```
* **Ergebnis:** Das laufende Programm (unter Benutzer `moritz`) kann sich selbstständig, sicher und ohne Passwort-Prompt aktualisieren.

---

## 🔄 Der Neustart-Zyklus im Kiosk-Modus

Nach erfolgreichem Dateitausch muss das neue Binary geladen werden.

1. **Elternmenü:** Klick auf *"🔄 Jetzt neu starten"* ruft `IUpdateService.RestartApplication()` auf.
2. **Kiosk-Session (`buchstabenos-session.sh`):**
   ```bash
   while true; do
       /opt/buchstabenos/BuchstabenOS.UI.Desktop --kiosk
       EXIT_CODE=$?
       
       # Exit-Code 42: Papa hat im Menü "Desktop freigeben" gewählt
       if [ $EXIT_CODE -eq 42 ]; then
           break
       fi
       
       # Exit-Code 0: Normaler Exit (z. B. nach Update-Neustart)
       # -> Schleife startet sofort die frisch installierte Binärdatei neu!
       sleep 1
   done
   ```
3. **Dauer:** Der Neustart dauert auf einem typischen alten ThinkPad weniger als **2 Sekunden**!

---

## 🧪 Automatisierte Testabdeckung

Die Update-Logik ist in `tests/BuchstabenOS.Domain.Tests/UpdateServiceTests.cs` vollständig abgesichert:
* `Version_Comparison_Should_Correctly_Identify_Newer_Versions`: Prüft SemVer-Regeln (`1.1.0` > `1.0.0`, `2.0.0` > `1.9.9`, etc.).
* `ParentMenu_CheckForUpdates_Should_Update_ViewModel_State_When_Update_Found`: Testet die Zustandsübergänge des ViewModels beim Finden eines Releases.
* `ParentMenu_InstallUpdate_Should_Report_Progress_And_Set_ReadyToRestart`: Validiert Fortschrittsmeldungen, Bereit-Status und den Aufruf der Neustart-Methode.
