import argparse
import os
import re
import sys
import xml.etree.ElementTree as ET


def repair(src, dst, tile_px):
    tree = ET.parse(src)
    root = tree.getroot()

    old_tile = int(root.get("tilewidth"))
    if old_tile == tile_px:
        print(f"이미 {tile_px}px 격자다: {src}")
        return False

    scale = tile_px / float(old_tile)

    root.set("tilewidth", str(tile_px))
    root.set("tileheight", str(tile_px))

    for tileset in root.findall("tileset"):
        tileset.set("tilewidth", str(tile_px))
        tileset.set("tileheight", str(tile_px))

        image = tileset.find("image")
        if image is None:
            continue

        for attr in ("width", "height"):
            value = image.get(attr)
            if value:
                image.set(attr, str(int(round(int(value) * scale))))

    moved = 0
    for group in root.findall("objectgroup"):
        for obj in group.findall("object"):
            for attr in ("x", "y", "width", "height"):
                value = obj.get(attr)
                if value is None:
                    continue
                obj.set(attr, f"{float(value) * scale:g}")
            moved += 1

    tree.write(dst, encoding="UTF-8", xml_declaration=True)
    print(f"{os.path.basename(src)}: 격자 {old_tile}->{tile_px}px, 오브젝트 {moved}개 좌표 재계산")
    print(f"  -> {dst}")
    return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("src")
    parser.add_argument("dst")
    parser.add_argument("--tile", type=int, default=32)
    args = parser.parse_args()

    if not os.path.exists(args.src):
        print(f"없는 파일: {args.src}", file=sys.stderr)
        return 2

    os.makedirs(os.path.dirname(os.path.abspath(args.dst)), exist_ok=True)
    return 0 if repair(args.src, args.dst, args.tile) else 1


if __name__ == "__main__":
    sys.exit(main())
