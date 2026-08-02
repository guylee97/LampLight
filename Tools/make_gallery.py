#!/usr/bin/env python3
"""map_gallery.txt -> docs/img/mapgen_gallery_l{n}.png

먼저 덤프를 만든다:
  ./Tools/ci_unity.sh -nographics -runTests -testPlatform EditMode \
      -testFilter "MapGalleryExportTests"
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

DUMP = Path(sys.argv[1] if len(sys.argv) > 1 else "../LampLight_CI/QAReports/map_gallery.txt")
OUT = Path("docs/img")

WALL = (60, 68, 74)
FLOOR = (150, 168, 168)
START = (120, 210, 255)
EXIT = (255, 120, 120)
ART = (240, 200, 110)
CELL, GAP, COLS = 4, 10, 6


def load(path):
    maps, cur = [], None
    for line in path.read_text().splitlines():
        if line.startswith("#MAP"):
            d = dict(kv.split("=") for kv in line[5:].split())
            cur = {
                "level": int(d["level"]), "seed": int(d["seed"]),
                "w": int(d["w"]), "h": int(d["h"]), "rooms": int(d["rooms"]),
                "start": tuple(map(int, d["start"].split(","))),
                "exit": tuple(map(int, d["exit"].split(","))),
                "art": [], "grid": [],
            }
            maps.append(cur)
        elif line.startswith("#ART"):
            cur["art"].append(tuple(map(int, line[5:].split(","))))
        elif line:
            cur["grid"].append(line)
    return maps


def sheet(samples, path):
    tw = max(m["w"] for m in samples) * CELL
    th = max(m["h"] for m in samples) * CELL
    rows = (len(samples) + COLS - 1) // COLS
    img = Image.new("RGB", (COLS * (tw + GAP) + GAP, rows * (th + GAP + 14) + GAP), (18, 20, 22))
    d = ImageDraw.Draw(img)

    for i, m in enumerate(samples):
        ox = GAP + (i % COLS) * (tw + GAP)
        oy = GAP + (i // COLS) * (th + GAP + 14)

        for r, line in enumerate(m["grid"]):
            for c, ch in enumerate(line):
                d.rectangle([ox + c * CELL, oy + r * CELL,
                             ox + c * CELL + CELL - 1, oy + r * CELL + CELL - 1],
                            fill=FLOOR if ch == "." else WALL)

        def dot(p, colour, sz=2):
            x, y = p
            d.rectangle([ox + x * CELL - sz, oy + y * CELL - sz,
                         ox + x * CELL + CELL - 1 + sz, oy + y * CELL + CELL - 1 + sz], fill=colour)

        for a in m["art"]:
            dot(a, ART)
        dot(m["start"], START)
        dot(m["exit"], EXIT)
        d.text((ox, oy + th + 2), f'{m["w"]}x{m["h"]}  방{m["rooms"]}  #{m["seed"]}', fill=(150, 158, 165))

    img.save(path)
    return img.size


def main():
    maps = load(DUMP)
    OUT.mkdir(parents=True, exist_ok=True)

    for level in sorted({m["level"] for m in maps}):
        samples = [m for m in maps if m["level"] == level]
        path = OUT / f"mapgen_gallery_l{level}.png"
        print(f"L{level}: {len(samples)}개 -> {path} {sheet(samples, path)}")


if __name__ == "__main__":
    main()
