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
    doc = json.load(open(path))
    rule = doc["footprint_rule"]
    return doc["assets"], rule["block_threshold"], rule["noise_threshold"], rule["alpha_min"]


def find_asset(assets, key):
    for name in (f"obj_{key}.png", f"{key}.png", f"extra_{key}.png"):
        if name in assets:
            return assets[name]
    return None


def sprite_path(resource):
    return os.path.join(ROOT, "Assets", "Resources", resource + ".png")


def coverage(resource, cols, rows, alpha_min, cache):
    """스프라이트를 cols x rows 격자로 잘라 칸별 불투명 픽셀 비율을 낸다."""
    key = (resource, cols, rows)
    if key in cache:
        return cache[key]

    from PIL import Image

    path = sprite_path(resource)
    if not os.path.exists(path):
        cache[key] = None
        return None

    alpha = Image.open(path).convert("RGBA").split()[3]
    w, h = alpha.size
    px = alpha.load()
    grid = []

    for r in range(rows):
        row = []
        for c in range(cols):
            x0, x1 = w * c // cols, w * (c + 1) // cols
            y0, y1 = h * r // rows, h * (r + 1) // rows
            area = max(1, (x1 - x0) * (y1 - y0))
            solid = sum(
                1
                for y in range(y0, y1)
                for x in range(x0, x1)
                if px[x, y] >= alpha_min
            )
            row.append(solid / area)
        grid.append(row)

    cache[key] = grid
    return grid


def bake(level, assets, block_t, noise_t, alpha_min, cache):
    path = os.path.join(DATA, f"map_l{level}.json")
    data = json.load(open(path))
    width, height = data["width"], data["height"]
    walls = data["walls"]

    collision = [BLOCK if walls[i] != 0 else WALK for i in range(width * height)]
    unknown = set()

    for deco in data.get("decorations", []):
        entry = find_asset(assets, deco["key"])
        if entry is None:
            unknown.add(deco["key"])
            continue

        verdict = CODE.get(entry["passability"], WALK)
        if verdict == WALK:
            continue

        trim = DEBRIS_DISPLAY_SCALE if deco["key"].startswith("debris_") else 1.0
        span_x = deco["width"] * trim
        span_y = deco["height"] * trim
        left = deco["x"] + deco["width"] * 0.5 - span_x * 0.5
        bottom = height - deco["y"] + deco["height"] * 0.5 - span_y * 0.5

        cols = max(1, int(round(span_x)))
        rows = max(1, int(round(span_y)))
        grid = coverage(deco["resource"], cols, rows, alpha_min, cache)
        if grid is None:
            unknown.add(deco["resource"])
            continue

        threshold = block_t if verdict == BLOCK else noise_t

        for gr in range(rows):
            for gc in range(cols):
                if grid[gr][gc] < threshold:
                    continue

                col = math.floor(left + (gc + 0.5) * span_x / cols)
                world_y = bottom + (rows - 1 - gr + 0.5) * span_y / rows
                row = height - 1 - math.floor(world_y)

                if not (0 <= col < width and 0 <= row < height):
                    continue

                index = row * width + col
                if RANK[verdict] > RANK[collision[index]]:
                    collision[index] = verdict

    data["collision"] = collision
    json.dump(data, open(path, "w"), ensure_ascii=False, separators=(",", ":"))

    counts = {v: collision.count(v) for v in (WALK, BLOCK, NOISE, MUFFLED)}
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
        )

    if missing:
        print("판정표에 없거나 스프라이트를 못 찾은 키:", sorted(missing), file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
