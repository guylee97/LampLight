"""스프라이트 시트에서 노멀맵을 만들어 URP 2D 보조 텍스처로 붙인다.

2D 스프라이트는 모든 픽셀이 빛을 같은 방식으로 받아서, 등불이 움직여도 평평하게
보인다. 휘도를 높이로 보고 기울기를 구해 노멀맵을 만들면 빛의 방향에 따라 픽셀별
밝기가 달라진다. 실루엣 경계는 바깥쪽으로 말아 줘야 덩어리감이 산다.
"""

import argparse
import hashlib
import math
import os
import re

from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

NORMAL_SUFFIX = "_normal"
SECONDARY_NAME = "_NormalMap"

NORMAL_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 0
    linearTexture: 1
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
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 0
  spriteTessellationDetail: -1
  textureType: 1
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
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def stable_guid(seed):
    return hashlib.md5(seed.encode("utf-8")).hexdigest()


def height_field(image, blur):
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    grey = rgba.convert("L")

    if blur > 0.0:
        grey = grey.filter(ImageFilter.GaussianBlur(blur))

    width, height = grey.size
    heights = grey.load()
    mask = alpha.load()

    field = [[0.0] * width for _ in range(height)]
    for y in range(height):
        for x in range(width):
            field[y][x] = heights[x, y] / 255.0 if mask[x, y] > 16 else 0.0

    return field, mask


def build_normal(image, strength, blur, rim):
    width, height = image.size
    field, mask = height_field(image, blur)
    out = Image.new("RGBA", (width, height), (128, 128, 255, 255))
    pixels = out.load()

    def sample(x, y):
        if x < 0 or y < 0 or x >= width or y >= height:
            return 0.0
        return field[y][x]

    for y in range(height):
        for x in range(width):
            if mask[x, y] <= 16:
                pixels[x, y] = (128, 128, 255, 255)
                continue

            dx = (sample(x + 1, y) - sample(x - 1, y)) * strength
            dy = (sample(x, y + 1) - sample(x, y - 1)) * strength

            edge = 0
            for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + ox, y + oy
                if nx < 0 or ny < 0 or nx >= width or ny >= height or mask[nx, ny] <= 16:
                    edge += 1
                    dx += ox * rim
                    dy += oy * rim

            nz = 1.0
            length = math.sqrt(dx * dx + dy * dy + nz * nz)
            nx = -dx / length
            ny = dy / length
            nz = nz / length

            pixels[x, y] = (
                int((nx * 0.5 + 0.5) * 255),
                int((ny * 0.5 + 0.5) * 255),
                int((nz * 0.5 + 0.5) * 255),
                255,
            )

    return out


def attach_secondary(sheet_meta, normal_guid):
    with open(sheet_meta, encoding="utf-8") as handle:
        text = handle.read()

    entry = (
        "    secondaryTextures:\n"
        "    - name: " + SECONDARY_NAME + "\n"
        "      texture: {fileID: 2800000, guid: " + normal_guid + ", type: 3}\n"
    )

    if SECONDARY_NAME in text:
        text = re.sub(
            r"    secondaryTextures:\n(?:    - name: .*\n      texture: .*\n)*",
            entry,
            text,
        )
    else:
        text = text.replace("    secondaryTextures: []\n", entry)

    with open(sheet_meta, "w", encoding="utf-8") as handle:
        handle.write(text)


def process(path, strength, blur, rim):
    directory = os.path.dirname(path)
    stem = os.path.splitext(os.path.basename(path))[0]

    if stem.endswith(NORMAL_SUFFIX):
        return None

    normal_path = os.path.join(directory, stem + NORMAL_SUFFIX + ".png")
    image = Image.open(path).convert("RGBA")

    build_normal(image, strength, blur, rim).save(normal_path)

    guid = stable_guid(os.path.relpath(normal_path, ROOT))
    with open(normal_path + ".meta", "w", encoding="utf-8") as handle:
        handle.write(NORMAL_META.format(guid=guid))

    sheet_meta = path + ".meta"
    if os.path.isfile(sheet_meta):
        attach_secondary(sheet_meta, guid)

    return normal_path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("targets", nargs="+", help="스프라이트 시트 png 경로")
    parser.add_argument("--strength", type=float, default=3.2)
    parser.add_argument("--blur", type=float, default=0.6)
    parser.add_argument("--rim", type=float, default=0.9)
    args = parser.parse_args()

    for target in args.targets:
        made = process(target, args.strength, args.blur, args.rim)
        if made is None:
            continue

        print(f"  {os.path.relpath(made, ROOT)}")


if __name__ == "__main__":
    main()
