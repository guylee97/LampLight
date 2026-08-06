"""맵의 decorations 를 읽어 collision 배열을 다시 굽는다.

판정 규칙과 에셋 분류는 기획이 준 통과판정 문서에서 가져오고, 위치는 게임이
실제로 렌더하는 decorations 에서 읽는다. 그래서 보이는 그림과 판정이 어긋나지
않는다. 데코를 옮기거나 추가한 뒤 다시 돌리면 된다.

    python3 Tools/bake_collision.py <collision_map.json>
"""

import json
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "Assets", "Resources", "Data")
ART = os.path.join(ROOT, "Assets", "Resources", "Art", "Objects")

WALK, BLOCK, NOISE, MUFFLED = 0, 1, 2, 3
RANK = {BLOCK: 3, NOISE: 2, MUFFLED: 1, WALK: 0}
CODE = {"walk": WALK, "block": BLOCK, "noise": NOISE, "muffled": MUFFLED}

DEBRIS_DISPLAY_SCALE = 0.7


def load_rules(path):
    with open(path, encoding="utf-8") as handle:
        doc = json.load(handle)
    rule = doc["footprint_rule"]
    return doc["assets"], rule["block_threshold"], rule["noise_threshold"], rule["alpha_min"]


def find_asset(assets, key):
    for name in (f"obj_{key}.png", f"{key}.png", f"extra_{key}.png"):
        if name in assets:
            return assets[name]
    return None


def sprite_path(resource):
    return os.path.join(ROOT, "Assets", "Resources", resource + ".png")


def opaque_pixels(resource, alpha_min, cache):
    """스프라이트의 불투명 픽셀 좌표와 크기. 없으면 None."""
    if resource in cache:
        return cache[resource]

    from PIL import Image

    path = sprite_path(resource)
    if not os.path.exists(path):
        cache[resource] = None
        return None

    alpha = Image.open(path).convert("RGBA").split()[3]
    w, h = alpha.size
    px = alpha.load()
    solid = [
        (x, y)
        for y in range(h)
        for x in range(w)
        if px[x, y] >= alpha_min
    ]

    cache[resource] = (w, h, solid)
    return cache[resource]


def tile_coverage(resource, left, bottom, span_x, span_y, alpha_min, cache):
    """그려지는 자리를 기준으로 타일별로 덮은 면적을 낸다.

    스프라이트를 격자로 잘라 칸마다 비율을 내면 반 칸짜리 그림도 한 칸을
    통째로 막는다. 그림이 실제로 놓이는 월드 좌표에서 재야 판정이 보이는
    것과 맞는다.
    """
    data = opaque_pixels(resource, alpha_min, cache)
    if data is None:
        return None

    w, h, solid = data
    per_pixel = (span_x / w) * (span_y / h)
    tiles = {}

    for x, y in solid:
        world_x = left + (x + 0.5) * span_x / w
        world_y = bottom + (h - 1 - y + 0.5) * span_y / h
        cell = (math.floor(world_x), math.floor(world_y))
        tiles[cell] = tiles.get(cell, 0.0) + per_pixel

    return tiles


def bake(level, assets, block_t, noise_t, alpha_min, cache):
    path = os.path.join(DATA, f"map_l{level}.json")
    with open(path, encoding="utf-8") as handle:
        data = json.load(handle)
    width, height = data["width"], data["height"]
    walls = data["walls"]

    collision = [BLOCK if walls[i] != 0 else WALK for i in range(width * height)]
    unknown = set()

    for deco in data.get("decorations", []):
        entry = find_asset(assets, deco["key"])
        if entry is None:
            unknown.add(deco["key"])
            continue

        if entry["passability"] not in CODE:
            unknown.add(f'{deco["key"]}:{entry["passability"]}')
            continue

        verdict = CODE[entry["passability"]]
        if verdict == WALK:
            continue

        trim = DEBRIS_DISPLAY_SCALE if deco["key"].startswith("debris_") else 1.0
        span_x = deco["width"] * trim
        span_y = deco["height"] * trim
        left = deco["x"] + deco["width"] * 0.5 - span_x * 0.5
        bottom = height - deco["y"] + deco["height"] * 0.5 - span_y * 0.5

        tiles = tile_coverage(
            deco["resource"], left, bottom, span_x, span_y, alpha_min, cache)
        if tiles is None:
            unknown.add(deco["resource"])
            continue

        threshold = block_t if verdict == BLOCK else noise_t

        for (col, world_row), covered in tiles.items():
            if covered < threshold:
                continue

            row = height - 1 - world_row
            if not (0 <= col < width and 0 <= row < height):
                continue

            index = row * width + col
            if RANK[verdict] > RANK[collision[index]]:
                collision[index] = verdict

    counts = {v: collision.count(v) for v in (WALK, BLOCK, NOISE, MUFFLED)}
    if unknown:
        return counts, unknown

    data["collision"] = collision
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))

    return counts, unknown


def main():
    if len(sys.argv) != 2:
        print(__doc__.strip(), file=sys.stderr)
        return 2

    assets, block_t, noise_t, alpha_min = load_rules(sys.argv[1])
    cache = {}
    missing = set()

    for level in (1, 2, 3):
        counts, unknown = bake(level, assets, block_t, noise_t, alpha_min, cache)
        missing |= unknown
        print(
            f"L{level}: 통과 {counts[WALK]}  차단 {counts[BLOCK]}"
            f"  큰소리 {counts[NOISE]}  카펫 {counts[MUFFLED]}"
            + ("  (미확인 항목이 있어 저장하지 않았다)" if unknown else "")
        )

    if missing:
        print("판정표에 없거나 스프라이트를 못 찾은 키:", sorted(missing), file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
