# 05. UI/UX & Schriftgrößen-Engine 🎨

> **Das visuelle Erlebnis:** Wie fühlt sich BuchstabenOS für ein 4–6 jähriges Kind an?  
> Riesige, klare Buchstaben, kein Menü-Wirrwarr, maximale visuelle Ruhe und eine magische, stufenlose Schriftgrößen-Choreografie.

---

## 🔤 Die dynamische Font-Skalierungs-Engine

Ein Kernfeature von BuchstabenOS ist das proportionale Schrumpfen der Schrift, damit Moritz die Entstehung eines Wortes als geschlossene visuelle Einheit wahrnimmt:

```
[       A       ]   <- 1 Buchstabe: Riesig zentriert (z.B. 240 pt)
[     A U T     ]   <- 3 Buchstaben: Schrumpft proportional (z.B. 160 pt)
[   A U T O S   ]   <- 5 Buchstaben: Passt perfekt in eine Zeile (z.B. 110 pt)
[ MORITZ LIEBT..]   <- Viele Zeichen: Bis Min-Grenze (z.B. 48 pt)
[ MORITZ LIEBT  ]   <- Ab Unterschreiten von 48 pt:
[ EINEN BAGGER  ]      Sanfter Zeilenumbruch (Multi-Line-Modus)!
```

### Der Skalierungs-Algorithmus (Mathematisches Modell)

Gegeben sei:
*   $W_{\text{screen}}, H_{\text{screen}}$: Bildschirmauflösung (z. B. $1366 \times 768$ Pixel).
*   $W_{\text{max}} = W_{\text{screen}} \times 0.85$: Maximale Nutzbreite mit kindgerechtem Rand.
*   $FS_{\text{max}} = \min(H_{\text{screen}} \times 0.50, 260\text{ pt})$: Maximale Schriftgröße für den ersten Buchstaben.
*   $FS_{\text{min}} = 48\text{ pt}$: Schwellenwert für Zeilenumbruch.
*   $L$: Länge der aktuellen Zeile (Zeichenanzahl).
*   $C_{\text{glyph}} \approx 0.62$: Durchschnittliches Breiten-/Höhenverhältnis der Glyphen im gewählten Zeichensatz.

**Formel zur Größenberechnung:**
$$\text{FontSize}_{\text{ideal}} = \min \left( FS_{\text{max}}, \frac{W_{\text{max}}}{L \cdot C_{\text{glyph}}} \right)$$

*   **Bedingung 1 ($L = 1$):** Ergibt $FS_{\text{max}}$. Der Buchstabe thront riesig im Zentrum des Bildschirms.
*   **Bedingung 2 ($\text{FontSize}_{\text{ideal}} \ge FS_{\text{min}}$):** Alle Zeichen verbleiben in einer einzigen Zeile.
*   **Bedingung 3 ($\text{FontSize}_{\text{ideal}} < FS_{\text{min}}$):** Der `WordWrap`-Modus wird aktiviert. Die Schrift bleibt bei $48\text{ pt}$ und bricht an Wortgrenzen sanft in die nächste Zeile um.

### 📜 Die vertikal zentrierte Schreib-Bühne & verblassender Verlauf

Damit Moritz' Blick nicht im Bildschirm umherspringen muss, bleibt die **aktive Schreibzeile immer vertikal in der Mitte**:

```
+-------------------------------------------------------------+
|  (Fast unsichtbar: Opacity 0.05)                            |
|  (Alte Zeile 3: Opacity 0.12)                               |
|  (Alte Zeile 2: Opacity 0.28)                               |
|  (Alte Zeile 1: Opacity 0.55)                               |
|                                                             |
| >>>               M O R I T Z   T I P P T               <<< | <- MITTE (100% Opacity)
|                                                             |
|                                                             |
+-------------------------------------------------------------+
```

*   **Verlaufshistorie nach oben:**  
    Beim Druck auf `Enter` oder wenn ein Zeilenumbruch erfolgt, gleitet der bisherige Text nach oben.
*   **Fading ins Tiefschwarz:**  
    Ältere Zeilen verblassen schrittweise (`Opacity = 0.55 -> 0.28 -> 0.12 -> 0.05`), bis sie am oberen Bildschirmrand nahtlos im Hintergrund verschwinden. Der Fokus bleibt immer ruhig in der Mitte.

### ⌨️ Native Tastatur-Anbindung (`TextInput`)
Anstelle starrer Key-Code-Mappings nutzt BuchstabenOS die nativen `TextInput`-Events des Fenstersystems:
*   **Volle Unterstützung für deutsche Umlaute:** `Ä`, `Ö`, `Ü`, `ß` sowie Sonderzeichen werden direkt durch das X11/Wayland-Layout verarbeitet.
*   **Steuertasten:** Steuertasten (`Backspace`, `Space`, `Enter`, Eltern-Shortcut `Strg+Alt+Shift+P`) werden präzise im `KeyDown`-Event abgefangen.

---

## 👁️ Farbwelt & Typografie (Augenschonend)

### Farbschema Version 1.0: "Midnight Chalkboard"
Vermeidet grelles Blendlicht und schützt die Augen bei dämmrigem Licht:

*   **Hintergrund:** Tiefes Mattschwarz / Anthrazit (`#121316`).
*   **Schriftfarbe:** Gedecktes, warmes Elfenbeinweiß (`#F5F4EE`).
*   **Akzentfarbe (Wort erkannt):** Sanftes, feierliches Goldgelb (`#FFD166`) mit dezentem Leuchteffekt für 1,5 Sekunden.

### Schriftart-Auswahl (Schulausgangsschrift-nah)
Für Vorschulkinder sind Zierschriften oder Standard-Systemschriften (wie Arial mit zweistöckigem kleinem `a` oder geschwungenem `g`) ungeeignet.
*   **Empfehlung:** **`Andika`** oder **`Lexend`** (Open Source / SIL Open Font License).
    *   Klar unterscheidbare Formen (keine Verwechslung von großem `I`, kleiner Zahl `1` und kleinem `l`).
    *   Große Punzen (Innenräume der Buchstaben) für optimale Lesbarkeit.

---

## 🛡️ Das Eltern-Overlay (Verstecktes Menü)

Das Elternmenü ist für Moritz komplett unsichtbar und kann nicht versehentlich ausgelöst werden.

### Aktivierung
*   **Tastenkombination:** `Strg + Alt + Shift + P` (oder `Super + Escape`)
*   Es erscheint ein schlichtes, zentriertes Dialogfeld: **"PIN eingeben"**.
*   Standard-PIN: `1337` (oder frei konfigurierbar).

### Funktionsumfang des Eltern-Overlays
```
+-------------------------------------------------------+
|                 ⚙️ BuchstabenOS Elternmenü            |
+-------------------------------------------------------+
|                                                       |
|  Lautstärke:       [ - ]  ==========O===  [ + ] (75%) |
|                                                       |
|  Sprachmodus:      (•) Lautieren (Phoneme: M, A, P)   |
|                    ( ) Alphabet (Buchstaben: Em, Ah)  |
|                                                       |
|  Sprechtempo:      [ Normal ]                         |
|                                                       |
|  Wörterbuch:       128 Kinderwörter geladen           |
|                    [ + Eigenes Wort hinzufügen ]      |
|                                                       |
|  ---------------------------------------------------  |
|  [ Zurück zum Spiel ]  [ Desktop (Linux) ]  [ Ausschalten ] |
+-------------------------------------------------------+
```

1.  **Zurück zum Spiel:** Schließt das Menü (`Escape`).
2.  **Desktop (Linux):** Beendet die Kiosk-App mit Return-Code 42 $\rightarrow$ X11 Kiosk schließt sich, normaler BunsenLabs Desktop für Papa startet.
3.  **Ausschalten:** Fährt den Laptop sicher per Systemd-Shutdown herunter.
