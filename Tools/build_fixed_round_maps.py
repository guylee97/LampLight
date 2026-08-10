#!/usr/bin/env python3
"""Build the three fixed Unity map JSON files from the FullMap TMX layouts."""

import json
import math
import pathlib
import xml.etree.ElementTree as ET
from collections import deque


ROOT = pathlib.Path(__file__).resolve().parents[1]
FULL_MAP = ROOT / "MapSource" / "FullMap"
MAP_SOURCES = FULL_MAP / "Map"
DATA = ROOT / "Assets" / "Resources" / "Data"
CATALOG = DATA / "temple_catalog.json"
OBJECTS = ROOT / "Assets" / "Resources" / "Art" / "Objects"
OBJECT_MANIFEST = DATA / "objects_manifest_merged.json"

ARTIFACT_COUNTS = {1: 2, 2: 3, 3: 4}
SPAWN_COUNT = 8
FLIP_HORIZONTAL = 0x80000000
FLIP_VERTICAL = 0x40000000
FLIP_DIAGONAL = 0x20000000
GID_MASK = 0x0FFFFFFF


def csv_layer(root, name, width, height):
    for layer in root.findall("layer"):
        if layer.get("name", "").lower() == name:
            raw = layer.findtext("data", "")
            values = [int(value.strip()) for value in raw.split(",") if value.strip()]
            if len(values) != width * height:
                raise ValueError(f"{name}: expected {width * height} cells, got {len(values)}")
            return values
    raise ValueError(f"missing '{name}' layer")


def neighbours(cell, width, height):
    col, row = cell
    for dc, dr in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        nc, nr = col + dc, row + dr
        if 0 <= nc < width and 0 <= nr < height:
            yield nc, nr


def distances(start, walkable, width, height):
    found = {start: 0}
    queue = deque([start])
    while queue:
        current = queue.popleft()
        for nxt in neighbours(current, width, height):
            if nxt not in walkable or nxt in found:
                continue
            found[nxt] = found[current] + 1
            queue.append(nxt)
    return found


def farthest(start, walkable, width, height):
    found = distances(start, walkable, width, height)
    return max(found, key=lambda cell: found[cell])


def distributed_cells(walkable, width, height, count):
    first = min(walkable)
    a = farthest(first, walkable, width, height)
    b = farthest(a, walkable, width, height)
    picked = [a, b]

    while len(picked) < count:
        candidate = max(
            (cell for cell in walkable if cell not in picked),
            key=lambda cell: min(
                abs(cell[0] - chosen[0]) + abs(cell[1] - chosen[1])
                for chosen in picked
            ),
        )
        picked.append(candidate)
    return picked


def point(name, cell, height):
    col, row = cell
    return {
        "name": name,
        "col": col,
        "row": row,
        "x": col + 0.5,
        "y": height - row - 0.5,
    }


def room_bounds(root, width, height, walkable):
    rooms = []
    for group in root.findall("objectgroup"):
        objects = group.findall("object")
        if not objects:
            continue
        cols = []
        rows = []
        for obj in objects:
            x = float(obj.get("x", 0)) / 64.0
            y = float(obj.get("y", 0)) / 64.0
            w = max(1.0, float(obj.get("width", 64)) / 64.0)
            h = max(1.0, float(obj.get("height", 64)) / 64.0)
            cols.extend((math.floor(x), math.ceil(x + w) - 1))
            rows.extend((math.floor(y - h), math.ceil(y) - 1))

        left = max(0, min(cols) - 1)
        right = min(width - 1, max(cols) + 1)
        top = max(0, min(rows) - 1)
        bottom = min(height - 1, max(rows) + 1)
        cells = [
            (col, row)
            for row in range(top, bottom + 1)
            for col in range(left, right + 1)
            if (col, row) in walkable
        ]
        if not cells:
            continue
        left = min(cell[0] for cell in cells)
        right = max(cell[0] for cell in cells)
        top = min(cell[1] for cell in cells)
        bottom = max(cell[1] for cell in cells)
        rooms.append(
            {
                "col": left,
                "row": top,
                "width": right - left + 1,
                "height": bottom - top + 1,
            }
        )
    return rooms


def resource_table():
    manifest = json.loads(OBJECT_MANIFEST.read_text(encoding="utf-8"))
    return {
        pathlib.Path(entry["file"]).stem: entry
        for entry in manifest["objects"]
    }


def object_tileset(map_path, root):
    for tileset in root.findall("tileset"):
        source = tileset.get("source")
        tileset_root = (
            ET.parse(map_path.parent / source).getroot()
            if source
            else tileset
        )
        if tileset_root.get("name") != "one_lantern_objects":
            continue

        table = {}
        for tile in tileset_root.findall("tile"):
            image = tile.find("image")
            if image is not None:
                table[int(tile.get("id"))] = {
                    "name": pathlib.Path(image.get("source")).stem,
                    "width": int(image.get("width", 64)),
                    "height": int(image.get("height", 64)),
                }
        return int(tileset.get("firstgid")), table

    raise ValueError(f"{map_path}: object tileset is missing")


def fixed_decorations(map_path, root, level, table):
    first_gid, tile_names = object_tileset(map_path, root)
    decorations = []

    for obj in root.findall(".//object[@gid]"):
        raw_gid = int(obj.get("gid"))
        tile_id = (raw_gid & GID_MASK) - first_gid
        tile_image = tile_names.get(tile_id)
        if tile_image is None:
            continue
        image_name = tile_image["name"]

        entry = table.get(image_name)
        if entry is None:
            fallback_stems = {
                "extra_cobweb_corner_b": "obj_cobweb_corner",
                "extra_container_stone_box": "obj_container_crate",
                "extra_prop_skull_b": "obj_prop_skull",
                "obj_large_carpet_runner_v": "obj_large_carpet_runner",
            }
            resource_categories = {
                "extra_cobweb_corner_b": "cobweb",
                "extra_container_stone_box": "container",
                "extra_prop_skull_b": "prop",
                "obj_large_carpet_runner_v": "large",
            }
            fallback = table.get(fallback_stems.get(image_name))
            if fallback is None:
                raise ValueError(
                    f"Round{level}: no Unity resource mapping for {image_name}"
                )
            entry = dict(fallback)
            entry["key"] = image_name.removeprefix("obj_")
            entry["resource"] = (
                f"Art/Objects/{resource_categories[image_name]}/{image_name}"
            )

        # Gameplay artifacts and the exit are instantiated by MapObjectPlacer.
        if entry["key"].startswith("artifact_") or entry["key"].startswith("exit_"):
            continue

        sorting_offset = 0
        sorting_property = obj.find('properties/property[@name="sortingOffset"]')
        if sorting_property is not None:
            sorting_offset = int(sorting_property.get("value", 0))

        width = float(obj.get("width", 64)) / 64.0
        height = float(obj.get("height", 64)) / 64.0
        if "candle" in image_name:
            width = tile_image["width"] / 64.0
            height = tile_image["height"] / 64.0

        collision = dict(entry["collision"])
        collision_overrides = {
            "obj_prop_railing": (0.90, 0.30, 0.0, 0.15),
            "obj_prop_basin": (0.70, 0.45, 0.0, 0.23),
        }
        if image_name in collision_overrides:
            collider = collision_overrides[image_name]
            collision = {
                "enabled": True,
                "width": collider[0],
                "height": collider[1],
                "offsetX": collider[2],
                "offsetY": collider[3],
            }

        decorations.append(
            {
                "key": entry["key"],
                "resource": entry["resource"],
                "x": float(obj.get("x", 0)) / 64.0,
                "y": float(obj.get("y", 0)) / 64.0,
                "width": width,
                "height": height,
                "flipHorizontal": bool(raw_gid & FLIP_HORIZONTAL),
                "flipVertical": bool(raw_gid & FLIP_VERTICAL),
                "flipDiagonal": bool(raw_gid & FLIP_DIAGONAL),
                "collisionEnabled": bool(collision["enabled"]),
                "colliderWidth": float(collision["width"]),
                "colliderHeight": float(collision["height"]),
                "colliderOffsetX": float(collision["offsetX"]),
                "colliderOffsetY": float(collision["offsetY"]),
                "sortingOffset": sorting_offset,
            }
        )

    return decorations


def build(level, catalog, object_table):
    map_path = MAP_SOURCES / f"Level{level}" / "Map.tmx"
    tree = ET.parse(map_path)
    root = tree.getroot()
    width = int(root.get("width"))
    height = int(root.get("height"))
    tile_size = int(root.get("tilewidth"))
    source_floor = csv_layer(root, "floor", width, height)
    walkable = {
        (index % width, index // width)
        for index, gid in enumerate(source_floor)
        if gid != 0
    }
    if not walkable:
        raise ValueError(f"Round{level}: no walkable floor")

    floor = [0] * (width * height)
    walls = [0] * (width * height)
    deco = [0] * (width * height)
    base_floor_gid = 1
    wall_base = int(catalog["autotileBase"])

    for row in range(height):
        for col in range(width):
            index = row * width + col
            if (col, row) in walkable:
                floor[index] = base_floor_gid
                continue

            mask = 0
            for dc, dr, bit in (
                (0, -1, int(catalog["maskNorth"])),
                (1, 0, int(catalog["maskEast"])),
                (0, 1, int(catalog["maskSouth"])),
                (-1, 0, int(catalog["maskWest"])),
            ):
                adjacent = (col + dc, row + dr)
                if adjacent not in walkable:
                    mask |= bit
            walls[index] = wall_base + mask + 1

    artifact_count = ARTIFACT_COUNTS[level]
    selected = distributed_cells(
        walkable, width, height, 2 + artifact_count + SPAWN_COUNT
    )
    start, exit_cell = selected[0], selected[1]
    artifact_cells = selected[2 : 2 + artifact_count]
    spawn_cells = selected[2 + artifact_count :]

    objects = [point("player_start", start, height), point("exit_door", exit_cell, height)]
    objects.extend(
        point(f"artifact_{index + 1}", cell, height)
        for index, cell in enumerate(artifact_cells)
    )

    tile_props = [
        {
            "gid": int(tile["id"]) + 1,
            "walkable": bool(tile["walkable"]),
            "noisy": bool(tile["noisy"]),
        }
        for tile in catalog["tiles"]
    ]

    result = {
        "width": width,
        "height": height,
        "tileSize": tile_size,
        "pixelWidth": width * tile_size,
        "pixelHeight": height * tile_size,
        "floor": floor,
        "walls": walls,
        "deco": deco,
        "tileProps": tile_props,
        "tilesets": [
            {
                "firstGid": 1,
                "count": max(int(tile["id"]) for tile in catalog["tiles"]) + 1,
                "name": "Temple",
            }
        ],
        "objects": objects,
        "spawns": [
            point(f"SP{index + 1}", cell, height)
            for index, cell in enumerate(spawn_cells)
        ],
        "rooms": room_bounds(root, width, height, walkable),
        "decorations": fixed_decorations(
            map_path, root, level, object_table
        ),
    }

    output = DATA / f"map_l{level}.json"
    output.write_text(
        json.dumps(result, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )
    return output, width, height, len(walkable)


def main():
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    object_table = resource_table()
    for level in (1, 2, 3):
        output, width, height, walkable = build(level, catalog, object_table)
        data = json.loads(output.read_text(encoding="utf-8"))
        print(
            f"{output.relative_to(ROOT)}: {width}x{height}, {walkable} walkable cells, "
            f"{len(data['decorations'])} decorations"
        )


if __name__ == "__main__":
    main()
