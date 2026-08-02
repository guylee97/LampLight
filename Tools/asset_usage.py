#!/usr/bin/env python3
"""아티스트 애셋이 어디에 쓰이는지 훑어서 docs/ASSET_USAGE.md 를 만든다."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs/ASSET_USAGE.md"


def short(path):
    return "/".join(str(path).split("/")[-2:])


def read(path):
    return (ROOT / path).read_text(errors="ignore")


def array(source, name):
    m = re.search(rf"{name}\s*=\s*\{{(.*?)\}}", source, re.S)
    return set(re.findall(r'"([a-z_]+)"', m.group(1))) if m else set()


DECO = read("Assets/Scripts/Contents/Map/DecoSpec.cs")

RULES = {
    "DebrisKeys": ("잔해", "바닥의 2.5%, 벽 인접 70% 가중"),
    "MossKeys": ("이끼", "벽 인접 칸의 9%, 코너 3배 가중"),
    "PillarIntactKeys": ("기둥(온전)", "L2~L3 · 6x6 이상 방 · 벽 2칸 이격 · 좌우 대칭 쌍"),
    "PillarBrokenKeys": ("기둥(파손)", "L2~L3 · 기둥 쌍의 변형"),
    "GlassKeys": ("깨진 유리", "복도/출입구 앞 · L1 3 · L2 4 · L3 5"),
    "PlankKeys": ("널빤지", "L2 2 · L3 3"),
    "ArtifactKeys": ("유물", "방 안 · 서로 6타일 이상 · 출구 3칸 제외"),
}

USE = {}
for name, entry in RULES.items():
    for key in array(DECO, name):
        USE[key] = entry

USE["container_drawer_closed"] = ("서랍", "L3 · 방당 1~2개")
USE["container_sarcophagus_closed"] = ("석관", "L3 · 6x6 이상 방 · 벽 1면 접촉")
USE["door_broken"] = ("출구(잠김)", "방 북쪽 벽면 3x3 · 문틀을 파내어 자리 확보")
USE["door_open"] = ("출구(열림)", "유물을 전부 모으면 교체")

BANNED = array(DECO, "Banned")
BAN_REASON = {
    "walldeco": "벽 위에 캐릭터가 올라가는 문제로 전면 제외",
    "large": "바닥 전체를 덮어 통행 판정과 충돌",
}


def lookup(key, category):
    base = key.replace("_buried", "")
    if key in BANNED or base in BANNED:
        return "제외", BAN_REASON.get(category, "명시적 제외")
    if key.endswith("_buried") and base in USE:
        return "유물(반매몰)", "은폐도 1 이상일 때 교체"
    if key in USE:
        return USE[key]
    return "미사용", ""


def objects(lines):
    catalog = json.loads(read("Assets/Resources/Data/temple_catalog.json"))
    by_category = {}
    for entry in catalog["objects"]:
        by_category.setdefault(entry["category"], []).append(entry)

    for category in sorted(by_category):
        items = sorted(by_category[category], key=lambda x: x["key"])
        used = sum(1 for i in items if lookup(i["key"], category)[0] not in ("미사용", "제외"))
        lines.append(f"\n### {category} — {used}/{len(items)}\n")
        lines.append("| 파일 | 크기 | 용도 | 규칙 |")
        lines.append("|---|---|---|---|")
        for item in items:
            label, rule = lookup(item["key"], category)
            mark = f"**{label}**" if label in ("미사용", "제외") else label
            lines.append(f"| `{short(item['file'])}` | {item['footprint']} | {mark} | {rule} |")


def tiles(lines):
    manifest = json.loads(read("Assets/Resources/Data/tileset_manifest.json"))
    entries = sorted(manifest["tiles"].items(), key=lambda kv: int(kv[0]))
    lines.append(f"\n## 타일 — {len(entries) - 1}/{len(entries)}\n")
    lines.append("| 파일 | 이름 | 용도 |")
    lines.append("|---|---|---|")
    for tile_id, tile in entries:
        name = tile["name"]
        if name.startswith("floor"):
            if tile_id == "0":
                use = "바닥 기본"
            elif tile["noisy"]:
                use = "noisy 바닥 · 10% 확률"
            else:
                use = "바닥 변형 · 18% 확률"
        elif name == "wall_solid":
            use = "**미사용** — 오토타일이 대체"
        else:
            mask = int(name.split("_")[2])
            sides = "".join(s for b, s in ((1, "N"), (2, "E"), (4, "S"), (8, "W")) if mask & b)
            use = f"벽 오토타일 · 이웃 {sides or '없음'}"
        lines.append(f"| `{short(tile['file'])}` | {name} | {use} |")


def characters(lines):
    catalog = json.loads(read("Assets/Resources/Data/character_catalog.json"))
    total = sum(len(c["states"]) for c in catalog["characters"])
    lines.append(f"\n## 캐릭터 — {total}/{total}\n")
    lines.append("| 파일 | 캐릭터 | 상태 | 프레임 |")
    lines.append("|---|---|---|---|")
    for character in catalog["characters"]:
        for state in character["states"]:
            lines.append(f"| `{short(state['resource'])}.png` | {character['key']} | "
                         f"{state['name']} | {state['frames']} x 8방향 |")


WIRING = {
    "step_walk": "Player `_walkFootstepClips`",
    "step_sneak": "Player `_sneakFootstepClips`",
    "step_run": "Player `_runFootstepClips`",
    "step_noisy_floor": "Player `_noisyFloorFootstepClips`",
    "walker_step": "WalkerZombie 발소리",
    "walker_breath": "WalkerZombie 숨소리",
    "wanderer_step": "WandererZombie 발소리",
    "wanderer_alert": "WandererZombie 경보",
    "runner_pass": "RunnerZombie 통과",
    "runner_hit": "RunnerZombie 접촉",
}


def sounds(lines):
    guids = {}
    for meta in (ROOT / "Assets/Resources/Sounds").rglob("*.wav.meta"):
        found = re.search(r"guid: ([0-9a-f]{32})", meta.read_text())
        if found:
            guids[meta.name[:-9]] = (found.group(1), short(str(meta)[:-5]))

    blob = "".join(p.read_text(errors="ignore")
                   for p in (ROOT / "Assets/Resources/Prefabs").rglob("*.prefab"))
    blob += read("Assets/Scenes/InGame.unity")
    code = "".join(p.read_text(errors="ignore")
                   for p in (ROOT / "Assets/Scripts").rglob("*.cs"))

    rows = []
    live = 0
    for key in sorted(guids):
        guid, path = guids[key]
        base = re.sub(r"(_\d+)?(_muffled)?$", "", key)
        wired = guid in blob or f'"{key}"' in code
        if wired:
            live += 1
            state = "연결됨"
            target = WIRING.get(base, f'`"{key}"` 코드 호출')
        elif base in WIRING:
            state = "**끊김** — AudioSetup 재실행 필요"
            target = WIRING[base]
        else:
            state = "**미사용**"
            target = "—"
        rows.append(f"| `{path}` | {target} | {state} |")

    lines.append(f"\n## 사운드 — {live}/{len(guids)}\n")
    lines.append("| 파일 | 연결 대상 | 상태 |")
    lines.append("|---|---|---|")
    lines.extend(rows)


def main():
    lines = ["# 애셋 사용 현황",
             "",
             "`Tools/asset_usage.py` 가 카탈로그와 코드를 대조해 생성한다. 수동으로 고치지 말 것.",
             "",
             "## 오브젝트"]
    objects(lines)
    tiles(lines)
    characters(lines)
    sounds(lines)
    OUT.write_text("\n".join(lines) + "\n")
    print(f"{OUT.relative_to(ROOT)} 생성 ({len(lines)}줄)")


if __name__ == "__main__":
    main()
