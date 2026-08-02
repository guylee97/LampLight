import argparse
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA_DIR = os.path.join(ROOT, "Assets", "Resources", "Data")

DEFAULT_OBJECTS = os.path.join(DATA_DIR, "objects_manifest.json")
DEFAULT_TILESET = os.path.join(DATA_DIR, "tileset_manifest.json")
DEFAULT_DST = os.path.join(DATA_DIR, "temple_catalog.json")

OBJECT_RESOURCE_ROOT = "Art/Objects"


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def autotile_base(tileset):
    auto = tileset.get("autotile") or {}
    formula = auto.get("formula", "")
    match = re.search(r"(\d+)\s*\+\s*mask", formula)
    if not match:
        raise ValueError(f"autotile.formula 에서 base 를 못 읽었다: {formula!r}")
    return int(match.group(1))


def autotile_bits(tileset):
    auto = tileset.get("autotile") or {}
    bits = auto.get("bits") or {}
    out = {}
    for value, name in bits.items():
        out[name.upper()] = int(value)
    for name in ("N", "E", "S", "W"):
        if name not in out:
            raise ValueError(f"autotile.bits 에 {name} 없음: {bits!r}")
    return out


def convert_tiles(tileset):
    rows = []
    for raw_id, tile in sorted(tileset["tiles"].items(), key=lambda kv: int(kv[0])):
        rows.append({
            "id": int(raw_id),
            "name": tile.get("name", ""),
            "walkable": bool(tile.get("walkable", False)),
            "noisy": bool(tile.get("noisy", False)),
        })
    return rows


def resource_path(file_path):
    stem, _ = os.path.splitext(file_path)
    parts = stem.split("/")
    if parts and parts[0] == "objects":
        parts = parts[1:]
    return "/".join([OBJECT_RESOURCE_ROOT] + parts)


def convert_objects(objects):
    rows = []
    for key, entry in sorted(objects["objects"].items()):
        px = entry.get("px") or [0, 0]
        rows.append({
            "key": key,
            "file": entry["file"],
            "resource": resource_path(entry["file"]),
            "category": entry.get("category", ""),
            "footprint": entry.get("footprint", ""),
            "w": int(px[0]),
            "h": int(px[1]),
        })
    return rows


def convert(objects_path, tileset_path):
    objects = load(objects_path)
    tileset = load(tileset_path)

    bits = autotile_bits(tileset)

    return {
        "tilePx": int(tileset.get("tilePx", 32)),
        "displayScale": int(tileset.get("displayScale", 2)),
        "columns": int(tileset.get("columns", 8)),
        "autotileBase": autotile_base(tileset),
        "maskNorth": bits["N"],
        "maskEast": bits["E"],
        "maskSouth": bits["S"],
        "maskWest": bits["W"],
        "tiles": convert_tiles(tileset),
        "objects": convert_objects(objects),
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--objects", default=DEFAULT_OBJECTS)
    parser.add_argument("--tileset", default=DEFAULT_TILESET)
    parser.add_argument("--dst", default=DEFAULT_DST)
    args = parser.parse_args()

    for path in (args.objects, args.tileset):
        if not os.path.exists(path):
            print(f"없는 파일: {path}", file=sys.stderr)
            return 2

    catalog = convert(args.objects, args.tileset)

    with open(args.dst, "w", encoding="utf-8") as handle:
        json.dump(catalog, handle, ensure_ascii=False, indent=1)
        handle.write("\n")

    print(f"타일 {len(catalog['tiles'])}종 · 오브젝트 {len(catalog['objects'])}종 -> {args.dst}")
    print(f"오토타일 base={catalog['autotileBase']} "
          f"N={catalog['maskNorth']} E={catalog['maskEast']} "
          f"S={catalog['maskSouth']} W={catalog['maskWest']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
