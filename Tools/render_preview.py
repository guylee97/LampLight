import argparse
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TILE_DIR = os.path.join(ROOT, "Assets", "Art", "Temple", "Tiles")
OBJECT_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Objects")
CATALOG = os.path.join(ROOT, "Assets", "Resources", "Data", "temple_catalog.json")

BACKDROP = (14, 14, 18, 255)

MARKERS = {
    "player_start": (90, 200, 255, 255),
    "exit_door": (255, 190, 60, 255),
}
ARTIFACT_COLOR = (235, 80, 200, 255)


def tile_images():
    images = {}
    for name in os.listdir(TILE_DIR):
        if not name.endswith(".png"):
            continue
        tile_id = int(name.split("_")[1])
        images[tile_id] = Image.open(os.path.join(TILE_DIR, name)).convert("RGBA")
    return images


def object_images(catalog):
    images = {}
    for entry in catalog["objects"]:
        path = os.path.join(OBJECT_DIR, *entry["file"].split("/")[1:])
        if os.path.exists(path):
            images[entry["key"]] = Image.open(path).convert("RGBA")
    return images


def paste_layer(canvas, layer, width, height, tiles, tile_px):
    for row in range(height):
        for col in range(width):
            gid = layer[row * width + col]
            if gid == 0:
                continue
            image = tiles.get(gid - 1)
            if image is None:
                continue
            canvas.alpha_composite(image, (col * tile_px, row * tile_px))


def draw_markers(canvas, game_map, tile_px):
    from PIL import ImageDraw

    draw = ImageDraw.Draw(canvas)
    radius = max(3, tile_px // 4)

    for point in game_map.get("objects", []):
        name = point["name"]
        color = MARKERS.get(name, ARTIFACT_COLOR if name.startswith("artifact_") else None)
        if color is None:
            continue
        cx = point["col"] * tile_px + tile_px // 2
        cy = point["row"] * tile_px + tile_px // 2
        draw.ellipse([cx - radius, cy - radius, cx + radius, cy + radius],
                     fill=color, outline=(0, 0, 0, 255))


def render(dump, catalog, tiles, objects, tile_px, scale):
    game_map = dump["map"]
    width, height = game_map["width"], game_map["height"]

    canvas = Image.new("RGBA", (width * tile_px, height * tile_px), BACKDROP)

    paste_layer(canvas, game_map["floor"], width, height, tiles, tile_px)
    paste_layer(canvas, game_map["walls"], width, height, tiles, tile_px)

    for placement in sorted(dump["deco"], key=lambda d: d["tileY"]):
        image = objects.get(placement["key"])
        if image is None:
            continue
        w, h = image.size
        x = int(placement["tileX"] * tile_px - w / 2)
        y = int(placement["tileY"] * tile_px - h / 2)
        canvas.alpha_composite(image, (x, y))

    draw_markers(canvas, game_map, tile_px)

    if scale != 1:
        canvas = canvas.resize((canvas.width * scale, canvas.height * scale), Image.NEAREST)

    return canvas


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", default=os.path.join(ROOT, "QAReports", "MapPreview"))
    parser.add_argument("--dst", default=os.path.join(ROOT, "QAReports", "MapPreview"))
    parser.add_argument("--scale", type=int, default=2)
    args = parser.parse_args()

    with open(CATALOG, encoding="utf-8") as handle:
        catalog = json.load(handle)

    tiles = tile_images()
    objects = object_images(catalog)
    tile_px = catalog["tilePx"]

    os.makedirs(args.dst, exist_ok=True)
    rendered = 0

    for name in sorted(os.listdir(args.src)):
        if not name.startswith("preview_") or not name.endswith(".json"):
            continue

        with open(os.path.join(args.src, name), encoding="utf-8") as handle:
            dump = json.load(handle)

        image = render(dump, catalog, tiles, objects, tile_px, args.scale)
        out = os.path.join(args.dst, name.replace(".json", ".png"))
        image.save(out)
        rendered += 1

        print(f"L{dump['level']} seed={dump['seed']} "
              f"{dump['map']['width']}x{dump['map']['height']} "
              f"장식 {len(dump['deco'])}개 -> {out}")

    if rendered == 0:
        print(f"미리보기 json 을 못 찾았다: {args.src}", file=sys.stderr)
        return 2

    return 0


if __name__ == "__main__":
    sys.exit(main())
