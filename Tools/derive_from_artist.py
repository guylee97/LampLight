import argparse
import contextlib
import os
import subprocess
import sys
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC_DIR = os.path.join(ROOT, "Audio", "Incoming")
DST_DIR = os.path.join(ROOT, "Assets", "Resources", "Sounds")

RATE = 44100
PEAK_CEILING = 0.95
MUFFLED_CUTOFF_HZ = 700.0
MUFFLED_GAIN_DB = -7.0
HIGHPASS_HZ = 70.0

JOBS = [
    ("moster growl (1).wav", "walker_breath", None, -24.0, True),
    ("moster growl (2).wav", "wanderer_alert", None, -19.0, True),
    ("moster growl (3).wav", "runner_pass", None, -19.0, True),
    ("moster growl (4).mp3", "death_contact", None, -14.0, False),
    ("moster growl (3).wav", "runner_hit", (0.0, 0.45), -16.0, True),
    ("Open the door (1).wav", "exit_unlock", None, -18.0, False),
    ("Open the door (2).wav", "door_creak", None, -20.0, False),
    ("Background horror laughter sound.wav", "ambient_temple", None, -7.0, False),
    ("Normal walking sound.wav", "walker_step", (3.30, 3.70), -22.0, True),
    ("Normal walking sound.wav", "wanderer_step", (6.26, 6.60), -21.0, True),
    ("Stepping on a stone sound (1).wav", "stone_land", (0.30, 0.75), -17.0, False),
    ("glass stepping sound (2).wav", "stone_throw", (0.05, 0.35), -20.0, False),
]


def decode(path, tmp_dir):
    out = os.path.join(tmp_dir, os.path.basename(path).replace(" ", "_") + ".pcm.wav")
    result = subprocess.run(
        ["afconvert", "-f", "WAVE", "-d", f"LEI16@{RATE}", "-c", "1", path, out],
        capture_output=True)

    if result.returncode != 0 or not os.path.exists(out):
        return None

    with contextlib.closing(wave.open(out)) as handle:
        raw = handle.readframes(handle.getnframes())

    return np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768.0


def highpass(x, cutoff_hz, rate):
    if x.size < 8:
        return x

    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / rate)
    ratio = (freqs / cutoff_hz) ** 4
    rolloff = ratio / (1.0 + ratio)
    rolloff[freqs < cutoff_hz * 0.55] = 0.0
    return np.fft.irfft(spectrum * rolloff, n=len(x)).astype(np.float32)


def lowpass(x, cutoff_hz):
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / RATE)
    rolloff = 1.0 / (1.0 + (freqs / cutoff_hz) ** 4)
    return np.fft.irfft(spectrum * rolloff, n=len(x)).astype(np.float32)


def soften(x, drive):
    if drive <= 1.0:
        return x

    return np.tanh(x * drive) / np.tanh(drive)


def normalize(x, target_rms_db):
    rms = float(np.sqrt(np.mean(x ** 2)))
    if rms < 1e-6:
        return x

    gain = (10.0 ** (target_rms_db / 20.0)) / rms
    peak = float(np.abs(x).max()) * gain
    if peak > PEAK_CEILING:
        gain *= PEAK_CEILING / peak

    return x * gain


def shape(x, span):
    if span is not None:
        start = int(span[0] * RATE)
        end = min(len(x), int(span[1] * RATE))
        x = x[start:end]

    if x.size == 0:
        return x

    fade = min(int(0.02 * RATE), x.size // 2)
    if fade > 0:
        x = x.copy()
        x[:fade] *= np.linspace(0.0, 1.0, fade)
        x[-fade:] *= np.linspace(1.0, 0.0, fade)

    return x


def write_wav(path, samples):
    pcm = (np.clip(samples, -1.0, 1.0) * 32767.0).astype("<i2").tobytes()
    with contextlib.closing(wave.open(path, "wb")) as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(pcm)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dst", default=DST_DIR)
    parser.add_argument("--tmp", default="/tmp")
    args = parser.parse_args()

    os.makedirs(args.dst, exist_ok=True)
    made = 0

    for source, stem, span, target_db, with_muffled in JOBS:
        path = os.path.join(SRC_DIR, source)
        if not os.path.exists(path):
            print(f"  ! 원본 없음: {source}", file=sys.stderr)
            continue

        raw = decode(path, args.tmp)
        if raw is None:
            print(f"  ! 디코드 실패: {source}", file=sys.stderr)
            continue

        shaped = highpass(shape(raw, span), HIGHPASS_HZ, RATE)
        if stem == "ambient_temple":
            shaped = soften(normalize(shaped, -12.0), 3.5)

        clear = normalize(shaped, target_db)
        if clear.size == 0:
            print(f"  ! 빈 구간: {source} {span}", file=sys.stderr)
            continue

        write_wav(os.path.join(args.dst, f"{stem}.wav"), clear)
        made += 1
        line = f"  {source[:34]:36s} -> {stem}.wav  {clear.size / RATE:.2f}s"

        if with_muffled:
            muffled = normalize(lowpass(clear, MUFFLED_CUTOFF_HZ), target_db + MUFFLED_GAIN_DB)
            write_wav(os.path.join(args.dst, f"{stem}_muffled.wav"), muffled)
            made += 1
            line += f" + {stem}_muffled.wav"

        print(line)

    print(f"총 {made}개 생성 -> {args.dst}")
    return 0 if made else 2


if __name__ == "__main__":
    sys.exit(main())
