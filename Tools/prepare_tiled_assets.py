#!/usr/bin/env python3
"""Preserve the raw asset metadata and rebuild the Tiled tilesets used by FullMap."""

import json
import pathlib
import re
import shutil
import xml.etree.ElementTree as ET


ROOT = pathlib.Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "Assets" / "Resources"
RAW = RESOURCES / "Rawzip"
OBJECTS = RESOURCES / "Art" / "Objects"
TILES = RESOURCES / "Tileset" / "tiles_single"
DATA = RESOURCES / "Data"
FULL_MAP = RESOURCES / "FullMap"
MERGED = DATA / "objects_manifest_merged.json"


def raw_pack(version):
    matches = list(RAW.glob(f"assets_{version}-*/assets_{version}"))
    if len(matches) != 1:
        raise FileNotFoundError(f"expected one Rawzip assets_{version} pack, got {len(matches)}")
    return matches[0]


def collision_for(key, category):
    if category == "container":
        return {"enabled": True, "width": 0.8, "height": 0.5, "offsetX": 0.0, "offsetY": 0.25}

    if category == "prop":
        if any(word in key for word in ("pillar", "gravestone")):
            return {"enabled": True, "width": 0.7, "height": 0.45, "offsetX": 0.0, "offsetY": 0.225}
        if key == "prop_bench":
            return {"enabled": True, "width": 0.85, "height": 0.4, "offsetX": 0.0, "offsetY": 0.2}

    if category != "large":
        return {"enabled": False, "width": 0.0, "height": 0.0, "offsetX": 0.0, "offsetY": 0.0}

    if "carpet" in key or key == "large_steps":
        return {"enabled": False, "width": 0.0, "height": 0.0, "offsetX": 0.0, "offsetY": 0.0}

    sizes = {
        "large_benches_pair": (1.8, 0.55),
        "large_firepit": (2.0, 1.1),
        "large_sacrificial_slab": (2.0, 0.75),
        "large_shelf_fallen": (2.0, 0.65),
        "large_statue_fallen": (1.8, 0.75),
        "large_statue_kneeling": (1.35, 0.65),
    }
    width, height = sizes.get(key, (1.4, 0.65))
    return {
        "enabled": True,
        "width": width,
        "height": height,
        "offsetX": 0.0,
        "offsetY": height * 0.5,
    }


def preserve_and_merge():
    if RAW.exists() is False:
        if MERGED.exists() is False:
            raise FileNotFoundError("Rawzip and the preserved merged manifest are both missing")
        return json.loads(MERGED.read_text(encoding="utf-8"))

    packs = [
        (raw_pack("v1"), "objects_manifest.json"),
        (raw_pack("v2"), "objects_manifest_v2.json"),
    ]
    merged = {}

    for pack, manifest_name in packs:
        manifest = json.loads((pack / manifest_name).read_text(encoding="utf-8"))
        for key, source in manifest["objects"].items():
            relative = pathlib.Path(source["file"]).relative_to("objects")
            raw_image = pack / "objects" / relative
            destination = OBJECTS / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            if destination.exists() is False:
                shutil.copy2(raw_image, destination)

            entry = {
                "key": key,
                "file": source["file"],
                "resource": destination.relative_to(RESOURCES).with_suffix("").as_posix(),
                "category": source["category"],
                "footprint": source["footprint"],
                "px": source["px"],
                "collision": collision_for(key, source["category"]),
            }
            merged[key] = entry

    ordered = sorted(merged.values(), key=lambda entry: pathlib.Path(entry["file"]).name)
    result = {
        "schema": "lamplight.objects.merged.v1",
        "tileSizePx": 32,
        "displayScale": 2,
        "objects": ordered,
    }
    MERGED.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return result


def image_element(parent, source, image):
    from PIL import Image

    width, height = Image.open(image).size
    ET.SubElement(parent, "image", source=source, width=str(width), height=str(height))


def write_objects_tsx(manifest):
    root = ET.Element(
        "tileset",
        version="1.10",
        tiledversion="1.12.2",
        name="one_lantern_objects",
        tilewidth="96",
        tileheight="96",
        tilecount=str(len(manifest["objects"])),
        columns="0",
    )
    ET.SubElement(root, "grid", orientation="orthogonal", width="1", height="1")

    for tile_id, entry in enumerate(manifest["objects"]):
        tile = ET.SubElement(root, "tile", id=str(tile_id))
        path = RESOURCES / f"{entry['resource']}.png"
        image_element(tile, f"../{path.relative_to(RESOURCES).as_posix()}", path)

    write_xml(FULL_MAP / "objects.tsx", root)


def write_tiles_tsx():
    images = sorted(TILES.glob("tile_*.png"), key=lambda path: path.name)
    root = ET.Element(
        "tileset",
        version="1.10",
        tiledversion="1.12.2",
        name="one_lantern_tiles",
        tilewidth="32",
        tileheight="32",
        tilecount=str(len(images)),
        columns="0",
    )
    ET.SubElement(root, "grid", orientation="orthogonal", width="1", height="1")
    for tile_id, path in enumerate(images):
        tile = ET.SubElement(root, "tile", id=str(tile_id))
        image_element(tile, f"../Tileset/tiles_single/{path.name}", path)
    write_xml(FULL_MAP / "tiles.tsx", root)


def write_floor_tsx():
    from PIL import Image

    source = TILES / "tile_00_floor_a_base.png"
    displayed = TILES.parent / "floor_gravel_64.png"
    with Image.open(source) as image:
        image.resize((64, 64), Image.Resampling.NEAREST).save(displayed)

    root = ET.Element(
        "tileset",
        version="1.10",
        tiledversion="1.12.2",
        name="floor_gravel",
        tilewidth="64",
        tileheight="64",
        tilecount="1",
        columns="1",
    )
    image_element(root, "../Tileset/floor_gravel_64.png", displayed)
    write_xml(FULL_MAP / "floor.tsx", root)


def write_xml(path, root):
    ET.indent(root, space=" ")
    body = ET.tostring(root, encoding="unicode")
    path.write_text('<?xml version="1.0" encoding="UTF-8"?>\n' + body + "\n", encoding="utf-8")


def repair_tmx_references():
    for level in (1, 2, 3):
        path = FULL_MAP / f"Round{level}.tmx"
        text = path.read_text(encoding="utf-8")

        if level == 1:
            by_name = {
                image.name: image.relative_to(RESOURCES).as_posix()
                for image in OBJECTS.rglob("obj_*.png")
            }

            def local_image(match):
                file_name = pathlib.Path(match.group(1)).name
                relative = by_name.get(file_name)
                if relative is None:
                    raise FileNotFoundError(f"Round1 references unknown image: {file_name}")
                return f'source="../{relative}"'

            text = re.sub(
                r'source="[^"]*/(obj_[^"/]+\.png)"',
                local_image,
                text,
            )
            text = re.sub(
                r'source="[^"]*/floor_gravel_64\.png"',
                'source="../Tileset/floor_gravel_64.png"',
                text,
                count=1,
            )
        elif level == 2:
            text = re.sub(
                r' <tileset firstgid="1" name="floor_gravel".*?</tileset>\s*',
                ' <tileset firstgid="1" source="floor.tsx"/>\n ',
                text,
                count=1,
                flags=re.DOTALL,
            )
            text = re.sub(
                r' <tileset firstgid="2" source="[^"]*objects\.tsx"/>',
                ' <tileset firstgid="2" source="objects.tsx"/>',
                text,
                count=1,
            )
        else:
            text = re.sub(
                r' <tileset firstgid="1" source="[^"]*tiles\.tsx"/>',
                ' <tileset firstgid="1" source="tiles.tsx"/>',
                text,
                count=1,
            )
            text = re.sub(
                r' <tileset firstgid="23" source="[^"]*objects\.tsx"/>',
                ' <tileset firstgid="23" source="objects.tsx"/>',
                text,
                count=1,
            )

        path.write_text(text, encoding="utf-8")


def main():
    manifest = preserve_and_merge()
    write_objects_tsx(manifest)
    write_tiles_tsx()
    write_floor_tsx()
    repair_tmx_references()
    print(f"preserved {len(manifest['objects'])} objects")
    print(MERGED.relative_to(ROOT))
    print((FULL_MAP / "objects.tsx").relative_to(ROOT))
    print((FULL_MAP / "tiles.tsx").relative_to(ROOT))
    print((FULL_MAP / "floor.tsx").relative_to(ROOT))


if __name__ == "__main__":
    main()
