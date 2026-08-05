#!/bin/bash
# CI 와 같은 두 모드를 로컬에서 돌리고, 기준선에 없던 실패만 새 회귀로 보고한다.
#   Tools/run_tests.sh              두 모드 실행 후 기준선과 비교
#   Tools/run_tests.sh --update     현재 실패 목록을 기준선으로 저장
#   Tools/run_tests.sh EditMode     한 모드만 실행
set -uo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.3.20f1/Unity.app/Contents/MacOS/Unity}"
OUT="${LAMPLIGHT_TEST_OUT:-$SRC/QAReports/tests}"
BASELINE="$SRC/Tools/known_failures.txt"

update=0
modes=(EditMode PlayMode)

for arg in "$@"; do
	case "$arg" in
		--update) update=1 ;;
		EditMode|PlayMode) modes=("$arg") ;;
		*) echo "알 수 없는 인자: $arg" >&2; exit 2 ;;
	esac
done

if [ "$update" = "1" ] && [ "${#modes[@]}" -ne 2 ]; then
	echo "--update 는 두 모드를 모두 돌려야 한다. 한쪽만 갱신하면 나머지 기준선이 지워진다." >&2
	exit 2
fi

if [ -f "$SRC/Temp/UnityLockfile" ] && pgrep -f "Unity.*-projectpath.*$SRC" >/dev/null 2>&1; then
	echo "에디터가 프로젝트를 잠그고 있다. 닫거나 Tools/ci_unity.sh 를 써라." >&2
	exit 2
fi

mkdir -p "$OUT"
: > "$OUT/failures.txt"

for mode in "${modes[@]}"; do
	echo "== $mode 실행 중"
	args=(-batchmode -projectPath "$SRC" -runTests -testPlatform "$mode"
		-testResults "$OUT/$mode.xml" -logFile "$OUT/$mode.log")

	# PlayMode 는 -nographics 로 돌리면 렌더 경로에서 네이티브 크래시가 난다.
	if [ "$mode" = "EditMode" ]; then
		args+=(-nographics)
	fi

	"$UNITY" "${args[@]}" >/dev/null 2>&1

	if [ ! -f "$OUT/$mode.xml" ]; then
		echo "  결과 파일이 없다. $OUT/$mode.log 를 봐라" >&2
		grep -m 3 "error CS" "$OUT/$mode.log" >&2 || true
		exit 1
	fi

	if ! python3 "$SRC/Tools/summarize_tests.py" "$OUT/$mode.xml" "$mode" "$OUT/failures.txt"; then
		echo "  결과 요약에 실패했다. $OUT/$mode.xml 를 확인해라" >&2
		exit 1
	fi
done

sort -o "$OUT/failures.txt" "$OUT/failures.txt"

if [ "$update" = "1" ]; then
	cp "$OUT/failures.txt" "$BASELINE"
	echo "기준선 갱신: $(wc -l < "$BASELINE" | tr -d ' ') 건"
	exit 0
fi

touch "$BASELINE"
new=$(comm -13 "$BASELINE" "$OUT/failures.txt")
fixed=$(comm -23 "$BASELINE" "$OUT/failures.txt")

if [ -n "$fixed" ]; then
	echo
	echo "기준선에 있었지만 이제 통과한다:"
	echo "$fixed" | sed 's/^/  /'
fi

if [ -n "$new" ]; then
	echo
	echo "새로 깨진 테스트:"
	echo "$new" | sed 's/^/  /'
	exit 1
fi

echo
echo "새 회귀 없음. 기준선 밖 실패 0 건."
