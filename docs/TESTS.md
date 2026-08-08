# 테스트

`.github/workflows/tests.yml` 은 푸시마다 `editmode` / `playmode` 두 잡을 병렬로 돌린다.
둘 다 `game-ci/unity-test-runner@v4` 로 실행하고, 커버리지와 결과 xml 을 아티팩트로 올린다.
`UNITY_LICENSE` 시크릿이 없으면 잡은 실행되지 않고 요약에 안내만 남긴다.

로컬에서 같은 두 모드를 돌리는 방법:

```
Tools/run_tests.sh              # 두 모드 + 기준선 비교
Tools/run_tests.sh EditMode     # 한 모드만
Tools/ci_unity.sh -runTests ... # 에디터를 열어둔 채로 그림자 프로젝트에서
```

에디터를 켜둔 채라면 `run_tests.sh` 가 락을 감지하고 멈춘다. 그때는 `ci_unity.sh` 를 쓴다.
xml 은 실행 전에 지운다 — 남아 있는 이전 결과를 요약기가 그대로 읽어서 컴파일 실패를 통과로 보고한 적이 있다.

## EditMode — 141개

플레이 모드에 들어가지 않고 에디터 API 와 순수 로직만 본다. 빠르고, 씬을 띄우지 않는다.

| 클래스 | 수 | 무엇을 지키는가 |
| --- | --- | --- |
| `AudioRigTests` | 3 | 리스너·믹서·클립 경로 배선 |
| `AudioTuningTests` | 15 | 볼륨·감쇠·소리 반경 수치 |
| `DecoSpecTests` | 15 | 장식 키 규칙, 금지 목록, 컨테이너 등급 |
| `HardConstraintTests` | 6 | 절대 어겨선 안 되는 맵 규약 |
| `InteractableTests` | 6 | 상호작용 프롬프트와 홀드 시간 |
| `InteractionTests` | 3 | 상호작용 우선순위 선택 |
| `InvulnerabilityTests` | 1 | 피격 후 무적 구간 |
| `ItemSpecTests` | 6 | 공양물 개수·소리 반경·은닉도별 스프라이트 |
| `LevelTests` | 14 | 레벨 테이블 — 등불 시간, 필요 공양물, 의식 시간 |
| `MapBakeTests` | 12 | 구운 맵 json — 크기, 연결성, 최단 동선이 등불 안에 드는지 |
| `MapContractTests` | 3 | 맵 데이터 스키마 |
| `MapDataTests` | 10 | gid·좌표·프로퍼티 읽기 |
| `MapGalleryExportTests` | 1 | 갤러리 프리뷰 내보내기 |
| `MapGeneratorTests` | 7 | 생성기가 레벨 정의대로 오브젝트를 놓는지 |
| `MapPathfinderTests` | 9 | 거리장·경로 탐색 |
| `MapSizeSpecTests` | 12 | 레벨별 맵 치수 |
| `StageFlowTests` | 9 | 스테이지 진행·결과 전이 |
| `TempleTilesetTests` | 7 | 타일셋 매니페스트와 리소스 존재 |
| `UISpriteImportTests` | 2 | UI 스프라이트 임포트 설정 |

## PlayMode — 47개

실제 씬을 띄우고 물리·코루틴·렌더를 태운다. `-nographics` 로 돌리면 렌더 경로에서 네이티브 크래시가 나서
그래픽을 켜고 돌린다. 그래서 느리다.

| 클래스 | 수 | 무엇을 지키는가 |
| --- | --- | --- |
| `CorridorTraversalTests` | 1 | 좁은 복도를 실제 콜라이더로 통과 |
| `DecoOverlapTests` | 3 | 장식이 서로 겹쳐 쌓이지 않음 |
| `LampTests` | 8 | 등불 연소·점멸·재점화 |
| `LightingTests` | 1 | URP 2D 라이트 배선 |
| `MapRenderProofTests` | 3 | 타일이 실제로 그려짐 |
| `NavigationSweepTests` | 1 | 걸을 수 있는 칸을 전수로 훑어 끼임 검출 |
| `PlayerStatusTests` | 6 | 소음 반경·은신·사망 |
| `SceneSmokeTests` | 5 | 씬이 뜨고, 공양물·제단이 전부 도달 가능 |
| `SpawnPlacementTests` | 6 | 요괴 스폰이 벽 안이나 시작점 위가 아님 |
| `StageCompletionTests` | 1 | 봇이 공양물을 다 모아 의식까지 끝냄 |
| `TilemapAlignmentTests` | 3 | 타일맵 원점 정렬 |
| `WallCollisionTests` | 1 | 벽을 뚫지 못함 |
| `WallFaceOverlapTests` | 6 | 벽면 장식이 벽을 벗어나지 않음 |
| `YokaiMovementTests` | 2 | 요괴 순찰·추적 이동 |

## 어디에 무엇을 두는가

맵 데이터가 지켜야 할 성질(연결성, 목표 도달성, 공양물 간격, 제단 여유칸)은
`Tools/import_authored_maps.py` 가 구울 때 실패시킨다. 잘못 구운 맵은 커밋에 들어가지 못한다.
테스트는 그 결과물을 읽어 재확인하는 정도로만 남긴다 — 코드 행동은 테스트가, 데이터 불변식은 굽는 쪽이 책임진다.
