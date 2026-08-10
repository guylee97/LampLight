"""플레이 방법 팝업(popup.png)의 텍스트를 픽셀 단위로 고친다.

이 팝업은 코드가 아니라 그림 한 장이고 텍스트가 구워져 있다. 한글 픽셀 폰트가
저장소에 없어서 다시 조판할 수 없으므로, 같은 그림 안에 이미 있는 성한 글자를
가져다 쓰고 지운 자리는 배경으로 되돌린다.

배경은 두 카드 모두 세로로 균일하다 — 열마다 색이 하나뿐이라 글자 없는 행에서
열 프로파일을 뜨면 지운 자리를 정확히 복원할 수 있다.

  1. 카드1 "괴물을 피해" — 정상 "을" 위에 큰 "라"가 덧그려져 있다.
     칸을 지우고 카드4의 "몸을"에서 같은 크기 "을"을 옮겨 온다.
  2. 카드2 "E를 3초간 누르면" — 홀드 시간은 전각마다 같고 가이드에 필요 없다.
     "3초간"을 지우고 뒷부분을 그만큼 왼쪽으로 당긴다.
"""

import os
import shutil

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGET = os.path.join(ROOT, "Assets", "Resources", "Art", "UI", "popup.png")
ORIGINAL = os.path.join(ROOT, "Tools", "popup_original.png")


def column_background(px, x0, x1, y0, y1):
    profile = {}
    for x in range(x0, x1 + 1):
        samples = sorted(px[x, y] for y in range(y0, y1 + 1))
        profile[x] = samples[len(samples) // 2]
    return profile


def erase(px, background, x0, x1, y0, y1):
    for x in range(x0, x1 + 1):
        colour = background[x]
        for y in range(y0, y1 + 1):
            px[x, y] = colour


def stamp(px, src_background, dst_background, box, dx, dy):
    x0, y0, x1, y1 = box
    lifted = []

    for x in range(x0, x1 + 1):
        base = src_background[x]
        for y in range(y0, y1 + 1):
            pixel = px[x, y]
            delta = tuple(pixel[i] - base[i] for i in range(3))
            if max(delta) > 6:
                lifted.append((x + dx, y + dy, delta))

    for x, y, delta in lifted:
        base = dst_background[x]
        px[x, y] = tuple(min(255, max(0, base[i] + delta[i])) for i in range(3)) + (255,)


def fix_card_one(im, px):
    background = column_background(px, 380, 430, 478, 486)
    source_background = column_background(px, 1015, 1050, 845, 853)

    erase(px, background, 388, 417, 434, 478)
    erase(px, background, 418, 421, 434, 453)

    stamp(px, source_background, background,
          (1023, 820, 1043, 842), 390 - 1025, 455 - 823)


def fix_card_two(im, px):
    background = column_background(px, 1030, 1345, 509, 518)

    glyphs = []
    for x in range(1094, 1331):
        base = background[x]
        for y in range(483, 516):
            pixel = px[x, y]
            delta = tuple(pixel[i] - base[i] for i in range(3))
            if max(delta) > 6:
                glyphs.append((x, y, delta))

    erase(px, background, 1035, 1340, 483, 515)

    for x, y, delta in glyphs:
        base = background[x - 55]
        px[x - 55, y] = tuple(min(255, max(0, base[i] + delta[i])) for i in range(3)) + (255,)


def main():
    if not os.path.exists(ORIGINAL):
        shutil.copy2(TARGET, ORIGINAL)

    im = Image.open(ORIGINAL).convert("RGBA")
    px = im.load()

    fix_card_one(im, px)
    fix_card_two(im, px)

    im.save(TARGET)
    print(f"wrote {TARGET}")


if __name__ == "__main__":
    main()
