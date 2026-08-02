#!/usr/bin/env python3
"""walldeco 의 벽돌 결에 맞춰 벽 오토타일 17장을 다시 그린다.

색과 벽돌 단 높이는 obj_walldeco_plain.png 에서 가져오고,
테두리 규칙은 기존 타일에서 실측한 것을 그대로 따른다.

  노출된 N/E/W -> 밝은 립      (이웃이 바닥)
  노출된 S     -> 어두운 그림자선
  벽과 맞닿은 면 -> 그냥 벽돌이 이어짐
"""
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TILES = ROOT / "Assets/Art/Temple/Tiles"
SIZE = 32

MORTAR = (12, 18, 20)
FACE = (41, 51, 52)
FACE_DARK = (31, 41, 40)
FACE_LIGHT = (51, 62, 64)
TOP_LIGHT = (60, 73, 74)
BOTTOM_DARK = (18, 24, 26)
RIM = (67, 80, 81)
RIM_SOFT = (51, 62, 64)
BASE_SHADOW = (7, 11, 17)

# obj_walldeco_plain.png 실측: 단 높이 중앙값 6 (5~7), 벽돌 폭 중앙값 17 (11~35)
COURSES = [6, 5, 7, 6, 5, 3]

# 가로로 이어붙여도 이음새가 없도록 각 줄의 벽돌 길이 합을 32 로 맞춘다.
PARTITIONS = [
    [16, 16], [32], [17, 15], [20, 12], [12, 20], [15, 17], [32], [18, 14],
]

FACES = [
    (41, 51, 52), (31, 41, 40), (51, 62, 64), (37, 47, 48),
    (45, 55, 56), (35, 44, 45),
]
NAMES = [
    "00_open", "01_n", "02_e", "03_ne", "04_s", "05_ns", "06_es", "07_nes",
    "08_w", "09_nw", "10_ew", "11_new", "12_sw", "13_nsw", "14_esw", "15_nesw",
]


def brick_base():
    img = Image.new("RGB", (SIZE, SIZE), MORTAR)
    px = img.load()

    y = 0
    for course, height in enumerate(COURSES):
        lengths = PARTITIONS[(course * 5 + 3) % len(PARTITIONS)]
        shift = (course * 7) % SIZE

        x = 0
        for index, length in enumerate(lengths):
            face = FACES[(course * 3 + index * 2) % len(FACES)]

            for dx in range(length):
                column = (x + dx + shift) % SIZE

                for row in range(height):
                    if dx == 0 or row == height - 1:
                        colour = MORTAR
                    elif row == 0 and dx < length - 1:
                        colour = TOP_LIGHT if index % 2 == 0 else FACE_LIGHT
                    elif row == height - 2 and dx > 1:
                        colour = BOTTOM_DARK
                    else:
                        colour = face

                    px[column, (y + row) % SIZE] = colour

            x += length

        y += height

    # 결이 죽지 않게 낟알을 조금 뿌린다.
    for seed in range(18):
        gx = (seed * 13 + 5) % SIZE
        gy = (seed * 21 + 11) % SIZE
        r, g, b = px[gx, gy]
        if r < 20:
            continue
        step = 6 if seed % 2 else -6
        px[gx, gy] = (max(0, r + step), max(0, g + step), max(0, b + step))

    return img


def with_rims(base, mask):
    img = base.copy()
    px = img.load()
    north, east, south, west = (mask & 1, mask & 2, mask & 4, mask & 8)

    if not north:
        for x in range(SIZE):
            px[x, 0] = RIM
            px[x, 1] = RIM_SOFT

    if not west:
        for y in range(SIZE):
            px[0, y] = RIM
            px[1, y] = RIM_SOFT

    if not east:
        for y in range(SIZE):
            px[SIZE - 1, y] = RIM
            px[SIZE - 2, y] = RIM_SOFT

    if not south:
        for x in range(SIZE):
            px[x, SIZE - 1] = BASE_SHADOW
            px[x, SIZE - 2] = BOTTOM_DARK

    return img


def main():
    if not TILES.exists():
        sys.exit(f"{TILES} 없음")

    base = brick_base()
    base.save(TILES / "tile_01_wall_solid.png")
    written = 1

    for mask, name in enumerate(NAMES):
        path = TILES / f"tile_{8 + mask:02d}_wall_edge_{name}.png"
        if not path.exists():
            print(f"건너뜀 (원본 없음): {path.name}")
            continue

        with_rims(base, mask).save(path)
        written += 1

    print(f"벽 타일 {written}장 다시 그림 -> {TILES.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
