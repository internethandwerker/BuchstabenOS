#!/usr/bin/env python3
"""
Generiert alle vorgerenderten Sprach-Audiodateien für BuchstabenOS mit Piper TTS:
- assets/audio/laute/ (Pädagogisches Lautieren für Vorschulkinder)
- assets/audio/alphabet/ (Klassische Buchstabennamen)
- assets/audio/jingles/ (Feier-Klang bei Worterkennung)
"""

import os
import subprocess
import math
import wave
import struct

PIPER_BIN = os.path.expanduser("~/.local/bin/piper")
MODEL_PATH = os.path.expanduser("~/.local/share/piper/de_DE-thorsten-medium.onnx")

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LAUTE_DIR = os.path.join(BASE_DIR, "assets", "audio", "laute")
ALPHABET_DIR = os.path.join(BASE_DIR, "assets", "audio", "alphabet")
JINGLES_DIR = os.path.join(BASE_DIR, "assets", "audio", "jingles")

os.makedirs(LAUTE_DIR, exist_ok=True)
os.makedirs(ALPHABET_DIR, exist_ok=True)
os.makedirs(JINGLES_DIR, exist_ok=True)

# 1. Pädagogische Laute (Lautieren für Leseanfänger)
LAUTE_MAP = {
    'A': 'A',
    'B': 'b.',
    'C': 'k.',
    'D': 'd.',
    'E': 'E',
    'F': 'Fff.',
    'G': 'g.',
    'H': 'h.',
    'I': 'I',
    'J': 'j.',
    'K': 'k.',
    'L': 'Lll.',
    'M': 'Mmm.',
    'N': 'Nnn.',
    'O': 'O',
    'P': 'p.',
    'Q': 'Qu.',
    'R': 'Rrr.',
    'S': 'Sss.',
    'T': 't.',
    'U': 'U',
    'V': 'Fff.',
    'W': 'Www.',
    'X': 'Ks.',
    'Y': 'Ü',
    'Z': 'Tss.',
    'Ä': 'Ä',
    'Ö': 'Ö',
    'Ü': 'Ü',
    'ß': 'Sss.',
    '0': 'Null',
    '1': 'Eins',
    '2': 'Zwei',
    '3': 'Drei',
    '4': 'Vier',
    '5': 'Fünf',
    '6': 'Sechs',
    '7': 'Sieben',
    '8': 'Acht',
    '9': 'Neun'
}

# 2. Buchstabennamen (Klassisches ABC)
ALPHABET_MAP = {
    'A': 'Ah',
    'B': 'Be',
    'C': 'Tse',
    'D': 'De',
    'E': 'Ee',
    'F': 'Eff',
    'G': 'Ge',
    'H': 'Ha',
    'I': 'I',
    'J': 'Jott',
    'K': 'Ka',
    'L': 'Ell',
    'M': 'Emm',
    'N': 'Enn',
    'O': 'Oh',
    'P': 'Pe',
    'Q': 'Ku',
    'R': 'Err',
    'S': 'Ess',
    'T': 'Te',
    'U': 'U',
    'V': 'Vau',
    'W': 'We',
    'X': 'Iks',
    'Y': 'Ypsilon',
    'Z': 'Zett',
    'Ä': 'Ä',
    'Ö': 'Ö',
    'Ü': 'Ü',
    'ß': 'Eszett',
    '0': 'Null',
    '1': 'Eins',
    '2': 'Zwei',
    '3': 'Drei',
    '4': 'Vier',
    '5': 'Fünf',
    '6': 'Sechs',
    '7': 'Sieben',
    '8': 'Acht',
    '9': 'Neun'
}

def render_with_piper(text, out_path):
    cmd = [
        PIPER_BIN,
        "--model", MODEL_PATH,
        "--output_file", out_path
    ]
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    proc.communicate(input=text.encode('utf-8'))

def generate_chime(file_path):
    """Erzeugt einen warmen, dreistufigen Belohnungs-Akkord (C5, E5, G5)."""
    sample_rate = 44100
    notes = [523.25, 659.25, 783.99] # C5, E5, G5
    note_duration = 0.18 # Sekunden
    total_duration = note_duration * len(notes) + 0.3
    total_samples = int(sample_rate * total_duration)
    
    samples = [0.0] * total_samples
    
    for idx, freq in enumerate(notes):
        start_sample = int(idx * note_duration * sample_rate)
        duration_samples = int(0.45 * sample_rate) # Ausklingdauer
        for i in range(duration_samples):
            if start_sample + i >= total_samples:
                break
            t = i / sample_rate
            # Sanfter Envelope (Decay)
            env = math.exp(-i / (sample_rate * 0.15))
            val = math.sin(2 * math.pi * freq * t) * env
            # Sanfte Obertöne
            val += 0.3 * math.sin(4 * math.pi * freq * t) * env
            samples[start_sample + i] += val * 0.4
            
    with wave.open(file_path, 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(sample_rate)
        data = bytearray()
        for s in samples:
            clamped = max(-1.0, min(1.0, s))
            data += struct.pack('<h', int(clamped * 32767))
        wav.writeframes(data)

print("🎙️ Generiere vorgerenderte Audio-Dateien für BuchstabenOS...")

# 1. Laute
print("➡️ Generiere Laute (Vorschul-Modus: Lautieren)...")
for letter, text in LAUTE_MAP.items():
    dest = os.path.join(LAUTE_DIR, f"{letter}.wav")
    render_with_piper(text, dest)

# 2. Alphabet
print("➡️ Generiere Buchstabennamen (Alphabet)...")
for letter, text in ALPHABET_MAP.items():
    dest = os.path.join(ALPHABET_DIR, f"{letter}.wav")
    render_with_piper(text, dest)

# 3. Jingle
print("➡️ Generiere Belohnungs-Jingle (word_success.wav)...")
generate_chime(os.path.join(JINGLES_DIR, "word_success.wav"))

print("✅ Alle 80+ Audio-Dateien wurden erfolgreich erstellt!")
