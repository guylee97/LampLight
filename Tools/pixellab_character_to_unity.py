import argparse
import hashlib
import json
import os
import struct
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CHAR_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "Characters")

DIRECTIONS = ["s", "se", "e", "ne", "n", "nw", "w", "sw"]
LONG_NAME = {
    "s": "south",
    "se": "south-east",
    "e": "east",
    "ne": "north-east",
    "n": "north",
    "nw": "north-west",
    "w": "west",
    "sw": "south-west",
}

BOTTOM_MARGIN = 10
SIDE_MARGIN = 8
PIXELS_PER_UNIT = 32

META_FIXED = """  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: %d
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
""" % PIXELS_PER_UNIT


def stable_hex(seed):
    return hashlib.md5(seed.encode("utf-8")).hexdigest()


def stable_int32(seed):
    digest = hashlib.md5(("id:" + seed).encode("utf-8")).digest()
    return struct.unpack("<i", digest[:4])[0]


def load_state_frames(src, state):
    state_dir = os.path.join(src, state)
    if not os.path.isdir(state_dir):
        return None

    frames = {}
    for short in DIRECTIONS:
        candidates = [
            os.path.join(state_dir, LONG_NAME[short] + ".png"),
            os.path.join(state_dir, short + ".png"),
        ]
        direction_dir = os.path.join(state_dir, LONG_NAME[short])

        if os.path.isdir(direction_dir):
            files = sorted(
                f for f in os.listdir(direction_dir) if f.lower().endswith(".png")
            )
            if not files:
                raise SystemExit(f"{direction_dir} 에 png 가 없다")
            frames[short] = [
                Image.open(os.path.join(direction_dir, f)).convert("RGBA")
                for f in files
            ]
            continue

        found = next((c for c in candidates if os.path.isfile(c)), None)
        if found is None:
            raise SystemExit(f"{state} 상태의 {short} 방향 이미지를 못 찾겠다")

        frames[short] = [Image.open(found).convert("RGBA")]

    counts = {len(v) for v in frames.values()}
    if len(counts) != 1:
        raise SystemExit(f"{state} 방향별 프레임 수가 다르다: {counts}")

    return frames


def union_bbox(states):
    left = top = 10 ** 9
    right = bottom = -(10 ** 9)

    for frames in states.values():
        for images in frames.values():
            for image in images:
                box = image.getbbox()
                if box is None:
                    continue
                left = min(left, box[0])
                top = min(top, box[1])
                right = max(right, box[2])
                bottom = max(bottom, box[3])

    if right < left:
        raise SystemExit("모든 프레임이 비어 있다")

    return left, top, right, bottom


def solve_frame(states, source_size):
    left, top, right, bottom = union_bbox(states)

    content_w = right - left
    content_h = bottom - top

    frame = max(content_w + SIDE_MARGIN * 2, content_h + BOTTOM_MARGIN * 2)
    frame = int((frame + 3) // 4 * 4)
    frame = min(frame, min(source_size))

    center_x = (left + right) // 2
    crop_left = center_x - frame // 2
    crop_left = max(0, min(crop_left, source_size[0] - frame))

    crop_top = bottom + BOTTOM_MARGIN - frame
    crop_top = max(0, min(crop_top, source_size[1] - frame))

    foot_baseline = bottom - crop_top

    return frame, crop_left, crop_top, foot_baseline


def compose(states, frame, crop_left, crop_top):
    sheets = {}

    for state, frames in states.items():
        count = len(frames[DIRECTIONS[0]])
        sheet = Image.new("RGBA", (frame * count, frame * len(DIRECTIONS)), (0, 0, 0, 0))

        for row, short in enumerate(DIRECTIONS):
            for col, image in enumerate(frames[short]):
                tile = image.crop(
                    (crop_left, crop_top, crop_left + frame, crop_top + frame)
                )
                sheet.paste(tile, (col * frame, row * frame))

        sheets[state] = (sheet, count)

    return sheets


def write_meta(png_path, key, state, frame, cols, pivot_y):
    sheet_h = frame * len(DIRECTIONS)
    entries = []

    for row, short in enumerate(DIRECTIONS):
        for col in range(cols):
            name = f"{key}_{state}_{short}_{col:02d}"
            entries.append(
                {
                    "name": name,
                    "x": col * frame,
                    "y": sheet_h - (row + 1) * frame,
                    "internal_id": stable_int32(name),
                    "sprite_id": stable_hex(name),
                }
            )

    lines = ["fileFormatVersion: 2", f"guid: {stable_hex(png_path)}", "TextureImporter:"]
    lines.append("  internalIDToNameTable:")
    for entry in entries:
        lines.append("  - first:")
        lines.append(f"      213: {entry['internal_id']}")
        lines.append(f"    second: {entry['name']}")

    lines.append(META_FIXED.rstrip("\n"))
    lines.append("  spriteSheet:")
    lines.append("    serializedVersion: 2")
    lines.append("    sprites:")

    for entry in entries:
        lines.extend(
            [
                "    - serializedVersion: 2",
                f"      name: {entry['name']}",
                "      rect:",
                "        serializedVersion: 2",
                f"        x: {entry['x']}",
                f"        y: {entry['y']}",
                f"        width: {frame}",
                f"        height: {frame}",
                "      alignment: 9",
                f"      pivot: {{x: 0.5, y: {pivot_y}}}",
                "      border: {x: 0, y: 0, z: 0, w: 0}",
                "      customData: ",
                "      outline: []",
                "      physicsShape: []",
                "      tessellationDetail: 0",
                "      bones: []",
                f"      spriteID: {entry['sprite_id']}",
                f"      internalID: {entry['internal_id']}",
                "      vertices: []",
                "      indices: ",
                "      edges: []",
                "      weights: []",
            ]
        )

    lines.extend(
        [
            "    outline: []",
            "    customData: ",
            "    physicsShape: []",
            "    bones: []",
            f"    spriteID: {stable_hex(png_path + ':sheet')}",
            "    internalID: 0",
            "    vertices: []",
            "    indices: ",
            "    edges: []",
            "    weights: []",
            "    secondaryTextures: []",
            "    spriteCustomMetadata:",
            "      entries: []",
            "    nameFileIdTable:",
        ]
    )

    for entry in sorted(entries, key=lambda e: e["name"]):
        lines.append(f"      {entry['name']}: {entry['internal_id']}")

    lines.extend(
        [
            "  mipmapLimitGroupName: ",
            "  pSDRemoveMatte: 0",
            "  userData: ",
            "  assetBundleName: ",
            "  assetBundleVariant: ",
            "",
        ]
    )

    with open(png_path + ".meta", "w", encoding="utf-8") as handle:
        handle.write("\n".join(lines))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--key", required=True)
    parser.add_argument("--src", required=True)
    parser.add_argument("--label", default="")
    parser.add_argument("--collider", type=int, default=44)
    parser.add_argument("--walk-fps", type=float, default=5.0)
    parser.add_argument("--speed-multiplier", type=float, default=1.0)
    parser.add_argument("--noise-radius", type=float, default=5.0)
    parser.add_argument("--search-seconds", type=float, default=3.0)
    args = parser.parse_args()

    states = {}
    for state in ("idle", "walk", "chase", "run"):
        frames = load_state_frames(args.src, state)
        if frames is not None:
            states[state] = frames

    if not states:
        raise SystemExit(f"{args.src} 아래에 idle/walk 폴더가 없다")

    source_size = states[next(iter(states))][DIRECTIONS[0]][0].size
    frame, crop_left, crop_top, foot_baseline = solve_frame(states, source_size)
    pivot_y = round((frame - foot_baseline) / frame, 6)

    out_dir = os.path.join(CHAR_DIR, args.key)
    os.makedirs(out_dir, exist_ok=True)

    sheets = compose(states, frame, crop_left, crop_top)
    anims = {}

    for state, (sheet, cols) in sheets.items():
        png_path = os.path.join(out_dir, f"{args.key}_{state}_sheet.png")
        sheet.save(png_path)
        write_meta(png_path, args.key, state, frame, cols, pivot_y)

        anims[state] = {"frames": cols}
        if state == "walk" and cols > 1:
            anims[state]["fps"] = args.walk_fps

        print(f"{os.path.relpath(png_path, ROOT)}  {sheet.size[0]}x{sheet.size[1]}  frames={cols}")

    manifest = {
        "character": args.key,
        "label": args.label or args.key,
        "frameWidth": frame,
        "frameHeight": frame,
        "displayScale": 1,
        "directions": DIRECTIONS,
        "directionIndex": {d: i for i, d in enumerate(DIRECTIONS)},
        "footBaselineY": foot_baseline,
        "anchor": "bottom-center",
        "anims": anims,
        "behavior": {
            "speedMultiplier": args.speed_multiplier,
            "noiseDetectRadiusTiles": args.noise_radius,
            "searchDurationSec": args.search_seconds,
        },
        "collisionBox": {
            "w": args.collider,
            "h": args.collider,
            "anchor": "bottom-center",
        },
        "render": {"imageSmoothing": False, "integerScaleOnly": True},
    }

    manifest_path = os.path.join(out_dir, f"{args.key}_sprite.json")
    with open(manifest_path, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, ensure_ascii=False, indent=2)

    print(
        f"frame={frame} footBaselineY={foot_baseline} pivotY={pivot_y} "
        f"collider={args.collider}px ({args.collider / PIXELS_PER_UNIT:.2f} tiles)"
    )
    print(f"manifest -> {os.path.relpath(manifest_path, ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
