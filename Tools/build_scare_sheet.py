"""점프스케어 프레임들을 한 장의 시트로 굽고 슬라이스를 메타에 적는다.

UI_GameOver 는 ScareSheet 를 Resources.LoadAll<Sprite> 로 읽어 이름순으로 재생한다.
슬라이스가 없으면 빈 배열이 돌아와 정지 얼굴만 흔들리므로, 자르는 정보를 여기서
직접 써 넣는다.
"""

import argparse
import hashlib
import os
import re

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = os.path.join(ROOT, "Assets", "Resources", "Art", "UI", "Game_Over_Screen")

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{id_table}
  externalObjects: {{}}
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
  maxTextureSize: 4096
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
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
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
    maxTextureSize: 4096
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
    sprites:
{sprites}
    outline: []
    customData:
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
    nameFileIdTable:
{name_table}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def frame_ids(name):
    digest = hashlib.md5(name.encode()).hexdigest()
    return digest[:32], int(digest[:15], 16) % (2 ** 62)


def natural(path):
    numbers = re.findall(r"\d+", os.path.basename(path))
    return (int(numbers[-1]) if numbers else 0, path)


def build(key, frame_paths, columns):
    frames = [Image.open(p).convert("RGBA") for p in frame_paths]
    width, height = frames[0].size

    for path, frame in zip(frame_paths, frames):
        if frame.size != (width, height):
            raise SystemExit(f"{path} 크기가 {frame.size} 로 다르다 (기준 {width}x{height})")

    rows = (len(frames) + columns - 1) // columns
    sheet = Image.new("RGBA", (columns * width, rows * height), (0, 0, 0, 0))

    for i, frame in enumerate(frames):
        sheet.paste(frame, ((i % columns) * width, (i // columns) * height))

    png = os.path.join(OUT_DIR, f"{key}.png")
    sheet.save(png)

    sheet_height = rows * height
    entries = []
    for i in range(len(frames)):
        name = f"{key}_{i:02d}"
        sprite_id, internal = frame_ids(name)
        x = (i % columns) * width
        y = sheet_height - (i // columns + 1) * height     # 유니티 원점은 좌하단
        entries.append((name, x, y, sprite_id, internal))

    id_table = "\n".join(
        f"  - first:\n      213: {e[4]}\n    second: {e[0]}" for e in entries)
    sprites = "\n".join(
        f"""    - serializedVersion: 2
      name: {e[0]}
      rect:
        serializedVersion: 2
        x: {e[1]}
        y: {e[2]}
        width: {width}
        height: {height}
      alignment: 0
      pivot: {{x: 0, y: 0}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {e[3]}
      internalID: {e[4]}
      vertices: []
      indices:
      edges: []
      weights: []""" for e in entries)
    name_table = "\n".join(f"      {e[0]}: {e[4]}" for e in entries)

    guid = hashlib.md5(png.encode()).hexdigest()
    open(png + ".meta", "w", encoding="utf-8").write(
        META.format(guid=guid, id_table=id_table, sprites=sprites, name_table=name_table))

    print(f"{key}.png  {sheet.size[0]}x{sheet.size[1]}  "
          f"{len(frames)}프레임 {width}x{height}  {columns}x{rows}")
    return png


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--key", required=True)
    parser.add_argument("--frames", required=True, help="프레임 png 가 든 폴더")
    parser.add_argument("--columns", type=int, default=5)
    args = parser.parse_args()

    paths = sorted(
        (os.path.join(args.frames, f) for f in os.listdir(args.frames)
         if f.lower().endswith(".png")),
        key=natural)

    if not paths:
        raise SystemExit(f"{args.frames} 에 png 가 없다")

    build(args.key, paths, args.columns)


if __name__ == "__main__":
    main()
