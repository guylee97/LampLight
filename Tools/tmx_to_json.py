
import argparse
import json
import os
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_SRC = os.path.join(ROOT, "Map", "one_lantern_demo60.tmx")
DEFAULT_DST = os.path.join(ROOT, "Assets", "Resources", "Data", "MapData.json")
DEMO_NAME = "one_lantern_demo60.tmx"

FLIP_FLAGS = 0x80000000 | 0x40000000 | 0x20000000

LAYER_NAMES = ("floor", "walls", "deco")

INSET_TILES = 8

LAYER_ALIASES = {
    "floor": "floor", "바닥": "floor", "바닥레이어": "floor",
    "walls": "walls", "벽": "walls", "벽레이어": "walls", "wall": "walls",
    "deco": "deco", "꾸밈": "deco", "장식": "deco", "decoration": "deco",
}

TILESET_KEYWORDS = (
    ("walls", ("벽", "옆면", "윗면", "wall", "side", "top")),
    ("floor", ("바닥", "floor", "ground")),
    ("deco", ("꾸밈", "장식", "deco")),
)

NOISY_KEYWORDS = ("noisy", "노이즈", "자갈", "gravel", "debris", "rubble")


CATALOG_PATH = os.path.join(ROOT, "Assets", "Resources", "Data", "temple_catalog.json")


def load_catalog():
    if not os.path.exists(CATALOG_PATH):
        return None
    with open(CATALOG_PATH, encoding="utf-8") as handle:
        return json.load(handle)


def catalog_props(catalog, first_gid, declared_gids):
    added = []
    for tile in catalog["tiles"]:
        gid = first_gid + tile["id"]
        if gid in declared_gids:
            continue
        added.append({"gid": gid, "walkable": tile["walkable"], "noisy": tile["noisy"]})
    return added


def canonical_layer_name(raw):
    key = (raw or "").strip().replace(" ", "").replace("_", "").lower()
    return LAYER_ALIASES.get(key)


def classify_tileset(name):
    lowered = (name or "").lower()
    for target, keywords in TILESET_KEYWORDS:
        for keyword in keywords:
            if keyword in lowered:
                return target
    return "deco"


def parse_csv(text):
    return [int(v) & ~FLIP_FLAGS for v in text.replace("\n", "").split(",") if v.strip()]


def parse_layer_sparse(layer):
    data = layer.find("data")
    if data is None:
        raise ValueError(f"data 없음: {layer.get('name')}")
    if data.get("encoding") != "csv":
        raise ValueError(f"지원하지 않는 인코딩: {layer.get('name')} ({data.get('encoding')})")

    cells = {}
    chunks = data.findall("chunk")

    if chunks:
        for chunk in chunks:
            ox = int(chunk.get("x"))
            oy = int(chunk.get("y"))
            cw = int(chunk.get("width"))
            values = parse_csv(chunk.text)
            for i, gid in enumerate(values):
                if gid:
                    cells[(ox + i % cw, oy + i // cw)] = gid
        return cells

    width = int(layer.get("width"))
    for i, gid in enumerate(parse_csv(data.text)):
        if gid:
            cells[(i % width, i // width)] = gid
    return cells


def bounds_of(layers):
    coords = [xy for cells in layers.values() for xy in cells]
    if not coords:
        raise ValueError("모든 레이어가 비어 있다")
    xs = [c[0] for c in coords]
    ys = [c[1] for c in coords]
    return min(xs), min(ys), max(xs), max(ys)


def densify(cells, min_x, min_y, width, height):
    grid = [0] * (width * height)
    for (x, y), gid in cells.items():
        grid[(y - min_y) * width + (x - min_x)] = gid
    return grid


def split_single_layer(cells, tileset_targets, notes):
    out = {name: {} for name in LAYER_NAMES}
    for xy, gid in cells.items():
        out[tileset_targets.get(gid, "deco")][xy] = gid

    summary = ", ".join(f"{name}={len(out[name])}칸" for name in LAYER_NAMES)
    notes.append(f"타일 레이어가 1장뿐이라 타일셋 이름으로 자동 분리했다 ({summary})")
    return out


def parse_tile_props(tileset):
    first_gid = int(tileset.get("firstgid"))
    out = []
    for tile in tileset.findall("tile"):
        gid = first_gid + int(tile.get("id"))
        entry = {"gid": gid, "walkable": False, "noisy": False}
        props = tile.find("properties")
        if props is not None:
            for p in props.findall("property"):
                name = p.get("name")
                if name in entry:
                    entry[name] = p.get("value") == "true"
        out.append(entry)
    return out


def infer_tile_props(tileset, target):
    first_gid = int(tileset.get("firstgid"))
    count = int(tileset.get("tilecount") or 1)
    lowered = (tileset.get("name") or "").lower()
    walkable = target != "walls"
    noisy = any(keyword in lowered for keyword in NOISY_KEYWORDS)
    return [
        {"gid": first_gid + offset, "walkable": walkable, "noisy": noisy}
        for offset in range(count)
    ]


def to_unity(x_px, y_px, tile_size, pixel_height):
    return round(x_px / tile_size, 4), round((pixel_height - y_px) / tile_size, 4)


def parse_objects(group, tile_size, pixel_height, offset_x_px, offset_y_px):
    out = []
    if group is None:
        return out

    for obj in group.findall("object"):
        x_px = float(obj.get("x")) - offset_x_px
        y_px = float(obj.get("y")) - offset_y_px
        ux, uy = to_unity(x_px, y_px, tile_size, pixel_height)
        out.append({
            "name": obj.get("name"),
            "col": int(x_px // tile_size),
            "row": int(y_px // tile_size),
            "x": ux,
            "y": uy,
        })
    return out


def walkable_cells(walls, width, height):
    return [(c, r) for r in range(height) for c in range(width) if walls[r * width + c] == 0]


def open_cells(walls, width, height):
    out = []
    for c, r in walkable_cells(walls, width, height):
        if all(
            0 <= c + dc < width and 0 <= r + dr < height and walls[(r + dr) * width + (c + dc)] == 0
            for dc in (-1, 0, 1)
            for dr in (-1, 0, 1)
        ):
            out.append((c, r))

    return out or walkable_cells(walls, width, height)


def bfs_far(walls, width, height, start):
    from collections import deque

    seen = {start: 0}
    q = deque([start])
    far = start
    while q:
        c, r = q.popleft()
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (c + dc, r + dr)
            if n in seen:
                continue
            if not (0 <= n[0] < width and 0 <= n[1] < height):
                continue
            if walls[n[1] * width + n[0]] != 0:
                continue
            seen[n] = seen[(c, r)] + 1
            if seen[n] > seen[far]:
                far = n
            q.append(n)

    return far, seen


def dead_ends(walls, width, height):
    out = []
    for c, r in walkable_cells(walls, width, height):
        n = sum(
            1
            for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1))
            if 0 <= c + dc < width and 0 <= r + dr < height and walls[(r + dr) * width + (c + dc)] == 0
        )
        if n <= 2:
            out.append((c, r))

    return out


def nearest_open(cell, room):
    if not room:
        return cell
    return min(room, key=lambda c: (c[0] - cell[0]) ** 2 + (c[1] - cell[1]) ** 2)


def as_point(name, cell, height):
    col, row = cell
    return {
        "name": name,
        "col": col,
        "row": row,
        "x": round(col + 0.5, 4),
        "y": round((height - 1 - row) + 0.5, 4),
    }


def spread(cells, count, height):
    if not cells:
        return []

    picked = [cells[len(cells) // 2]]
    while len(picked) < count and len(picked) < len(cells):
        best = max(cells, key=lambda c: min((c[0] - p[0]) ** 2 + (c[1] - p[1]) ** 2 for p in picked))
        picked.append(best)

    return picked


def synth_objects(walls, width, height):
    cells = walkable_cells(walls, width, height)
    if not cells:
        return []

    room = set(open_cells(walls, width, height))

    inset = INSET_TILES
    inner = {c for c in room
             if inset <= c[0] < width - inset and inset <= c[1] < height - inset}
    room = inner or room

    corner, _ = bfs_far(walls, width, height, cells[0])
    far_corner, _ = bfs_far(walls, width, height, corner)

    start = nearest_open(corner, room)
    exit_cell = nearest_open(far_corner, room)

    taken = {start, exit_cell}
    pockets = [c for c in dead_ends(walls, width, height) if c not in taken]
    artifacts = spread(pockets, 4, height) if len(pockets) >= 4 else \
        spread([c for c in room if c not in taken], 4, height)

    out = [as_point("player_start", start, height), as_point("exit_door", exit_cell, height)]
    for i, cell in enumerate(artifacts):
        out.append(as_point(f"artifact_{i + 1}", cell, height))

    return out


def synth_spawns(walls, width, height):
    cells = open_cells(walls, width, height)
    names = ["SP1_WN", "SP2_EN", "SP3_WN", "SP4_CM", "SP5_WS", "SP6_ES", "SP7_CS", "SP8_ES"]
    return [as_point(n, c, height) for n, c in zip(names, spread(cells, len(names), height))]


def convert(src):
    notes = []
    root = ET.parse(src).getroot()

    tile_size = int(root.get("tilewidth"))
    if tile_size != int(root.get("tileheight")):
        raise ValueError("정사각 타일이 아니다")

    catalog = load_catalog()

    tilesets = root.findall("tileset")
    tile_props = []
    tileset_targets = {}
    for ts in tilesets:
        first_gid = int(ts.get("firstgid"))
        target = classify_tileset(ts.get("name"))

        props = parse_tile_props(ts)
        if not props:
            props = infer_tile_props(ts, target)

        if catalog:
            declared = {p["gid"] for p in props}
            filled = catalog_props(catalog, first_gid, declared)
            if filled:
                notes.append(f"타일셋 '{ts.get('name')}': 미선언 타일 {len(filled)}개를 카탈로그에서 채웠다")
            props.extend(filled)

        tile_props.extend(props)

        for offset in range(int(ts.get("tilecount") or 1)):
            tileset_targets[first_gid + offset] = target

    if catalog:
        walkable_by_gid = {p["gid"]: p["walkable"] for p in tile_props}
        for gid, walkable in walkable_by_gid.items():
            if gid in tileset_targets:
                tileset_targets[gid] = "floor" if walkable else "walls"

    raw_layers = root.findall("layer")
    if not raw_layers:
        raise ValueError("타일 레이어가 없다")

    named = {}
    unnamed = []
    for layer in raw_layers:
        canonical = canonical_layer_name(layer.get("name"))
        if canonical and canonical not in named:
            named[canonical] = parse_layer_sparse(layer)
        else:
            unnamed.append((layer.get("name"), parse_layer_sparse(layer)))

    if len(named) == len(LAYER_NAMES):
        layers = named
    elif not named and len(unnamed) == 1:
        layers = split_single_layer(unnamed[0][1], tileset_targets, notes)
    else:
        have = sorted(named) or [n for n, _ in unnamed]
        raise ValueError(
            f"레이어 이름이 floor/walls/deco와 맞지 않다 (발견: {have}). "
            "Tiled에서 레이어를 floor/walls/deco 3장으로 나누거나, 타일 레이어를 1장만 두면 자동 분리한다"
        )

    min_x, min_y, max_x, max_y = bounds_of(layers)
    width = max_x - min_x + 1
    height = max_y - min_y + 1

    declared_w = int(root.get("width") or 0)
    declared_h = int(root.get("height") or 0)
    if root.get("infinite") == "1":
        notes.append(f"무한 맵이라 실제 타일 범위로 잘랐다 ({declared_w}x{declared_h} 선언 → {width}x{height})")
    elif (declared_w, declared_h) != (width, height):
        notes.append(f"선언 크기 {declared_w}x{declared_h} → 실제 사용 범위 {width}x{height}")

    pixel_width = width * tile_size
    pixel_height = height * tile_size

    groups = {g.get("name"): g for g in root.findall("objectgroup")}
    for name in ("objects", "spawns"):
        if name not in groups:
            notes.append(f"오브젝트 그룹 '{name}' 없음 — 통행 가능 칸에서 자동 생성한다")

    walls_grid = densify(layers["walls"], min_x, min_y, width, height)
    objects = parse_objects(groups.get("objects"), tile_size, pixel_height,
                            min_x * tile_size, min_y * tile_size)
    spawns = parse_objects(groups.get("spawns"), tile_size, pixel_height,
                           min_x * tile_size, min_y * tile_size)

    if not objects:
        objects = synth_objects(walls_grid, width, height)
        notes.append(f"objects 자동 생성: {', '.join(o['name'] for o in objects)}")
    if not spawns:
        spawns = synth_spawns(walls_grid, width, height)
        notes.append(f"spawns 자동 생성: {len(spawns)}곳")

    return {
        "width": width,
        "height": height,
        "tileSize": tile_size,
        "pixelWidth": pixel_width,
        "pixelHeight": pixel_height,
        "floor": densify(layers["floor"], min_x, min_y, width, height),
        "walls": walls_grid,
        "deco": densify(layers["deco"], min_x, min_y, width, height),
        "tileProps": tile_props,
        "tilesets": [
            {
                "firstGid": int(ts.get("firstgid")),
                "count": int(ts.get("tilecount") or 1),
                "name": ts.get("name") or "",
            }
            for ts in tilesets
        ],
        "objects": objects,
        "spawns": spawns,
    }, notes


def verify_demo(m):
    problems = []
    total = m["width"] * m["height"]
    walkable = sum(1 for v in m["walls"] if v == 0)

    expected_artifacts = {
        "artifact_1": (6, 4),
        "artifact_2": (27, 4),
        "artifact_3": (27, 17),
        "artifact_4": (6, 17),
    }
    by_name = {o["name"]: o for o in m["objects"]}
    for name, tile in expected_artifacts.items():
        o = by_name.get(name)
        if o is None:
            problems.append(f"{name} 없음")
        elif (o["col"], o["row"]) != tile:
            problems.append(f"{name} 타일 {(o['col'], o['row'])} != 스펙 {tile}")

    expected_spawns = ["SP1_WN", "SP2_EN", "SP3_WN", "SP4_CM",
                       "SP5_WS", "SP6_ES", "SP7_CS", "SP8_ES"]
    got = [s["name"] for s in m["spawns"]]
    if got != expected_spawns:
        problems.append(f"앵커 불일치: {got}")

    if total != 748:
        problems.append(f"전체 칸 {total} != 748")
    if walkable != 297:
        problems.append(f"이동 가능 {walkable} != 스펙 297")

    return problems


def verify_generic(m):
    problems = []
    expected = m["width"] * m["height"]

    for name in LAYER_NAMES:
        if len(m[name]) != expected:
            problems.append(f"{name} 셀 수 {len(m[name])} != {expected}")

    if sum(1 for v in m["walls"] if v == 0) == 0:
        problems.append("이동 가능한 칸이 하나도 없다")

    return problems


def main():
    parser = argparse.ArgumentParser(description="Tiled TMX를 MapData.json으로 변환한다")
    parser.add_argument("src", nargs="?", default=DEFAULT_SRC, help="입력 .tmx")
    parser.add_argument("-o", "--out", default=DEFAULT_DST, help="출력 .json")
    parser.add_argument("--dry-run", action="store_true", help="파일을 쓰지 않고 결과만 출력한다")
    args = parser.parse_args()

    m, notes = convert(args.src)
    is_demo = os.path.basename(args.src) == DEMO_NAME
    problems = verify_demo(m) if is_demo else verify_generic(m)

    walkable = sum(1 for v in m["walls"] if v == 0)
    total = m["width"] * m["height"]

    if not args.dry_run:
        os.makedirs(os.path.dirname(args.out), exist_ok=True)
        with open(args.out, "w", encoding="utf-8") as f:
            json.dump(m, f, ensure_ascii=False, separators=(",", ":"))

    print(f"입력  {args.src}")
    if args.dry_run:
        print("출력  (dry-run, 쓰지 않음)")
    else:
        print(f"출력  {args.out}  ({os.path.getsize(args.out):,} bytes)")
    print()
    print(f"맵        {m['width']}x{m['height']} 타일, {m['tileSize']}px")
    print(f"픽셀      {m['pixelWidth']}x{m['pixelHeight']}")
    print(f"이동가능  {walkable}/{total} ({100 * walkable / total:.0f}%)")
    print(f"타일속성  {len(m['tileProps'])}개")
    print(f"오브젝트  {len(m['objects'])}개")
    print(f"스폰앵커  {len(m['spawns'])}개")

    if notes:
        print()
        print("변환 메모")
        for n in notes:
            print(f"  · {n}")

    artifacts = [o for o in m["objects"] if (o["name"] or "").startswith("artifact")]
    if artifacts:
        print()
        print("유물 좌표 (타일 → Unity 월드)")
        for o in artifacts:
            print(f"  {o['name']:<12} ({o['col']:>2},{o['row']:>2})  →  ({o['x']:>7}, {o['y']:>7})")

    print()
    if problems:
        print("검증 실패")
        for p in problems:
            print(f"  ✗ {p}")
        sys.exit(1)
    print("검증 통과" + (" (데모 스펙 대조)" if is_demo else ""))


if __name__ == "__main__":
    main()
