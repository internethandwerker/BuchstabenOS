# 08. Git- & Release-Handbuch für Alex 🛠️

> **Dein praktischer Leitfaden:** Wie du neue Funktionen entwickelst, saubere Git-Commits erstellst, Versionen taggst und auf Knopfdruck ein neues Release für Moritz' Laptop und die Community veröffentlichst.

---

## 📌 Schnellübersicht: In 4 Schritten zum Release

Wenn du ein neues Feature fertiggestellt hast und es als Update bereitstellen möchtest:

```bash
# 1. Tests & Build prüfen
dotnet test

# 2. Änderungen committen und auf GitHub pushen
git add .
git commit --no-gpg-sign -m "feat(math): add multiplication mini game"
git push origin main

# 3. Release bauen & automatisch auf GitHub veröffentlichen
bash scripts/build-release.sh v1.1.0 --publish

# 4. Fertig! 
# Moritz' Laptop kann das Update jetzt im Eltern-Menü mit 1 Klick installieren.
```

---

## 🌿 Git-Grundregeln für BuchstabenOS

Wir halten unseren Git-Workflow pragmatisch, sauber und nachvollziehbar:

### 1. Unser Branch-Modell: Alles auf `main`
* Unser Haupt- und Produktions-Branch heißt **`main`**.
* Für kleine bis mittlere Änderungen arbeiten wir direkt auf `main` oder mergen kurzlebige Feature-Branches dorthin.
* Vor jedem Release muss `main` grün (`dotnet test` = 0 Fehler) sein.

### 2. Commit-Nachrichten (Conventional Commits)
Gute Commit-Nachrichten machen die Historie lesbar und erleichtern das spätere Changelog:

| Typ | Zweck | Beispiel |
| :--- | :--- | :--- |
| `feat:` | Ein neues Feature | `feat(math): add spoken templates for addition` |
| `fix:` | Ein Bugfix | `fix(tts): prevent overlapping speech prompts` |
| `docs:` | Nur Dokumentation | `docs: add git release handbook for alex` |
| `style:` | Formatierung, UI-Kosmetik | `style(ui): adjust font contrast in parent menu` |
| `refactor:` | Code-Umbau ohne Verhaltensänderung | `refactor(audio): decouple speech queue channel` |
| `test:` | Hinzufügen/Anpassen von Tests | `test: add unit tests for update semver compare` |

> 💡 **Wichtig:** Verwende immer das Flag `--no-gpg-sign` beim Committen, falls du keinen GPG-Key im Terminal konfiguriert hast:
> ```bash
> git commit --no-gpg-sign -m "feat: deine nachricht"
> ```

---

## 🏷️ Versionsnummern (Semantic Versioning)

Wir nutzen das Standard-Schema **`MAJOR.MINOR.PATCH`** (z. B. `v1.2.3`):

$$\underbrace{1}_{\text{Major}}.\underbrace{2}_{\text{Minor}}.\underbrace{3}_{\text{Patch}}$$

* **MAJOR (1.x.x $\rightarrow$ 2.0.0):**  
  Grundlegende Neuausrichtung oder bahnbrechende Meilensteine (z. B. Phase 3: Belohnungs-Videos, großes UI-Redesign, neuer Unterbau).
* **MINOR (1.0.x $\rightarrow$ 1.1.0):**  
  Neue Spiele oder große Features (z. B. neues Mathespiel, neue Sprech-Features, neue Spiele-Module).
* **PATCH (1.0.0 $\rightarrow$ 1.0.1):**  
  Fehlerbehebungen, Audio-Feinjustierungen, kleine UI-Korrekturen, neue Wörter im Standard-Wörterbuch.

> ⚠️ **Versionsnummer im Code anpassen:**  
> Vor dem Release-Bau passt du die Version in `src/BuchstabenOS.UI.Desktop/App.axaml.cs` an:
> ```csharp
> CurrentVersion = "1.1.0";
> ```

---

## 🚀 Release erstellen: Die 2 Wege

Unser Skript [`scripts/build-release.sh`](file:///home/alex/Projekte/Coding/BuchstabenOS/scripts/build-release.sh) kompiliert ein eigenständiges (self-contained), einzelnes Linux-Binary und packt es in `releases/buchstabenos-linux-x64.tar.gz`.

### Weg A: Vollautomatisch mit der GitHub CLI (Empfohlen!)

Da du `gh` auf deinem System eingerichtet hast, geht alles in einem Befehl:

```bash
bash scripts/build-release.sh v1.1.0 --publish
```

**Was das Skript für dich tut:**
1. Es kompiliert das Projekt im Release-Modus für `linux-x64` (`PublishSingleFile=true`).
2. Es schnürt `releases/buchstabenos-linux-x64.tar.gz`.
3. Es berechnet die SHA-256-Checksumme in `buchstabenos-linux-x64.tar.gz.sha256`.
4. Es erstellt auf GitHub den Git-Tag `v1.1.0`.
5. Es lädt das Archiv und die Checksumme direkt in das GitHub-Release hoch.

---

### Weg B: Manuell über das GitHub Web-Interface

Falls du ein Release lieber über den Browser mit formatierten Release Notes erstellen möchtest:

1. **Paket lokal bauen (ohne `--publish`):**
   ```bash
   bash scripts/build-release.sh v1.1.0
   ```
   * Erzeugt das Archiv unter: `releases/buchstabenos-linux-x64.tar.gz`

2. **Auf GitHub surfen:**
   * Gehe zu: [github.com/internethandwerker/BuchstabenOS/releases](https://github.com/internethandwerker/BuchstabenOS/releases)
   * Klicke auf **"Draft a new release"**.

3. **Release ausfüllen:**
   * **Tag version:** `v1.1.0` (neu erstellen auf `main`)
   * **Release title:** `BuchstabenOS v1.1.0 – Mathe-Addition & Updates`
   * **Description:** Schreibe eine kurze Zusammenfassung für Eltern (z. B. Stichpunkte, was neu ist).
   * **Binaries anhängen:** Ziehe per Drag & Drop die beiden Dateien aus deinem `releases/`-Ordner in das Upload-Feld:
     - `buchstabenos-linux-x64.tar.gz`
     - `buchstabenos-linux-x64.tar.gz.sha256`

4. Klicke auf **"Publish release"**.

---

## 🧪 So testest du den Update-Loop in der Praxis

Um das Zusammenspiel zwischen GitHub und Moritz' Laptop selbst zu überprüfen:

1. **Ausgangslage:**  
   Auf Moritz' Laptop läuft Version `v1.0.0`.
2. **Neues Release erstellen:**  
   Du baust und veröffentlichst z. B. `v1.0.1` auf GitHub.
3. **Auf Moritz' Laptop:**  
   * Drücke `Strg + Alt + Shift + P` $\rightarrow$ PIN `1337`.
   * Scrolle zu **"💻 Systemeinstellungen"** $\rightarrow$ **"🚀 Software-Updates"**.
   * Klicke auf **"🔍 Nach Updates suchen"**.
   * **Ergebnis:** Das System meldet sofort: *"Neues Update verfügbar: v1.0.1"* und blendet das Changelog ein!
   * Klicke auf **"⬇️ Jetzt installieren"** $\rightarrow$ Der grüne Fortschrittsbalken lädt das Update herunter und entpackt es.
   * Klicke auf **"🔄 Jetzt neu starten"** $\rightarrow$ Nach 1-2 Sekunden startet die neue Version und zeigt im Elternmenü stolz `v1.0.1`!

---

## 🚨 Notfall-Rollback / Hotfix-Verfahren

Was tun, wenn sich in einem veröffentlichten Release ein unerwarteter Fehler eingeschlichen hat?

### Szenario 1: Schneller Hotfix
1. Fehler auf `main` beheben.
2. Version hochzählen (z. B. von `v1.1.0` auf `v1.1.1`).
3. Release bauen & veröffentlichen:
   ```bash
   bash scripts/build-release.sh v1.1.1 --publish
   ```
4. Die Laptops aktualisieren sich beim nächsten Klick einfach auf `v1.1.1`.

### Szenario 2: Release auf GitHub zurückziehen
1. Öffne das fehlerhafte Release auf GitHub und markiere es als **Pre-release** oder lösche das Release (bzw. nimm das Asset `buchstabenos-linux-x64.tar.gz` heraus).
2. Der Update-Checker ignoriert Releases ohne passendes Asset automatisch.

---

## 💡 Nützliche Git-Befehle im Alltag

```bash
# Status prüfen (geänderte und neue Dateien)
git status

# Schöne einzeilige Historie ansehen
git log --oneline -n 10

# Ungespeicherte Änderungen einer Datei verwerfen
git restore dateiname

# Lokale Commits zu GitHub übertragen
git push origin main

# Neueste Änderungen von GitHub holen
git pull origin main
```
