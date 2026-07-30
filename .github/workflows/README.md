# CI

`tests.yml`은 push(`main`, `dev/**`)와 모든 PR에서 EditMode·PlayMode 테스트를 병렬로 돌린다.

## 누구의 계정을 넣을 것인가

**저장소 소유자의 계정을 쓴다.** 시크릿은 UI에서 다시 열어볼 수 없을 뿐, 워크플로 파일을 고칠 수 있는 사람은 값을 밖으로 빼낼 수 있다. `UNITY_PASSWORD`는 Unity 계정 비밀번호 그 자체이므로, 자기 소유가 아닌 저장소에 개인 계정을 넣으면 그 저장소에 push 권한이 있는 모든 사람에게 계정을 넘기는 것과 같다. 협업에서 빠진 뒤에도 시크릿은 남는다.

기여자가 자기 브랜치를 CI로 검증하고 싶다면 개인 fork에 시크릿을 넣고 fork에서 돌린다. 시크릿이 없는 저장소에서는 아래 워크플로가 조용히 건너뛴다.

## 필요한 시크릿

Unity를 CI에서 실행하려면 라이선스가 있어야 한다. 저장소 Settings → Secrets and variables → Actions에 등록한다.

Personal 라이선스(현재 이 프로젝트):

| 시크릿 | 값 |
|---|---|
| `UNITY_LICENSE` | 활성화한 `.ulf` 파일의 **전체 내용** |
| `UNITY_EMAIL` | Unity 계정 이메일 |
| `UNITY_PASSWORD` | Unity 계정 비밀번호 |

`.ulf`를 얻는 절차는 game-ci 문서의 activation을 따른다. 요약하면 `game-ci/unity-request-activation-file` 워크플로를 한 번 돌려 `.alf`를 받고, <https://license.unity3d.com/manual>에 업로드해 `.ulf`를 내려받아 그 내용을 시크릿에 붙여넣는다.

Plus/Pro라면 `UNITY_LICENSE` 대신 `UNITY_SERIAL`을 쓴다.

## 산출물

| 아티팩트 | 내용 |
|---|---|
| `editmode-results` / `playmode-results` | NUnit XML 결과 |
| `qa-reports` | `QAReports/` — 내비게이션 히트맵 PNG와 스윕 리포트 |

`qa-reports`는 PlayMode 잡에서만 올라간다. 통과했더라도 히트맵을 열어보면 맵의 어느 구간이 얼마나 빡빡한지 눈으로 확인할 수 있다.

## 주의

- Unity 에디터 도커 이미지는 버전별로 게시 시점이 다르다. `6000.3.20f1` 이미지가 아직 없으면 잡이 이미지 pull 단계에서 실패한다. 그때는 `unity-test-runner`에 `unityVersion`을 사용 가능한 근접 버전으로 명시하거나 이미지가 올라올 때까지 기다린다.
- `Library` 캐시가 비어 있는 첫 실행은 임포트만 10~30분 걸린다. 이후 실행은 캐시로 짧아진다.
- PlayMode 테스트는 입력을 가상 키보드로 주입한다. headless에는 포커스가 없어 Input System이 디바이스를 끄므로, `QaScene.AllowHeadlessInput()`이 `backgroundBehavior`와 `editorInputBehaviorInPlayMode`를 먼저 풀어준다. 이 호출을 지우면 봇이 조작하지 못하고 전부 "움직이지 못했다"로 실패한다.
