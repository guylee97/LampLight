#!/usr/bin/env python3
"""Apply reviewed, explicit object corrections to the fixed round TMX files."""

import pathlib
import xml.etree.ElementTree as ET


ROOT = pathlib.Path(__file__).resolve().parents[1]
FULL_MAP = ROOT / "Assets" / "Resources" / "FullMap"
GID_MASK = 0x0FFFFFFF
FLIP_MASK = 0xF0000000

ROUND_1_OBJECTS = {
    60: "obj_large_pillar_broken_pair",
    65: "obj_prop_pebbles",
    68: "obj_debris_shards",
    69: "obj_prop_bones_pile",
    126: "obj_prop_pillar_column",
    127: "obj_prop_floor_seal_broken",
    149: "obj_prop_roots",
    150: "obj_debris_shards",
    155: "obj_prop_pillar_column",
    156: "obj_prop_pillar_column",
}

ROUND_1_FRONT_OBJECTS = (14, 15, 100, 102, 139, 140)

ROUND_1_HALF_SIZE_OBJECTS = (17, 19, 43, 44)


def object_tileset(root):
    for tileset in root.findall("tileset"):
        if tileset.get("name") == "one_lantern_objects":
            return tileset
    raise ValueError("Round1 embedded object tileset is missing")


def image_ids(tileset):
    found = {}
    for tile in tileset.findall("tile"):
        image = tile.find("image")
        if image is not None:
            found[pathlib.Path(image.get("source")).stem] = int(tile.get("id"))
    return found


def set_property(obj, name, value):
    properties = obj.find("properties")
    if properties is None:
        properties = ET.Element("properties")
        obj.insert(0, properties)

    prop = properties.find(f'property[@name="{name}"]')
    if prop is None:
        prop = ET.SubElement(properties, "property", name=name)
    prop.set("value", str(value))


def apply_round_1():
    path = FULL_MAP / "Round1.tmx"
    tree = ET.parse(path)
    root = tree.getroot()
    tileset = object_tileset(root)
    first_gid = int(tileset.get("firstgid"))
    by_name = image_ids(tileset)

    for object_id, image_name in ROUND_1_OBJECTS.items():
        obj = root.find(f'.//object[@id="{object_id}"]')
        if obj is None:
            raise ValueError(f"Round1 object id {object_id} is missing")
        if image_name not in by_name:
            raise ValueError(f"Round1 embedded tileset has no {image_name}")

        raw_gid = int(obj.get("gid"))
        flags = raw_gid & FLIP_MASK
        obj.set("gid", str(flags | (first_gid + by_name[image_name])))

    for object_id in ROUND_1_FRONT_OBJECTS:
        for group in root.findall("objectgroup"):
            obj = group.find(f'object[@id="{object_id}"]')
            if obj is None:
                continue
            set_property(obj, "sortingOffset", 2)
            group.remove(obj)
            group.append(obj)
            break
        else:
            raise ValueError(f"Round1 object id {object_id} is missing")

    for object_id in ROUND_1_HALF_SIZE_OBJECTS:
        obj = root.find(f'.//object[@id="{object_id}"]')
        if obj is None:
            raise ValueError(f"Round1 object id {object_id} is missing")

        old_width = float(obj.get("width"))
        new_width = 32.0
        new_height = 32.0
        if old_width != new_width:
            obj.set("x", str(float(obj.get("x")) + (old_width - new_width) * 0.5))
        obj.set("width", str(new_width))
        obj.set("height", str(new_height))

    ET.indent(tree, space=" ")
    tree.write(path, encoding="UTF-8", xml_declaration=True)
    print(f"updated {path.relative_to(ROOT)}")


if __name__ == "__main__":
    apply_round_1()
