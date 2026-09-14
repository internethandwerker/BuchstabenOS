# BuchstabenOS 🎈

> **"Ein Betriebssystem, das mit jedem Tastendruck spricht und wächst."**  
> Ein kindgerechtes, ablenkungsfreies Kiosk-Lernspiel für Vorschulkinder (speziell für Moritz entwickelt).

---

## 🎯 Vision & Spielidee

Kinder im Vorschulalter sind fasziniert von Tastaturen. Sie wollen tippen, Knöpfe drücken und Reaktionen sehen. Standard-Betriebssysteme sind jedoch überfordernd, gefährlich für Systemdateien und bieten kein sofortiges auditiv-visuelles Feedback.

**BuchstabenOS** verwandelt einen alten Laptop in ein interaktives Lern-Terminal:
1. **Sofortiges Feedback:** Jeder getippte Buchstabe wird riesig auf dem Bildschirm dargestellt und sofort auf Deutsch ausgesprochen.
2. **Magische Worterkennung:** Tippt Moritz ein echtes deutsches Wort (wie `MAMA`, `AUTO`, `BALL` oder seinen eigenen Namen `MORITZ`), erkennt das System dies und spricht das ganze Wort stolz und feierlich aus.
3. **Fantasiewörter & Leertaste:** Drückt Moritz die Leertaste, versucht das System auch Buchstabensalat oder Fantasiewörter ganzheitlich per Sprachsynthese vorzulesen.
4. **Dynamische Schrift-Choreografie:** Der erste Buchstabe ist gigantisch. Mit jedem weiteren Buchstaben verkleinert sich die Schrift harmonisch, damit alles in eine Zeile passt – erst bei einer Untergrenze bricht der Text sanft um.
5. **Kugelsicherer Kiosk:** Das System startet direkt beim Einschalten des Laptops. Moritz kann nichts kaputt machen. Eltern gelangen nur über eine geheime Tastenkombination und PIN ins System oder ins Einstellungsmenü.

---

## 💻 Ziel-Hardware & Betriebsumgebung

*   **Gerät:** Alter Lenovo Laptop (z. B. ThinkPad T- oder X-Serie, Intel Core 2 Duo / Core i3/i5, 2–4 GB RAM).
*   **Betriebssystem:** **BunsenLabs Linux** (Debian-Basis, extrem leichtgewichtig, minimaler X11-Overhead).
*   **Betriebsmodus:** Exklusiver Vollbild-Kiosk-Modus direkt nach dem Systemstart (ohne Desktop-Ablenkung).
*   **Audio:** Lokale, latenzfreie Audioausgabe (PulseAudio/PipeWire oder ALSA).

---

## 🧱 Kernanforderungen (Version 1.0)

| Anforderung | Beschreibung | Priorität |
| :--- | :--- | :--- |
| **Instant TTS** | Sofortige Aussprache (< 50 ms) des getippten Buchstabens auf Deutsch. | Must-Have |
| **Dynamischer Font-Scaler** | 1. Buchstabe riesig, schrumpft dynamisch mit jedem Zeichen, Zeilenumbruch erst ab Schwellenwert. | Must-Have |
| **Wort-Detektor** | O(1) Erkennung von echten Wörtern aus einem Kinderwörterbuch + automatische Sprachausgabe. | Must-Have |
| **Leertasten-Synthese** | Leertaste liest den aktuellen Puffer (auch Quatschwörter) vor und leert/schließt das Wort ab. | Must-Have |
| **High-Contrast Dark Mode** | Schlichtes, augenschonendes Design: Gedecktes Warmweiß (`#F0EFEA`) auf Tiefschwarz (`#121212`). | Must-Have |
| **Eltern-Schutzschild** | Tastenkombination (`Ctrl+Alt+P` o.ä.) + PIN öffnet Einstellungs- und Systemmenü. | Must-Have |
| **Autostart-Kiosk** | Startet direkt nach Booten ohne sichtbares Linux-Terminal oder Desktop-Leisten. | Must-Have |

---

## 🧭 Navigations-Index der Dokumentation

*   [[01-System-Architektur-DDD|01. System-Architektur & DDD-Design]] – Schichten, Events, Entitäten und Erweiterbarkeit
*   [[02-Technologie-Stack-Entscheidung|02. Technologie-Stack & Sprachauswahl]] – Warum C# / Avalonia UI die beste Wahl ist
*   [[03-OS-Integration-Kiosk-Modus|03. OS-Integration & Kiosk-Modus]] – BunsenLabs, X11-Kiosk, LightDM und Kindsicherung
*   [[04-Sprachausgabe-TTS-und-Audio|04. Sprachausgabe (TTS) & Audio-Pipeline]] – Piper TTS, Pre-rendered WAVs, Latenz & Pädagogik
*   [[05-UI-UX-und-Schrift-Engine|05. UI/UX & Schriftgrößen-Engine]] – Dynamic Font Scaling, Layout-Algorithmus & Eltern-Overlay
*   [[06-Roadmap-und-Erweiterungsplan|06. Roadmap & Zukunftsmodule]] – Von V1 bis Belohnungsvideos, Mathe und Minispielen
