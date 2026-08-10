"""의식 동선에 맞춘 고정 맵 3종을 만든다.

데드셀처럼 방은 손으로 정한 템플릿을 쓰고, 조합만 코드가 한다. 무작위는 없다 —
같은 입력이면 같은 맵이 나온다. 복도는 3타일이라 몸집 큰 요괴도 중앙을 따라
걸을 수 있고, 장식은 통행 척추를 침범하지 않는 자리에만 놓는다.
"""

import argparse
import json
import os
from collections import deque

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "Assets", "Resources", "Data")

WALK, BLOCK, NOISE, MUFFLED = 0, 1, 2, 3
FLOOR_GID = 1
WALL_BASE = 9
TILE_SIZE = 64
CORRIDOR_HALF = 1
RADIUS = {1: 12.0, 2: 9.0, 3: 7.0}

WALL_DECOR = [
    ("prop_candle_stub", "Art/Objects/prop/obj_prop_candle_stub"),
    ("prop_skull", "Art/Objects/prop/obj_prop_skull"),
    ("prop_bones_pile", "Art/Objects/prop/obj_prop_bones_pile"),
    ("debris_gravel_a", "Art/Objects/debris/obj_debris_gravel_a"),
    ("prop_pebbles", "Art/Objects/prop/obj_prop_pebbles"),
    ("debris_stones", "Art/Objects/debris/obj_debris_stones"),
    ("prop_candle_short", "Art/Objects/prop/obj_prop_candle_short"),
    ("prop_bowl", "Art/Objects/prop/obj_prop_bowl"),
]

MANIFEST = os.path.join(DATA, "objects_manifest_merged.json")

# 프롭은 전부 제 캔버스를 꽉 채우게 그려져 있어서 크기 위계가 없다 — 자갈이 사람의
# 43%로 렌더된다. 실제 물건 크기에 맞춰 줄이되, 배율은 픽셀이 뭉개지지 않는 분수로만
# 고른다 (1/6, 1/4, 1/3, 1/2, 3/4, 1, 5/2 …). 플레이어 렌더 높이는 42px 이다.
PROP_SCALE = {
    "prop_pebbles": 1 / 8,
    "prop_candle_stub": 1 / 6,
    "prop_bowl": 1 / 6,
    "debris_gravel_a": 1 / 6,
    "debris_gravel_b": 1 / 6,
    "prop_skull": 1 / 4,
    "debris_stones": 1 / 4,
    "debris_stone": 1 / 4,
    "prop_bones_pile": 1 / 3,
    "prop_bones_long": 1 / 3,
    "large_steps": 1 / 3,
    "large_benches_pair": 1 / 3,
    "prop_candle_short": 1 / 2,
    "prop_basin": 1 / 2,
    "container_chest": 3 / 4,
    "large_statue_kneeling": 3 / 4,
    "prop_candle_tall": 2 / 5,
    # 아래 넷은 배율이 없어 1.0 으로 그려지고 있었다. 촛대가 사람 키의 절반이었다.
    "prop_candle_holder": 0.45,
    "prop_candle_leaning": 0.42,
    "prop_candle_melted": 0.55,
    "prop_candle_fallen": 0.80,
    "prop_railing": 1.0,
    "prop_gravestone": 5 / 8,
    "debris_shelf": 1.0,
    "large_shelf_fallen": 3 / 5,
    "large_firepit": 3 / 5,
    "large_sacrificial_slab": 2 / 3,
    "prop_pillar_intact": 1.0,
}

# 방마다 하나씩 박는 랜드마크. 화면에 20x11 타일밖에 안 보이는 게임에서
# 방이 서로 구분되지 않으면 길을 잃는다. (col, row) 는 방 좌상단 기준 오프셋.
LANDMARKS = {
    "entry": [("large_benches_pair", 0, 0), ("prop_candle_tall", 2, 0),
              ("prop_candle_tall", 2, 1)],
    "hall": [("large_statue_kneeling", 0, 0), ("prop_candle_tall", 2, 1),
             ("prop_bowl", 2, 0)],
    "crypt": [("prop_gravestone", 0, 0), ("prop_gravestone", 1, 1),
              ("prop_gravestone", 2, 0)],
    "ossuary": [("prop_bones_pile", 0, 0), ("prop_skull", 1, 0),
                ("prop_bones_pile", 1, 1), ("prop_skull", 0, 1)],
    "vault": [("container_chest", 0, 0), ("debris_shelf", 2, 0)],
    "gallery": [("prop_pillar_intact", 0, 0), ("prop_pillar_intact", 2, 0),
                ("prop_pillar_intact", 0, 2), ("prop_pillar_intact", 2, 2)],
    "stair": [("large_steps", 0, 0), ("prop_railing", 2, 1)],
    "sanctum": [("prop_pillar_intact", 0, 0), ("prop_pillar_intact", 2, 0)],
}


class Room:
    def __init__(self, name, col, row, width, height):
        self.name = name
        self.col = col
        self.row = row
        self.width = width
        self.height = height

    @property
    def center(self):
        return (self.col + self.width // 2, self.row + self.height // 2)

    def tiles(self):
        for r in range(self.row, self.row + self.height):
            for c in range(self.col, self.col + self.width):
                yield c, r


LAYOUTS = {
    1: {
        "size": (34, 24),
        "rooms": [
            Room("entry", 2, 9, 8, 7),
            Room("hall", 13, 3, 9, 8),
            Room("crypt", 13, 14, 9, 7),
            Room("sanctum", 25, 8, 7, 8),
        ],
        "links": [("entry", "hall"), ("entry", "crypt"), ("hall", "sanctum"),
                  ("crypt", "sanctum")],
        "start": "entry",
        "altar": "sanctum",
        "artifacts": ["hall", "crypt"],
    },
    2: {
        "size": (42, 30),
        "rooms": [
            Room("entry", 2, 12, 8, 7),
            Room("hall", 12, 3, 9, 8),
            Room("crypt", 12, 19, 9, 8),
            Room("gallery", 23, 11, 8, 8),
            Room("vault", 23, 2, 8, 7),
            Room("sanctum", 33, 12, 7, 9),
        ],
        "links": [("entry", "hall"), ("entry", "crypt"), ("hall", "vault"),
                  ("hall", "gallery"), ("crypt", "gallery"), ("gallery", "sanctum"),
                  ("vault", "sanctum")],
        "start": "entry",
        "altar": "sanctum",
        "artifacts": ["hall", "crypt", "vault"],
    },
    3: {
        "size": (52, 36),
        "rooms": [
            Room("entry", 2, 15, 8, 8),
            Room("hall", 12, 3, 10, 9),
            Room("crypt", 12, 23, 10, 9),
            Room("gallery", 24, 14, 9, 9),
            Room("vault", 25, 2, 9, 8),
            Room("ossuary", 25, 25, 9, 8),
            Room("stair", 36, 15, 7, 8),
            Room("sanctum", 42, 3, 8, 9),
        ],
        "links": [("entry", "hall"), ("entry", "crypt"), ("hall", "vault"),
                  ("hall", "gallery"), ("crypt", "ossuary"), ("crypt", "gallery"),
                  ("gallery", "stair"), ("vault", "sanctum"), ("ossuary", "stair"),
                  ("stair", "sanctum")],
        "start": "entry",
        "altar": "sanctum",
        "artifacts": ["hall", "crypt", "vault", "ossuary"],
    },
}


def index(width, col, row):
    return row * width + col


def carve_room(free, width, height, room):
    for c, r in room.tiles():
        if 1 <= c < width - 1 and 1 <= r < height - 1:
            free[index(width, c, r)] = True


def carve_corridor(free, width, height, a, b):
    ac, ar = a
    bc, br = b

    for c in range(min(ac, bc), max(ac, bc) + 1):
        for d in range(-CORRIDOR_HALF, CORRIDOR_HALF + 1):
            r = ar + d
            if 1 <= c < width - 1 and 1 <= r < height - 1:
                free[index(width, c, r)] = True

    for r in range(min(ar, br), max(ar, br) + 1):
        for d in range(-CORRIDOR_HALF, CORRIDOR_HALF + 1):
            c = bc + d
            if 1 <= c < width - 1 and 1 <= r < height - 1:
                free[index(width, c, r)] = True


def autotile(free, width, height):
    walls = [0] * (width * height)

    def is_wall(c, r):
        if c < 0 or r < 0 or c >= width or r >= height:
            return True
        return not free[index(width, c, r)]

    for r in range(height):
        for c in range(width):
            if not is_wall(c, r):
                continue

            mask = 0
            if is_wall(c, r - 1):
                mask |= 1
            if is_wall(c + 1, r):
                mask |= 2
            if is_wall(c, r + 1):
                mask |= 4
            if is_wall(c - 1, r):
                mask |= 8

            walls[index(width, c, r)] = WALL_BASE + mask

    return walls


def wide_tiles(free, width, height):
    result = set()

    for r in range(1, height - 1):
        for c in range(1, width - 1):
            if all(
                free[index(width, c + dc, r + dr)]
                for dr in (-1, 0, 1)
                for dc in (-1, 0, 1)
            ):
                result.add((c, r))

    return result


def connected(wides, start):
    if start not in wides:
        return set()

    seen = {start}
    queue = deque([start])

    while queue:
        c, r = queue.popleft()
        for dc, dr in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            nxt = (c + dc, r + dr)
            if nxt in wides and nxt not in seen:
                seen.add(nxt)
                queue.append(nxt)

    return seen


def spread_artifacts(layout, by_name, reach):
    """유물 소리 반경이 서로 덜 겹치도록 방 안에서 가장 먼 지점을 고른다."""

    rooms = [by_name[name] for name in layout["artifacts"]]
    chosen = []

    for room in rooms:
        options = [t for t in room.tiles() if t in reach]
        if not options:
            options = [room.center]

        if not chosen:
            anchor = by_name[layout["start"]].center
            options.sort(key=lambda t: -((t[0] - anchor[0]) ** 2 + (t[1] - anchor[1]) ** 2))
            chosen.append(options[0])
            continue

        best = max(
            options,
            key=lambda t: min((t[0] - c[0]) ** 2 + (t[1] - c[1]) ** 2 for c in chosen),
        )
        chosen.append(best)

    return chosen


def overlap_pairs(points, radius):
    count = 0

    for i in range(len(points)):
        for j in range(i + 1, len(points)):
            dx = points[i][0] - points[j][0]
            dy = points[i][1] - points[j][1]
            if (dx * dx + dy * dy) ** 0.5 < radius * 2.0:
                count += 1

    return count


def point(name, col, row, height):
    return {
        "name": name,
        "col": col,
        "row": row,
        "x": col + 0.5,
        "y": height - row - 0.5,
    }


def path_field(free, width, height, origin):
    dist = {origin: 0}
    queue = deque([origin])

    while queue:
        c, r = queue.popleft()
        for dc, dr in ((0, 1), (0, -1), (1, 0), (-1, 0)):
            nxt = (c + dc, r + dr)
            if nxt in dist:
                continue
            if not (0 <= nxt[0] < width and 0 <= nxt[1] < height):
                continue
            if not free[index(width, nxt[0], nxt[1])]:
                continue
            dist[nxt] = dist[(c, r)] + 1
            queue.append(nxt)

    return dist


def pick_spawns(free, width, height, start, artifacts, reach):
    """스폰은 시작에서 7타일 이상 떨어지고, 유물이 5~9타일 안에 있어야 한다."""

    from_start = path_field(free, width, height, start)
    from_artifact = [path_field(free, width, height, a) for a in artifacts]

    candidates = []
    for tile in reach:
        if from_start.get(tile, -1) < 7:
            continue

        near = any(5 <= field.get(tile, 10 ** 9) <= 9 for field in from_artifact)
        if not near:
            continue

        candidates.append(tile)

    if len(candidates) < 8:
        raise SystemExit(f"스폰 후보가 {len(candidates)}개뿐이다 — 방 배치를 넓혀라")

    candidates.sort(key=lambda t: (-from_start[t], t))
    step = max(1, len(candidates) // 8)
    picked = [candidates[min(i * step, len(candidates) - 1)] for i in range(8)]

    return [point(f"spawn_{i}", c, r, height) for i, (c, r) in enumerate(picked)]


def load_objects():
    with open(MANIFEST, encoding="utf-8") as handle:
        data = json.load(handle)

    return {o["key"]: o for o in data["objects"]}


def footprint(entry):
    cols, rows = entry.get("footprint", "1x1").split("x")
    return int(cols), int(rows)


def as_decoration(entry, col, row):
    """타일 바닥선에 세우고 가로만 가운데 맞춘다. 바닥선을 유지해야 Y 정렬이 안 틀어진다."""

    cols, rows = footprint(entry)
    collision = entry.get("collision", {})
    scale = PROP_SCALE.get(entry["key"], 1.0)

    draw_w = cols * scale
    draw_h = rows * scale

    return {
        "key": entry["key"],
        "resource": entry["resource"],
        "x": float(col) + (cols - draw_w) * 0.5,
        "y": float(row + rows),
        "width": draw_w,
        "height": draw_h,
        "flipHorizontal": False,
        "flipVertical": False,
        "flipDiagonal": False,
        "collisionEnabled": bool(collision.get("enabled", False)),
        "colliderWidth": float(collision.get("width", 0.0)) * scale,
        "colliderHeight": float(collision.get("height", 0.0)) * scale,
        "colliderOffsetX": float(collision.get("offsetX", 0.0)) * scale,
        "colliderOffsetY": float(collision.get("offsetY", 0.0)) * scale,
        "sortingOffset": 0,
    }


def place_landmarks(free, width, height, spine, layout, keep_clear):
    """방마다 고정 랜드마크를 하나씩 놓는다. 척추와 목표 지점은 비껴간다."""

    catalog = load_objects()
    decorations = []
    skipped = []
    placed_rooms = 0

    for room in layout["rooms"]:
        recipe = LANDMARKS.get(room.name)
        if not recipe:
            continue

        span_c = max(c for _, c, _ in recipe)
        span_r = max(r for _, _, r in recipe)

        # 방 안을 전부 훑되 벽에 붙는 자리를 먼저 본다 — 랜드마크가 통로 한복판에
        # 서면 길을 막고, 방 가장자리에 서야 방의 성격으로 읽힌다.
        cx, cy = room.center
        anchors = sorted(
            (
                (c, r)
                for r in range(room.row, room.row + room.height - span_r)
                for c in range(room.col, room.col + room.width - span_c)
            ),
            key=lambda t: (-((t[0] - cx) ** 2 + (t[1] - cy) ** 2), t[1], t[0]),
        )

        placed = None
        for base_c, base_r in anchors:
            tiles = [(base_c + dc, base_r + dr) for _, dc, dr in recipe]

            if any(not (0 <= c < width and 0 <= r < height) for c, r in tiles):
                continue
            if any(not free[index(width, c, r)] for c, r in tiles):
                continue
            if any((c, r) in spine for c, r in tiles):
                continue
            if any((c, r) in keep_clear for c, r in tiles):
                continue

            placed = (base_c, base_r)
            break

        if placed is None:
            skipped.append(room.name)
            continue

        base_c, base_r = placed
        for key, dc, dr in recipe:
            entry = catalog.get(key)
            if entry is None:
                raise SystemExit(f"매니페스트에 '{key}' 가 없다")

            decorations.append(as_decoration(entry, base_c + dc, base_r + dr))

        placed_rooms += 1

    return decorations, skipped, placed_rooms


def place_decor(free, width, height, spine, layout):
    catalog = load_objects()
    decorations = []
    slot = 0

    for room in layout["rooms"]:
        edges = []
        for c, r in room.tiles():
            if not free[index(width, c, r)]:
                continue
            if (c, r) in spine:
                continue

            clear = all(
                free[index(width, c + dc, r + dr)]
                for dr in (-1, 0, 1)
                for dc in (-1, 0, 1)
            )
            if not clear:
                continue

            near_wall = any(
                not free[index(width, c + dc, r + dr)]
                for dc in (-2, -1, 0, 1, 2)
                for dr in (-2, -1, 0, 1, 2)
            )
            if near_wall:
                edges.append((c, r))

        for i, (c, r) in enumerate(edges):
            if i % 4 != 0:
                continue

            key, _ = WALL_DECOR[slot % len(WALL_DECOR)]
            slot += 1

            entry = catalog.get(key)
            if entry is None:
                raise SystemExit(f"매니페스트에 '{key}' 가 없다")

            piece = as_decoration(entry, c, r)
            piece["collisionEnabled"] = False
            piece["colliderWidth"] = 0.0
            piece["colliderHeight"] = 0.0
            decorations.append(piece)

    return decorations


def build(level, template):
    layout = LAYOUTS[level]
    width, height = layout["size"]
    free = [False] * (width * height)

    by_name = {room.name: room for room in layout["rooms"]}

    for room in layout["rooms"]:
        carve_room(free, width, height, room)

    spine = set()
    for a, b in layout["links"]:
        ca, cb = by_name[a].center, by_name[b].center
        carve_corridor(free, width, height, ca, cb)

        for c in range(min(ca[0], cb[0]), max(ca[0], cb[0]) + 1):
            spine.add((c, ca[1]))
        for r in range(min(ca[1], cb[1]), max(ca[1], cb[1]) + 1):
            spine.add((cb[0], r))

    walls = autotile(free, width, height)
    floor = [FLOOR_GID if free[i] else 0 for i in range(width * height)]
    collision = [WALK if free[i] else BLOCK for i in range(width * height)]

    start_room = by_name[layout["start"]]
    altar_room = by_name[layout["altar"]]

    wides = wide_tiles(free, width, height)
    reach = connected(wides, tuple(start_room.center))

    objects = [point("player_start", *start_room.center, height=height)]
    objects.append(point("exit_door", *altar_room.center, height=height))

    spread = spread_artifacts(layout, by_name, reach)
    for i, (col, row) in enumerate(spread, start=1):
        objects.append(point(f"artifact_{i}", col, row, height))

    spawns = pick_spawns(free, width, height, start_room.center, spread, reach)

    keep_clear = set()
    for o in objects:
        for dr in (-1, 0, 1):
            for dc in (-1, 0, 1):
                keep_clear.add((o["col"] + dc, o["row"] + dr))

    landmarks, skipped, landmark_rooms = place_landmarks(free, width, height, spine, layout, keep_clear)

    taken = {(int(d["x"]) + dc, int(d["y"] - d["height"]) + dr)
             for d in landmarks
             for dc in range(int(d["width"]))
             for dr in range(int(d["height"]))}

    decorations = landmarks + [
        d for d in place_decor(free, width, height, spine, layout)
        if (int(d["x"]), int(d["y"] - d["height"])) not in taken
    ]

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
        "rooms": [
            {"col": r.col, "row": r.row, "width": r.width, "height": r.height}
            for r in layout["rooms"]
        ],
        "decorations": decorations,
    })

    missing = [o["name"] for o in objects if (o["col"], o["row"]) not in reach]
    ratio = len(wides) / max(1, sum(1 for v in free if v)) * 100
    overlaps = overlap_pairs(spread, RADIUS[level])

    print(
        f"L{level} {width}x{height}  통과 {sum(1 for v in free if v)}"
        f"  3x3여유 {len(wides)} ({ratio:.0f}%)  장식 {len(decorations)}"
        f"  랜드마크 {landmark_rooms}/{len(layout['rooms'])}방"
        f"  소리겹침 {overlaps}쌍"
        + ("  전 목표 연결됨" if not missing else f"  미연결 {missing}")
        + (f"  랜드마크실패 {skipped}" if skipped else "")
    )

    return data, (not missing) and overlaps <= 1 and not skipped


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dry-run", action="store_true")
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

        with open(path, "w", encoding="utf-8") as handle:
            json.dump(data, handle, ensure_ascii=False, separators=(",", ":"))

    if not ok:
        raise SystemExit("목표 지점이 넓은 통로로 이어지지 않는다")


if __name__ == "__main__":
    main()
