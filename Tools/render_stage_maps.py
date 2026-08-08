"""전각별 구성을 한 장씩 그린다 — 시작, 공양물, 제단, 요괴 스폰, 그리고 최단 동선.

게임과 같은 좌표로 바닥·벽·장식을 합성한 뒤 그 위에 목표 지점을 얹는다.
숫자만 보고 맵이 어떻게 짜였는지 판단할 수 없어서 만든 물건이다.

    python3 Tools/render_stage_maps.py <출력폴더>
"""

import argparse
import json
import os
import re
from collections import deque

from PIL import Image, ImageDraw

from PIL import ImageFont

from render_collision_overlay import (
    DATA, RES, ROOT, TILE_PX,
    paste_decorations, paste_layer, tile_images,
)

# 라벨이 전부 한국어다. LiberationSans 로는 네모만 나온다.
KOREAN_FONTS = [
    ("/System/Library/Fonts/AppleSDGothicNeo.ttc", 2),
    ("/System/Library/Fonts/AppleGothic.ttf", 0),
    ("/System/Library/Fonts/Supplemental/NotoSansGothic-Regular.ttf", 0),
]


def load_font(size):
    for path, index in KOREAN_FONTS:
        if not os.path.exists(path):
            continue
        try:
            return ImageFont.truetype(path, size, index=index)
        except OSError:
            continue

    raise SystemExit("한글 글리프가 있는 폰트를 못 찾았다")

LEVEL_CONFIG = os.path.join(
    ROOT, "Assets", "Scripts", "Contents", "LevelConfig.cs")
YOKAI_SPEC = os.path.join(
    ROOT, "Assets", "Scripts", "Contents", "Enemy", "YokaiSpec.cs")

SCALE = 0.5
HEADER = 92
FOOTER = 76
MARGIN = 24

INK = (232, 228, 218, 255)
MUTED = (150, 146, 138, 255)
PAPER = (18, 18, 22, 255)

START = (118, 214, 132, 255)
ALTAR = (255, 168, 64, 255)
OFFER = (250, 224, 116, 255)
SPAWN = (226, 96, 96, 255)
ROUTE = (250, 224, 116, 150)

ARTIFACT_NAMES = ["종", "문장", "가면", "인장"]
CONCEALMENT_NAMES = ["노출", "잔해 속", "석관 속"]
HALLS = ["외전", "본전", "내전"]


def level_table():
    """LevelConfig.cs 를 그대로 읽는다. 수치를 두 군데 적으면 반드시 갈라진다."""

    text = open(LEVEL_CONFIG, encoding="utf-8").read()
    rows = {}

    for block in re.findall(r"new LevelConfig\s*\{(.*?)\}", text, re.S):
        fields = dict(re.findall(r"(\w+)\s*=\s*([0-9.]+)f?", block))
        if "Level" not in fields:
            continue
        rows[int(fields["Level"])] = {
            "placed": int(float(fields["ArtifactsPlaced"])),
            "required": int(float(fields["ArtifactsRequired"])),
            "lamp": float(fields["LampSeconds"]),
            "ritual": float(fields["RitualSeconds"]),
        }

    return rows


def yokai_labels():
    text = open(YOKAI_SPEC, encoding="utf-8").read()
    return re.findall(r'Label\s*=\s*"([^"]+)"', text)


def concealment_for(level, index):
    if level <= 1:
        return 0
    if level == 2:
        return 0 if index % 2 == 0 else 1
    return 1 if index % 2 == 0 else 2


def passable(data):
    width, height, collision = data["width"], data["height"], data["collision"]

    def ok(col, row):
        return (0 <= col < width and 0 <= row < height
                and collision[row * width + col] != 1)

    return ok


def field_from(data, origin):
    ok = passable(data)
    dist = {origin: 0}
    queue = deque([origin])

    while queue:
        col, row = queue.popleft()
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nxt = (col + dc, row + dr)
            if nxt not in dist and ok(*nxt):
                dist[nxt] = dist[(col, row)] + 1
                queue.append(nxt)

    return dist


def walk_back(data, dist, target):
    """거리장을 거슬러 올라가 실제 경로 타일을 뽑는다."""

    if target not in dist:
        return []

    path = [target]
    while dist[path[-1]] > 0:
        col, row = path[-1]
        here = dist[(col, row)]
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nxt = (col + dc, row + dr)
            if dist.get(nxt, here) == here - 1:
                path.append(nxt)
                break
        else:
            break

    return path[::-1]


def greedy_route(data, start, offerings, altar):
    """가장 가까운 공양물부터 줍고 제단으로 — 테스트가 재는 것과 같은 순서다.

    다리 목록과 함께 실제로 줍게 되는 순서를 돌려준다. 맵 파일 순서와는 다르다.
    """

    legs = []
    order = []
    here = start
    left = list(offerings)

    while left:
        dist = field_from(data, here)
        left.sort(key=lambda t: dist.get(t, 10 ** 9))
        nxt = left.pop(0)
        legs.append(walk_back(data, dist, nxt))
        order.append(nxt)
        here = nxt

    legs.append(walk_back(data, field_from(data, here), altar))
    return legs, order


def marker(draw, col, row, colour, label, font, radius=17):
    x = (col + 0.5) * TILE_PX * SCALE
    y = (row + 0.5) * TILE_PX * SCALE

    draw.ellipse((x - radius - 2, y - radius - 2, x + radius + 2, y + radius + 2),
                 fill=(0, 0, 0, 200))
    draw.ellipse((x - radius, y - radius, x + radius, y + radius),
                 fill=colour, outline=(0, 0, 0, 255), width=2)

    box = draw.textbbox((0, 0), label, font=font)
    draw.text((x - (box[2] - box[0]) / 2, y - (box[3] - box[1]) / 2 - box[1]),
              label, font=font, fill=(12, 12, 14, 255))


def caption(draw, col, row, text, font, colour=INK):
    x = (col + 0.5) * TILE_PX * SCALE
    y = (row + 0.5) * TILE_PX * SCALE + 20

    box = draw.textbbox((0, 0), text, font=font)
    w, h = box[2] - box[0], box[3] - box[1]
    draw.rectangle((x - w / 2 - 5, y - 2, x + w / 2 + 5, y + h + 6),
                   fill=(0, 0, 0, 190))
    draw.text((x - w / 2, y - box[1] + 2), text, font=font, fill=colour)


def render(level, data, tiles, cache, config, yokai):
    width, height = data["width"], data["height"]

    art = Image.new("RGBA", (width * TILE_PX, height * TILE_PX), PAPER)
    paste_layer(art, data["floor"], width, height, tiles)
    paste_layer(art, data["walls"], width, height, tiles)
    paste_decorations(art, data, cache)

    art = art.resize((round(width * TILE_PX * SCALE), round(height * TILE_PX * SCALE)),
                     Image.LANCZOS)

    # 아트를 그대로 두면 표식이 안 읽힌다. 한 겹 눌러 깔개로 쓴다.
    art.alpha_composite(Image.new("RGBA", art.size, (10, 10, 14, 96)))

    canvas = Image.new(
        "RGBA",
        (art.width + MARGIN * 2, art.height + HEADER + FOOTER + MARGIN),
        PAPER)
    canvas.alpha_composite(art, (MARGIN, HEADER))

    board = Image.new("RGBA", art.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(board)

    points = {o["name"]: (o["col"], o["row"]) for o in data["objects"]}
    start = points["player_start"]
    altar = points["exit_door"]
    offerings = [points[f"artifact_{i + 1}"]
                 for i in range(config["placed"]) if f"artifact_{i + 1}" in points]

    legs, order = greedy_route(data, start, offerings, altar)
    for leg in legs:
        line = [((c + 0.5) * TILE_PX * SCALE, (r + 0.5) * TILE_PX * SCALE)
                for c, r in leg]
        if len(line) > 1:
            draw.line(line, fill=ROUTE, width=5, joint="curve")

    mark_font = load_font(20)
    tag_font = load_font(17)

    for spawn in data.get("spawns", []):
        x = (spawn["col"] + 0.5) * TILE_PX * SCALE
        y = (spawn["row"] + 0.5) * TILE_PX * SCALE
        draw.line((x - 8, y - 8, x + 8, y + 8), fill=SPAWN, width=4)
        draw.line((x - 8, y + 8, x + 8, y - 8), fill=SPAWN, width=4)

    # 이름과 은닉도는 맵 파일 순서(= 코드가 스프라이트를 고르는 순서)를 따르고,
    # 원 안 번호는 실제로 줍게 되는 순서를 따른다. 둘은 같지 않다.
    for i, spot in enumerate(offerings):
        name = ARTIFACT_NAMES[i % len(ARTIFACT_NAMES)]
        hidden = CONCEALMENT_NAMES[concealment_for(level, i)]
        step = order.index(spot) + 1 if spot in order else i + 1

        marker(draw, *spot, OFFER, str(step), mark_font)
        caption(draw, *spot, f"{name} · {hidden}", tag_font, OFFER)

    marker(draw, *start, START, "S", mark_font)
    caption(draw, *start, "시작", tag_font, START)

    marker(draw, *altar, ALTAR, "A", mark_font)
    caption(draw, *altar, "제단", tag_font, ALTAR)

    canvas.alpha_composite(board, (MARGIN, HEADER))

    tiles_walked = sum(max(0, len(leg) - 1) for leg in legs)
    seconds = tiles_walked / 4.0 + config["required"] * config["ritual"]

    head = ImageDraw.Draw(canvas)
    title = load_font(34)
    sub = load_font(20)

    label = yokai[level - 1] if level - 1 < len(yokai) else "?"
    head.text((MARGIN, 18), f"{level}전각 · {HALLS[level - 1]}",
              font=title, fill=INK)
    head.text((MARGIN, 60),
              f"{width}×{height}타일 · 공양물 {config['placed']}개 전부 필요 · "
              f"등불 {config['lamp']:.0f}초 · 요괴 {label}",
              font=sub, fill=MUTED)

    foot = HEADER + art.height + 14
    head.text((MARGIN, foot),
              f"최단 동선 {tiles_walked}칸 ≈ 걷기 {tiles_walked / 4.0:.0f}초 + "
              f"의식 {config['required'] * config['ritual']:.0f}초 = {seconds:.0f}초 "
              f"(등불의 {seconds / config['lamp'] * 100:.0f}%)",
              font=sub, fill=MUTED)
    head.text((MARGIN, foot + 28),
              "S 시작    A 제단    1·2·3 공양물(줍는 순서)    X 요괴 스폰 후보    "
              "노란 선 = 가장 가까운 것부터 주웠을 때의 경로",
              font=sub, fill=MUTED)

    return canvas.convert("RGB")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("out")
    args = parser.parse_args()

    os.makedirs(args.out, exist_ok=True)

    tiles = tile_images()
    cache = {}
    config = level_table()
    yokai = yokai_labels()

    for level in sorted(config):
        path = os.path.join(DATA, f"map_l{level}.json")
        if not os.path.exists(path):
            print(f"L{level}: {path} 없음")
            continue

        data = json.load(open(path, encoding="utf-8"))
        image = render(level, data, tiles, cache, config[level], yokai)

        out = os.path.join(args.out, f"stage_l{level}.png")
        image.save(out)
        print(f"{out}  {image.width}x{image.height}")


if __name__ == "__main__":
    main()
