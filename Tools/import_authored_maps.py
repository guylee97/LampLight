"""손으로 그린 Tiled 맵을 게임이 읽는 map_lN.json 으로 굽는다.

바닥과 배치물은 저작본을 그대로 쓰고, 시작·공양물·제단 같은 게임플레이 지점만
build_ritual_maps 의 규칙으로 얹는다. 저작본에는 그 지점들이 없다.
"""

import argparse
import json
import os
import re
import sys
from collections import deque

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from build_ritual_maps import (  # noqa: E402
    BLOCK,
    DATA,
    FLOOR_GID,
    RADIUS,
    TILE_SIZE,
    WALK,
    as_decoration,
    autotile,
    connected,
    load_objects,
    overlap_pairs,
    path_field,
    point,
    wide_tiles,
)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "MapSource", "FullMap", "Map")

ARTIFACTS = {1: 2, 2: 3, 3: 4}
GID_MASK = 0x1FFFFFFF

# 저작본에만 있는 이름들. 게임 매니페스트의 같은 물건으로 보낸다.
ALIASES = {
    "extra_prop_skull_b": "prop_skull",
    "extra_cobweb_corner_b": "cobweb_corner",
    "extra_prop_bones_b": "prop_bones_pile",
}


def parse_tmx(path):
    text = open(path, encoding="utf-8").read()

    head = re.search(r'<map [^>]*?width="(\d+)" height="(\d+)"', text)
    width, height = int(head.group(1)), int(head.group(2))
    tile_px = int(re.search(r'tilewidth="(\d+)"', text).group(1))

    layer = re.search(
        r'<layer[^>]*name="floor".*?<data encoding="csv">(.*?)</data>', text, re.S)
    gids = [int(v) for v in layer.group(1).replace("\n", "").split(",") if v.strip()]

    art = {}
    for ts in re.finditer(r'<tileset firstgid="(\d+)"([^>]*)(?:/>|>(.*?)</tileset>)',
                          text, re.S):
        firstgid = int(ts.group(1))
        attrs, inline = ts.group(2), ts.group(3)

        src = re.search(r'source="([^"]+)"', attrs)
        body = open(os.path.join(os.path.dirname(path), src.group(1)),
                    encoding="utf-8").read() if src else (inline or "")

        for tid, image in re.findall(r'<tile id="(\d+)">\s*<image source="([^"]+)"', body):
            base = os.path.splitext(os.path.basename(image))[0]
            art[firstgid + int(tid)] = base[4:] if base.startswith("obj_") else base

    objects = []
    for group in re.finditer(r'<objectgroup[^>]*name="([^"]+)"[^>]*>(.*?)</objectgroup>',
                             text, re.S):
        name = group.group(1)
        for o in re.finditer(
                r'<object [^>]*gid="(\d+)"[^>]*x="([\d.-]+)"[^>]*y="([\d.-]+)"'
                r'[^>]*width="([\d.]+)"[^>]*height="([\d.]+)"', group.group(2)):
            gid, x, y, w, h = o.groups()
            objects.append({
                "room": name,
                "key": art.get(int(gid) & GID_MASK),
                "col": float(x) / tile_px,
                "bottom": float(y) / tile_px,
                "cols": float(w) / tile_px,
                "rows": float(h) / tile_px,
            })

    return width, height, [g != 0 for g in gids], objects


def rooms_from_clearance(free, width, height):
    """3x3 여유가 있는 칸들을 이어서 방으로 본다. 복도는 좁아서 남지 않는다."""

    wide = wide_tiles(free, width, height)
    seen, rooms = set(), []

    for cell in sorted(wide):
        if cell in seen:
            continue

        blob, q = [], deque([cell])
        seen.add(cell)
        while q:
            c, r = q.popleft()
            blob.append((c, r))
            for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (c + dc, r + dr)
                if n in wide and n not in seen:
                    seen.add(n)
                    q.append(n)

        if len(blob) < 6:
            continue

        cols = [c for c, _ in blob]
        rows = [r for _, r in blob]
        avg = (sum(cols) // len(cols), sum(rows) // len(rows))
        centre = min(blob, key=lambda t: (t[0] - avg[0]) ** 2 + (t[1] - avg[1]) ** 2)
        rooms.append({
            "col": min(cols), "row": min(rows),
            "width": max(cols) - min(cols) + 1,
            "height": max(rows) - min(rows) + 1,
            "center": centre,
            "area": len(blob),
        })

    rooms.sort(key=lambda r: -r["area"])
    return rooms


def farthest(free, width, height, origin, candidates):
    field = {origin: 0}
    q = deque([origin])
    while q:
        c, r = q.popleft()
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (c + dc, r + dr)
            if (0 <= n[0] < width and 0 <= n[1] < height
                    and free[n[1] * width + n[0]] and n not in field):
                field[n] = field[(c, r)] + 1
                q.append(n)

    return max(candidates, key=lambda room: field.get(room["center"], -1))


def nearest_free(free, width, height, reach, target):
    best, bestd = None, None
    for (c, r) in reach:
        d = (c - target[0]) ** 2 + (r - target[1]) ** 2
        if bestd is None or d < bestd:
            best, bestd = (c, r), d
    return best


def place_artifacts(free, width, height, reach, rooms, start, altar, want, gap):
    picked = []
    pool = [r for r in rooms if r is not start and r is not altar]
    pool.sort(key=lambda r: -r["area"])

    for room in pool + pool:
        if len(picked) >= want:
            break

        spot = nearest_free(free, width, height, reach, room["center"])
        if spot is None:
            continue

        if any((spot[0] - p[0]) ** 2 + (spot[1] - p[1]) ** 2 < gap ** 2
               for p in picked):
            continue

        picked.append(spot)

    for cell in sorted(reach):
        if len(picked) >= want:
            break
        if all((cell[0] - p[0]) ** 2 + (cell[1] - p[1]) ** 2 >= gap ** 2
               for p in picked):
            picked.append(cell)

    return picked[:want]


def named_altar(rooms, authored):
    """저작자가 방 이름에 제단을 적어뒀으면 그 방을 쓴다."""

    tally = {}
    for a in authored:
        if "altar" not in a["room"] and "sanctum" not in a["room"]:
            continue

        col, row = int(round(a["col"])), int(round(a["bottom"]))
        for room in rooms:
            if (room["col"] <= col < room["col"] + room["width"]
                    and room["row"] <= row < room["row"] + room["height"]):
                tally[id(room)] = tally.get(id(room), 0) + 1

    if not tally:
        return None

    best = max(tally, key=tally.get)
    return next(r for r in rooms if id(r) == best)


def pick_spawns(free, width, height, start, reach, map_height):
    """EnemySpawner 는 자기 거리장으로 자리를 고른다. 여기 값은 Validate 용 앵커다."""

    field = path_field(free, width, height, start)
    far = sorted((t for t in reach if field.get(t, -1) >= 7),
                 key=lambda t: (-field[t], t))

    if not far:
        far = sorted(reach, key=lambda t: (-field.get(t, 0), t))

    step = max(1, len(far) // 8)
    picked = [far[min(i * step, len(far) - 1)] for i in range(min(8, len(far)))]

    return [point(f"spawn_{i}", c, r, map_height) for i, (c, r) in enumerate(picked)]


def build(level, template):
    tmx = os.path.join(SOURCE, f"Level{level}", "Map.tmx")
    width, height, free, authored = parse_tmx(tmx)

    walls = autotile(free, width, height)
    floor = [FLOOR_GID if free[i] else 0 for i in range(width * height)]
    collision = [WALK if free[i] else BLOCK for i in range(width * height)]

    rooms = rooms_from_clearance(free, width, height)
    if len(rooms) < 3:
        raise SystemExit(f"L{level}: 방을 3개 이상 못 찾았다 ({len(rooms)}개)")

    altar_room = named_altar(rooms, authored) or rooms[0]
    start_room = farthest(free, width, height, altar_room["center"],
                          [r for r in rooms if r is not altar_room])

    # 도달성은 게임과 같은 기준으로 본다. 3x3 여유 그래프는 좁은 복도에서 끊긴다.
    reach = set(path_field(free, width, height, start_room["center"]))
    wide = wide_tiles(free, width, height)

    objects = [point("player_start", *start_room["center"], height=height)]
    objects.append(point("exit_door", *altar_room["center"], height=height))

    spread = place_artifacts(free, width, height, reach & wide or reach, rooms,
                             start_room, altar_room, ARTIFACTS[level],
                             RADIUS[level] + 1.0)
    for i, (col, row) in enumerate(spread, start=1):
        objects.append(point(f"artifact_{i}", col, row, height))

    spawns = pick_spawns(free, width, height, start_room["center"],
                         reach & wide or reach, height)

    keep_clear = set()
    for o in objects:
        for dr in (-1, 0, 1):
            for dc in (-1, 0, 1):
                keep_clear.add((o["col"] + dc, o["row"] + dr))

    catalog = load_objects()
    decorations, dropped = [], 0
    for a in authored:
        entry = catalog.get(ALIASES.get(a["key"], a["key"]))
        if entry is None:
            dropped += 1
            continue

        col = int(round(a["col"]))
        row = int(round(a["bottom"] - a["rows"]))

        if not (0 <= col < width and 0 <= row < height):
            dropped += 1
            continue

        if (col, row) in keep_clear:
            dropped += 1
            continue

        decorations.append(as_decoration(entry, col, row))

    data = dict(template)
    data.update({
        "width": width,
        "height": height,
        "tileSize": TILE_SIZE,
        "pixelWidth": width * TILE_SIZE,
        "pixelHeight": height * TILE_SIZE,
        "floor": floor,
        "walls": walls,
        "deco": [0] * (width * height),
        "collision": collision,
        "objects": objects,
        "spawns": spawns,
        "rooms": [{"col": r["col"], "row": r["row"],
                   "width": r["width"], "height": r["height"]} for r in rooms],
        "decorations": decorations,
    })

    missing = [o["name"] for o in objects if (o["col"], o["row"]) not in reach]
    overlaps = overlap_pairs(spread, RADIUS[level])
    walkable = sum(1 for v in free if v)

    print(f"L{level} {width}x{height}  통과 {walkable}  방 {len(rooms)}"
          f"  장식 {len(decorations)}/{len(authored)} (버림 {dropped})"
          f"  소리겹침 {overlaps}쌍"
          + ("  전 목표 연결됨" if not missing else f"  미연결 {missing}"))

    return data, (not missing) and overlaps <= 1


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--out", default=DATA)
    args = parser.parse_args()

    ok = True
    for level in (1, 2, 3):
        path = os.path.join(DATA, f"map_l{level}.json")
        with open(path, encoding="utf-8") as handle:
            template = json.load(handle)

        data, good = build(level, template)
        ok &= good

        if args.dry_run:
            continue

        with open(os.path.join(args.out, f"map_l{level}.json"), "w",
                  encoding="utf-8") as handle:
            json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))

    if not ok:
        raise SystemExit("목표 지점이 이어지지 않는다")


if __name__ == "__main__":
    main()
