"""게임을 켜지 않고 죽음 연출을 눈으로 확인하기 위한 미리보기를 굽는다.

UI_GameOver 의 단계와 타이밍을 그대로 흉내 내서 GIF 로 뽑는다. 실제 렌더가 아니라
연출 판단용이며, 상수를 바꾸면 여기 값도 같이 맞춰야 한다.
"""

import argparse
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UI_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "UI")

WIDTH, HEIGHT = 384, 216
FPS = 30

SILENCE = 0.10
LUNGE = 0.30
FACE = 0.34
FLICKER_ON = 0.03
FLICKER_OFF = 0.03
FLICKER_COUNT = 3
SETTLE = 0.28


def out_quint(t):
    return 1.0 - pow(1.0 - min(1.0, max(0.0, t)), 5)


def frames_for(seconds):
    return max(1, int(round(seconds * FPS)))


def load(path):
    return Image.open(path).convert("RGBA")


def scene_backdrop(creature, tile):
    """플레이 화면을 흉내 낸 배경 — 어둡고 등불 주변만 밝다."""

    board = Image.new("RGBA", (WIDTH, HEIGHT), (10, 9, 12, 255))
    draw = ImageDraw.Draw(board)

    for y in range(0, HEIGHT, tile):
        for x in range(0, WIDTH, tile):
            shade = 26 if (x // tile + y // tile) % 2 == 0 else 21
            draw.rectangle([x, y, x + tile - 1, y + tile - 1], fill=(shade, shade - 3, shade - 6))

    glow = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    cx, cy = WIDTH // 2, HEIGHT // 2 + 10
    for r in range(64, 0, -4):
        a = int(120 * (1.0 - r / 64.0))
        gd.ellipse([cx - r, cy - r * 0.7, cx + r, cy + r * 0.7], fill=(255, 168, 84, a))

    board.alpha_composite(glow)

    if creature is not None:
        small = creature.resize(
            (creature.size[0] // 3, creature.size[1] // 3), Image.NEAREST)
        board.alpha_composite(small, (cx + 40, cy - small.size[1] + 12))

    return board


def compose(face, creature, tile):
    backdrop = scene_backdrop(creature, tile)
    out = []

    def push(image, seconds):
        for _ in range(frames_for(seconds)):
            out.append(image.copy())

    # 힛스톱 — 화면은 멈춰 있고 소리만 끊긴다
    push(backdrop, SILENCE)

    # 돌진 — 잡은 놈이 화면으로 밀려온다
    lunge_frames = frames_for(LUNGE)
    for i in range(lunge_frames):
        k = out_quint((i + 1) / lunge_frames)
        frame = backdrop.copy()

        if creature is not None:
            scale = 0.55 + (3.1 - 0.55) * k
            w = max(1, int(creature.size[0] * scale))
            h = max(1, int(creature.size[1] * scale))
            grown = creature.resize((w, h), Image.NEAREST)

            dim = int(255 * (0.85 - 0.57 * k))
            shade = Image.new("RGBA", grown.size, (dim, dim, dim, 255))
            shade.putalpha(grown.getchannel("A"))

            frame.alpha_composite(
                shade,
                (int(WIDTH / 2 + 40 * (1 - k) - w / 2), int(HEIGHT / 2 - h / 2)))

        out.append(frame)

    # 얼굴 — 한 프레임에 꽉 찬다
    face_frames = frames_for(FACE)
    for i in range(face_frames):
        frame = face.resize((WIDTH, HEIGHT), Image.NEAREST).copy()
        jitter = 3 if i % 2 == 0 else -3
        shifted = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 255))
        shifted.alpha_composite(frame, (jitter, -jitter))

        if i < face_frames * 0.4:
            wash = Image.new("RGBA", (WIDTH, HEIGHT), (140, 6, 6, 90))
            shifted.alpha_composite(wash)

        out.append(shifted)

    # 깜빡임 — 플레이 화면이 몇 번 끊긴다
    for _ in range(FLICKER_COUNT):
        dark = backdrop.copy()
        dark.alpha_composite(Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 224)))
        push(dark, FLICKER_OFF)
        push(backdrop, FLICKER_ON)

    push(Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 255)), SETTLE)
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--face", required=True, help="점프스케어 얼굴 png")
    parser.add_argument("--creature", default="", help="몬스터 남향 스프라이트 png")
    parser.add_argument("--out", required=True)
    parser.add_argument("--tile", type=int, default=32)
    args = parser.parse_args()

    face = load(args.face)
    creature = load(args.creature) if args.creature else None

    frames = compose(face, creature, args.tile)
    flat = [f.convert("RGB") for f in frames]

    flat[0].save(
        args.out,
        save_all=True,
        append_images=flat[1:],
        duration=int(1000 / FPS),
        loop=0,
        optimize=True)

    total = len(frames) / FPS
    print(f"{args.out}  {len(frames)}프레임  {total:.2f}초")


if __name__ == "__main__":
    main()
