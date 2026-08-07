"""타이틀 화면 Start 버튼과 같은 결로 문구 애셋을 굽는다.

Start 버튼은 위쪽이 크림색, 아래로 갈수록 따뜻한 갈색으로 떨어지고 획 꼭대기마다
작은 촛불이 얹혀 있다. 같은 그라데이션과 촛불을 픽셀 폰트로 조판한 글자 위에
올려서, 이미지 모델이 글자를 흘리는 문제 없이 톤만 맞춘다.
"""

import argparse
import hashlib
import os
import re

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "UI", "Common")
META_SOURCE = os.path.join(
    ROOT, "Assets", "Resources", "Art", "UI", "Game_Over_Screen", "wooden_planks.png.meta")

TOP = (222, 208, 170)
BOTTOM = (176, 138, 102)
OUTLINE = (28, 20, 16)
FLAME_CORE = (255, 236, 176)
FLAME_MID = (245, 186, 84)
FLAME_TIP = (196, 108, 40)

PAD = 6


def glyph_mask(text, ttf, size):
    font = ImageFont.truetype(ttf, size)
    box = ImageDraw.Draw(Image.new("L", (8, 8))).textbbox((0, 0), text, font=font)
    width = box[2] - box[0] + PAD * 2
    height = box[3] - box[1] + PAD * 2

    flat = Image.new("L", (width, height), 0)
    ImageDraw.Draw(flat).text((-box[0] + PAD, -box[1] + PAD), text, font=font, fill=255)
    return flat.point(lambda v: 255 if v > 110 else 0)


def gradient_fill(mask):
    width, height = mask.size
    out = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    pixels = out.load()
    lit = mask.load()

    rows = [y for y in range(height) if any(lit[x, y] for x in range(width))]
    if not rows:
        return out

    top, bottom = rows[0], rows[-1]
    span = max(1, bottom - top)

    for y in range(height):
        k = min(1.0, max(0.0, (y - top) / span))
        colour = tuple(
            int(TOP[i] + (BOTTOM[i] - TOP[i]) * k) for i in range(3))

        for x in range(width):
            if lit[x, y]:
                pixels[x, y] = colour + (255,)

    return out


def add_outline(image, mask):
    width, height = image.size
    out = image.copy()
    pixels = out.load()
    lit = mask.load()

    for y in range(height):
        for x in range(width):
            if lit[x, y]:
                continue

            touching = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < width and 0 <= ny < height and lit[nx, ny]:
                    touching = True
                    break

            if touching:
                pixels[x, y] = OUTLINE + (255,)

    return out


def stroke_tops(mask, min_gap):
    width, height = mask.size
    lit = mask.load()
    tops = []

    for x in range(width):
        column = [y for y in range(height) if lit[x, y]]
        if not column:
            continue

        top = column[0]
        if tops and x - tops[-1][0] < min_gap:
            if top < tops[-1][1]:
                tops[-1] = (x, top)
            continue

        tops.append((x, top))

    return tops


def add_flames(image, mask, min_gap, every):
    out = image.copy()
    draw = ImageDraw.Draw(out)

    tops = stroke_tops(mask, min_gap)

    for index, (x, top) in enumerate(tops):
        if index % max(1, every) != 0:
            continue

        draw.point((x, top - 1), FLAME_MID)
        draw.point((x, top - 2), FLAME_CORE)
        draw.point((x, top - 3), FLAME_TIP)

    return out


def write_meta(path):
    template = open(META_SOURCE, encoding="utf-8").read()
    guid = hashlib.md5(path.encode("utf-8")).hexdigest()

    template = re.sub(r"^guid: [0-9a-f]{32}$", "guid: " + guid, template, flags=re.M)
    for key, value in (("filterMode", 0), ("textureCompression", 0),
                       ("crunchedCompression", 0), ("spriteMode", 1)):
        template = re.sub(
            rf"^(\s*){key}: \d+$", rf"\g<1>{key}: {value}", template, flags=re.M)

    open(path + ".meta", "w", encoding="utf-8").write(template)


def bake(text, name, ttf, size, scale, colours, min_gap, every):
    mask = glyph_mask(text, ttf, size)
    image = gradient_fill(mask)
    image = add_outline(image, mask)
    image = add_flames(image, mask, min_gap, every)

    alpha = image.getchannel("A")
    flat = Image.new("RGB", image.size, (0, 0, 0))
    flat.paste(image.convert("RGB"), mask=alpha)
    reduced = flat.quantize(colors=colours, method=Image.MEDIANCUT).convert("RGBA")
    reduced.putalpha(alpha)

    final = reduced.resize(
        (reduced.size[0] * scale, reduced.size[1] * scale), Image.NEAREST)

    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name + ".png")
    final.save(path)
    write_meta(path)

    print(f"  {name}.png  {final.size[0]}x{final.size[1]}  색 {colours} 이하")
    return path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ttf", required=True)
    parser.add_argument("--text", required=True)
    parser.add_argument("--name", required=True)
    parser.add_argument("--size", type=int, default=16)
    parser.add_argument("--scale", type=int, default=4)
    parser.add_argument("--colours", type=int, default=20)
    parser.add_argument("--flame-gap", type=int, default=4)
    parser.add_argument("--flame-every", type=int, default=3)
    args = parser.parse_args()

    bake(args.text, args.name, args.ttf, args.size, args.scale,
         args.colours, args.flame_gap, args.flame_every)


if __name__ == "__main__":
    main()
