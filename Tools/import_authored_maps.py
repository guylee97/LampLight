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
NEAR_START = {1: (5, 9)}

# 게임이 실제 오브젝트로 스폰하는 것들. 저작본이 그려놨어도 장식으로 깔지 않는다.
GAMEPLAY_CATEGORIES = ("artifact", "exit", "door")
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


def farthest(free, width, height, origin, candidates, wide):
    """제단에서 가장 먼 방 칸을 시작점으로 쓴다. 방 중심으로 재면 통로 낀 방이 손해다."""

    field = path_field(free, width, height, origin)

    best_room, best_tile, best_d = None, None, -1
    for room in candidates:
        for r in range(room["row"], room["row"] + room["height"]):
            for c in range(room["col"], room["col"] + room["width"]):
                if (c, r) not in wide:
                    continue

                d = field.get((c, r), -1)
                if d > best_d:
                    best_room, best_tile, best_d = room, (c, r), d

    if best_room is None:
        best_room = candidates[0]
        best_tile = best_room["center"]

    return best_room, best_tile


def nearest_free(free, width, height, reach, target):
    best, bestd = None, None
    for (c, r) in reach:
        d = (c - target[0]) ** 2 + (r - target[1]) ** 2
        if bestd is None or d < bestd:
            best, bestd = (c, r), d
    return best


def place_artifacts(free, width, height, reach, rooms, start, altar, want, gap,
                    from_start, near_band):
    picked = []

    if near_band:
        lo, hi = near_band
        band = sorted(t for t in reach if lo <= from_start.get(t, -1) <= hi)
        if band:
            picked.append(band[len(band) // 2])

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


ACTOR_HALF_W, ACTOR_HALF_H, ACTOR_FOOT = 0.31, 0.16, -0.317


def collider_tiles(decorations, width, height):
    """장식 콜라이더가 덮는 칸. MapCoord.BuildBlockedTiles 와 같은 식이어야 한다.

    베이크는 스프라이트 픽셀로, 콜라이더는 매니페스트 박스로 만들어진다. 어긋나면
    통행 표시는 됐는데 몸이 못 들어가는 칸이 생긴다.
    """

    blocked = set()
    for d in decorations:
        if (not d["collisionEnabled"]
                or d["colliderWidth"] <= 0.0 or d["colliderHeight"] <= 0.0):
            continue

        cx = d["x"] + d["width"] * 0.5 + d["colliderOffsetX"]
        cy = height - d["y"] + d["colliderOffsetY"]
        hw = d["colliderWidth"] * 0.5 + ACTOR_HALF_W
        hh = d["colliderHeight"] * 0.5 + ACTOR_HALF_H

        for row in range(height):
            ty = height - 1 - row + 0.5 + ACTOR_FOOT
            if abs(ty - cy) >= hh:
                continue

            for col in range(width):
                if abs(col + 0.5 - cx) < hw:
                    blocked.add((col, row))

    return blocked


def collision_for(data, rules, cache):
    """bake_collision 과 같은 규칙으로, 파일이 아니라 주어진 데이터에 대해 판정한다."""

    import bake_collision as bc

    assets, block_t, noise_t, alpha_min = rules
    width, height = data["width"], data["height"]
    walls = data["walls"]
    collision = [BLOCK if walls[i] != 0 else WALK for i in range(width * height)]

    for deco in data["decorations"]:
        entry = bc.find_asset(assets, deco["key"])
        if entry is None or entry["passability"] not in bc.CODE:
            continue

        verdict = bc.CODE[entry["passability"]]
        if verdict == WALK:
            continue

        trim = bc.DEBRIS_DISPLAY_SCALE if deco["key"].startswith("debris_") else 1.0
        span_x = deco["width"] * trim
        span_y = deco["height"] * trim
        left = deco["x"] + deco["width"] * 0.5 - span_x * 0.5
        bottom = height - deco["y"] + deco["height"] * 0.5 - span_y * 0.5

        tiles = bc.tile_coverage(
            deco["resource"], left, bottom, span_x, span_y, alpha_min, cache)
        if tiles is None:
            continue

        threshold = block_t if verdict == BLOCK else noise_t

        for (col, world_row), covered in tiles.items():
            if covered < threshold:
                continue

            row = height - 1 - world_row
            if not (0 <= col < width and 0 <= row < height):
                continue

            index = row * width + col
            if bc.RANK[verdict] > bc.RANK[collision[index]]:
                collision[index] = verdict

    for col, row in collider_tiles(data["decorations"], width, height):
        collision[row * width + col] = BLOCK

    return collision


def components(collision, width, height):
    walk = {(c, r) for r in range(height) for c in range(width)
            if collision[r * width + c] != BLOCK}
    seen, found = set(), []

    for cell in walk:
        if cell in seen:
            continue

        blob, q = [], deque([cell])
        seen.add(cell)
        while q:
            c, r = q.popleft()
            blob.append((c, r))
            for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                n = (c + dc, r + dr)
                if n in walk and n not in seen:
                    seen.add(n)
                    q.append(n)

        found.append(blob)

    found.sort(key=len, reverse=True)
    return found


def open_severed_passages(data, rules, cache):
    """통행 영역이 하나로 이어질 때까지, 길을 끊은 장식을 걷어낸다.

    막는 장식 하나가 복도를 잘라 놓으면 플레이어는 보이는데 못 가는 바닥을 만난다.
    콜라이더와 판정이 같이 움직여야 하므로 장식 자체를 뺀 뒤 다시 굽는다.
    """

    width, height = data["width"], data["height"]
    dropped = []

    for _ in range(40):
        comps = components(data["collision"], width, height)
        if len(comps) <= 1:
            break

        blocking = [d for d in data["decorations"] if d["collisionEnabled"]]
        best = None

        for deco in blocking:
            trial = [d for d in data["decorations"] if d is not deco]
            probe = dict(data)
            probe["decorations"] = trial
            rebaked = collision_for(probe, rules, cache)
            merged = components(rebaked, width, height)

            if len(merged) < len(comps):
                gain = len(merged[0]) - len(comps[0])
                if best is None or gain > best[0]:
                    best = (gain, deco, trial, rebaked)

        if best is None:
            break

        _, deco, trial, rebaked = best
        dropped.append(deco["key"])
        data["decorations"] = trial
        data["collision"] = rebaked

    comps = components(data["collision"], width, height)
    stranded = 0
    for blob in comps[1:]:
        for col, row in blob:
            data["collision"][row * width + col] = BLOCK
            stranded += 1

    return dropped, stranded


def build_terrain(level, template):
    """1패스 — 바닥·벽·저작 장식만. 충돌은 베이커가 굽는다."""

    tmx = os.path.join(SOURCE, f"Level{level}", "Map.tmx")
    width, height, free, authored = parse_tmx(tmx)

    catalog = load_objects()
    decorations = []
    for a in authored:
        entry = catalog.get(ALIASES.get(a["key"], a["key"]))
        if entry is None or entry.get("category") in GAMEPLAY_CATEGORIES:
            continue

        col = int(round(a["col"]))
        row = int(round(a["bottom"] - a["rows"]))
        if not (0 <= col < width and 0 <= row < height):
            continue

        # 밑동은 바닥에 서야 한다. 벽에 붙는 것은 walldeco/cobweb 만.
        base = int(round(a["bottom"])) - 1
        wall_mounted = entry["key"].startswith(("walldeco_", "cobweb_"))
        if not wall_mounted and (not (0 <= base < height) or not free[base * width + col]):
            continue

        decorations.append(as_decoration(entry, col, row))

    data = dict(template)
    data.update({
        "width": width, "height": height,
        "tileSize": TILE_SIZE,
        "pixelWidth": width * TILE_SIZE, "pixelHeight": height * TILE_SIZE,
        "floor": [FLOOR_GID if free[i] else 0 for i in range(width * height)],
        "walls": autotile(free, width, height),
        "deco": [0] * (width * height),
        "collision": [WALK if free[i] else BLOCK for i in range(width * height)],
        "decorations": decorations,
    })
    return data, authored, free, len(authored)


def place_objects(level, data, authored, free):
    """2패스 — 구워진 충돌 위에 시작·공양물·제단을 놓는다."""

    width, height = data["width"], data["height"]
    collision = data["collision"]
    walk = [c != BLOCK for c in collision]

    # 방은 건축(바닥)에서 읽고, 통행은 구워진 충돌에서 읽는다.
    rooms = rooms_from_clearance(free, width, height)
    if len(rooms) < 3:
        raise SystemExit(f"L{level}: 방을 3개 이상 못 찾았다 ({len(rooms)}개)")

    altar_room = named_altar(rooms, authored) or rooms[0]
    wide = wide_tiles(walk, width, height)
    start_room, start_tile = farthest(walk, width, height, altar_room["center"],
                                      [r for r in rooms if r is not altar_room], wide)

    reach = set(path_field(walk, width, height, start_tile))
    if not reach:
        raise SystemExit(f"L{level}: 시작점이 막혔다")

    altar_tile = altar_room["center"]
    if altar_tile not in reach:
        altar_tile = min(reach, key=lambda t: (t[0] - altar_room["center"][0]) ** 2
                         + (t[1] - altar_room["center"][1]) ** 2)

    objects = [point("player_start", *start_tile, height=height),
               point("exit_door", *altar_tile, height=height)]

    spread = place_artifacts(walk, width, height, reach & wide or reach, rooms,
                             start_room, altar_room, ARTIFACTS[level],
                             RADIUS[level] + 1.0,
                             path_field(walk, width, height, start_tile),
                             NEAR_START.get(level))
    for i, (col, row) in enumerate(spread, start=1):
        objects.append(point(f"artifact_{i}", col, row, height))

    data["objects"] = objects
    data["spawns"] = pick_spawns(walk, width, height, start_tile,
                                 reach & wide or reach, height)
    data["rooms"] = [{"col": r["col"], "row": r["row"],
                      "width": r["width"], "height": r["height"]} for r in rooms]

    missing = [o["name"] for o in objects if (o["col"], o["row"]) not in reach]
    return spread, missing, len(reach)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--out", default=DATA)
    args = parser.parse_args()

    import bake_collision

    stash = {}
    for level in (1, 2, 3):
        path = os.path.join(DATA, f"map_l{level}.json")
        with open(path, encoding="utf-8") as handle:
            template = json.load(handle)

        data, authored, free, authored_n = build_terrain(level, template)
        stash[level] = (authored, free, authored_n)

        with open(path, "w", encoding="utf-8") as handle:
            json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))

    rules = bake_collision.load_rules(os.path.join(ROOT, "docs", "collision_map.json"))
    cache = {}
    repairs = {}
    for level in (1, 2, 3):
        _, unknown = bake_collision.bake(level, *rules, cache)
        if unknown:
            raise SystemExit(f"L{level}: 판정표에 없는 키 {sorted(unknown)}")

        path = os.path.join(DATA, f"map_l{level}.json")
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)

        data["collision"] = collision_for(data, rules, cache)
        opened, stranded = open_severed_passages(data, rules, cache)
        repairs[level] = (opened, stranded)

        with open(path, "w", encoding="utf-8") as handle:
            json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))

    ok = True
    for level in (1, 2, 3):
        path = os.path.join(DATA, f"map_l{level}.json")
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)

        authored, free, authored_n = stash[level]
        spread, missing, walkable = place_objects(level, data, authored, free)
        overlaps = overlap_pairs(spread, RADIUS[level])
        ok &= (not missing) and overlaps <= 1

        with open(os.path.join(args.out, f"map_l{level}.json"), "w",
                  encoding="utf-8") as handle:
            json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))

        print(f"L{level} {data['width']}x{data['height']}  통행 {walkable}"
              f"(바닥 {sum(1 for v in free if v)})  장식 {len(data['decorations'])}/{authored_n}"
              f"  소리겹침 {overlaps}쌍"
              + ("  전 목표 연결됨" if not missing else f"  미연결 {missing}"))

        if len(components(data["collision"], data["width"], data["height"])) != 1:
            raise SystemExit(f"L{level}: 통행 영역이 하나로 안 이어진다")

        opened, stranded = repairs[level]
        if opened:
            print(f"     길을 끊던 장식 {len(opened)}개 제거: {sorted(set(opened))}")
        if stranded:
            print(f"     닿을 수 없어 막은 바닥 {stranded}칸")

    if not ok:
        raise SystemExit("목표 지점이 이어지지 않는다")


if __name__ == "__main__":
    main()
