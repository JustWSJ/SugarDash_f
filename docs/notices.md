# 공지

팀에 전할 말을 적는 곳이다. 누구나 올릴 수 있다. 에이전트는 작업을 시작할 때 `git pull` 뒤에 이 문서를 읽고, 아직 전하지 않은 공지를 사람에게 알린다([team-rules.md](team-rules.md) 0절).

- 공지 번호는 1부터 하나씩 올린다. 새 공지는 맨 아래에 붙이고, 이미 쓴 번호는 바꾸거나 다시 쓰지 않는다.
- 더는 필요 없는 공지는 지워도 된다(번호는 다시 쓰지 않는다).

## 공지 1

- 날짜: 2026-11-05
- 내용:
  - 작업 전에는 무조건 `git pull`한다. 에이전트가 작업을 시작할 때 Unity를 닫았는지 묻고 직접 pull한다(team-rules 0절, 8절).
  - 앞으로 전할 말은 이 문서(`docs/notices.md`)에 올린다. 에이전트가 pull한 뒤 새 공지를 알려 준다.

## 공지 2

- 날짜: 2026-11-05
- 내용:
  - 의학 출처 판본을 정했다: [medical-sources.md](medical-sources.md). 출처 발췌, 문구, 그림에 의학 내용이 들어가면 이 문서의 판본과 장만 쓴다.
  - 기사나 요약은 권고 내용의 근거로 쓰지 않는다. 원문을 열고 장·쪽을 적는다(team-rules 4절).

## 공지 3

- 날짜: 2026-11-05
- 내용:
  - 2구간 2-B(맵별 조건)가 들어왔다: [section2.md](section2.md) 7절. 1형은 운동장·집에 들어갈 때 측정기 위에 "혈당 재 볼까?" 말풍선이 잠깐 뜬다(혈당 재기 습관 순간). 2형은 놀이터에 쉬는 곳이 더 자주, 나들이에 음식 줄이 더 자주 나온다. 수치는 아직 개발용이다.
  - 말풍선 프리팹은 `Assets/_Project/Art/UI/Screens/HabitPrompt.prefab`, 문구는 `HabitPromptText`에 있다.
  - 확인 방법은 [dev-scene.md](dev-scene.md) 9.14절이다.

## 공지 4

- 날짜: 2026-11-05
- 내용:
  - 일시정지 메뉴와 탈락 장면이 들어왔다: [pause-elimination.md](pause-elimination.md). 주행 중 오른쪽 위 `II` 버튼으로 멈추고 `계속하기`·`타이틀로`를 고른다. 탈락하면 바로 결과 화면으로 가지 않고, 어른이 달려와 옆에 서서 말풍선 한 줄을 한 뒤 약 2.5초 뒤 결과 화면이 뜬다.
  - 어른 캐릭터는 `Character/GuardianArrivalView`가 띄운다. 프리팹은 발밑이 기준점이다.
  - 프리팹은 `Assets/_Project/Art/UI/Screens/PauseMenu.prefab`, `EliminationSpeech.prefab`이고, 말풍선 문구는 `EliminationText`에 있다.
  - 작업 브랜치는 그대로 `feat/section2-frame`이다.
  - 확인 방법은 [dev-scene.md](dev-scene.md) 9.15절이다.

## 공지 5

- 날짜: 2026-11-09
- 내용:
  - 장애물이 패턴(묶음) 단위로 나오게 바뀌었다: [track-patterns.md](track-patterns.md). 지금은 예전과 같은 "장애물 하나" 패턴만 들어 있어 보기에는 거의 같다. 1형 저혈당 1단계(측정기 누르기) 동안에는 쉬운 패턴만 새로 나온다.
  - 패턴은 `Assets/_Project/Art/World/TrackPatterns.asset`에 있다. Inspector에서 고치고, Console 경고가 없고 `ProjectAsset_PassesRules_AndSpacing` 테스트가 통과하면 push한다.
  - 확인 방법은 [dev-scene.md](dev-scene.md) 9.16절이다.

## 공지 6

- 날짜: 2026-11-19
- 내용:
  - 남은 소리: 발소리, 점프, 슬라이드, 부딪힘, 배경음, 맵 환경음.
  - 화면 효과음 9개(버튼, 선택, 경고, 알림, 카운트, 출발, 결과, 레벨 업, 먹기)는 들어왔다. 지금은 임시 소리라 파일만 바꾸면 된다: `Assets/_Project/Art/Audio/`의 `Sfx_*.wav`를 같은 이름의 새 파일로 덮어쓰면 그대로 들어간다. 바꾼 소리의 출처는 `Assets/_Project/Art/LICENSES.md`에 적는다.

## 공지 7

- 날짜: 2026-11-20
- 내용:
  - 영역 담당(캐릭터, 월드, 화면)과 roadmap 할 일 목록을 없앴다. 각자 받은 작업 문서대로 진행한다.
  - 고칠 수 있는 파일 제한을 없앴다. 작업에 필요하면 어느 파일이든 고친다. 게임 내용 규칙(죽음 표현·나쁜 음식·의학 정보 지어내기 금지 등)은 그대로다([team-rules.md](team-rules.md) 3~5절).
  - 공지 대상과 에이전트 설정의 팀원 번호 줄을 없앴다. 공지는 모두 전원에게 보여 준다. `CLAUDE.md`·`AGENTS.md`에 있던 `나는 팀원N(영역)이다.` 줄은 지워도 된다.
