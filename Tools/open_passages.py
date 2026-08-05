"""고립된 통과 영역을 주 영역에 잇는다.

막은 것이 벽이면 손대지 않고, 바닥에 놓인 오브젝트가 통로를 끊은 경우에만
그 오브젝트를 통로 밖으로 민다. 그림이 함께 움직이므로 보이는 것과 판정이
어긋나지 않는다. 민 뒤에는 collision 을 다시 구워야 한다.

    python3 Tools/open_passages.py <collision_map.json>
"""

import json
import math
import os
import subprocess
import sys
from collections import deque

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "Assets", "Resources", "Data")

STEP = 0.25
MAX_SHIFT = 3.0
DEBRIS_DISPLAY_SCALE = 0.7


def tiles_of(deco, height):
    trim = DEBRIS_DISPLAY_SCALE if deco["key"].startswith("debris_") else 1.0
    cx = deco["x"] + deco["width"] * 0.5
    cy = height - deco["y"] + deco["height"] * 0.5
    sx = deco["width"] * trim
    sy = deco["height"] * trim

    out = set()
    for y in range(math.floor(cy - sy / 2 + 0.01), math.ceil(cy + sy / 2 - 0.01)):
        for x in range(math.floor(cx - sx / 2 + 0.01), math.ceil(cx + sx / 2 - 0.01)):
            out.add((x, height - 1 - y))
    return out


def regions_of(data):
    width, height, collision = data["width"], data["height"], data["collision"]

    def open_at(col, row):
        return 0 <= col < width and 0 <= row < height and collision[row * width + col] != 1

    seen = set()
    found = []

    for row in range(height):
        for col in range(width):
            if not open_at(col, row) or (col, row) in seen:
                continue

            comp = set()
            queue = deque([(col, row)])
            seen.add((col, row))

            while queue:
                x, y = queue.popleft()
                comp.add((x, y))
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nxt = (x + dx, y + dy)
                    if nxt in seen or not open_at(*nxt):
                        continue
                    seen.add(nxt)
                    queue.append(nxt)

            found.append(comp)

    found.sort(key=len, reverse=True)
    return found


def gates_of(data):
    """고립 영역과 주 영역 사이에서 오브젝트가 막고 있는 칸."""
    width, height = data["width"], data["height"]
    collision, walls = data["collision"], data["walls"]
    found = regions_of(data)
    if len(found) < 2:
        return set()

    main = found[0]
    gates = set()

    for iso in found[1:]:
        if len(iso) < 2:
            continue
        for col, row in iso:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nc, nr = col + dx, row + dy
                if not (0 <= nc < width and 0 <= nr < height):
                    continue
                if collision[nr * width + nc] != 1 or walls[nr * width + nc] != 0:
                    continue
                if (nc + dx, nr + dy) in main:
                    gates.add((nc, nr))

    return gates


def clear_gates(level, rules_path):
    path = os.path.join(DATA, f"map_l{level}.json")
    data = json.load(open(path))
    height = data["height"]
    gates = gates_of(data)

    if not gates:
        return 0, 0

    moved = dropped = 0

    for deco in data["decorations"]:
        if not (tiles_of(deco, height) & gates):
            continue

        origin = (deco["x"], deco["y"])
        placed = None
        steps = int(MAX_SHIFT / STEP)

        for radius in range(1, steps + 1):
            for dx in range(-radius, radius + 1):
                for dy in range(-radius, radius + 1):
                    if max(abs(dx), abs(dy)) != radius:
                        continue
                    deco["x"] = origin[0] + dx * STEP
                    deco["y"] = origin[1] + dy * STEP
                    if not (tiles_of(deco, height) & gates):
                        placed = (deco["x"], deco["y"])
                        break
                if placed:
                    break
            if placed:
                break

        if placed:
            moved += 1
        else:
            deco["x"], deco["y"] = origin
            deco["_drop"] = True
            dropped += 1

    data["decorations"] = [d for d in data["decorations"] if not d.get("_drop")]
    json.dump(data, open(path, "w"), ensure_ascii=False, separators=(",", ":"))
    return moved, dropped


def main():
    if len(sys.argv) != 2:
        print(__doc__.strip(), file=sys.stderr)
        return 2

    rules = sys.argv[1]

    for _ in range(4):
        total = 0
        for level in (1, 2, 3):
            moved, dropped = clear_gates(level, rules)
            if moved or dropped:
                print(f"L{level}: 이동 {moved}개, 제거 {dropped}개")
            total += moved + dropped

        subprocess.run(
            [sys.executable, os.path.join(ROOT, "Tools", "bake_collision.py"), rules],
            check=True,
        )

        if total == 0:
            break

    for level in (1, 2, 3):
        data = json.load(open(os.path.join(DATA, f"map_l{level}.json")))
        found = regions_of(data)
        stranded = sum(len(r) for r in found[1:])
        print(f"L{level}: 영역 {len(found)}개, 최대 {len(found[0])}칸, 고립 {stranded}칸")

    return 0


if __name__ == "__main__":
    sys.exit(main())
