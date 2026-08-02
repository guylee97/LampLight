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

TARGET_RATE = 44100
HOP = 512

JOBS = [
    ("Walking quietly sound.wav", "step_sneak", 4, 0.34, -30.0),
    ("Normal walking sound.wav", "step_walk", 4, 0.40, -23.0),
    ("Stepping on a stone sound (1).wav", "step_noisy_floor", 3, 0.36, -18.0),
    ("glass stepping sound (1).wav", "step_glass", 3, 0.30, -16.0),
    ("Running sound.wav", "step_run", 4, 0.30, -14.0),
]

PEAK_CEILING = 0.95
HIGHPASS_HZ = 110.0


def decode(path, tmp_dir):
    out = os.path.join(tmp_dir, os.path.basename(path).replace(" ", "_") + ".pcm.wav")
    result = subprocess.run(
        ["afconvert", "-f", "WAVE", "-d", f"LEI16@{TARGET_RATE}", "-c", "1", path, out],
        capture_output=True)

    if result.returncode != 0 or not os.path.exists(out):
        return None

    with contextlib.closing(wave.open(out)) as handle:
        raw = handle.readframes(handle.getnframes())

    return np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768.0


def envelope(x):
    count = len(x) // HOP
    return np.array([np.sqrt(np.mean(x[i * HOP:(i + 1) * HOP] ** 2)) for i in range(count)])


def find_onsets(x, min_gap):
    env = envelope(x)
    if env.size == 0 or env.max() <= 0:
        return []

    norm = env / env.max()
    threshold = max(0.15, float(np.percentile(norm, 70)))

    picked = []
    for i in range(2, len(norm) - 2):
        if norm[i] < threshold or norm[i] < norm[i - 1] or norm[i] <= norm[i + 1]:
            continue
        if norm[i] <= norm[i - 2] * 1.5:
            continue
        if picked and (i - picked[-1]) * HOP / TARGET_RATE < min_gap:
            continue
        picked.append(i)

    return [p * HOP / TARGET_RATE for p in picked]


def carve(x, start_sec, length_sec, target_rms_db):
    pre = int(0.012 * TARGET_RATE)
    start = max(0, int(start_sec * TARGET_RATE) - pre)
    end = min(len(x), start + int(length_sec * TARGET_RATE))

    chunk = x[start:end].copy()
    if chunk.size < TARGET_RATE // 50:
        return None

    fade_in = min(64, chunk.size)
    fade_out = min(int(0.05 * TARGET_RATE), chunk.size)
    chunk[:fade_in] *= np.linspace(0.0, 1.0, fade_in)
    chunk[-fade_out:] *= np.linspace(1.0, 0.0, fade_out)

    chunk = highpass(chunk, HIGHPASS_HZ, TARGET_RATE)

    rms = float(np.sqrt(np.mean(chunk ** 2)))
    if rms < 0.002:
        return None

    gain = (10.0 ** (target_rms_db / 20.0)) / rms
    peak = float(np.abs(chunk).max()) * gain
    if peak > PEAK_CEILING:
        gain *= PEAK_CEILING / peak

    return chunk * gain


def highpass(x, cutoff_hz, rate):
    if x.size < 8:
        return x

    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / rate)
    ratio = (freqs / cutoff_hz) ** 4
    rolloff = ratio / (1.0 + ratio)
    rolloff[freqs < cutoff_hz * 0.55] = 0.0
    return np.fft.irfft(spectrum * rolloff, n=len(x)).astype(np.float32)


def write_wav(path, samples):
    data = np.clip(samples, -1.0, 1.0)
    pcm = (data * 32767.0).astype("<i2").tobytes()

    with contextlib.closing(wave.open(path, "wb")) as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(TARGET_RATE)
        handle.writeframes(pcm)


def run_job(source, stem, count, length, target_rms_db, tmp_dir, dst_dir):
    path = os.path.join(SRC_DIR, source)
    if not os.path.exists(path):
        print(f"  ! 원본 없음: {source}", file=sys.stderr)
        return 0

    x = decode(path, tmp_dir)
    if x is None:
        print(f"  ! 디코드 실패: {source}", file=sys.stderr)
        return 0

    hits = find_onsets(x, length * 0.8)
    if not hits:
        print(f"  ! 타격을 못 찾음: {source}", file=sys.stderr)
        return 0

    scored = []
    for start in hits:
        chunk = carve(x, start, length, target_rms_db)
        if chunk is not None:
            scored.append((float(np.sqrt(np.mean(chunk ** 2))), start, chunk))

    scored.sort(key=lambda item: item[0], reverse=True)
    picked = scored[:count]
    picked.sort(key=lambda item: item[1])

    written = 0
    for index, (_, start, chunk) in enumerate(picked):
        name = stem if index == 0 else f"{stem}_{index + 1}"
        write_wav(os.path.join(dst_dir, f"{name}.wav"), chunk)
        written += 1

    print(f"  {source[:36]:38s} 타격 {len(hits):3d} -> {written}개 ({stem})")
    return written


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dst", default=DST_DIR)
    parser.add_argument("--tmp", default="/tmp")
    args = parser.parse_args()

    os.makedirs(args.dst, exist_ok=True)

    total = 0
    for source, stem, count, length, target_rms_db in JOBS:
        total += run_job(source, stem, count, length, target_rms_db, args.tmp, args.dst)

    print(f"총 {total}개 생성 -> {args.dst}")
    return 0 if total else 2


if __name__ == "__main__":
    sys.exit(main())
