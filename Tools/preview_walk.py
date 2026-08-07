"""캐릭터 걷기 시트를 GIF 로 뽑아 실제 재생 속도로 확인한다.

카탈로그의 fps 와 DirectionalSpriteAnimator 의 스터터(프레임 홀드 불균등)를 그대로
반영한다. 게임 안에서 어떻게 움직이는지 눈으로 보기 위한 것이지 렌더 결과물은 아니다.
"""

import argparse
import json
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CATALOG = os.path.join(ROOT, "Assets", "Resources", "Data", "character_catalog.json")
CHAR_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Characters")

DIRECTIONS = ["s", "se", "e", "ne", "n", "nw", "w", "sw"]


def spec_for(key):
    data = json.load(open(CATALOG, encoding="utf-8"))

    for entry in data["characters"]:
        if entry["key"] == key:
            return entry

    raise SystemExit(f"카탈로그에 '{key}' 가 없다")


def hold_seconds(frame, fps, stutter):
    """DirectionalSpriteAnimator.HoldSeconds 와 같은 규칙."""

    step = 1.0 / fps

    if stutter <= 0.0:
        return step

    phase = (frame * 0.6180339887) % 1.0
    return step * ((1.0 - stutter) + (2.0 * stutter) * phase)


def frames_for(spec, state, direction):
    entry = next((s for s in spec["states"] if s["name"] == state), None)
    if entry is None:
        raise SystemExit(f"'{state}' 상태가 없다")

    sheet = Image.open(os.path.join(ROOT, "Assets", "Resources", entry["resource"] + ".png"))
    sheet = sheet.convert("RGBA")

    size = spec["frameWidth"]
    row = DIRECTIONS.index(direction)

    return [
        sheet.crop((c * size, row * size, (c + 1) * size, (row + 1) * size))
        for c in range(entry["cols"])
    ], entry["fps"] or 6.0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--key", required=True)
    parser.add_argument("--state", default="walk")
    parser.add_argument("--direction", default="s")
    parser.add_argument("--stutter", type=float, default=0.45)
    parser.add_argument("--scale", type=int, default=3)
    parser.add_argument("--intensity", type=float, default=1.0)
    parser.add_argument("--out", required=True)
    args = parser.parse_args()

    spec = spec_for(args.key)
    frames, fps = frames_for(spec, args.state, args.direction)
    fps *= args.intensity

    board = []
    durations = []

    for index, frame in enumerate(frames):
        flat = Image.new("RGBA", frame.size, (16, 14, 18, 255))
        flat.alpha_composite(frame)
        flat = flat.resize(
            (frame.size[0] * args.scale, frame.size[1] * args.scale), Image.NEAREST)

        board.append(flat.convert("RGB"))
        durations.append(int(hold_seconds(index, fps, args.stutter) * 1000))

    board[0].save(
        args.out,
        save_all=True,
        append_images=board[1:],
        duration=durations,
        loop=0,
        optimize=True)

    total = sum(durations) / 1000.0
    print(f"{args.out}  {len(board)}프레임  {fps:.1f}fps  주기 {total:.2f}초")


if __name__ == "__main__":
    main()
