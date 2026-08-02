import argparse
import json
import os
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_SRC = os.path.join(ROOT, "SetPieces")
DEFAULT_DST = os.path.join(ROOT, "Assets", "Resources", "Data", "setpiece_catalog.json")
CATALOG = os.path.join(ROOT, "Assets", "Resources", "Data", "temple_catalog.json")

OBJECT_LAYERS = ("deco", "objects", "꾸밈", "장식")
DEFAULT_TAG = "generic"


def map_properties(root):
    props = {}
    block = root.find("properties")
    if block is None:
        return props
    for prop in block.findall("property"):
        props[prop.get("name")] = prop.get("value")
    return props


def convert(path, known_keys, problems):
    root = ET.parse(path).getroot()
    name = os.path.splitext(os.path.basename(path))[0]

    tile_w = int(root.get("tilewidth"))
    tile_h = int(root.get("tileheight"))
    if tile_w != tile_h:
        problems.append(f"{name}: 정사각 타일이 아니다 ({tile_w}x{tile_h})")
        return None

    cols = int(root.get("width"))
    rows = int(root.get("height"))
    props = map_properties(root)

    groups = [g for g in root.findall("objectgroup")
              if (g.get("name") or "").strip().lower() in OBJECT_LAYERS]
    if not groups:
        names = [g.get("name") for g in root.findall("objectgroup")]
        problems.append(f"{name}: 오브젝트 레이어 이름이 {OBJECT_LAYERS} 중 하나여야 한다 (발견: {names})")
        return None

    entries = []
    for group in groups:
        for obj in group.findall("object"):
            key = (obj.get("name") or "").strip()
            if not key:
                problems.append(f"{name}: 이름 없는 오브젝트 (id={obj.get('id')})")
                continue
            if key not in known_keys:
                problems.append(f"{name}: 카탈로그에 없는 키 '{key}'")
                continue

            entries.append({
                "key": key,
                "tileX": float(obj.get("x", 0)) / tile_w,
                "tileY": float(obj.get("y", 0)) / tile_h,
            })

    if not entries:
        problems.append(f"{name}: 배치된 오브젝트가 없다")
        return None

    for entry in entries:
        if not (0.0 <= entry["tileX"] <= cols and 0.0 <= entry["tileY"] <= rows):
            problems.append(f"{name}: '{entry['key']}' 이 {cols}x{rows} 밖에 있다 "
                            f"({entry['tileX']:.2f}, {entry['tileY']:.2f})")

    return {
        "name": name,
        "tag": props.get("tag", DEFAULT_TAG),
        "cols": cols,
        "rows": rows,
        "objects": entries,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", default=DEFAULT_SRC)
    parser.add_argument("--dst", default=DEFAULT_DST)
    parser.add_argument("--strict", action="store_true")
    args = parser.parse_args()

    if not os.path.isdir(args.src):
        print(f"없는 폴더: {args.src}", file=sys.stderr)
        return 2

    with open(CATALOG, encoding="utf-8") as handle:
        known_keys = {o["key"] for o in json.load(handle)["objects"]}

    problems = []
    pieces = []

    for name in sorted(os.listdir(args.src)):
        if not name.endswith(".tmx"):
            continue
        piece = convert(os.path.join(args.src, name), known_keys, problems)
        if piece:
            pieces.append(piece)

    for problem in problems:
        print(f"  ! {problem}", file=sys.stderr)

    if not pieces:
        print("세트를 하나도 못 읽었다", file=sys.stderr)
        return 2

    if problems and args.strict:
        return 1

    with open(args.dst, "w", encoding="utf-8") as handle:
        json.dump({"pieces": pieces}, handle, ensure_ascii=False, indent=1)
        handle.write("\n")

    for piece in pieces:
        print(f"{piece['name']:20s} {piece['cols']}x{piece['rows']}  "
              f"tag={piece['tag']:8s} 오브젝트 {len(piece['objects'])}")

    print(f"-> {args.dst}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
