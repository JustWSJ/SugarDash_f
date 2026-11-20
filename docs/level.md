# 플레이어 레벨

- 범위: 여러 판에 걸쳐 쌓이는 경험치와 레벨, 타이틀의 레벨 표시. 두 유형이 같이 쓴다.
- 바탕: [game-values.md](game-values.md) 8절(수치), [result-screen.md](result-screen.md)

## 1. 목적과 하지 않는 것

- 판을 거듭해 달릴 이유를 준다.

하지 않는 것:
- 경험치를 깎거나 레벨을 내리지 않는다. 오답·탈락으로 잃는 것이 없다. 왜: 아이가 실수를 겁내 대처를 피하지 않게 한다.
- 음식을 먹거나 측정기를 누르는 행동 자체에 경험치를 주지 않는다. 경험치는 이미 있는 점수 규칙(거리, 혈당 유지, 대처 가중치)에서만 나온다. 왜: 아이템 점수를 주지 않는 것과 같은 이유다.
- 주행 중에는 경험치·레벨을 띄우지 않는다. 결과 화면과 타이틀에서만 보인다. 왜: 달리는 도중 세계를 멈추지 않는다.

## 2. 규칙

- 레벨은 아이 한 명에 하나다. 1형·2형 판을 오가도 같은 레벨이 이어진다.
- 기록한 판이 끝나 결과 화면으로 갈 때만 경험치를 더한다.
  - 튜토리얼 판, 일시정지 메뉴의 "타이틀로"로 그만둔 판은 경험치가 없다(최고 점수와 같다).
- 한 판 경험치 = `xp.perRun` + 그 판 점수 × `xp.scoreRate`, 내림. 점수가 없거나 음수면 기본만.
- 레벨 1은 경험치 0에서 시작한다. `levels`의 각 줄은 그 레벨이 되는 누적 경험치(`xpTotal`)다.
- 레벨은 저장하지 않고 누적 경험치와 레벨 표로 매번 계산한다. 표를 바꿔도 맞게 다시 나온다.

## 3. 설정과 검증

- 빌드: `Assets/_Project/Config/level-config.json`. 개발: `Assets/_Project/Config/Dev/level-config.dev.json`(`configVersion: dev-not-real-level`, 테스트 픽스처 값, 빌드에 포함되지 않음).
- 불러올 때 검사(`LevelRules.Validate`):
  1. `xp.perRun`, `xp.scoreRate`는 0 이상의 유한수
  2. `levels`가 비어 있지 않고, `level`은 2부터 1씩 오른다
  3. `xpTotal`은 0보다 크고 앞 레벨보다 크다
- 빌드 설정에 빈 값(`null`)이 있으면 경고만 남기고 레벨 기능(경험치, 레벨 표시)을 끈다. 게임은 그대로 돈다. 규칙 위반이면 오류를 남기고 끈다.
  - EditMode 테스트가 빌드 파일을 읽어 "빈 값만 있거나 규칙에 맞음"을 확인한다.

## 4. 저장

- `persistentDataPath/player-progress.json`: `{"schemaVersion":1,"totalXp":120}`. 임시 파일에 쓴 뒤 교체한다.
- 읽지 못하거나 스키마가 다르면 경험치 0으로 본다.

## 5. 화면

| 화면 | 보이는 것 |
| --- | --- |
| 타이틀 | 왼쪽 위 원 안에 레벨 숫자, 원과 이어진 상자에 `경험치 25 / 60`과 채움 막대(지금 레벨에서 다음 레벨까지 온 만큼). 마지막 레벨이면 `경험치 300`, 막대는 꽉 참. 레벨 기능이 꺼져 있으면 숨김 |
| 결과 | `경험치 +12`, 레벨이 오르면 `레벨이 올랐어요! 레벨 3`. 튜토리얼 판은 줄이 없음 |

- 문구는 `LevelText`(문구 파일)에 있다. 원은 `Art/UI/Sprites/CircleFill.png`·`CircleBorder.png`로 그리고, 그림과 색은 자리 채움이다.

## 6. 모듈

| 계층 | 이름 | 하는 일 |
| --- | --- | --- |
| Domain | `LevelRules`, `LevelConfig` | 경험치 계산, 레벨, 검증 |
| Domain 포트 | `IPlayerProgressStore` | 누적 경험치 읽기·쓰기 |
| Infrastructure | `LevelConfigProvider`, `PlayerProgressRepository` | 설정 읽기·검증, 진행 파일 원자적 쓰기 |
| App | `GameFlowController` | 판이 끝날 때 경험치 더하기, 레벨·경험치 값 노출 |
| Presentation | `TitleScreenView`, `ResultScreenView`, `LevelText` | 레벨 표시, 결과 줄 |

## 7. 남은 것

- 수치(담당자): `xp.perRun`, `xp.scoreRate`, 레벨별 `xpTotal`. 처음 레벨 5까지 추천.
- 레벨 표시 그림(화면): 원, 상자, 채움 막대, 레벨 오름 연출.
