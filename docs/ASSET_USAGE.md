# 애셋 사용 현황

`Tools/asset_usage.py` 가 카탈로그와 코드를 대조해 생성한다. 수동으로 고치지 말 것.

## 오브젝트

### artifact — 8/8

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `artifact/obj_artifact_bell.png` | 1x1 | 유물 | 방 안 · 서로 6타일 이상 · 출구 3칸 제외 |
| `artifact/obj_artifact_bell_buried.png` | 1x1 | 유물(반매몰) | 은폐도 1 이상일 때 교체 |
| `artifact/obj_artifact_crest.png` | 1x1 | 유물 | 방 안 · 서로 6타일 이상 · 출구 3칸 제외 |
| `artifact/obj_artifact_crest_buried.png` | 1x1 | 유물(반매몰) | 은폐도 1 이상일 때 교체 |
| `artifact/obj_artifact_mask.png` | 1x1 | 유물 | 방 안 · 서로 6타일 이상 · 출구 3칸 제외 |
| `artifact/obj_artifact_mask_buried.png` | 1x1 | 유물(반매몰) | 은폐도 1 이상일 때 교체 |
| `artifact/obj_artifact_seal.png` | 1x1 | 유물 | 방 안 · 서로 6타일 이상 · 출구 3칸 제외 |
| `artifact/obj_artifact_seal_buried.png` | 1x1 | 유물(반매몰) | 은폐도 1 이상일 때 교체 |

### cobweb — 0/3

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `cobweb/obj_cobweb_corner.png` | 1x1 | **미사용** |  |
| `cobweb/obj_cobweb_radial.png` | 1x1 | **미사용** |  |
| `cobweb/obj_cobweb_torn.png` | 1x1 | **미사용** |  |

### container — 2/5

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `container/obj_container_crate.png` | 1x1 | **미사용** |  |
| `container/obj_container_drawer_closed.png` | 1x1 | 서랍 | L3 · 방당 1~2개 |
| `container/obj_container_drawer_open.png` | 1x1 | **미사용** |  |
| `container/obj_container_sarcophagus_closed.png` | 1x1 | 석관 | L3 · 6x6 이상 방 · 벽 1면 접촉 |
| `container/obj_container_sarcophagus_open.png` | 1x1 | **미사용** |  |

### debris — 9/17

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `debris/obj_debris_arch.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_blocks.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_capital.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_fragments.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_frame.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_gravel_a.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_gravel_b.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_masonry.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_mossy.png` | 1x1 | 이끼 | 벽 인접 칸의 9%, 코너 3배 가중 |
| `debris/obj_debris_pile.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_planks.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_shards.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_shelf.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_stone.png` | 1x1 | 잔해 | 바닥의 2.5%, 벽 인접 70% 가중 |
| `debris/obj_debris_structure.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_timber.png` | 1x1 | **미사용** |  |
| `debris/obj_debris_wall.png` | 1x1 | **미사용** |  |

### door — 2/2

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `door/obj_door_broken.png` | 3x1 | 출구(잠김) | 방 북쪽 벽면 3x3 · 문틀을 파내어 자리 확보 |
| `door/obj_door_open.png` | 3x1 | 출구(열림) | 유물을 전부 모으면 교체 |

### exit — 0/2

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `exit/obj_exit_locked.png` | 1x1 | **미사용** |  |
| `exit/obj_exit_open.png` | 1x1 | **미사용** |  |

### large — 0/22

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `large/obj_large_altar_low.png` | 2x2 | **미사용** |  |
| `large/obj_large_arch_fallen.png` | 2x2 | **미사용** |  |
| `large/obj_large_benches_pair.png` | 2x2 | **미사용** |  |
| `large/obj_large_carpet.png` | 3x3 | **제외** | 바닥 전체를 덮어 통행 판정과 충돌 |
| `large/obj_large_carpet_round_seal.png` | 2x2 | **제외** | 바닥 전체를 덮어 통행 판정과 충돌 |
| `large/obj_large_carpet_runner.png` | 3x1 | **미사용** |  |
| `large/obj_large_firepit.png` | 3x3 | **미사용** |  |
| `large/obj_large_lid_pit.png` | 2x2 | **미사용** |  |
| `large/obj_large_offering_table.png` | 2x2 | **미사용** |  |
| `large/obj_large_pillar_broken_pair.png` | 2x2 | **미사용** |  |
| `large/obj_large_pillar_fallen_long.png` | 2x2 | **미사용** |  |
| `large/obj_large_pillar_fallen_short.png` | 1x2 | **미사용** |  |
| `large/obj_large_pillar_pile.png` | 2x2 | **미사용** |  |
| `large/obj_large_rubble_heap.png` | 2x2 | **미사용** |  |
| `large/obj_large_rubble_mossy.png` | 2x2 | **미사용** |  |
| `large/obj_large_sacrificial_slab.png` | 3x3 | **미사용** |  |
| `large/obj_large_shelf_fallen.png` | 3x3 | **미사용** |  |
| `large/obj_large_statue_fallen.png` | 3x3 | **미사용** |  |
| `large/obj_large_statue_kneeling.png` | 2x3 | **제외** | 바닥 전체를 덮어 통행 판정과 충돌 |
| `large/obj_large_steps.png` | 2x2 | **미사용** |  |
| `large/obj_large_trough.png` | 2x2 | **미사용** |  |
| `large/obj_large_wall_collapse.png` | 2x2 | **미사용** |  |

### noise — 2/2

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `noise/obj_noise_glass.png` | 1x1 | 깨진 유리 | 복도/출입구 앞 · L1 3 · L2 4 · L3 5 |
| `noise/obj_noise_planks.png` | 1x1 | 널빤지 | L2 2 · L3 3 |

### prop — 4/16

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `prop/obj_prop_basin.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_bowl.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_floor_seal.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_floor_seal_broken.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_floor_tile.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_grate.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_ledge.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_pebbles.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_pillar_broken.png` | 1x1 | 기둥(파손) | L2~L3 · 기둥 쌍의 변형 |
| `prop/obj_prop_pillar_column.png` | 1x1 | 기둥(온전) | L2~L3 · 6x6 이상 방 · 벽 2칸 이격 · 좌우 대칭 쌍 |
| `prop/obj_prop_pillar_intact.png` | 1x1 | 기둥(온전) | L2~L3 · 6x6 이상 방 · 벽 2칸 이격 · 좌우 대칭 쌍 |
| `prop/obj_prop_pillar_stump.png` | 1x1 | 기둥(파손) | L2~L3 · 기둥 쌍의 변형 |
| `prop/obj_prop_step.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_tablet_a.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_tablet_b.png` | 1x1 | **미사용** |  |
| `prop/obj_prop_tablet_c.png` | 1x1 | **미사용** |  |

### walldeco — 0/12

| 파일 | 크기 | 용도 | 규칙 |
|---|---|---|---|
| `walldeco/obj_walldeco_arcade.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_arcade_mirror.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_arch.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_arch_mirror.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_brickband.png` | tile | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_chains.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_chains_mirror.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_hole.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_hole_mirror.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_plain.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_sag.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |
| `walldeco/obj_walldeco_sag_mirror.png` | 3x1 | **제외** | 벽 위에 캐릭터가 올라가는 문제로 전면 제외 |

## 타일 — 21/22

| 파일 | 이름 | 용도 |
|---|---|---|
| `tiles_single/tile_00_floor_a_base.png` | floor_a_base | 바닥 기본 |
| `tiles_single/tile_01_wall_solid.png` | wall_solid | **미사용** — 오토타일이 대체 |
| `tiles_single/tile_02_floor_b_cracked.png` | floor_b_cracked | 바닥 변형 · 18% 확률 |
| `tiles_single/tile_03_floor_c_gravel.png` | floor_c_gravel | noisy 바닥 · 10% 확률 |
| `tiles_single/tile_04_floor_d_moss.png` | floor_d_moss | 바닥 변형 · 18% 확률 |
| `tiles_single/tile_05_floor_e_wet.png` | floor_e_wet | 바닥 변형 · 18% 확률 |
| `tiles_single/tile_08_wall_edge_00_open.png` | wall_edge_00_open | 벽 오토타일 · 이웃 없음 |
| `tiles_single/tile_09_wall_edge_01_n.png` | wall_edge_01_n | 벽 오토타일 · 이웃 N |
| `tiles_single/tile_10_wall_edge_02_e.png` | wall_edge_02_e | 벽 오토타일 · 이웃 E |
| `tiles_single/tile_11_wall_edge_03_ne.png` | wall_edge_03_ne | 벽 오토타일 · 이웃 NE |
| `tiles_single/tile_12_wall_edge_04_s.png` | wall_edge_04_s | 벽 오토타일 · 이웃 S |
| `tiles_single/tile_13_wall_edge_05_ns.png` | wall_edge_05_ns | 벽 오토타일 · 이웃 NS |
| `tiles_single/tile_14_wall_edge_06_es.png` | wall_edge_06_es | 벽 오토타일 · 이웃 ES |
| `tiles_single/tile_15_wall_edge_07_nes.png` | wall_edge_07_nes | 벽 오토타일 · 이웃 NES |
| `tiles_single/tile_16_wall_edge_08_w.png` | wall_edge_08_w | 벽 오토타일 · 이웃 W |
| `tiles_single/tile_17_wall_edge_09_nw.png` | wall_edge_09_nw | 벽 오토타일 · 이웃 NW |
| `tiles_single/tile_18_wall_edge_10_ew.png` | wall_edge_10_ew | 벽 오토타일 · 이웃 EW |
| `tiles_single/tile_19_wall_edge_11_new.png` | wall_edge_11_new | 벽 오토타일 · 이웃 NEW |
| `tiles_single/tile_20_wall_edge_12_sw.png` | wall_edge_12_sw | 벽 오토타일 · 이웃 SW |
| `tiles_single/tile_21_wall_edge_13_nsw.png` | wall_edge_13_nsw | 벽 오토타일 · 이웃 NSW |
| `tiles_single/tile_22_wall_edge_14_esw.png` | wall_edge_14_esw | 벽 오토타일 · 이웃 ESW |
| `tiles_single/tile_23_wall_edge_15_nesw.png` | wall_edge_15_nesw | 벽 오토타일 · 이웃 NESW |

## 캐릭터 — 11/11

| 파일 | 캐릭터 | 상태 | 프레임 |
|---|---|---|---|
| `player/player_idle_sheet.png` | player | idle | 1 x 8방향 |
| `player/player_walk_sheet.png` | player | walk | 2 x 8방향 |
| `zombie_runner/zombie_runner_idle_sheet.png` | zombie_runner | idle | 1 x 8방향 |
| `zombie_runner/zombie_runner_run_sheet.png` | zombie_runner | run | 1 x 8방향 |
| `zombie_runner/zombie_runner_walk_sheet.png` | zombie_runner | walk | 2 x 8방향 |
| `zombie_walker/zombie_walker_chase_sheet.png` | zombie_walker | chase | 1 x 8방향 |
| `zombie_walker/zombie_walker_idle_sheet.png` | zombie_walker | idle | 1 x 8방향 |
| `zombie_walker/zombie_walker_walk_sheet.png` | zombie_walker | walk | 2 x 8방향 |
| `zombie_wanderer/zombie_wanderer_chase_sheet.png` | zombie_wanderer | chase | 1 x 8방향 |
| `zombie_wanderer/zombie_wanderer_idle_sheet.png` | zombie_wanderer | idle | 1 x 8방향 |
| `zombie_wanderer/zombie_wanderer_walk_sheet.png` | zombie_wanderer | walk | 2 x 8방향 |

## 사운드 — 8/36

| 파일 | 연결 대상 | 상태 |
|---|---|---|
| `Sounds/ambient_temple.wav` | `"ambient_temple"` 코드 호출 | 연결됨 |
| `Sounds/death_contact.wav` | `"death_contact"` 코드 호출 | 연결됨 |
| `Sounds/door_creak.wav` | `"door_creak"` 코드 호출 | 연결됨 |
| `Sounds/exit_unlock.wav` | `"exit_unlock"` 코드 호출 | 연결됨 |
| `Sounds/runner_hit.wav` | RunnerZombie 접촉 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/runner_hit_muffled.wav` | RunnerZombie 접촉 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/runner_pass.wav` | RunnerZombie 통과 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/runner_pass_muffled.wav` | RunnerZombie 통과 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_glass.wav` | `"step_glass"` 코드 호출 | 연결됨 |
| `Sounds/step_glass_2.wav` | — | **미사용** |
| `Sounds/step_glass_3.wav` | — | **미사용** |
| `Sounds/step_noisy_floor.wav` | Player `_noisyFloorFootstepClips` | 연결됨 |
| `Sounds/step_noisy_floor_2.wav` | Player `_noisyFloorFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_noisy_floor_3.wav` | Player `_noisyFloorFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_run.wav` | Player `_runFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_run_2.wav` | Player `_runFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_run_3.wav` | Player `_runFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_run_4.wav` | Player `_runFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_sneak.wav` | Player `_sneakFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_sneak_2.wav` | Player `_sneakFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_sneak_3.wav` | Player `_sneakFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_sneak_4.wav` | Player `_sneakFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_walk.wav` | Player `_walkFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_walk_2.wav` | Player `_walkFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_walk_3.wav` | Player `_walkFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/step_walk_4.wav` | Player `_walkFootstepClips` | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/stone_land.wav` | `"stone_land"` 코드 호출 | 연결됨 |
| `Sounds/stone_throw.wav` | `"stone_throw"` 코드 호출 | 연결됨 |
| `Sounds/walker_breath.wav` | WalkerZombie 숨소리 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/walker_breath_muffled.wav` | WalkerZombie 숨소리 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/walker_step.wav` | WalkerZombie 발소리 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/walker_step_muffled.wav` | WalkerZombie 발소리 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/wanderer_alert.wav` | WandererZombie 경보 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/wanderer_alert_muffled.wav` | WandererZombie 경보 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/wanderer_step.wav` | WandererZombie 발소리 | **끊김** — AudioSetup 재실행 필요 |
| `Sounds/wanderer_step_muffled.wav` | WandererZombie 발소리 | **끊김** — AudioSetup 재실행 필요 |
