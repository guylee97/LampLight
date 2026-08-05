"""맵 판정을 기획이 준 오버레이와 같은 형식으로 그린다.

바닥과 벽 타일, decorations 를 게임과 같은 좌표로 합성한 뒤 collision 값을
색으로 덮는다. 0 통과 초록 / 1 차단 빨강 / 2 큰소리 노랑 / 3 카펫 파랑.

    python3 Tools/render_collision_overlay.py <출력폴더> [--suffix after]
    python3 Tools/render_collision_overlay.py <출력폴더> --data <맵json폴더>
"""

import argparse
import json
import os

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "Assets", "Resources", "Data")
TILES = os.path.join(ROOT, "Assets", "Resources", "Tileset", "tiles_single")
RES = os.path.join(ROOT, "Assets", "Resources")

FONT = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"

TILE_PX = 64
GUTTER = 56
HEADER = 44
LEGEND = 150
OUT_WIDTH = 1500

BACKDROP = (24, 24, 28, 255)
GRID = (255, 255, 255, 70)
MAJOR = (255, 255, 255, 190)
TEXT = (236, 236, 240, 255)

WALK, BLOCK, NOISE, MUFFLED = 0, 1, 2, 3
PAINT = {
    WALK: (60, 220, 120),
    BLOCK: (230, 50, 60),
    NOISE: (255, 215, 0),
    MUFFLED: (70, 140, 255),
}
ALPHA = 130

DEBRIS_DISPLAY_SCALE = 0.7

GROUND_PREFIXES = ("prop_floor_", "large_carpet_", "cobweb_", "extra_cobweb_",
                   "debris_", "noise_", "prop_candle_")
GROUND_KEYS = {"prop_grate", "prop_pebbles", "prop_roots", "prop_roots_stone",
               "prop_bones_long", "prop_bones_pile", "prop_skull",
               "extra_prop_skull_b"}


def is_ground(key):
    return key.startswith(GROUND_PREFIXES) or key in GROUND_KEYS


def tile_images():
    images = {}
    for name in sorted(os.listdir(TILES)):
        if name.endswith(".png"):
            index = int(name.split("_")[1])
            images[index] = Image.open(os.path.join(TILES, name)).convert("RGBA")
    return images


def paste_layer(canvas, layer, width, height, tiles):
    for row in range(height):
        for col in range(width):
            gid = layer[row * width + col]
            image = tiles.get(gid - 1) if gid else None
            if image is None:
                continue
            if image.size != (TILE_PX, TILE_PX):
                image = image.resize((TILE_PX, TILE_PX), Image.NEAREST)
            canvas.alpha_composite(image, (col * TILE_PX, row * TILE_PX))


def paste_decorations(canvas, data, cache):
    height = data["height"]
    order = sorted(
        data.get("decorations", []),
        key=lambda d: (0 if is_ground(d["key"]) else 1, -(height - d["y"])),
    )

    for deco in order:
        path = os.path.join(RES, deco["resource"] + ".png")
        if path not in cache:
            cache[path] = Image.open(path).convert("RGBA") if os.path.exists(path) else None
        sprite = cache[path]
        if sprite is None:
            continue

        trim = DEBRIS_DISPLAY_SCALE if deco["key"].startswith("debris_") else 1.0
        span_x = deco["width"] * trim
        span_y = deco["height"] * trim

        image = sprite
        if deco.get("flipDiagonal"):
            image = image.transpose(Image.ROTATE_90)
            if deco.get("flipVertical"):
                image = image.transpose(Image.FLIP_LEFT_RIGHT)
            if deco.get("flipHorizontal"):
                image = image.transpose(Image.FLIP_TOP_BOTTOM)
        else:
            if deco.get("flipHorizontal"):
                image = image.transpose(Image.FLIP_LEFT_RIGHT)
            if deco.get("flipVertical"):
                image = image.transpose(Image.FLIP_TOP_BOTTOM)

        box = (max(1, round(span_x * TILE_PX)), max(1, round(span_y * TILE_PX)))
        if image.size != box:
            image = image.resize(box, Image.NEAREST)

        centre_x = deco["x"] + deco["width"] * 0.5
        centre_y = height - deco["y"] + deco["height"] * 0.5
        left = round((centre_x - span_x * 0.5) * TILE_PX)
        top = round((height - centre_y - span_y * 0.5) * TILE_PX)
        canvas.alpha_composite(image, (left, top))


def paint_collision(canvas, data):
    width, height = data["width"], data["height"]
    collision = data["collision"]
    wash = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(wash)

    for row in range(height):
        for col in range(width):
            colour = PAINT.get(collision[row * width + col])
            if colour is None:
                continue
            draw.rectangle(
                [col * TILE_PX, row * TILE_PX,
                 (col + 1) * TILE_PX - 1, (row + 1) * TILE_PX - 1],
                fill=colour + (ALPHA,),
            )

    canvas.alpha_composite(wash)


def draw_grid(canvas, width, height):
    lines = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(lines)

    for col in range(width + 1):
        x = col * TILE_PX
        draw.line([(x, 0), (x, height * TILE_PX)],
                  fill=MAJOR if col % 5 == 0 else GRID,
                  width=3 if col % 5 == 0 else 1)

    for row in range(height + 1):
        y = row * TILE_PX
        draw.line([(0, y), (width * TILE_PX, y)],
                  fill=MAJOR if row % 5 == 0 else GRID,
                  width=3 if row % 5 == 0 else 1)

    canvas.alpha_composite(lines)


def compose(data, level, tiles, cache, subtitle):
    width, height = data["width"], data["height"]

    body = Image.new("RGBA", (width * TILE_PX, height * TILE_PX), BACKDROP)
    paste_layer(body, data["floor"], width, height, tiles)
    paste_layer(body, data["walls"], width, height, tiles)
    paste_decorations(body, data, cache)
    paint_collision(body, data)
    draw_grid(body, width, height)

    page = Image.new("RGBA",
                     (GUTTER + width * TILE_PX, HEADER + height * TILE_PX + LEGEND),
                     BACKDROP)
    page.alpha_composite(body, (GUTTER, HEADER))

    draw = ImageDraw.Draw(page)
    label = ImageFont.truetype(FONT, 30)
    title = ImageFont.truetype(FONT, 34)
    small = ImageFont.truetype(FONT, 26)

    for col in range(0, width, 5):
        draw.text((GUTTER + col * TILE_PX + 6, 6), str(col), font=label, fill=TEXT)

    for row in range(0, height, 5):
        draw.text((6, HEADER + row * TILE_PX + 4), str(row), font=label, fill=TEXT)

    base = HEADER + height * TILE_PX + 22
    draw.text((GUTTER, base),
              f"Level{level}   {width} x {height} tiles   /   1 tile = {TILE_PX} px"
              f"   /   numbers = col(top) , row(left)   {subtitle}",
              font=title, fill=TEXT)

    entries = [
        (PAINT[WALK], "0 walk : pass, normal step", 0, 0),
        (PAINT[BLOCK], "1 block : cannot pass", 0, 1),
        (PAINT[MUFFLED], "3 muffled : pass, QUIET (carpet)", 1, 0),
        (PAINT[NOISE], "2 noise : pass, LOUD", 2, 0),
        (PAINT[NOISE], "     glass / gravel / bone / debris", 1, 1),
    ]

    span = (width * TILE_PX) // 3

    for colour, text, column, line in entries:
        x = GUTTER + column * span
        y = base + 48 + line * 40
        draw.rectangle([x, y + 4, x + 24, y + 26], fill=colour + (255,))
        draw.text((x + 34, y), text, font=small, fill=TEXT)

    scale = OUT_WIDTH / page.width
    return page.resize((OUT_WIDTH, round(page.height * scale)), Image.LANCZOS)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("out")
    parser.add_argument("--data", default=DATA)
    parser.add_argument("--suffix", default="after")
    parser.add_argument("--subtitle", default="")
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)
    tiles = tile_images()
    cache = {}

    for level in (1, 2, 3):
        with open(os.path.join(args.data, f"map_l{level}.json"), encoding="utf-8") as handle:
            data = json.load(handle)

        image = compose(data, level, tiles, cache, args.subtitle)
        path = os.path.join(args.out, f"collision_overlay_Level{level}_{args.suffix}.png")
        image.convert("RGB").save(path)

        counts = {v: data["collision"].count(v) for v in (WALK, BLOCK, NOISE, MUFFLED)}
        print(f"L{level}: 통과 {counts[WALK]}  차단 {counts[BLOCK]}"
              f"  큰소리 {counts[NOISE]}  카펫 {counts[MUFFLED]}  -> {path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
