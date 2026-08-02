import argparse
import glob
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHAR_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Characters")
DEFAULT_DST = os.path.join(ROOT, "Assets", "Resources", "Data", "character_catalog.json")

RESOURCE_ROOT = "Art/Characters"
DIRECTION_COUNT = 8


def sheet_files(char_dir):
    found = {}
    for path in sorted(glob.glob(os.path.join(char_dir, "*_sheet.png"))):
        stem = os.path.splitext(os.path.basename(path))[0]
        state = stem.rsplit("_sheet", 1)[0].split("_")[-1]
        found[state] = path
    return found


def grid_of(path, frame_w, frame_h):
    width, height = Image.open(path).size
    if width % frame_w or height % frame_h:
        raise ValueError(f"{os.path.basename(path)} 가 {frame_w}x{frame_h} 로 안 나뉜다 ({width}x{height})")
    return width // frame_w, height // frame_h


def direction_axis(cols, rows, path):
    if rows == DIRECTION_COUNT:
        return "row", cols
    if cols == DIRECTION_COUNT and rows == 1:
        return "col", 1
    raise ValueError(f"{os.path.basename(path)} 의 8방향 축을 못 찾겠다 (cols={cols} rows={rows})")


def convert_character(char_dir):
    manifests = glob.glob(os.path.join(char_dir, "*_sprite.json"))
    if not manifests:
        return None

    with open(manifests[0], encoding="utf-8") as handle:
        spec = json.load(handle)

    key = os.path.basename(char_dir.rstrip(os.sep))
    frame_w = int(spec["frameWidth"])
    frame_h = int(spec["frameHeight"])
    foot = int(spec.get("footBaselineY", frame_h))
    box = spec.get("collisionBox") or {}
    behavior = spec.get("behavior") or {}
    anims = spec.get("anims") or {}

    states = []
    for state, path in sorted(sheet_files(char_dir).items()):
        cols, rows = grid_of(path, frame_w, frame_h)
        axis, frames = direction_axis(cols, rows, path)

        anim = anims.get(state) or {}
        by_state = anim.get("fpsByState") or {}

        states.append({
            "name": state,
            "resource": f"{RESOURCE_ROOT}/{key}/{os.path.splitext(os.path.basename(path))[0]}",
            "cols": cols,
            "rows": rows,
            "frames": min(frames, int(anim.get("frames", frames))),
            "directionAxis": axis,
            "fps": float(anim.get("fps", 0.0)),
            "fpsSneak": float(by_state.get("sneak", 0.0)),
            "fpsWalk": float(by_state.get("walk", 0.0)),
            "fpsRun": float(by_state.get("run", 0.0)),
        })

    return {
        "key": key,
        "frameWidth": frame_w,
        "frameHeight": frame_h,
        "footBaselineY": foot,
        "pivotX": 0.5,
        "pivotY": (frame_h - foot) / float(frame_h),
        "colliderW": float(box.get("w", 0)) ,
        "colliderH": float(box.get("h", 0)),
        "speedMultiplier": float(behavior.get("speedMultiplier", 1.0)),
        "noiseDetectRadiusTiles": float(behavior.get("noiseDetectRadiusTiles",
                                                    behavior.get("awarenessRadiusTiles", 0.0))),
        "searchDurationSec": float(behavior.get("searchDurationSec",
                                                behavior.get("lastNoiseSearchSec", 0.0))),
        "directions": spec.get("directions", []),
        "states": states,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--src", default=CHAR_DIR)
    parser.add_argument("--dst", default=DEFAULT_DST)
    args = parser.parse_args()

    if not os.path.isdir(args.src):
        print(f"없는 폴더: {args.src}", file=sys.stderr)
        return 2

    characters = []
    for name in sorted(os.listdir(args.src)):
        path = os.path.join(args.src, name)
        if not os.path.isdir(path):
            continue
        entry = convert_character(path)
        if entry:
            characters.append(entry)

    if not characters:
        print("캐릭터를 못 찾았다", file=sys.stderr)
        return 2

    with open(args.dst, "w", encoding="utf-8") as handle:
        json.dump({"characters": characters}, handle, ensure_ascii=False, indent=1)
        handle.write("\n")

    for entry in characters:
        states = ", ".join(f"{s['name']}({s['frames']}f,{s['directionAxis']})" for s in entry["states"])
        print(f"{entry['key']:16s} {entry['frameWidth']}x{entry['frameHeight']} "
              f"pivotY={entry['pivotY']:.4f} 충돌 {entry['colliderW']:.0f}x{entry['colliderH']:.0f} | {states}")

    print(f"-> {args.dst}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
