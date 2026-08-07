"""점프스케어 버전별 연출을 GIF 로 굽는다.

UI_GameOver 의 커밋별 스냅샷마다 단계 구성과 상수가 다르다. 어느 쪽이 나았는지
코드로는 못 고르니, 각 버전의 타임라인을 그대로 재생해서 눈으로 비교한다.
실제 렌더가 아니라 연출 판단용이다 — 얼굴 아트는 전 버전 동일한 걸 써서
차이가 타이밍과 움직임에서만 나오게 한다.
"""

import argparse
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UI_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "UI")
CHAR_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Characters")

WIDTH, HEIGHT = 384, 216
FPS = 30


def out_quint(t):
    t = min(1.0, max(0.0, t))
    return 1.0 - pow(1.0 - t, 5)


# 각 버전의 실제 상수를 소스에서 옮겨 적었다. (단계, 인자) 순서가 곧 Play() 순서다.
VERSIONS = {
    "v03_0f5e293": [
        ("face", dict(seconds=0.25, flash=False, jitter=0)),
        ("flicker", dict(count=3, on=0.03, off=0.03, dim=0.88)),
        ("blackout", dict(seconds=0.20)),
        ("reveal", dict(seconds=0.25)),
    ],
    "v05_b3931dc": [
        ("hitstop", dict(seconds=0.15, zoom=0.55)),
        ("slam", dict(seconds=0.10, start_scale=1.9)),
        ("hold", dict(seconds=0.55)),
        ("flicker", dict(count=6, on=0.045, off=0.075, dim=0.86)),
        ("blackout", dict(seconds=0.30)),
        ("wait", dict(seconds=0.35)),
        ("reveal", dict(seconds=0.25)),
    ],
    "v06_ed36715": [
        ("hitstop", dict(seconds=0.10, zoom=0.0)),
        ("flicker", dict(count=3, on=0.03, off=0.03, dim=0.88)),
        ("face", dict(seconds=0.34, flash=True, jitter=3)),
        ("settle", dict(seconds=0.28)),
        ("reveal", dict(seconds=0.22)),
    ],
    "v09_9e8de3e": [
        ("hitstop", dict(seconds=0.10, zoom=0.0)),
        ("flicker", dict(count=3, on=0.03, off=0.03, dim=0.88)),
        ("face", dict(seconds=0.34, flash=True, jitter=3)),
        ("settle", dict(seconds=0.28)),
        ("reveal", dict(seconds=0.22)),
    ],
    "v10_e62d02d": [
        ("hitstop", dict(seconds=0.10, zoom=0.0)),
        ("lunge", dict(seconds=0.30, scale=3.1)),
        ("face", dict(seconds=0.34, flash=True, jitter=3)),
        ("flicker", dict(count=3, on=0.03, off=0.03, dim=0.88)),
        ("settle", dict(seconds=0.28)),
        ("reveal", dict(seconds=0.22)),
    ],
    "v11_97605c7": [
        ("hitstop", dict(seconds=0.10, zoom=0.0)),
        ("lunge", dict(seconds=0.30, scale=3.6)),
        ("face", dict(seconds=0.46, flash=True, jitter=3)),
        ("flicker", dict(count=3, on=0.03, off=0.03, dim=0.88)),
        ("settle", dict(seconds=0.28)),
        ("reveal", dict(seconds=0.22)),
    ],
}


def frames_for(seconds):
    return max(1, int(round(seconds * FPS)))


def load(path):
    return Image.open(path).convert("RGBA")


def backdrop(creature, zoom=0.0):
    board = Image.new("RGBA", (WIDTH, HEIGHT), (10, 9, 12, 255))
    draw = ImageDraw.Draw(board)

    tile = 32
    for y in range(0, HEIGHT, tile):
        for x in range(0, WIDTH, tile):
            shade = 26 if (x // tile + y // tile) % 2 == 0 else 21
            draw.rectangle([x, y, x + tile - 1, y + tile - 1],
                           fill=(shade, shade - 3, shade - 6))

    glow = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    cx, cy = WIDTH // 2, HEIGHT // 2 + 10
    for r in range(64, 0, -4):
        a = int(120 * (1.0 - r / 64.0))
        gd.ellipse([cx - r, cy - r * 0.7, cx + r, cy + r * 0.7], fill=(255, 168, 84, a))
    board.alpha_composite(glow)

    if creature is not None:
        small = creature.resize((creature.size[0] // 3, creature.size[1] // 3), Image.NEAREST)
        board.alpha_composite(small, (cx + 40, cy - small.size[1] + 12))

    if zoom > 0.0:
        w = int(WIDTH * (1.0 - zoom * 0.25))
        h = int(HEIGHT * (1.0 - zoom * 0.25))
        board = board.crop(((WIDTH - w) // 2, (HEIGHT - h) // 2,
                            (WIDTH - w) // 2 + w, (HEIGHT - h) // 2 + h))
        board = board.resize((WIDTH, HEIGHT), Image.NEAREST)

    return board


def face_frame(face, scale=1.0, alpha=1.0, jitter=0, flash=0.0):
    frame = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 255))

    w = max(1, int(WIDTH * scale))
    h = max(1, int(HEIGHT * scale))
    grown = face.resize((w, h), Image.NEAREST)

    if alpha < 1.0:
        band = grown.getchannel("A").point(lambda v: int(v * alpha))
        grown.putalpha(band)

    frame.alpha_composite(grown, ((WIDTH - w) // 2 + jitter, (HEIGHT - h) // 2 - jitter))

    if flash > 0.0:
        frame.alpha_composite(
            Image.new("RGBA", (WIDTH, HEIGHT), (140, 6, 6, int(140 * flash))))

    return frame


def render(steps, face, creature):
    out = []
    scene = backdrop(creature)

    for name, arg in steps:
        if name == "hitstop":
            shot = backdrop(creature, arg["zoom"])
            out += [shot.copy() for _ in range(frames_for(arg["seconds"]))]

        elif name == "lunge":
            n = frames_for(arg["seconds"])
            for i in range(n):
                k = out_quint((i + 1) / n)
                frame = scene.copy()
                if creature is not None:
                    s = 0.55 + (arg["scale"] - 0.55) * k
                    w = max(1, int(creature.size[0] * s))
                    h = max(1, int(creature.size[1] * s))
                    grown = creature.resize((w, h), Image.NEAREST)
                    dim = int(255 * (0.85 - 0.57 * k))
                    shade = Image.new("RGBA", grown.size, (dim, dim, dim, 255))
                    shade.putalpha(grown.getchannel("A"))
                    frame.alpha_composite(
                        shade, (int(WIDTH / 2 + 40 * (1 - k) - w / 2), int(HEIGHT / 2 - h / 2)))
                out.append(frame)

        elif name == "slam":
            n = frames_for(arg["seconds"])
            for i in range(n):
                k = (i + 1) / n
                s = arg["start_scale"] + (1.0 - arg["start_scale"]) * (k * k)
                out.append(face_frame(face, s, min(1.0, k * 2.5), 0, 0.5 * (1 - k)))

        elif name == "hold":
            n = frames_for(arg["seconds"])
            for i in range(n):
                out.append(face_frame(face, 1.0, 1.0, 3 if i % 2 == 0 else -3, 0.0))

        elif name == "face":
            n = frames_for(arg["seconds"])
            for i in range(n):
                j = arg["jitter"] if i % 2 == 0 else -arg["jitter"]
                flash = 0.5 * (1.0 - out_quint(i / n)) if arg["flash"] else 0.0
                out.append(face_frame(face, 1.0, 1.0, j, flash))

        elif name == "flicker":
            dark = scene.copy()
            dark.alpha_composite(
                Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, int(255 * arg["dim"]))))
            for _ in range(arg["count"]):
                out += [dark.copy() for _ in range(frames_for(arg["off"]))]
                out += [scene.copy() for _ in range(frames_for(arg["on"]))]

        elif name in ("blackout", "settle", "wait"):
            black = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 255))
            out += [black.copy() for _ in range(frames_for(arg["seconds"]))]

        elif name == "reveal":
            title = load(os.path.join(UI_DIR, "Game_Over_Screen", "Your Dead Title.png"))
            tw = int(WIDTH * 0.72)
            th = max(1, int(tw * title.size[1] / title.size[0]))
            title = title.resize((tw, th), Image.NEAREST)

            n = frames_for(arg["seconds"])
            for i in range(n):
                k = (i + 1) / n
                frame = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 255))
                faded = title.copy()
                faded.putalpha(title.getchannel("A").point(lambda v: int(v * k)))
                frame.alpha_composite(faded, ((WIDTH - tw) // 2, int(HEIGHT * 0.30)))
                out.append(frame)
            out += [out[-1].copy() for _ in range(frames_for(0.5))]

    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--face", default=os.path.join(
        UI_DIR, "Game_Over_Screen", "jumpscare_face.png"))
    parser.add_argument("--creature", default=os.path.join(
        CHAR_DIR, "sangju", "sangju_idle_sheet.png"))
    parser.add_argument("--out-dir", required=True)
    parser.add_argument("--only", default="")
    args = parser.parse_args()

    face = load(args.face)

    creature = None
    if os.path.exists(args.creature):
        sheet = load(args.creature)
        size = sheet.size[0]
        creature = sheet.crop((0, 0, size, size))

    os.makedirs(args.out_dir, exist_ok=True)

    for name, steps in VERSIONS.items():
        if args.only and args.only not in name:
            continue

        frames = render(steps, face, creature)
        path = os.path.join(args.out_dir, name + ".gif")
        flat = [f.convert("RGB") for f in frames]
        flat[0].save(path, save_all=True, append_images=flat[1:],
                     duration=int(1000 / FPS), loop=0, optimize=True)

        order = " → ".join(s for s, _ in steps)
        print(f"{name}.gif  {len(frames)}프레임  {len(frames)/FPS:.2f}초   {order}")


if __name__ == "__main__":
    main()
