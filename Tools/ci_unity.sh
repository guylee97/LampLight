#!/bin/bash
# 에디터를 열어둔 채로 배치 유니티를 돌리기 위한 그림자 프로젝트 러너.
# 소스만 동기화하고 Library 는 그림자 쪽에 캐시로 남긴다.
set -e

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DST="${LAMPLIGHT_CI_DIR:-${SRC}_CI}"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity}"

mkdir -p "$DST"

rsync -a --delete \
  --exclude '/Library/' --exclude '/Temp/' --exclude '/Logs/' --exclude '/obj/' \
  --exclude '/Build/' --exclude '/.git/' --exclude '/UserSettings/' \
  --exclude '/QAReports/' --exclude '.DS_Store' \
  "$SRC"/ "$DST"/

# 배치 실행이 사용자 스피커로 소리를 내지 않도록 그림자 쪽 오디오만 끈다.
sed -i '' 's/  m_DisableAudio: 0/  m_DisableAudio: 1/' "$DST/ProjectSettings/AudioManager.asset"

exec "$UNITY" -batchmode -projectPath "$DST" "$@"
