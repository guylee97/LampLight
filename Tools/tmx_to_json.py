#!/usr/bin/env python3

import json
import os
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Map", "one_lantern_demo60.tmx")
DST = os.path.join(ROOT, "Assets", "Resources", "Data", "MapData.json")


def parse_layer(layer):
    data = layer.find("data")
    if data is None or data.get("encoding") != "csv":
        raise ValueError(f"지원하지 않는 인코딩: {layer.get('name')}")
    return [int(v) for v in data.text.replace("\n", "").split(",") if v.strip()]


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


def to_unity(x_px, y_px, tile_size, pixel_height):
    return round(x_px / tile_size, 4), round((pixel_height - y_px) / tile_size, 4)


def parse_objects(group, tile_size, pixel_height):
    out = []
    for obj in group.findall("object"):
        x_px = float(obj.get("x"))
        y_px = float(obj.get("y"))
        ux, uy = to_unity(x_px, y_px, tile_size, pixel_height)
        out.append({
            "name": obj.get("name"),
            "col": int(x_px // tile_size),
            "row": int(y_px // tile_size),
            "x": ux,
            "y": uy,
        })
    return out


def convert():
    tree = ET.parse(SRC)
    root = tree.getroot()

    width = int(root.get("width"))
    height = int(root.get("height"))
    tile_size = int(root.get("tilewidth"))
    if tile_size != int(root.get("tileheight")):
        raise ValueError("정사각 타일이 아니다")

    pixel_width = width * tile_size
    pixel_height = height * tile_size

    layers = {l.get("name"): parse_layer(l) for l in root.findall("layer")}
    for name in ("floor", "walls", "deco"):
        if name not in layers:
            raise ValueError(f"레이어 누락: {name}")
        if len(layers[name]) != width * height:
            raise ValueError(f"{name} 셀 수 불일치: {len(layers[name])}")

    tile_props = []
    for ts in root.findall("tileset"):
        tile_props.extend(parse_tile_props(ts))

    groups = {g.get("name"): g for g in root.findall("objectgroup")}
    for name in ("objects", "spawns"):
        if name not in groups:
            raise ValueError(f"오브젝트 그룹 누락: {name}")

    return {
        "width": width,
        "height": height,
        "tileSize": tile_size,
        "pixelWidth": pixel_width,
        "pixelHeight": pixel_height,
        "floor": layers["floor"],
        "walls": layers["walls"],
        "deco": layers["deco"],
        "tileProps": tile_props,
        "objects": parse_objects(groups["objects"], tile_size, pixel_height),
        "spawns": parse_objects(groups["spawns"], tile_size, pixel_height),
    }


def verify(m):
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

    return walkable, total, problems


def main():
    m = convert()
    walkable, total, problems = verify(m)

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, "w", encoding="utf-8") as f:
        json.dump(m, f, ensure_ascii=False, separators=(",", ":"))

    print(f"입력  {os.path.relpath(SRC, ROOT)}")
    print(f"출력  {os.path.relpath(DST, ROOT)}  ({os.path.getsize(DST):,} bytes)")
    print()
    print(f"맵        {m['width']}x{m['height']} 타일, {m['tileSize']}px")
    print(f"픽셀      {m['pixelWidth']}x{m['pixelHeight']}")
    print(f"이동가능  {walkable}/{total} ({100*walkable/total:.0f}%)")
    print(f"타일속성  {len(m['tileProps'])}개")
    print(f"오브젝트  {len(m['objects'])}개")
    print(f"스폰앵커  {len(m['spawns'])}개")
    print()
    print("유물 좌표 (타일 → Unity 월드)")
    for o in m["objects"]:
        if o["name"].startswith("artifact"):
            print(f"  {o['name']:<12} ({o['col']:>2},{o['row']:>2})  →  ({o['x']:>7}, {o['y']:>7})")
    print()
    if problems:
        print("스펙 대조 실패")
        for p in problems:
            print(f"  ✗ {p}")
        sys.exit(1)
    print("스펙 대조 통과")


if __name__ == "__main__":
    main()
