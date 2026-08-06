# 통과판정

게임이 읽는 판정은 `Assets/Resources/Data/map_l{1,2,3}.json` 의 `collision` 배열
하나뿐이다. 이 배열은 `docs/collision_map.json` 을 기준으로 굽는다.

```
python3 Tools/bake_collision.py docs/collision_map.json
```

## 규칙

스프라이트의 불투명 픽셀이 한 타일 면적의 `block_threshold` 이상을 덮으면 그
타일에 판정을 준다. 현재 값은 차단 0.25, 큰소리 0.03, 알파 하한 32 이다.

값은 0 통과, 1 차단, 2 큰소리, 3 카펫. 우선순위는 차단 > 큰소리 > 카펫 > 통과.

폭이 1.5 미만이면 한 칸으로 반올림한다. 그래서 소품을 반 칸으로 줄여 그려도
자기 타일은 계속 막는다. 그림과 판정이 어긋나는 지점이니 크기를 조정할 때
염두에 둔다.

## 문서에서 실제로 쓰는 부분

`Tools/bake_collision.py` 는 `docs/collision_map.json` 에서 두 곳만 읽는다.

- `footprint_rule` — 임계값
- `assets[].passability` — 에셋별 판정

위치는 문서가 아니라 맵의 `decorations` 에서 읽는다. 그래서 데코를 옮기거나
크기를 바꾼 뒤 다시 구우면 보이는 그림과 판정이 따라온다.

## maps 절은 기준이 아니다

문서의 `maps[LevelN].collision` 과 `objects[].tiles` 는 기획이 넘겨준 시점의
스냅샷이고, 게임이 쓰는 배열과 다르다. 2026-08-06 기준 L1 42칸, L2 147칸,
L3 226칸이 어긋난다. `objects[].rule` 의 코드(E/F/A/S/P/R/G/W/T)도 문서에 설명이
없고 굽는 쪽에서 쓰지 않는다.

판정을 확인할 일이 있으면 `maps` 절이 아니라 위 명령으로 다시 구운 결과를
본다. 두 쪽을 맞추려면 기획과 합의가 필요하다.
