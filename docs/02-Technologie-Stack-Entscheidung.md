# 02. Technologie-Stack & Sprachauswahl ⚙️

> **Entscheidungs-Matrix:** Wie wählen wir die optimale Programmiersprache und das UI-Framework für ein langlebiges, performantes Linux-Projekt auf alter Hardware?

---

## 🔍 Das Anforderungsprofil

1. **Hardware:** Sehr alter Lenovo Laptop (evtl. Core 2 Duo oder alte i3-Generation, 2–4 GB RAM, Intel HD Graphics).
2. **Plattform:** BunsenLabs Linux (Debian-Derivat mit X11).
3. **Latenz:** < 20 ms Reaktionszeit bei Tastenanschlag (visuell) und < 50 ms für Sound.
4. **Schrift-Rendering:** Vektor-Text-Engine, die Glyphen stufenlos und ohne Pixelbildung von 240pt auf 40pt skalieren kann.
5. **Architektur:** Strikte Typisierung, Dependency Injection, SOLID, saubere Trennung von Logik und UI.
6. **Team-Kontext:** Unser Fokus liegt auf **C# / .NET**, pragmatisch und solide als "Internethandwerker".

---

## 📊 Technologie-Vergleich

| Kriterium | **C# / .NET 10 + Avalonia UI** | **Zig + DVUI / raylib-zig** | **Python + Pygame-ce** | **Rust + Slint** | **Web / Electron** |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Performance auf alter CPU** | ⭐⭐⭐⭐⭐ (Skia / Native AOT) | ⭐⭐⭐⭐⭐ (Pure Native / Kein GC) | ⭐⭐⭐ (GIL, CPU-Single-Thread) | ⭐⭐⭐⭐⭐ (Native Binary) | ⭐ (Hoher RAM- & CPU-Hunger) |
| **RAM-Verbrauch** | ~60 – 110 MB | **~5 – 15 MB** (Unschlagbar) | ~50 – 80 MB | ~15 – 30 MB | ~350 – 600 MB |
| **Text-Rendering & Skalierung** | ⭐⭐⭐⭐⭐ (Avalonia `FormattedText` / Skia) | ⭐⭐⭐ (Manuelles FreeType/Raylib) | ⭐⭐ (Bitmaps cachen oder font.render laggend) | ⭐⭐⭐⭐ (Vektororientiert) | ⭐⭐⭐⭐⭐ (CSS Flexbox) |
| **Architektur (SOLID, DDD, DI)** | ⭐⭐⭐⭐⭐ (Benchmark für Clean Code) | ⭐⭐⭐ (Data-Oriented, kein OOP/DI) | ⭐⭐⭐ (Konvention statt Compiler) | ⭐⭐⭐⭐ (Traits, stark, aber rigider) | ⭐⭐⭐ (Oft chaotisch) |
| **Linux X11 & Kiosk-Integration** | ⭐⭐⭐⭐⭐ (Natives X11, kein WM nötig) | ⭐⭐⭐⭐⭐ (X11 / Wayland via C-Lib) | ⭐⭐⭐⭐ (SDL2) | ⭐⭐⭐⭐ (Winit / X11) | ⭐⭐⭐ (Chromium-Flags) |
| **Entwicklungs-Geschwindigkeit** | ⭐⭐⭐⭐⭐ (Alex & Antigravity Heimspiel) | ⭐⭐ (Hoher Low-Level-Aufwand) | ⭐⭐⭐⭐ | ⭐⭐⭐ (Steile Lernkurve) | ⭐⭐⭐⭐ |
| **Ökosystem-Reife (GUI)** | ⭐⭐⭐⭐⭐ (Ausgereiftes Retained UI) | ⭐⭐ (Experimentell / Pre-1.0) | ⭐⭐⭐⭐ (Sehr viel Community-Code) | ⭐⭐⭐⭐ (Kommerziell gestützt) | ⭐⭐⭐⭐⭐ (Milliardenschwer) |

---

## 🏆 Die Entscheidung: C# (.NET 10) mit Avalonia UI

Wir setzen für **BuchstabenOS** auf **C# (.NET 10)** in Kombination mit **Avalonia UI** (mit Skia-Rendering-Backend).

### Warum Avalonia UI?

1. **Skia Graphics Engine:**  
   Avalonia nutzt Google Skia als Render-Backend. Skia läuft sowohl mit Hardware-Beschleunigung (OpenGL) als auch mit extrem performantem **Software-Rendering Fallback** (`--rendering-mode=software`). Sollte der alte Intel-Grafiktreiber auf dem Lenovo Laptop unter Linux fehlerhaftes OpenGL liefern, schaltet Avalonia lautlos auf Software-Rendering um und läuft trotzdem absolut flüssig.
2. **Stufenlose Vektor-Typografie:**  
   Das dynamische Verkleinern von Texten bei jedem Tastendruck (`FormattedText`, `Canvas`, stufenlose `FontSize`) ist in UI-Frameworks wie Avalonia nativ integriert. In Spiele-Engines wie Pygame oder Raylib müsste jede Schriftgröße bei jedem Tastendruck als neue Textur auf der CPU gerendert und zur GPU hochgeladen werden, was bei schnellem Tippen zu Rucklern führt.
3. **Echte Enterprise-Architektur (SOLID & DDD):**  
   C# bietet mit `Microsoft.Extensions.DependencyInjection`, starken Typen, Events und Schnittstellen die perfekte Grundlage für eine modulare Architektur. Wir können die gesamte `BuchstabenOS.Domain` ohne UI-Abhängigkeit programmieren und mit `xUnit` testen.
4. **Standalone Deployment (Single-File):**  
   Mit `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true` erzeugen wir eine einzige, schlanke Binärdatei. Moritz' Laptop benötigt **keine vorinstallierte .NET-Runtime** – wir kopieren einfach die fertige Executable auf den Laptop.
5. **Zukunftssicher:**  
   Avalonia unterstützt über `Avalonia.Labs` Lottie-Animationen (vektorbasierte Zeichentrick-Animationen für Kinderbelohnungen), Audio-Player und über `LibVLCSharp` sogar hochperformante Video-Wiedergabe.

---

---

## ⚡ Tiefen-Analyse: "Zig" als Alternative für BuchstabenOS

**Zig** ist eine moderne Low-Level-Systemprogrammiersprache, die als pragmatischer, sicherer Nachfolger von C konzipiert ist. Wie schlägt sich Zig speziell für ein interaktives Lernspiel auf einem alten Linux-Laptop?

### 1. Die gigantischen Stärken von Zig

*   **Unschlagbare Ressourcen-Effizienz:**  
    Ein in Zig geschriebenes Programm benötigt weder eine Garbage-Collected Runtime (.NET/Java) noch eine virtuelle Maschine. Der RAM-Verbrauch liegt typischerweise bei sensationellen **5 bis 15 MB**. Auf einem 15 Jahre alten Laptop mit z. B. nur 1 GB oder 2 GB RAM ist das konkurrenzlos.
*   **Keine versteckten Latenzen (Zero GC Pauses):**  
    In C# kann der Garbage Collector theoretisch (wenn auch extrem selten bei sauberem Code) für einige Millisekunden pausieren. In Zig gibt es keinen GC – alle Speicherallokationen sind explizit (`std.mem.Allocator`). Tastenanschläge und Audio haben garantierte Echtzeit-Reaktion.
*   **Perfekte C-Interoperabilität (`@cImport`):**  
    Zig kann C-Header direkt ohne Bindings oder Wrapper kompilieren. Linux-Systembibliotheken (X11, ALSA, PulseAudio, Piper C-API) lassen sich nahtlos einbinden.
*   **Audio-Exzellenz mit `zaudio` / `miniaudio`:**  
    Mit der C-Bibliothek `miniaudio` (oder dem idiomatischen Zig-Paket `zaudio`) lässt sich Audio unter Linux mit unübertroffen geringer Latenz (< 5 ms) direkt an ALSA/Pulse übergeben.

### 2. Die Herausforderungen & Nachteile von Zig für BuchstabenOS

*   **Der aktuelle Zustand des GUI-Ökosystems (2024–2026):**  
    Im Gegensatz zu C# (WPF, Avalonia) oder Web/Flutter existiert in Zig noch kein de-facto Standard für ausgereifte, deklarative Desktop-UIs:
    *   **DVUI:** Ein vielversprechendes "Semi-Immediate Mode" GUI-Toolkit für Zig, aber primär für Entwickler-Tools und Debug-UIs gedacht, nicht für fein abgestimmte Typografie-Choreografien.
    *   **raylib-zig:** Ermöglicht das Zeichnen eines Spiele-Fensters. Allerdings muss man das dynamische stufenlose Skalieren von Vektorschriften, Zeilenumbrüche und das Berechnen von Glyphen-Metriken (besonders bei deutschen Umlauten `ä, ö, ü, ß`) weitgehend hardwarenah selbst mit FreeType oder `stb_truetype` orchestrieren.
    *   **Capy / Zylix:** Versuchen native GTK-Bindings herzustellen, sind jedoch noch stark im Wandel und für rahmenlose Vollbild-Kioske auf Openbox/X11 unhandlich.
*   **Architektur: Data-Oriented Design statt DDD / SOLID:**  
    Zig distanziert sich bewusst von klassischer objektorientierter Architektur. Es gibt keine Klassen, keine Vererbung und kein Standard-Dependency-Injection-Framework.  
    *Zwar lässt sich saubere Architektur in Zig über Structs, `union(enum)` und Comptime-Duck-Typing realisieren*, doch klassische Domain-Driven-Design-Muster (Aggregates, Value Objects, Domain Events, Repositories) erfordern in Zig viel handgeschriebenen "Boilerplate"-Code.
*   **Pre-1.0 Entwicklungsstadium (Breaking Changes):**  
    Zig befindet sich aktuell im Zyklus 0.13.x / 0.14.x. Nahezu jedes Release bringt fundamentale Änderungen an der Standardbibliothek (`std`) und dem Build-System (`build.zig`). Für ein Projekt, das in 1–2 Jahren für Moritz schrittweise erweitert werden soll, bedeutet das: Nach einem Compiler-Update baut der Code oft nicht mehr ohne manuelle Refactorings.
*   **Zukunftsausbau (Videos & Animationen):**  
    In Avalonia binden wir Lottie-Animationen oder Videos (Phase 3 der Roadmap) mit einer Zeile XAML (`<Lottie .../>`) ein. In Zig müsste man hierfür FFMPEG- oder LibVLC-Bindings manuell verwalten und Frames in Texturen dekodieren.

### 3. Fazit & Empfehlung zu Zig

| Szenario | Empfehlung |
| :--- | :--- |
| **Wenn der Laptop < 512 MB RAM hätte oder Bare-Metal ohne OS liefe:** | **Zig wäre die absolute Nr. 1.** |
| **Für BunsenLabs Linux (2–4 GB RAM) mit Fokus auf Typografie, Kiosk, DDD & schneller Erweiterbarkeit:** | **C# / Avalonia UI bleibt der pragmatische Testsieger.** |

> 💡 **Option für Perfektionisten:**  
> Sollte der alte Laptop bei der Audio-Wiedergabe oder beim Abfangen von Sondertasten jemals Performance-Probleme zeigen, können wir diese kritische Komponente als winzige C-kompatible Shared Library (`.so`) in Zig schreiben und via P/Invoke direkt in unsere C#-Architektur einklinken!

---

## 🎧 Audio-Stack: NAudio / LibSoundIO / PulseAudio-Adapter

Unter Linux binden wir Audio wie folgt an:
*   Für die **Aussprache einzelner Buchstaben** (Latenz < 10 ms):  
    Ein leichtgewichtiger C#-Audio-Player (z. B. via `ManagedBass` oder einfachem Aufruf von `aplay` / `paplay` im Hintergrund-Worker, bzw. `Silk.NET.OpenAL` oder `miniaudio`).
*   Für **dynamische Worterkennung & Fantasiewörter**:  
    Die neuronale Sprachsynthese **Piper TTS** (läuft lokal als native Binärdatei und generiert Audio-Streams in Rekordzeit).

